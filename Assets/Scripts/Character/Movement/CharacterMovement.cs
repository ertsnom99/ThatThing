using UnityEngine;

// This script requires thoses components and will be added if they aren't already there
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float _rotationSpeed = 10.0f;
    private float _rotationToBeDone;

    [SerializeField]
    private float _acceleration = 20.0f;
    [SerializeField]
    private float _deceleration = 32.0f;

    [SerializeField]
    private float _maxWalkSpeed = 5.0f;

    private Vector3 _velocity = Vector3.zero;

    /*[Header("Airborne")]
    [SerializeField]
    private float _weightMultiplier = 2.8f;
    [SerializeField]
    private float _maxFallingVelocity = 100.0f;

    public bool IsAirborne { get; private set; }*/

    [Header("Animation")]
    [SerializeField]
    private bool _updateAnimatorVariables = false;

    [Header("Debug")]
    [SerializeField]
    private bool _debugMovement = false;

    private Inputs _currentInputs;

    private CapsuleCollider _capsuleCollider;
    private Rigidbody _rigidbody;
    private Animator _animator;

    // Animator params for forward movement
    private int _speedParamHashId = Animator.StringToHash(SpeedParamNameString);
    private int _rotationToBeDoneParamHashId = Animator.StringToHash(RotationToBeDoneParamNameString);
    //private int _lastAirborneYVelocityParamHashId = Animator.StringToHash(LastAirborneYVelocityParamNameString);
    //private int _isAirborneParamHashId = Animator.StringToHash(IsAirborneParamNameString);

    public const string SpeedParamNameString = "Speed";
    public const string RotationToBeDoneParamNameString = "RotationToBeDone";
    //public const string LastAirborneYVelocityParamNameString = "LastAirborneYVelocity";
    //public const string IsAirborneParamNameString = "IsAirborne";

    private void Awake()
    {
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        //IsAirborne = false;
    }

    private void Update()
    {
        if (_updateAnimatorVariables)
        {
            UpdateAnimator();
        }
    }

    public void UpdateInput(Inputs inputs)
    {
        _currentInputs = inputs;
    }

    public void FixedUpdate()
    {
        Rotate();
        Move();
    }

    private void Rotate()
    {
        Vector3 inputDirection = new Vector3(_currentInputs.Horizontal, .0f, _currentInputs.Vertical).normalized;

        if (inputDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
            _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
        }
        else
        {
            _rigidbody.angularVelocity = Vector3.zero;
        }

        _rotationToBeDone = Vector3.SignedAngle(transform.forward, inputDirection, Vector3.up);
    }

    private float CalculateSignedAngle(Quaternion rotationA, Quaternion rotationB)
    {
        // get a "forward vector" for each rotation
        Vector3 forwardA = rotationA * Vector3.forward;
        Vector3 forwardB = rotationB * Vector3.forward;

        return Vector3.SignedAngle(forwardA, forwardB, Vector3.up);
    }

    private void Move()
    {
        Vector3 movementInput = new Vector3(_currentInputs.Horizontal, .0f, _currentInputs.Vertical).normalized;

        if (movementInput.magnitude > 1.0f)
        {
            movementInput.Normalize();
        }

        UpdateVelocity(movementInput);

        _rigidbody.velocity = new Vector3(_velocity.x, _rigidbody.velocity.y, _velocity.z);
        //_rigidbody.MovePosition(transform.position + new Vector3(_velocity.x, .0f, _velocity.z) * Time.fixedDeltaTime);

        //UpdateAirborneState();

        if (_debugMovement)
        {
            DrawVelocityAtCharacterPos(_velocity, Color.green, false, true);
        }
    }

    private void UpdateVelocity(Vector3 movementInput)
    {
        Vector2 convertedInput = new Vector2(movementInput.x, movementInput.z);
        Vector3 flattenPreviousVelocity = new Vector3(_velocity.x, .0f, _velocity.z);
        Vector3 maxVelocity = _maxWalkSpeed * movementInput;
        Vector3 flattenVelocity = Vector3.zero;

        // If the character moves without having achieved is maximum velocity
        if (convertedInput != Vector2.zero && flattenPreviousVelocity != maxVelocity)
        {
            Vector3 velocityDirection = (maxVelocity - flattenPreviousVelocity).normalized;

            _velocity += convertedInput.magnitude * _acceleration * velocityDirection * Time.fixedDeltaTime;

            flattenVelocity = _velocity;
            flattenVelocity.y = 0;

            // If the velocity overshot the maximum
            if (Vector3.Dot(maxVelocity - flattenPreviousVelocity, maxVelocity - flattenVelocity) < .0f)
            {
                _velocity.x = maxVelocity.x;
                _velocity.z = maxVelocity.z;
            }
        }
        // If the character doesn't want to move, but didn't loose all is velocity
        else if (convertedInput == Vector2.zero && flattenPreviousVelocity != Vector3.zero)
        {
            Vector3 velocityDirection = flattenPreviousVelocity.normalized;

            _velocity -= _deceleration * velocityDirection * Time.fixedDeltaTime;

            flattenVelocity = _velocity;
            flattenVelocity.y = 0;

            // If the velocity overshot zero
            if (Vector3.Dot(flattenPreviousVelocity, flattenVelocity) < 0)
            {
                _velocity.x = .0f;
                _velocity.z = .0f;
            }
        }

        /*if (!IsAirborne)
        {
            _velocity.y = .0f;
        }
        
        // Increments the effect of the gravity on the character fall
        _velocity.y += Physics.gravity.y * _weightMultiplier * Time.fixedDeltaTime;

        if (_velocity.y < -_maxFallingVelocity) _velocity.y = -_maxFallingVelocity;*/
    }

    /*private void UpdateAirborneState()
    {
        Vector3 bottomSPherePosition = transform.position + Vector3.down * (_capsuleCollider.height / 2 - _capsuleCollider.radius);
        float rayDistance = _capsuleCollider.radius + .2f;
        RaycastHit hit;

        IsAirborne = !Physics.Raycast(bottomSPherePosition, Vector3.down, out hit, rayDistance);
    }*/

    private void UpdateAnimator()
    {
        float speed = new Vector3(_velocity.x, .0f, _velocity.z).magnitude;

        if (speed <= .001f)
        {
            speed = .0f;
        }
        /*else
        {
            speed /= _maxWalkSpeed;
        }*/

        _animator.SetFloat(_speedParamHashId, speed);
        _animator.SetFloat(_rotationToBeDoneParamHashId, _rotationToBeDone / 90.0f);

        /*if (IsAirborne)
        {
            _animator.SetFloat(_lastAirborneYVelocityParamHashId, _velocity.y);
        }

        _animator.SetBool(_isAirborneParamHashId, IsAirborne);*/
    }

    private void DrawVelocityAtCharacterPos(Vector3 velocity, Color color, bool ignoreXaxis = false, bool ignoreYaxis = false, bool ignoreZaxis = false)
    {
        if (ignoreXaxis) velocity.x = 0;
        if (ignoreYaxis) velocity.y = 0;
        if (ignoreZaxis) velocity.z = 0;

        Debug.DrawLine(transform.position, transform.position + velocity, color);
    }

    public void NullifyVelocity()
    {
        _velocity = Vector3.zero;

        UpdateAnimator();
    }

    public float GetAcceleration()
    {
        return _acceleration;
    }

    public void SetAcceleration(float acceleration)
    {
        _acceleration = acceleration;
    }

    public float GetDeceleration()
    {
        return _deceleration;
    }

    public void SetDeceleration(float deceleration)
    {
        _deceleration = deceleration;
    }

    public float GetMaxWalkSpeed()
    {
        return _maxWalkSpeed;
    }

    public void SetMaxWalkSpeed(float maxWalkSpeed)
    {
        _maxWalkSpeed = maxWalkSpeed;
    }
}
