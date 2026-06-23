using UnityEngine;

public class CameraFollowBehind : MonoBehaviour
{
    public Transform target;

    [Header("Camera Position")]
    public float distanceBehind = 7f;
    public float height = 4f;

    [Header("Smoothing")]
    public float followSmoothTime = 0.15f;
    public float lookHeight = 1f;

    private Vector3 behindDirection;
    private Vector3 followVelocity;

    void Start()
    {
        if (target == null)
            return;

        // Remember which side of the tomato the camera starts on.
        Vector3 startingOffset = transform.position - target.position;
        startingOffset.y = 0f;

        if (startingOffset.sqrMagnitude > 0.01f)
        {
            behindDirection = startingOffset.normalized;
        }
        else
        {
            behindDirection = Vector3.back;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // The direction stays fixed, preventing left/right camera swings.
        Vector3 desiredPosition =
            target.position
            + behindDirection * distanceBehind
            + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            followSmoothTime
        );

        transform.LookAt(
            target.position + Vector3.up * lookHeight
        );
    }
}