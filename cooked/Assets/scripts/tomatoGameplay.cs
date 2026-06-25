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

    [Header("Juice Explosion")]
    [SerializeField] private GameObject juiceExplosionPrefab;

    [Header("Font")]
    public Font gameFont;

    private Rigidbody rb;
    private bool   gameEnded;
    private bool   playerWon;
    private string _loseTitle   = "";
    private string _loseMessage = "";

    private struct ConfettiParticle
    {
        public Vector2 position;
        public Vector2 velocity;
        public Color   color;
        public float   size;
        public float   rotation;
        public float   rotationSpeed;
    }

    private ConfettiParticle[] _confetti;
    private bool _confettiInitialised = false;

    private struct SplatCircle
    {
        public Vector2 position;
        public float   size;
    }

    private SplatCircle[] _splats;
    private bool _splatsInitialised = false;

    // Random funny messages for each lose condition
    private string[] _knifeSubtitles = new string[]
    {
        "Welp. You're cooked. 🍅",
        "Diced. Sliced. Done.",
        "Today's special: tomato soup.",
        "The knife wins this round.",
        "You've been... chopped. 🔪"
    };

    private string[] _fallSubtitles = new string[]
    {
        "Should've stayed on the counter...",
        "Gravity: 1. Tomato: 0.",
        "That's a long way down for a little guy.",
        "Straight off the edge. Classic.",
        "Into the void you go! 🕳️"
    };

    private string[] _winSubtitles = new string[]
    {
        "Escaped successfully! 🎉",
        "You actually made it. Unbelievable.",
        "Into the bin, safe and sound!",
        "One small roll for tomato, one giant leap for tomatokind.",
        "The knife never stood a chance. 😎"
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!gameEnded && loseBelowHeight && transform.position.y < loseHeight)
            Lose("YOU DIED.", _fallSubtitles[Random.Range(0, _fallSubtitles.Length)]);

        if (gameEnded && Input.anyKeyDown)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (gameEnded && playerWon && _confettiInitialised)
            UpdateConfetti();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameEnded) return;

        if (HasTag(collision.collider, trashCanTag)) { Win(); return; }
        if (HasTag(collision.collider, groundTag))   { Lose("YOU DIED.", _fallSubtitles[Random.Range(0, _fallSubtitles.Length)]); return; }
        if (HasTag(collision.collider, knifeTag))    { SpawnJuiceExplosion(); Lose("YOU GOT CHOPPED", _knifeSubtitles[Random.Range(0, _knifeSubtitles.Length)]); }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameEnded) return;

        if (HasTag(other, trashCanTag)) { Win(); return; }
        if (HasTag(other, knifeTag))    { SpawnJuiceExplosion(); Lose("YOU GOT CHOPPED", _knifeSubtitles[Random.Range(0, _knifeSubtitles.Length)]); }
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
        gameEnded = true;
        playerWon = true;
        StopTomato();
        InitConfetti();
    }

    private void Lose(string title, string message)
    {
        _loseTitle   = title;
        _loseMessage = message;
        gameEnded    = true;
        playerWon    = false;
        StopTomato();
        InitSplats();
    }

    private void StopTomato()
    {
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;
    }

    private void InitConfetti()
    {
        _confetti = new ConfettiParticle[120];
        Color[] colors = new Color[]
        {
            new Color(1f,   0.2f, 0.2f),
            new Color(1f,   0.8f, 0f),
            new Color(0.2f, 0.9f, 0.2f),
            new Color(0.2f, 0.5f, 1f),
            new Color(1f,   0.4f, 0.8f),
            new Color(1f,   1f,   1f)
        };

        for (int i = 0; i < _confetti.Length; i++)
        {
            _confetti[i] = new ConfettiParticle
            {
                position      = new Vector2(Random.Range(0f, Screen.width), Random.Range(-50f, Screen.height)),
                velocity      = new Vector2(Random.Range(-60f, 60f), Random.Range(-200f, -80f)),
                color         = colors[Random.Range(0, colors.Length)],
                size          = Random.Range(8f, 18f),
                rotation      = Random.Range(0f, 360f),
                rotationSpeed = Random.Range(-180f, 180f)
            };
        }
        _confettiInitialised = true;
    }

    private void UpdateConfetti()
    {
        for (int i = 0; i < _confetti.Length; i++)
        {
            _confetti[i].position += _confetti[i].velocity * Time.deltaTime;
            _confetti[i].rotation += _confetti[i].rotationSpeed * Time.deltaTime;

            if (_confetti[i].position.y < -50f)
            {
                _confetti[i].position = new Vector2(
                    Random.Range(0f, Screen.width),
                    Screen.height + 10f
                );
            }
        }
    }

    private void InitSplats()
    {
        _splats = new SplatCircle[18];
        for (int i = 0; i < _splats.Length; i++)
        {
            _splats[i] = new SplatCircle
            {
                position = new Vector2(
                    Random.Range(0f, Screen.width),
                    Random.Range(0f, Screen.height)
                ),
                size = Random.Range(40f, 180f)
            };
        }
        _splatsInitialised = true;
    }

    private void OnGUI()
    {
        if (!gameEnded) return;

        if (playerWon)
            DrawWinScreen();
        else
            DrawLoseScreen();
    }

    private void DrawLoseScreen()
    {
        if (gameFont != null) GUI.skin.font = gameFont;

        GUI.color = new Color(0.15f, 0f, 0f, 1f);
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            Texture2D.whiteTexture
        );

        if (_splatsInitialised)
        {
            foreach (var splat in _splats)
            {
                GUI.color = new Color(Random.Range(0.6f, 0.9f), 0f, 0f, 0.85f);
                GUI.DrawTexture(
                    new Rect(
                        splat.position.x - splat.size * 0.5f,
                        splat.position.y - splat.size * 0.5f,
                        splat.size,
                        splat.size
                    ),
                    Texture2D.whiteTexture
                );
            }
        }

        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 120,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(1f, 0.9f, 0.9f);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 60
        };
        subStyle.normal.textColor = new Color(1f, 0.7f, 0.7f);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 60
        };
        hintStyle.normal.textColor = new Color(1f, 0.5f, 0.5f);

        GUI.Label(
            new Rect(0, Screen.height * 0.25f, Screen.width, 140f),
            _loseTitle,
            titleStyle
        );
        GUI.Label(
            new Rect(0, Screen.height * 0.52f, Screen.width, 50f),
            _loseMessage,
            subStyle
        );
        GUI.Label(
            new Rect(0, Screen.height * 0.62f, Screen.width, 40f),
            "Press any key to try again",
            hintStyle
        );
    }

    private void DrawWinScreen()
    {
        if (gameFont != null) GUI.skin.font = gameFont;

        GUI.color = new Color(0.05f, 0.5f, 0.1f, 1f);
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            Texture2D.whiteTexture
        );

        if (_confettiInitialised)
        {
            foreach (var p in _confetti)
            {
                GUI.color = p.color;
                GUIUtility.RotateAroundPivot(p.rotation, p.position);
                GUI.DrawTexture(
                    new Rect(
                        p.position.x - p.size * 0.5f,
                        Screen.height - p.position.y - p.size * 0.5f,
                        p.size,
                        p.size
                    ),
                    Texture2D.whiteTexture
                );
                GUI.matrix = Matrix4x4.identity;
            }
        }

        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 120,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = Color.white;

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 32
        };
        subStyle.normal.textColor = new Color(0.9f, 1f, 0.9f);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 24
        };
        hintStyle.normal.textColor = new Color(0.7f, 1f, 0.7f);

        string winSubtitle = _winSubtitles[Random.Range(0, _winSubtitles.Length)];

        GUI.Label(
            new Rect(0, Screen.height * 0.25f, Screen.width, 140f),
            "SAFE! 🍅",
            titleStyle
        );
        GUI.Label(
            new Rect(0, Screen.height * 0.52f, Screen.width, 50f),
            winSubtitle,
            subStyle
        );
        GUI.Label(
            new Rect(0, Screen.height * 0.62f, Screen.width, 40f),
            "Press any key to play again",
            hintStyle
        );
    }
}