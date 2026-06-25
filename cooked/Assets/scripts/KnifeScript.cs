using UnityEngine;

public class KnifeFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Behind Tomato")]
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float heightOffset = 0.9f;
    [SerializeField] private float followSharpness = 4f;
    [SerializeField] private float directionSharpness = 10f;

    [Header("Appear When Tomato Slows")]
    [SerializeField] private float spawnDelayAfterGameplayStarts = 5f;
    [SerializeField] private float movedSpeedThreshold = 0.1f;
    [SerializeField] private float slowSpeedThreshold = 3.5f;
    [SerializeField] private float secondsSlowBeforeAppearing = 0.2f;
    [SerializeField] private bool requireMovementBeforeAppearing = true;
    [SerializeField] private bool requireGrounded = true;
    [SerializeField] private float maxVerticalSpeedForSpawn = 0.35f;

    [Header("Sponge Safe Zone")]
    [SerializeField] private float spongeSafeDistance = 5f;

    [Header("Vertical Chop")]
    [SerializeField] private float raisedHeightOffset = 1f;
    [SerializeField] private float loweredHeightOffset = 0.1f;
    [SerializeField] private float chopSpeed = 3.2f;

    [Header("Rotation")]
    [SerializeField] private bool keepSceneRotation = true;

    private tomatoRoll tomato;
    private Rigidbody targetBody;
    private Transform spongeTarget;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Vector3 followVelocity;
    private Vector3 currentMoveDirection = Vector3.forward;
    private Quaternion initialRotation;
    private float slowTimer;
    private float gameplayStartTime;
    private bool hasMoved;
    private bool isVisible;

    private void Start()
    {
        initialRotation = transform.rotation;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        FindTarget();
        FindSponge();
        ResetSpawnDelay();
        SetKnifeVisible(false);

        if (target != null)
        {
            Vector3 startDirection = GetTargetMoveDirection();
            currentMoveDirection = startDirection;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            if (target == null)
            {
                return;
            }
        }

        if (spongeTarget == null)
        {
            FindSponge();
        }

        Vector3 targetDirection = GetTargetMoveDirection();
        float directionBlend = 1f - Mathf.Exp(-directionSharpness * Time.deltaTime);
        currentMoveDirection = Vector3.Slerp(
            currentMoveDirection,
            targetDirection,
            directionBlend
        ).normalized;

        if (!ShouldShowKnife())
        {
            SetKnifeVisible(false);
            return;
        }

        if (!isVisible)
        {
            transform.position = GetDesiredBasePosition(currentMoveDirection, 0f);
            followVelocity = Vector3.zero;
            SetKnifeVisible(true);
        }

        float chopPhase = Mathf.PingPong(Time.time * chopSpeed, 1f);
        float easedChop = Mathf.SmoothStep(0f, 1f, chopPhase);
        Vector3 desiredPosition =
            GetDesiredBasePosition(currentMoveDirection, easedChop);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            1f / Mathf.Max(0.01f, followSharpness)
        );

        if (keepSceneRotation)
        {
            transform.rotation = initialRotation;
        }
    }

    private bool ShouldShowKnife()
    {
        if (Time.time - gameplayStartTime < spawnDelayAfterGameplayStarts)
        {
            slowTimer = 0f;
            return false;
        }

        float speed = GetTargetFlatSpeed();

        if (speed > movedSpeedThreshold)
        {
            hasMoved = true;
        }

        if (!CanSpawnOnSurface() || IsNearSponge())
        {
            slowTimer = 0f;
            return false;
        }

        if (speed > slowSpeedThreshold)
        {
            slowTimer = 0f;
            return false;
        }

        if (requireMovementBeforeAppearing && !hasMoved)
        {
            return false;
        }

        slowTimer += Time.deltaTime;
        return slowTimer >= secondsSlowBeforeAppearing;
    }

    public void ResetSpawnDelay()
    {
        gameplayStartTime = Time.time;
        slowTimer = 0f;
        hasMoved = false;
        SetKnifeVisible(false);
    }

    private bool CanSpawnOnSurface()
    {
        if (!requireGrounded)
        {
            return true;
        }

        if (tomato != null && !tomato.IsGrounded)
        {
            return false;
        }

        if (targetBody != null && Mathf.Abs(targetBody.linearVelocity.y) > maxVerticalSpeedForSpawn)
        {
            return false;
        }

        return true;
    }

    private bool IsNearSponge()
    {
        if (spongeTarget == null || target == null || spongeSafeDistance <= 0f)
        {
            return false;
        }

        return Vector3.Distance(target.position, spongeTarget.position) <= spongeSafeDistance;
    }

    private float GetTargetFlatSpeed()
    {
        if (targetBody == null)
        {
            return 0f;
        }

        Vector3 velocity = targetBody.linearVelocity;
        velocity.y = 0f;
        return velocity.magnitude;
    }

    private void SetKnifeVisible(bool visible)
    {
        if (isVisible == visible)
        {
            return;
        }

        isVisible = visible;

        foreach (Renderer knifeRenderer in renderers)
        {
            if (knifeRenderer != null)
            {
                knifeRenderer.enabled = visible;
            }
        }

        foreach (Collider knifeCollider in colliders)
        {
            if (knifeCollider != null)
            {
                knifeCollider.enabled = visible;
            }
        }
    }

    private Vector3 GetDesiredBasePosition(Vector3 moveDirection, float chopAmount)
    {
        float chopHeight = Mathf.Lerp(
            raisedHeightOffset,
            loweredHeightOffset,
            chopAmount
        );

        return target.position -
            moveDirection * followDistance +
            Vector3.up * (heightOffset + chopHeight);
    }

    private Vector3 GetTargetMoveDirection()
    {
        Vector3 direction = tomato != null
            ? tomato.LastMoveDirection
            : target.forward;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = currentMoveDirection.sqrMagnitude > 0.01f
                ? currentMoveDirection
                : Vector3.forward;
        }

        return direction.normalized;
    }

    private void FindTarget()
    {
        if (target != null)
        {
            tomato = target.GetComponent<tomatoRoll>();
            targetBody = target.GetComponent<Rigidbody>();
            if (targetBody == null)
            {
                targetBody = target.GetComponentInParent<Rigidbody>();
            }
            return;
        }

        tomato = FindObjectOfType<tomatoRoll>();
        if (tomato != null)
        {
            target = tomato.transform;
            targetBody = target.GetComponent<Rigidbody>();
            if (targetBody == null)
            {
                targetBody = target.GetComponentInParent<Rigidbody>();
            }
        }
    }

    private void FindSponge()
    {
        SpongeBounce sponge = FindObjectOfType<SpongeBounce>();
        spongeTarget = sponge != null ? sponge.transform : null;
    }
}
