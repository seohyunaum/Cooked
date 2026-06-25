using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class tomatoGameplay : MonoBehaviour
{
    [Header("Win / Lose Tags")]
    [SerializeField] private string groundTag   = "Ground";
    [SerializeField] private string trashCanTag = "TrashCan";
    [SerializeField] private string knifeTag    = "Knife";

    [Header("Fall Check")]
    [SerializeField] private bool  loseBelowHeight = true;
    [SerializeField] private float loseHeight      = -5f;

    [Header("Restart")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    [Header("End Screen")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("Grace Period")]
    [SerializeField] private float gracePeriod = 3f;

    [Header("Juice Explosion")]
    [SerializeField] private GameObject juiceExplosionPrefab;

    private Rigidbody rb;
    private bool   gameEnded;
    private bool   playerWon;
    private string endMessage = "";
    private float  endTime;
    private float  _startTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _startTime = Time.time;
    }

    private void Update()
    {
        if (!gameEnded && loseBelowHeight && transform.position.y < loseHeight)
            Lose("You fell off the counter!");

        if (gameEnded && Input.GetKeyDown(restartKey))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private bool InGracePeriod()
    {
        return Time.time - _startTime < gracePeriod;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameEnded || InGracePeriod()) return;

        if (HasTag(collision.collider, trashCanTag)) { Win(); return; }
        if (HasTag(collision.collider, groundTag))   { Lose("You fell off the counter!"); return; }
        if (HasTag(collision.collider, knifeTag))    { SpawnJuiceExplosion(); Lose("Chopped!"); }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameEnded || InGracePeriod()) return;

        if (HasTag(other, trashCanTag)) { Win(); return; }
        if (HasTag(other, knifeTag))    { SpawnJuiceExplosion(); Lose("Chopped!"); }
    }

    private void SpawnJuiceExplosion()
    {
        if (juiceExplosionPrefab != null)
        {
            GameObject vfx = Instantiate(
                juiceExplosionPrefab,
                transform.position,
                Quaternion.identity
            );
            Destroy(vfx, 3f);
        }

        GetComponentInChildren<MeshRenderer>()?.gameObject.SetActive(false);
    }

    private bool HasTag(Collider other, string tagName)
    {
        return !string.IsNullOrEmpty(tagName) && other.gameObject.tag == tagName;
    }

    private void Win()
    {
        gameEnded  = true;
        playerWon  = true;
        endMessage = "You reached the trash can!";
        endTime    = Time.time;
        StopTomato();
    }

    private void Lose(string message)
    {
        gameEnded  = true;
        playerWon  = false;
        endMessage = message;
        endTime    = Time.time;
        StopTomato();
    }

    private void StopTomato()
    {
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;
    }

    private void OnGUI()
    {
        if (!gameEnded) return;

        float fadeAmount = Mathf.Clamp01((Time.time - endTime) / fadeDuration);

        if (!playerWon)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, fadeAmount);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture
            );
            GUI.color = previousColor;
        }

        if (fadeAmount < 1f && !playerWon) return;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 48,
            fontStyle = FontStyle.Bold
        };

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 22
        };

        string title    = playerWon ? "YOU WIN!" : "GAME OVER";
        Color  oldColor = GUI.color;
        GUI.color = Color.white;

        GUI.Label(new Rect(0f, Screen.height * 0.35f, Screen.width, 70f), title,                titleStyle);
        GUI.Label(new Rect(0f, Screen.height * 0.48f, Screen.width, 40f), endMessage,           bodyStyle);
        GUI.Label(new Rect(0f, Screen.height * 0.56f, Screen.width, 40f), "Press R to restart", bodyStyle);

        GUI.color = oldColor;
    }
}