using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugWindow : MonoBehaviour
{
    private Camera _camera;

    private float _pitch = 45.0f;
    private float _yaw = 45.0f;

    [SerializeField]
    private LayerMask _cullingMask;

    private RenderTexture _renderTexture;

    // Should always be powers of 2
    [SerializeField]
    private Vector2 _renderTextureSize = new Vector2(256.0f, 128.0f);

    private Dictionary<int, LevelStateSave> _levelStatesByBuildIndex = new Dictionary<int, LevelStateSave>();

    [SerializeField]
    private Dropdown _levelDropdown;

    private int _selectedLevel = -1;

    [SerializeField]
    private RawImage _levelRenderer;

    [SerializeField]
    private GameObject _vertexPrefab;
    [SerializeField]
    private GameObject _edgePrefab;

    private GameObject _levelGraphContainer;
    private GameObject[] _vertices;
    private GameObject[] _edges;
    private LineRenderer[] _lineRenderers;

    [SerializeField]
    private Material _traversableEdge;
    [SerializeField]
    private Material _notTraversableEdge;

    [SerializeField]
    private Vector3 _levelGraphOffset = new Vector3(0, -1000, 0);

    private bool _dragging = false;
    private bool _overImage = false;

    private const float _targetDistance = 20.0f;

    [SerializeField]
    private float _zoomCameraStrength = 1.0f;
    [SerializeField]
    private float _moveCameraSpeed = 2.0f;
    [SerializeField]
    private float _rotateCameraSpeed = 2.0f;

    private void Awake()
    {
        // Create a camera
        _camera = new GameObject("DebugWindowCamera").AddComponent<Camera>();
        _camera.transform.position = Vector3.up * 10;
        _camera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.black;
        _camera.cullingMask = _cullingMask;

        // Render the camera to a render texture
        _renderTexture = new RenderTexture((int)_renderTextureSize.x, (int)_renderTextureSize.y, 16, RenderTextureFormat.RGB565);
        _camera.targetTexture = _renderTexture;
        _camera.aspect = _renderTextureSize.x / _renderTextureSize.y;

        // Display the render texture
        _levelRenderer.texture = _renderTexture;

        // Create the container for the LevelGraph
        _levelGraphContainer = new GameObject("debug level graph");
        _levelGraphContainer.transform.position = _levelGraphOffset;

        // Add listener to level dropdown
        _levelDropdown.onValueChanged.AddListener(delegate { LevelDropdownValueChanged(); });
    }

    public void SetLevelStates(Dictionary<int, LevelStateSave> levelStatesByBuildIndex, int currentBuildIndex)
    {
        _levelStatesByBuildIndex = levelStatesByBuildIndex;
        
        // Fill the level dropdown options
        List<Dropdown.OptionData> levelOptions = new List<Dropdown.OptionData>();

        foreach (KeyValuePair<int, LevelStateSave> levelState in _levelStatesByBuildIndex)
        {
            if (currentBuildIndex == levelState.Key)
            {
                continue;
            }

            levelOptions.Add(new Dropdown.OptionData(levelState.Key.ToString()));
        }

        _levelDropdown.options = levelOptions;
        _selectedLevel = int.Parse(_levelDropdown.options[_levelDropdown.value].text);

        // Create the selected level graph
        DeleteLevelGraph();

        if (_levelDropdown.options.Count <= 0)
        {
            return;
        }

        CreateLevelGraph();
    }

    void LevelDropdownValueChanged()
    {
        DeleteLevelGraph();

        _selectedLevel = int.Parse(_levelDropdown.options[_levelDropdown.value].text);
        CreateLevelGraph();
    }

    private void CreateLevelGraph()
    {
        _vertices = new GameObject[_levelStatesByBuildIndex[_selectedLevel].Graph.Vertices.Length];

        for (int i = 0; i < _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices.Length; i++)
        {
            _vertices[i] = Instantiate(_vertexPrefab, _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices[i].Position + _levelGraphContainer.transform.position, Quaternion.identity, _levelGraphContainer.transform);
        }

        _edges = new GameObject[_levelStatesByBuildIndex[_selectedLevel].Graph.Edges.Length];
        _lineRenderers = new LineRenderer[_levelStatesByBuildIndex[_selectedLevel].Graph.Edges.Length];
        Vector3 roomAPosition;
        Vector3 roomBPosition;

        for (int i = 0; i < _levelStatesByBuildIndex[_selectedLevel].Graph.Edges.Length; i++)
        {
            roomAPosition = _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices[_levelStatesByBuildIndex[_selectedLevel].Graph.Edges[i].VertexA].Position + _levelGraphContainer.transform.position;
            roomBPosition = _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices[_levelStatesByBuildIndex[_selectedLevel].Graph.Edges[i].VertexB].Position + _levelGraphContainer.transform.position;

            _lineRenderers[i] = Instantiate(_edgePrefab, roomAPosition, Quaternion.identity, _levelGraphContainer.transform).GetComponent<LineRenderer>();
            _edges[i] = _lineRenderers[i].gameObject;

            _lineRenderers[i].positionCount = 2;
            _lineRenderers[i].SetPosition(0, roomAPosition);
            _lineRenderers[i].SetPosition(1, roomBPosition);
            _lineRenderers[i].material = _levelStatesByBuildIndex[_selectedLevel].Graph.Edges[i].Traversable ? _traversableEdge : _notTraversableEdge;
        }

        CenterCamera();
    }

    private void DeleteLevelGraph()
    {
        if (_vertices != null)
        {
            for (int i = _vertices.Length - 1; i >= 0; i--)
            {
                Destroy(_vertices[i]);
            }

            _vertices = null;
        }

        if (_edges != null)
        {
            for (int i = _edges.Length - 1; i >= 0; i--)
            {
                Destroy(_edges[i]);
            }

            _edges = null;
            _lineRenderers = null;
        }
    }

    private void Update()
    {
        // Update level graph
        if (_edges != null && _selectedLevel > -1)
        {
            for (int i = 0; i < _edges.Length; i++)
            {
                _lineRenderers[i].material = _levelStatesByBuildIndex[_selectedLevel].Graph.Edges[i].Traversable ? _traversableEdge : _notTraversableEdge;
            }
        }

        // Update camera
        if (!_camera || _levelStatesByBuildIndex == null || _selectedLevel < 0)
        {
            return;
        }

        if (_overImage && Input.mouseScrollDelta.y != .0f)
        {
            ZoomCamera(-Input.mouseScrollDelta.y);
        }

        if (_overImage && Input.GetKeyDown(KeyCode.F))
        {
            CenterCamera();
        }

        if(_dragging)
        {
            Vector2 movement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetMouseButton(0))
            {
                RotateCameraAroundTarget(movement);
            }
            else if (Input.GetMouseButton(1))
            {
                if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
                {
                    ZoomCamera(movement.y);
                }
                else
                {
                    RotateCamera(movement);
                }
            }
            else if (Input.GetMouseButton(2))
            {
                MoveCamera(movement);
            }
        }
    }

    #region Event Trigger
    public void OnBeginDrag()
    {
        _dragging = true;
    }

    public void OnEndDrag()
    {
        _dragging = false;
    }

    public void OnPointerEnter()
    {
        _overImage = true;
    }

    public void OnPointerExit()
    {
        _overImage = false;
    }
    #endregion

    #region Cemera Methods
    private void CenterCamera()
    {
        Vector3 averagePosition = Vector3.zero;

        foreach (Vertex vertex in _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices)
        {
            averagePosition += vertex.Position + _levelGraphContainer.transform.position;
        }

        averagePosition /= _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices.Length;
        PlaceCameraAtTargetDistance(averagePosition);
    }

    private void PlaceCameraAtTargetDistance(Vector3 target)
    {
        _camera.transform.position = target - _camera.transform.forward * _targetDistance;
    }

    private void ZoomCamera(float zoom)
    {
        _camera.transform.position -= _camera.transform.TransformDirection(Vector3.forward) * zoom * _zoomCameraStrength;
    }

    private void RotateCameraAroundTarget(Vector3 mouseMovement)
    {
        Vector3 target = _camera.transform.position + _camera.transform.forward * _targetDistance;

        RotateCamera(mouseMovement);
        PlaceCameraAtTargetDistance(target);
    }

    private void MoveCamera(Vector3 mouseMovement)
    {
        _camera.transform.position -= _camera.transform.TransformDirection(mouseMovement) * _moveCameraSpeed;
    }

    private void RotateCamera(Vector3 mouseMovement)
    {
        _pitch -= mouseMovement.y * _rotateCameraSpeed;
        _yaw += mouseMovement.x * _rotateCameraSpeed;
        
        _camera.transform.localEulerAngles = Vector3.right * _pitch + Vector3.up * _yaw;
    }
    #endregion
}
