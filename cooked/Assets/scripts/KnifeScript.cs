using UnityEngine;

public class KnifeFollow : MonoBehaviour
{
    [Header("Position Offset")]
    public float offsetX = 0f;
    public float offsetZ = 0f;
    
    [Header("Target")]
    public Transform target;

    [Header("Chop")]
    public float minChopInterval = 0.3f;
    public float maxChopInterval = 0.8f;
    public float chopHeight      = 23f;
    public float raisedHeight    = 29f;
    public float chopDownSpeed   = 20f;
    public float resetSpeed      = 10f;

    [Header("Airborne Avoidance")]
    public float airborneHorizontalOffset = 8f;
    public float airborneFollowSharpness = 8f;

    [Header("Rotation")]
    public float rotationSpeed = 6f;

    private float   _chopTimer;
    private float   _nextChopTime;
    private bool    _chopping = true;
    private float   _currentY;
    private Vector3 _currentDir = Vector3.forward;
    private Vector3 _currentXZ;

    void Start()
    {
        _currentY     = transform.position.y;  // use whatever Y you set in Inspector
        raisedHeight  = transform.position.y;  // treat it as the rest height too
        _nextChopTime = Random.Range(minChopInterval, maxChopInterval);
        _chopping     = true;
        _currentXZ    = new Vector3(transform.position.x, 0f, transform.position.z);
    }

    void Update()
    {
        if (target == null) return;

        tomatoRoll roll = target.GetComponent<tomatoRoll>();
        Vector3 targetDir = roll != null ? roll.LastMoveDirection : Vector3.forward;

        // Smoothly rotate to face movement direction
        _currentDir = Vector3.Slerp(
            _currentDir,
            targetDir,
            Time.deltaTime * rotationSpeed
        );

        Vector3 desiredXZ = GetDesiredXZPosition(roll, targetDir);
        _currentXZ = Vector3.Lerp(
            _currentXZ,
            desiredXZ,
            1f - Mathf.Exp(-airborneFollowSharpness * Time.deltaTime)
        );

        float x = _currentXZ.x;
        float z = _currentXZ.z;

        // Y: chop straight down and back up
        if (_chopping)
        {
            _currentY = Mathf.MoveTowards(
                _currentY, chopHeight, chopDownSpeed * Time.deltaTime);

            if (Mathf.Abs(_currentY - chopHeight) < 0.01f)
            {
                _chopping     = false;
                _chopTimer    = 0f;
                _nextChopTime = Random.Range(minChopInterval, maxChopInterval);
            }
        }
        else
        {
            _currentY = Mathf.MoveTowards(
                _currentY, raisedHeight, resetSpeed * Time.deltaTime);

            if (Mathf.Abs(_currentY - raisedHeight) < 0.05f)
            {
                _chopTimer += Time.deltaTime;
                if (_chopTimer >= _nextChopTime)
                    _chopping = true;
            }
        }

        transform.position = new Vector3(x + offsetX, _currentY, z + offsetZ);

        // Face the direction the tomato is moving
        if (_currentDir != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(_currentDir)
                * Quaternion.Euler(90f, 90f, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }

    private Vector3 GetDesiredXZPosition(tomatoRoll roll, Vector3 targetDir)
    {
        Vector3 targetXZ = new Vector3(target.position.x, 0f, target.position.z);

        if (roll == null || roll.IsGrounded)
        {
            return targetXZ;
        }

        Vector3 awayDirection = targetDir.sqrMagnitude > 0.01f
            ? -targetDir.normalized
            : -target.forward;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.01f)
        {
            awayDirection = Vector3.back;
        }

        return targetXZ + awayDirection.normalized * airborneHorizontalOffset;
    }
}