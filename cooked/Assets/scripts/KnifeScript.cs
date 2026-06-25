using UnityEngine;

public class KnifeFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Chop")]
    public float minChopInterval = 0.3f;
    public float maxChopInterval = 0.8f;
    public float chopHeight      = 23f;
    public float raisedHeight    = 29f;
    public float chopDownSpeed   = 20f;
    public float resetSpeed      = 10f;

    [Header("Rotation")]
    public float rotationSpeed = 6f;

    private float   _chopTimer;
    private float   _nextChopTime;
    private bool    _chopping = true;
    private float   _currentY;
    private Vector3 _currentDir = Vector3.forward;

    void Start()
    {
        _currentY     = raisedHeight;
        _nextChopTime = Random.Range(minChopInterval, maxChopInterval);
        _chopping     = true;
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

        // XZ: snap directly onto tomato, no lag
        float x = target.position.x;
        float z = target.position.z;

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

        transform.position = new Vector3(x, _currentY, z);

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
}