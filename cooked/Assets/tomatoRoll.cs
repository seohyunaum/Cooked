using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class tomatoRoll : MonoBehaviour
{
    [Header("Movement")]
    public float rollForce = 10f;
    public float maxSpeed = 8f;
    public float jumpForce = 6f;

    [Header("Camera-relative movement")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private bool isGrounded;

    public Vector3 LastMoveDirection { get; private set; }
        = Vector3.forward;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // The Rigidbody must be allowed to rotate.
        rb.freezeRotation = false;
    }

    void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection;

        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            moveDirection =
                forward * vertical +
                right * horizontal;
        }
        else
        {
            moveDirection = new Vector3(
                horizontal,
                0f,
                vertical
            );
        }

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            LastMoveDirection = moveDirection.normalized;

            // Rotate around the axis perpendicular to movement.
            Vector3 torqueDirection = new Vector3(
                moveDirection.z,
                0f,
                -moveDirection.x
            );

            rb.AddTorque(
                torqueDirection * rollForce,
                ForceMode.Force
            );
        }

        LimitSpeed();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            isGrounded = false;
        }
    }

    void LimitSpeed()
    {
        Vector3 velocity = rb.linearVelocity;

        Vector3 flatVelocity = new Vector3(
            velocity.x,
            0f,
            velocity.z
        );

        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedVelocity =
                flatVelocity.normalized * maxSpeed;

            rb.linearVelocity = new Vector3(
                limitedVelocity.x,
                velocity.y,
                limitedVelocity.z
            );
        }
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}