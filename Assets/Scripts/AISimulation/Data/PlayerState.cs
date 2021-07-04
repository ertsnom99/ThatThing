using UnityEngine;

[CreateAssetMenu(fileName = "PlayerState", menuName = "AI Simulation/States/Player State")]
public class PlayerState : ScriptableObject
{
    [SerializeField]
    private int _level = 0;

    public int Level
    {
        get { return _level; }
        private set { _level = value; }
    }

    [SerializeField]
    private Vector3 _position = Vector3.zero;

    public Vector3 Position
    {
        get { return _position; }
        private set { _position = value; }
    }

    [SerializeField]
    private Vector3 _rotatin = Vector3.zero;

    public Vector3 Rotation
    {
        get { return _rotatin; }
        private set { _rotatin = value; }
    }
}
