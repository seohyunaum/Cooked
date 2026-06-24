using UnityEngine;

public class KnifeFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Position")]
    public float behindDistance = 2f;
    public float restHeight     = 2f;
    public float smoothSpeed    = 6f;

    [Header("Chop")]
    public float minChopInterval = 0.3f;
    public float maxChopInterval = 1f;
    public float chopHeight      = 23f;
    public float restHeight2     = 29f;  // rename in inspector to "Raised Height"
    public float chopDownSpeed   = 16f;
    public float resetSpeed      = 8f;

    private Vector3 _currentVelocity;
    private float   _chopTimer;
    private float   _nextChopTime;
    private bool    _chopping = true;  // start chopping immediately
    private float   _currentY;

    void Start()
    {
        _currentY     = restHeight2;
        _nextChopTime = Random.Range(minChopInterval, maxChopInterval);
        _chopping     = true;
    }

    void Update()
    {
        if (target == null) return;

        tomatoRoll roll = target.GetComponent<tomatoRoll>();
        Vector3 dir = roll != null ? roll.LastMoveDirection : Vector3.forward;

        // XZ: always follow behind tomato
        Vector3 desiredXZ = target.position - dir * behindDistance;

        Vector3 smoothedXZ = Vector3.SmoothDamp(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(desiredXZ.x,          0f, desiredXZ.z),
            ref _currentVelocity,
            1f / smoothSpeed
        );

        // Y: constantly chop up and down with no idle hover
        if (_chopping)
        {
            // slam down
            _currentY = Mathf.MoveTowards(_currentY, chopHeight, chopDownSpeed * Time.deltaTime);
            if (Mathf.Abs(_currentY - chopHeight) < 0.01f)
            {
                _chopping     = false;
                _chopTimer    = 0f;
                _nextChopTime = Random.Range(minChopInterval, maxChopInterval);
            }
        }
        else
        {
            // raise back up quickly
            _currentY = Mathf.MoveTowards(_currentY, restHeight2, resetSpeed * Time.deltaTime);

            // as soon as it's back up, chop again immediately
            if (Mathf.Abs(_currentY - restHeight2) < 0.05f)
            {
                _chopTimer += Time.deltaTime;
                if (_chopTimer >= _nextChopTime)
                    _chopping = true;
            }
        }

        transform.position = new Vector3(smoothedXZ.x, _currentY, smoothedXZ.z);
        transform.rotation = Quaternion.Euler(90f, 90f, 0f);
    }
}