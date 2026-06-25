using UnityEngine;

public class SpongeBounce : MonoBehaviour
{
    [Header("Bounce")]
    [SerializeField] private float bounceVelocity = 18f;
    [SerializeField] private float minSecondsBetweenBounces = 0.15f;
    [SerializeField] private bool onlyBounceTomato = true;

    [Header("Colors")]
    [SerializeField] private bool colorSpongeOnStart = true;
    [SerializeField] private Color spongeYellow = new Color(1f, 0.86f, 0.08f);
    [SerializeField] private Color scrubGreen = new Color(0.12f, 0.62f, 0.24f);

    private float lastBounceTime = -999f;

    private void Start()
    {
        if (colorSpongeOnStart)
        {
            ApplySpongeColors();
        }
    }

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

    private void ApplySpongeColors()
    {
        Renderer[] spongeRenderers = GetComponentsInChildren<Renderer>(true);
        if (spongeRenderers.Length == 0)
        {
            return;
        }

        int materialIndex = 0;
        foreach (Renderer spongeRenderer in spongeRenderers)
        {
            Material[] materials = spongeRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Color color = materialIndex % 2 == 0 ? spongeYellow : scrubGreen;
                SetMaterialColor(materials[i], color);
                materialIndex++;
            }
        }

    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }
}
