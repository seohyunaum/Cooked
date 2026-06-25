using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class tomatoRoll : MonoBehaviour
{
    [Header("Movement")]
    public float rollForce = 10f;
    public float maxSpeed = 8f;
    public float jumpForce = 6f;
    public float airControlForce = 18f;
    public float maxAirSpeed = 18f;

    [Header("Camera-relative movement")]
    public Transform cameraTransform;

    [Header("Movement Sounds")]
    [SerializeField] private AudioClip rollingSound;
    [SerializeField] private AudioSource rollingAudioSource;
    [SerializeField, Range(0f, 15f)] private float rollingVolume = 15f;
    [SerializeField] private float rollingInputThreshold = 0.1f;

    [SerializeField] private AudioClip squishedSound;
    [SerializeField] private AudioSource squishedAudioSource;
    [SerializeField, Range(0f, 1f)] private float squishedVolume = 1f;
    [SerializeField] private float minSecondsBetweenSquishes = 0.15f;

    private Rigidbody rb;
    private AudioSource[] rollingAudioSources;
    private int configuredRollingSourceCount;
    private bool isGrounded;
    private bool hasBeenInAir = false;
    private float lastSquishTime = -999f;

    public bool IsGrounded => isGrounded;

    public Vector3 LastMoveDirection { get; private set; }
        = Vector3.forward;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // The Rigidbody must be allowed to rotate.
        rb.freezeRotation = false;

        SetupMovementAudio();
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

            Vector3 torqueDirection = new Vector3(
                moveDirection.z,
                0f,
                -moveDirection.x
            );

            rb.AddTorque(
                torqueDirection * rollForce,
                ForceMode.Force
            );

            if (!isGrounded)
            {
                rb.AddForce(
                    moveDirection.normalized * airControlForce,
                    ForceMode.Acceleration
                );
            }
        }

        LimitSpeed();
    }

    void Update()
    {
        UpdateRollingSound();

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            isGrounded = false;
            hasBeenInAir = true;

            StopRollingSound();
        }
    }

    private void OnDisable()
    {
        StopRollingSound();
    }

    private void SetupMovementAudio()
    {
        ConfigureRollingAudioSources();

        if (squishedSound != null && squishedAudioSource == null)
        {
            squishedAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (squishedAudioSource != null)
        {
            squishedAudioSource.playOnAwake = false;
            squishedAudioSource.loop = false;
            squishedAudioSource.spatialBlend = 0f;
            squishedAudioSource.volume = squishedVolume;
        }
    }

    private void ConfigureRollingAudioSources()
    {
        if (rollingSound == null)
        {
            return;
        }

        int sourceCount = Mathf.Max(1, Mathf.CeilToInt(rollingVolume));
        if (
            rollingAudioSources != null &&
            configuredRollingSourceCount == sourceCount
        )
        {
            ApplyRollingAudioSettings();
            return;
        }

        if (rollingAudioSource == null)
        {
            rollingAudioSource = gameObject.AddComponent<AudioSource>();
        }

        StopRollingSound();

        rollingAudioSources = new AudioSource[sourceCount];
        rollingAudioSources[0] = rollingAudioSource;

        for (int i = 1; i < rollingAudioSources.Length; i++)
        {
            rollingAudioSources[i] = gameObject.AddComponent<AudioSource>();
        }

        configuredRollingSourceCount = sourceCount;
        ApplyRollingAudioSettings();
    }

    private void ApplyRollingAudioSettings()
    {
        if (rollingAudioSources == null)
        {
            return;
        }

        float remainingVolume = rollingVolume;
        foreach (AudioSource source in rollingAudioSources)
        {
            if (source == null)
            {
                continue;
            }

            source.clip = rollingSound;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = Mathf.Clamp01(remainingVolume);
            remainingVolume -= 1f;
        }
    }

    private void UpdateRollingSound()
    {
        if (rollingSound == null)
        {
            return;
        }

        if (
            rollingAudioSources == null ||
            configuredRollingSourceCount != Mathf.Max(1, Mathf.CeilToInt(rollingVolume))
        )
        {
            ConfigureRollingAudioSources();
        }

        if (rollingAudioSources == null)
        {
            return;
        }

        bool shouldRollSoundPlay =
            isGrounded && IsMovementKeyHeld();

        ApplyRollingAudioSettings();

        if (shouldRollSoundPlay)
        {
            foreach (AudioSource source in rollingAudioSources)
            {
                if (source != null && !source.isPlaying && source.volume > 0f)
                {
                    source.Play();
                }
            }
        }
        else
        {
            StopRollingSound();
        }
    }

    private bool IsMovementKeyHeld()
    {
        return
            Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.UpArrow) ||
            Input.GetKey(KeyCode.DownArrow) ||
            Input.GetKey(KeyCode.LeftArrow) ||
            Input.GetKey(KeyCode.RightArrow);
    }

    private void StopRollingSound()
    {
        if (rollingAudioSources != null)
        {
            foreach (AudioSource source in rollingAudioSources)
            {
                if (source != null && source.isPlaying)
                {
                    source.Stop();
                }
            }
            return;
        }

        if (rollingAudioSource != null && rollingAudioSource.isPlaying)
        {
            rollingAudioSource.Stop();
        }
    }

    private void PlaySquishedSound()
    {
        if (
            squishedSound == null ||
            Time.time - lastSquishTime < minSecondsBetweenSquishes
        )
        {
            return;
        }

        if (squishedAudioSource == null)
        {
            SetupMovementAudio();
        }

        if (squishedAudioSource != null)
        {
            squishedAudioSource.PlayOneShot(
                squishedSound,
                squishedVolume
            );

            lastSquishTime = Time.time;
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

        float currentMaxSpeed =
            isGrounded ? maxSpeed : maxAirSpeed;

        if (flatVelocity.magnitude > currentMaxSpeed)
        {
            Vector3 limitedVelocity =
                flatVelocity.normalized * currentMaxSpeed;

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

    void OnCollisionEnter(Collision collision)
    {
        // Ignore sponge bounce objects.
        if (collision.gameObject.GetComponentInParent<SpongeBounce>() != null)
        {
            return;
        }

        bool collidedWithGround = collision.gameObject.CompareTag("ground");

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;

                // Squish sound ONLY plays when:
                // 1. the object is tagged ground
                // 2. tomato has been in the air
                // 3. tomato lands on top of it
                if (collidedWithGround && hasBeenInAir)
                {
                    PlaySquishedSound();
                    hasBeenInAir = false;
                }

                return;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
        hasBeenInAir = true;

        StopRollingSound();
    }
}
