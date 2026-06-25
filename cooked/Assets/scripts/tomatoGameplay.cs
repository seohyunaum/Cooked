using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class tomatoGameplay : MonoBehaviour
{
    private const string BestTimeKey = "CookedBestWinTime";

    [Header("Win / Lose Tags")]
    [SerializeField] private string groundTag   = "ground";
    [SerializeField] private string trashCanTag = "trashcan";
    [SerializeField] private string knifeTag    = "Knife";

    [Header("Fall Check")]
    [SerializeField] private bool  loseBelowHeight = true;
    [SerializeField] private float loseHeight      = -5f;

    [Header("Juice Explosion")]
    [SerializeField] private GameObject juiceExplosionPrefab;

    [Header("Font")]
    public Font gameFont;

    [Header("Restart")]
    [SerializeField] private float restartInputDelay = 0.35f;

    private Rigidbody rb;

    [Header("Music")]
    [SerializeField] private AudioSource bgm;
    [SerializeField] private AudioClip gameOverBGM;
    [SerializeField, Range(0f, 1f)] private float gameOverBGMVolume = 1f;

    [Header("Lose Scream")]
    [SerializeField] private AudioClip loseScream;
    [SerializeField] private AudioSource loseScreamSource;
    [SerializeField, Range(0f, 3f)] private float loseScreamVolume = 2.5f;

    [Header("Ground Squish Sound")]
    [SerializeField] private AudioClip squishedSound;
    [SerializeField] private AudioSource squishedAudioSource;
    [SerializeField, Range(0f, 3f)] private float squishedVolume = 1f;

    [Header("Win Cheer Sound")]
    [SerializeField] private AudioClip winCheer;
    [SerializeField] private AudioSource winCheerSource;
    [SerializeField, Range(0f, 3f)] private float winCheerVolume = 1f;

    private bool   gameEnded;
    private bool   playerWon;
    private bool   restartQueued;
    private string _loseTitle   = "";
    private string _loseMessage = "";
    private string _winMessage  = "";
    private float  _startTime;
    private float  _restartInputReadyTime;
    private float  _lastWinTime;
    private float  _bestWinTime;

    public bool HasEnded => gameEnded;

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
        SetupLoseScreamSource();
        SetupSquishedAudioSource();
        SetupWinCheerSource();
    }

    private void Update()
    {
        if (!gameEnded && loseBelowHeight && transform.position.y < loseHeight)
        {
            Lose(
                "YOU DIED.",
                _fallSubtitles[Random.Range(0, _fallSubtitles.Length)],
                false
            );
        }

        if (gameEnded && playerWon && _confettiInitialised)
        {
            UpdateConfetti();
        }
    }

    private bool TryRestartFromEndScreen()
    {
        if (!gameEnded || restartQueued || Time.time < _restartInputReadyTime)
        {
            return false;
        }

        restartQueued = true;
        GameStartSequence.ReloadToStartScreen();
        return true;
    }

    public void ResetRunTimer()
    {
        _startTime = Time.time;
    }

    public void LoseFromTimer()
    {
        if (gameEnded)
        {
            return;
        }

        Lose("TIME'S UP!", "The kitchen caught up with you.", false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameEnded)
        {
            return;
        }

        if (HasTag(collision.collider, trashCanTag))
        {
            Win();
            return;
        }

        if (HasTag(collision.collider, groundTag))
        {
            Lose(
                "YOU DIED.",
                _fallSubtitles[Random.Range(0, _fallSubtitles.Length)],
                true
            );
            return;
        }

        if (HasTag(collision.collider, knifeTag))
        {
            SpawnJuiceExplosion();

            Lose(
                "YOU GOT CHOPPED",
                _knifeSubtitles[Random.Range(0, _knifeSubtitles.Length)],
                false
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameEnded)
        {
            return;
        }

        if (HasTag(other, trashCanTag))
        {
            Win();
            return;
        }

        if (HasTag(other, knifeTag))
        {
            SpawnJuiceExplosion();

            Lose(
                "YOU GOT CHOPPED",
                _knifeSubtitles[Random.Range(0, _knifeSubtitles.Length)],
                false
            );
        }
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
        restartQueued = false;
        _restartInputReadyTime = Time.time + restartInputDelay;

        RecordWinTime();
        _winMessage = _winSubtitles[Random.Range(0, _winSubtitles.Length)];

        StopTomato();
        InitConfetti();
        PlayWinCheer();
        ChangeToGameOverMusic();
    }

    private void RecordWinTime()
    {
        _lastWinTime = Mathf.Max(0f, Time.time - _startTime);
        _bestWinTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);

        if (_bestWinTime <= 0f || _lastWinTime < _bestWinTime)
        {
            _bestWinTime = _lastWinTime;
            PlayerPrefs.SetFloat(BestTimeKey, _bestWinTime);
            PlayerPrefs.Save();
        }
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int wholeSeconds = Mathf.FloorToInt(seconds % 60f);
        int hundredths = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);

        return string.Format(
            "{0:00}:{1:00}.{2:00}",
            minutes,
            wholeSeconds,
            hundredths
        );
    }

    private void Lose(string title, string message, bool playSquishedSound)
    {
        _loseTitle = title;
        _loseMessage = message;
        gameEnded = true;
        playerWon = false;
        restartQueued = false;
        _restartInputReadyTime = Time.time + restartInputDelay;

        StopTomato();
        InitSplats();

        PlayLoseScream();

        if (playSquishedSound)
        {
            PlaySquishedSound();
        }

        ChangeToGameOverMusic();
    }

    private void StopTomato()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
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
                position = new Vector2(
                    Random.Range(0f, Screen.width),
                    Random.Range(-50f, Screen.height)
                ),
                velocity = new Vector2(
                    Random.Range(-60f, 60f),
                    Random.Range(-200f, -80f)
                ),
                color = colors[Random.Range(0, colors.Length)],
                size = Random.Range(8f, 18f),
                rotation = Random.Range(0f, 360f),
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

    private void ChangeToGameOverMusic()
    {
        if (bgm == null || gameOverBGM == null)
        {
            return;
        }

        bgm.Stop();
        bgm.clip = gameOverBGM;
        bgm.volume = gameOverBGMVolume;
        bgm.Play();
    }

    private void PlayLoseScream()
    {
        if (loseScream == null)
        {
            return;
        }

        SetupLoseScreamSource();

        loseScreamSource.PlayOneShot(
            loseScream,
            loseScreamVolume
        );
    }

    private void PlaySquishedSound()
    {
        if (squishedSound == null)
        {
            return;
        }

        SetupSquishedAudioSource();

        squishedAudioSource.PlayOneShot(
            squishedSound,
            squishedVolume
        );
    }

    private void PlayWinCheer()
    {
        if (winCheer == null)
        {
            return;
        }

        SetupWinCheerSource();

        winCheerSource.PlayOneShot(
            winCheer,
            winCheerVolume
        );
    }

    private void SetupLoseScreamSource()
    {
        if (loseScreamSource == null)
        {
            loseScreamSource = gameObject.AddComponent<AudioSource>();
        }

        loseScreamSource.playOnAwake = false;
        loseScreamSource.loop = false;
        loseScreamSource.spatialBlend = 0f;
        loseScreamSource.volume = loseScreamVolume;
        loseScreamSource.priority = 0;
    }

    private void SetupSquishedAudioSource()
    {
        if (squishedAudioSource == null)
        {
            squishedAudioSource = gameObject.AddComponent<AudioSource>();
        }

        squishedAudioSource.playOnAwake = false;
        squishedAudioSource.loop = false;
        squishedAudioSource.spatialBlend = 0f;
        squishedAudioSource.volume = squishedVolume;
        squishedAudioSource.priority = 0;
    }

    private void SetupWinCheerSource()
    {
        if (winCheerSource == null)
        {
            winCheerSource = gameObject.AddComponent<AudioSource>();
        }

        winCheerSource.playOnAwake = false;
        winCheerSource.loop = false;
        winCheerSource.spatialBlend = 0f;
        winCheerSource.volume = winCheerVolume;
        winCheerSource.priority = 0;
    }

    private void OnGUI()
    {
        if (!gameEnded)
        {
            return;
        }

        if (playerWon)
        {
            DrawWinScreen();
        }
        else
        {
            DrawLoseScreen();
        }

        DrawRestartClickArea();
        HandleEndScreenKeyRestart();
    }

    private void DrawRestartClickArea()
    {
        if (Time.time < _restartInputReadyTime)
        {
            return;
        }

        Color oldColor = GUI.color;
        GUI.color = Color.clear;

        if (GUI.Button(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, GUIStyle.none))
        {
            TryRestartFromEndScreen();
        }

        GUI.color = oldColor;
    }

    private void HandleEndScreenKeyRestart()
    {
        Event currentEvent = Event.current;

        if (currentEvent == null || currentEvent.type != EventType.KeyDown)
        {
            return;
        }

        if (TryRestartFromEndScreen())
        {
            currentEvent.Use();
        }
    }

    private void DrawLoseScreen()
    {
        if (gameFont != null)
        {
            GUI.skin.font = gameFont;
        }

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
            fontSize = 120,
            fontStyle = FontStyle.Bold
        };

        titleStyle.normal.textColor = new Color(1f, 0.9f, 0.9f);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 60
        };

        subStyle.normal.textColor = new Color(1f, 0.7f, 0.7f);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 60
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
            "Press any key or click to try again",
            hintStyle
        );
    }

    private void DrawWinScreen()
    {
        if (gameFont != null)
        {
            GUI.skin.font = gameFont;
        }

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
            fontSize = 120,
            fontStyle = FontStyle.Bold
        };

        titleStyle.normal.textColor = Color.white;

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 32
        };

        subStyle.normal.textColor = new Color(0.9f, 1f, 0.9f);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24
        };

        hintStyle.normal.textColor = new Color(0.7f, 1f, 0.7f);

        GUI.Label(
            new Rect(0, Screen.height * 0.25f, Screen.width, 140f),
            "SAFE! 🍅",
            titleStyle
        );

        GUI.Label(
            new Rect(0, Screen.height * 0.52f, Screen.width, 50f),
            _winMessage,
            subStyle
        );

        GUI.Label(
            new Rect(0, Screen.height * 0.58f, Screen.width, 50f),
            "Time: " + FormatTime(_lastWinTime) + "    Best: " + FormatTime(_bestWinTime),
            subStyle
        );

        GUI.Label(
            new Rect(0, Screen.height * 0.68f, Screen.width, 40f),
            "Press any key or click to play again",
            hintStyle
        );
    }
}
