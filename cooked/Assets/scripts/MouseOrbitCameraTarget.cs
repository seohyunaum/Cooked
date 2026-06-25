using UnityEngine;

public class MouseOrbitCameraTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool holdRightMouseButtonToOrbit = true;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minimumPitch = -15f;
    [SerializeField] private float maximumPitch = 65f;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = NormalizeAngle(angles.x);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            tomatoRoll tomato = FindObjectOfType<tomatoRoll>();
            if (tomato != null)
            {
                target = tomato.transform;
            }
        }

        bool canOrbit =
            !holdRightMouseButtonToOrbit ||
            Input.GetMouseButton(1);

        if (canOrbit)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
        }

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
