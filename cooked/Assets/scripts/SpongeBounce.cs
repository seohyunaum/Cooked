using UnityEngine;

public class SpongeBounce : MonoBehaviour
{
    [Header("Bounce")]
    [SerializeField] private float bounceVelocity = 18f;
    [SerializeField] private float minSecondsBetweenBounces = 0.15f;
    [SerializeField] private bool onlyBounceTomato = true;

    private float lastBounceTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastBounceTime < minSecondsBetweenBounces)
        {
            return;
        }

        Rigidbody tomatoBody = collision.rigidbody;
        if (tomatoBody == null)
        {
            return;
        }

        if (onlyBounceTomato && !collision.gameObject.TryGetComponent(out tomatoRoll _))
        {
            return;
        }

        Vector3 velocity = tomatoBody.linearVelocity;
        velocity.y = Mathf.Max(velocity.y, bounceVelocity);
        tomatoBody.linearVelocity = velocity;

        lastBounceTime = Time.time;
    }
}