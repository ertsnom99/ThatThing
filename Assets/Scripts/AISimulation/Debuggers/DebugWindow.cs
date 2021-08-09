using System.Collections.Generic;
using System.Linq;
using GraphCreator;
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
    private Vector2 _renderTextureSize = new Vector2(512.0f, 256.0f);

    private Dictionary<int, LevelStateSave> _levelStatesByBuildIndex = new Dictionary<int, LevelStateSave>();

    [SerializeField]
    private Dropdown _levelDropdown;

    private int _selectedLevel = -1;

    [SerializeField]
    private RawImage _levelRenderer;
    private RectTransform _levelRendererTransform;

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

    [SerializeField]
    private GameObject _characterPrefab;

    private Dictionary<CharacterState, GameObject> _characters = new Dictionary<CharacterState, GameObject>();

    [SerializeField]
    private Vector3 _characterPositionOffset = new Vector3(0, 1.0f, 0);

    private bool _dragging = false;
    private bool _overImage = false;
    private bool _clicked = false;

    [SerializeField]
    private float _zoomCameraStrength = 1.0f;
    [SerializeField]
    private float _moveCameraSpeed = 2.0f;
    [SerializeField]
    private float _rotateCameraSpeed = 2.0f;

    private const float _centerTargetDistance = 40.0f;
    private float _targetDistance = _centerTargetDistance;
    private bool _focusing = false;

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

        _levelRendererTransform = _levelRenderer.transform as RectTransform;
        DebugWindowLevelRenderer debugWindowLevelRenderer = _levelRenderer.GetComponent<DebugWindowLevelRenderer>();

        if (debugWindowLevelRenderer)
        {
            debugWindowLevelRenderer._onBeginDragDelegate = OnBeginDrag;
            debugWindowLevelRenderer._onEndDragDelegate = OnEndDrag;
            debugWindowLevelRenderer._onPointerEnterDelegate = OnPointerEnter;
            debugWindowLevelRenderer._onPointerExitDelegate = OnPointerExit;
            debugWindowLevelRenderer._onPointerLeftClickDelegate = OnPointerLeftClick;
        }

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
        // Create vertices
        _vertices = new GameObject[_levelStatesByBuildIndex[_selectedLevel].Graph.Vertices.Length];

        for (int i = 0; i < _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices.Length; i++)
        {
            _vertices[i] = Instantiate(_vertexPrefab, _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices[i].Position + _levelGraphContainer.transform.position, Quaternion.identity, _levelGraphContainer.transform);
        }

        // Create edges
        _edges = new GameObject[_levelStatesByBuildIndex[_selectedLevel].Graph.Edges.Length];
        _lineRenderers = new LineRenderer[_levelStatesByBuildIndex[_selectedLevel].Graph.Edges.Length];
        
        for (int i = 0; i < _levelStatesByBuildIndex[_selectedLevel].Graph.Edges.Length; i++)
        {
            Vector3 roomAPosition = _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices[_levelStatesByBuildIndex[_selectedLevel].Graph.Edges[i].VertexA].Position + _levelGraphContainer.transform.position;
            Vector3 roomBPosition = _levelStatesByBuildIndex[_selectedLevel].Graph.Vertices[_levelStatesByBuildIndex[_selectedLevel].Graph.Edges[i].VertexB].Position + _levelGraphContainer.transform.position;

            _lineRenderers[i] = Instantiate(_edgePrefab, roomAPosition, Quaternion.identity, _levelGraphContainer.transform).GetComponent<LineRenderer>();
            _edges[i] = _lineRenderers[i].gameObject;

            _lineRenderers[i].positionCount = 2;
            _lineRenderers[i].SetPosition(0, roomAPosition);
            _lineRenderers[i].SetPosition(1, roomBPosition);
            _lineRenderers[i].material = _levelStatesByBuildIndex[_selectedLevel].Graph.Edges[i].Traversable ? _traversableEdge : _notTraversableEdge;
        }

        CenterCamera();

        // Create characters
        for (int i = 0; i < _levelStatesByBuildIndex[_selectedLevel].CharacterSaves.Count; i++)
        {
            Vector3 characterPosition = _levelStatesByBuildIndex[_selectedLevel].CharacterSaves[i].Position + _levelGraphContainer.transform.position + _characterPositionOffset;
            CreateCharacter(_levelStatesByBuildIndex[_selectedLevel].CharacterSaves[i], characterPosition);
        }
    }

    private void CreateCharacter(CharacterState characterState, Vector3 characterPosition)
    {
        _characters.Add(characterState, Instantiate(_characterPrefab, characterPosition, Quaternion.identity, _levelGraphContainer.transform));
        _characters[characterState].transform.LookAt(characterPosition - _camera.transform.forward);
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

        if (_characters != null)
        {
            foreach (KeyValuePair<CharacterState, GameObject> entry in _characters)
            {
                Destroy(entry.Value);
            }

            _characters.Clear();
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

        // Update characters
        if (_characters != null && _selectedLevel > -1)
        {
            //--------HACK: NOT TESTED YET----------------------------
            // Check if any character might have exited or entered the level
            CharacterState[] characterExited = _characters.Keys.ToList().Except(_levelStatesByBuildIndex[_selectedLevel].CharacterSaves).ToArray();
            CharacterState[] characterEntered = _levelStatesByBuildIndex[_selectedLevel].CharacterSaves.Except(_characters.Keys.ToList()).ToArray();

            for (int i = 0; i < characterExited.Length; i++)
            {
                Destroy(_characters[characterExited[i]]);
                _characters.Remove(characterExited[i]);
            }

            for (int i = 0; i < characterEntered.Length; i++)
            {
                CreateCharacter(characterEntered[i], characterEntered[i].Position + _levelGraphContainer.transform.position + _characterPositionOffset);
            }
            //--------------------------------------------------------
            foreach (KeyValuePair<CharacterState, GameObject> entry in _characters)
            {
                entry.Value.transform.position = entry.Key.Position + _levelGraphContainer.transform.position + _characterPositionOffset;
                entry.Value.transform.LookAt(entry.Value.transform.position - _camera.transform.forward);
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

        // Detect if character was selected
        if (_clicked)
        {
            Vector2 positionOnRenderer;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_levelRendererTransform, Input.mousePosition, null, out positionOnRenderer);
            // Fix the position to fit with the coordinate system of the camera
            positionOnRenderer += _levelRendererTransform.pivot * _levelRendererTransform.rect.size;

            RaycastHit hit;
            Ray ray = _camera.ScreenPointToRay((positionOnRenderer / _levelRendererTransform.rect.size) * _renderTextureSize);
            
            if (Physics.Raycast(ray, out hit, _cullingMask))
            {
                // TODO: Display useful information about the character
                Debug.Log(hit.transform.gameObject.name);
            }
        }
    }

    private void LateUpdate()
    {
        _clicked = false;
    }

    public void SetContainerActive(bool active)
    {
        if (_levelGraphContainer)
        {
            _levelGraphContainer.SetActive(active);
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

    public void OnPointerLeftClick()
    {
        _clicked = true;
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
        PlaceCameraAtDistance(averagePosition, _centerTargetDistance);

        _focusing = true;
        _targetDistance = _centerTargetDistance;
    }

    private void PlaceCameraAtDistance(Vector3 target, float distance)
    {
        _camera.transform.position = target - _camera.transform.forward * distance;
    }

    private void ZoomCamera(float zoom)
    {
        Vector3 target = _camera.transform.position + _camera.transform.forward * _targetDistance;

        _camera.transform.position -= _camera.transform.forward * zoom * _zoomCameraStrength;

        // When focusing, the target distance must be adjusted to still rotate around the focus point
        if (_focusing)
        {
            Vector3 cameraToTarget = target - _camera.transform.position;

            if (Vector3.Dot(_camera.transform.forward, cameraToTarget) < 0)
            {
                _focusing = false;
                _targetDistance = _centerTargetDistance;
            }
            else
            {
                _targetDistance = cameraToTarget.magnitude;
            }
        }
    }

    private void RotateCameraAroundTarget(Vector3 mouseMovement)
    {
        Vector3 target = _camera.transform.position + _camera.transform.forward * _targetDistance;

        RotateCamera(mouseMovement, false);
        PlaceCameraAtDistance(target, _targetDistance);
    }

    private void MoveCamera(Vector3 mouseMovement)
    {
        _camera.transform.position -= _camera.transform.TransformDirection(mouseMovement) * _moveCameraSpeed;

        if (_focusing)
        {
            _focusing = false;
            _targetDistance = _centerTargetDistance;
        }
    }

    private void RotateCamera(Vector3 mouseMovement, bool looseFocus = true)
    {
        _pitch -= mouseMovement.y * _rotateCameraSpeed;
        _yaw += mouseMovement.x * _rotateCameraSpeed;
        
        _camera.transform.localEulerAngles = Vector3.right * _pitch + Vector3.up * _yaw;

        if (_focusing && looseFocus)
        {
            _focusing = false;
            _targetDistance = _centerTargetDistance;
        }
    }
    #endregion
}
