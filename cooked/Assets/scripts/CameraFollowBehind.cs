using UnityEngine;

/// <summary>
/// A stable third-person camera for a rolling Rigidbody character.
/// It follows the target's position but never copies its rotation.
/// </summary>
[RequireComponent(typeof(Camera))]
public class StableThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

    [Header("Distance")]
    [SerializeField, Min(0.1f)] private float distance = 6f;
    [SerializeField, Min(0.1f)] private float minimumDistance = 1f;

    [Header("Orbit")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float startingPitch = 20f;
    [SerializeField] private float minimumPitch = -10f;
    [SerializeField] private float maximumPitch = 65f;
    [SerializeField] private bool holdRightMouseButtonToOrbit = true;

    [Header("Smoothing")]
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.08f;
    [SerializeField, Min(0f)] private float rotationSharpness = 18f;

    [Header("Collision")]
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.25f;
    [SerializeField] private LayerMask collisionLayers = ~0;

    private Vector3 followVelocity;
    private readonly RaycastHit[] collisionHits = new RaycastHit[16];
    private float yaw;
    private float pitch;

    public Transform Target
    {
        get => target;
        set
        {
            target = value;
            SnapToTarget();
        }
    }

    private void Awake()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = startingPitch;
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        ReadOrbitInput();

        Vector3 focusPoint = target.position + targetOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 backwardDirection = orbitRotation * Vector3.back;

        float correctedDistance = GetCorrectedDistance(
            focusPoint,
            backwardDirection
        );

        Vector3 desiredPosition =
            focusPoint + backwardDirection * correctedDistance;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            positionSmoothTime
        );

        Quaternion desiredRotation = Quaternion.LookRotation(
            focusPoint - transform.position,
            Vector3.up
        );

        float rotationBlend =
            1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationBlend
        );
    }

    private void ReadOrbitInput()
    {
        bool canOrbit =
            !holdRightMouseButtonToOrbit ||
            Input.GetMouseButton(1);

        if (!canOrbit)
        {
            return;
        }

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
    }

    private float GetCorrectedDistance(
        Vector3 focusPoint,
        Vector3 backwardDirection
    )
    {
        if (!avoidObstacles)
        {
            return distance;
        }

        int hitCount = Physics.SphereCastNonAlloc(
            focusPoint,
            collisionRadius,
            backwardDirection,
            collisionHits,
            distance,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = distance;
        bool foundObstacle = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = collisionHits[i];

            // Do not let the tomato's own collider push the camera forward.
            if (hit.collider.transform.IsChildOf(target))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                foundObstacle = true;
            }
        }

        if (foundObstacle)
        {
            return Mathf.Clamp(
                closestDistance - collisionRadius,
                minimumDistance,
                distance
            );
        }

        return distance;
    }

    [ContextMenu("Snap To Target")]
    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        followVelocity = Vector3.zero;

        Vector3 focusPoint = target.position + targetOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position =
            focusPoint + orbitRotation * Vector3.back * distance;

        transform.rotation = Quaternion.LookRotation(
            focusPoint - transform.position,
            Vector3.up
        );
    }
}
