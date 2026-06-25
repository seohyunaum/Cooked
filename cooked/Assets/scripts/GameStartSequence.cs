using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class GameStartSequence : MonoBehaviour
{
    private const string BestTimeKey = "CookedBestWinTime";

    private enum GameIntroState
    {
        StartScreen,
        Panning,
        Playing
    }

    [Header("Player")]
    [SerializeField] private tomatoRoll playerController;
    [SerializeField] private tomatoGameplay playerGameplay;
    [SerializeField] private Rigidbody playerBody;

    [Header("Camera Pan")]
    [SerializeField] private Camera introCamera;
    [SerializeField] private string trashCanTag = "trashcan";
    [SerializeField] private string fallbackTrashCanTag = "TrashCan";
    [SerializeField, Min(0.1f)] private float panDuration = 4f;
    [SerializeField] private float panHeight = 7f;
    [SerializeField] private float panDistance = 9f;
    [SerializeField] private float overviewHeightMultiplier = 2.8f;
    [SerializeField] private float overviewDistanceMultiplier = 0.55f;
    [SerializeField] private float trashCanStopDistanceMultiplier = 1.8f;
    [SerializeField] private float minimumCameraClearanceHeight = 40f;
    [SerializeField] private float arcHeight = 12f;
    [SerializeField, Range(0.55f, 0.98f)] private float fallStartProgress = 0.98f;
    [SerializeField, Min(0f)] private float overviewDuration = 2f;
    [SerializeField] private float overviewRotationDegrees = 80f;
    [SerializeField] private Vector3 startFocusOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 goalFocusOffset = new Vector3(0f, 3.2f, 0f);

    [Header("Instructions")]
    [SerializeField] private string titleText = "COOKED";
    [SerializeField] private string startButtonText = "PLAY";
    [SerializeField] private string objectiveText = "Run to the trashcan to flee the human!";
    [SerializeField] private string spongeText = "Is that a sponge in the sink...?";
    [SerializeField] private string controlsText = "Move: WASD / arrow keys";
    [SerializeField, Min(0f)] private float instructionSeconds = 0f;

    [Header("Timer")]
    [SerializeField] private float timeLimitSeconds = 120f;

    [Header("Sponge Hint")]
    [SerializeField] private string spongeHintText = "Use the sponge!";
    [SerializeField] private float spongeHintDistance = 5f;

    private readonly List<Behaviour> disabledCameraBehaviours = new List<Behaviour>();
    private static GameStartSequence instance;
    private GameIntroState state = GameIntroState.StartScreen;
    private Transform trashCanTarget;
    private Transform spongeTarget;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private bool savedCameraPose;
    private bool playerWasKinematic;
    private bool gameplayWasEnabled;
    private float instructionStartTime;
    private float runStartTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfNeeded()
    {
        if (instance != null || FindObjectOfType<GameStartSequence>() != null)
        {
            return;
        }

        new GameObject("Game Start Sequence").AddComponent<GameStartSequence>();
    }

    public static void ReloadToStartScreen()
    {
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                activeScene.path,
                new LoadSceneParameters(LoadSceneMode.Single)
            );
            return;
        }
#endif

        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex, LoadSceneMode.Single);
            return;
        }

        SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ResetToStartScreen();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetToStartScreen();
    }

    private void ResetToStartScreen()
    {
        StopAllCoroutines();
        RestoreCamera();
        ClearSceneReferences();
        FindSceneReferences();
        FreezePlayer();
        instructionStartTime = 0f;
        runStartTime = 0f;
        state = GameIntroState.StartScreen;
    }

    private void ClearSceneReferences()
    {
        playerController = null;
        playerGameplay = null;
        playerBody = null;
        introCamera = null;
        trashCanTarget = null;
        spongeTarget = null;
        disabledCameraBehaviours.Clear();
        savedCameraPose = false;
    }

    private void FindSceneReferences()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<tomatoRoll>();
        }

        if (playerGameplay == null)
        {
            playerGameplay = FindObjectOfType<tomatoGameplay>();
        }

        if (playerBody == null)
        {
            if (playerController != null)
            {
                playerBody = playerController.GetComponent<Rigidbody>();
            }
            else if (playerGameplay != null)
            {
                playerBody = playerGameplay.GetComponent<Rigidbody>();
            }
        }

        if (introCamera == null)
        {
            introCamera = Camera.main;
        }

        trashCanTarget = FindTaggedTransform(trashCanTag);
        if (trashCanTarget == null)
        {
            trashCanTarget = FindTaggedTransform(fallbackTrashCanTag);
        }

        if (spongeTarget == null)
        {
            SpongeBounce sponge = FindObjectOfType<SpongeBounce>();
            if (sponge != null)
            {
                spongeTarget = sponge.transform;
            }
        }
    }

    private Transform FindTaggedTransform(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
        {
            return null;
        }

        try
        {
            GameObject taggedObject = GameObject.FindGameObjectWithTag(tagName);
            return taggedObject != null ? taggedObject.transform : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private void FreezePlayer()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (playerGameplay != null)
        {
            gameplayWasEnabled = playerGameplay.enabled;
            playerGameplay.enabled = false;
        }

        if (playerBody == null)
        {
            return;
        }

        playerWasKinematic = playerBody.isKinematic;
        playerBody.linearVelocity = Vector3.zero;
        playerBody.angularVelocity = Vector3.zero;
        playerBody.isKinematic = true;
    }

    private void BeginIntroPan()
    {
        if (state != GameIntroState.StartScreen)
        {
            return;
        }

        StartCoroutine(PlayIntroPan());
    }

    private IEnumerator PlayIntroPan()
    {
        state = GameIntroState.Panning;
        FindSceneReferences();
        TakeOverCamera();

        if (introCamera == null)
        {
            StartPlaying();
            yield break;
        }

        Vector3 playerFocus = GetPlayerPosition() + startFocusOffset;
        Vector3 goalFocus = GetGoalPosition() + goalFocusOffset;
        Vector3 pathDirection = goalFocus - playerFocus;

        if (pathDirection.sqrMagnitude < 0.01f)
        {
            pathDirection = Vector3.forward;
        }

        pathDirection.y = 0f;
        float routeLength = Mathf.Max(pathDirection.magnitude, panDistance);
        pathDirection.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, pathDirection).normalized;
        Vector3 overviewFocus = Vector3.Lerp(playerFocus, goalFocus, 0.5f);
        Vector3 endFocus = Vector3.Lerp(playerFocus, goalFocus, 1.08f);
        float clearHeight = Mathf.Max(
            minimumCameraClearanceHeight,
            panHeight * overviewHeightMultiplier
        );

        Vector3 startPosition =
            playerFocus -
            pathDirection * (panDistance * 0.8f) +
            side * (routeLength * 0.18f) +
            Vector3.up * clearHeight;

        Vector3 endPosition =
            goalFocus +
            pathDirection * (panDistance * 0.25f) +
            side * 3f +
            Vector3.up * clearHeight;

        Vector3 highTravelPosition =
            Vector3.Lerp(playerFocus, goalFocus, 1.14f) +
            side * (routeLength * 0.12f) +
            Vector3.up * clearHeight;

        if (overviewDuration > 0f)
        {
            yield return PlayKitchenOverview(
                overviewFocus,
                startPosition,
                routeLength,
                clearHeight
            );
        }

        float elapsed = 0f;
        while (elapsed < panDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / panDuration);
            Vector3 focus = Vector3.Lerp(overviewFocus, endFocus, t);

            introCamera.transform.position = GetDelayedFallPoint(
                startPosition,
                highTravelPosition,
                endPosition,
                t,
                fallStartProgress
            );
            introCamera.transform.rotation = Quaternion.LookRotation(
                focus - introCamera.transform.position,
                Vector3.up
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreCamera();
        StartPlaying();
    }

    private IEnumerator PlayKitchenOverview(
        Vector3 focus,
        Vector3 endPosition,
        float routeLength,
        float clearHeight
    )
    {
        Vector3 startOffset = endPosition - focus;
        startOffset.y = 0f;

        if (startOffset.sqrMagnitude < 0.01f)
        {
            startOffset = Vector3.back * Mathf.Max(panDistance, routeLength * 0.5f);
        }

        float radius = Mathf.Max(startOffset.magnitude, routeLength * 0.55f);
        Vector3 flatOffset = startOffset.normalized * radius;
        float elapsed = 0f;

        while (elapsed < overviewDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / overviewDuration);
            Quaternion orbitRotation = Quaternion.AngleAxis(
                Mathf.Lerp(-overviewRotationDegrees * 0.5f, 0f, t),
                Vector3.up
            );

            Vector3 orbitOffset = orbitRotation * flatOffset;
            introCamera.transform.position =
                focus + orbitOffset + Vector3.up * clearHeight;
            introCamera.transform.rotation = Quaternion.LookRotation(
                focus - introCamera.transform.position,
                Vector3.up
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        introCamera.transform.position = endPosition;
        introCamera.transform.rotation = Quaternion.LookRotation(
            focus - introCamera.transform.position,
            Vector3.up
        );
    }

    private Vector3 GetDelayedFallPoint(
        Vector3 start,
        Vector3 highPoint,
        Vector3 end,
        float t,
        float fallStart
    )
    {
        if (t < fallStart)
        {
            float travelT = Mathf.SmoothStep(0f, 1f, t / fallStart);
            return Vector3.Lerp(start, highPoint, travelT);
        }

        float fallT = Mathf.SmoothStep(
            0f,
            1f,
            (t - fallStart) / (1f - fallStart)
        );

        return Vector3.Lerp(highPoint, end, fallT);
    }

    private Vector3 GetPlayerPosition()
    {
        if (playerBody != null)
        {
            return playerBody.position;
        }

        if (playerController != null)
        {
            return playerController.transform.position;
        }

        return Vector3.zero;
    }

    private Vector3 GetGoalPosition()
    {
        return trashCanTarget != null ? trashCanTarget.position : GetPlayerPosition() + Vector3.forward * 10f;
    }

    private void TakeOverCamera()
    {
        if (introCamera == null)
        {
            return;
        }

        savedCameraPosition = introCamera.transform.position;
        savedCameraRotation = introCamera.transform.rotation;
        savedCameraPose = true;
        disabledCameraBehaviours.Clear();

        Behaviour[] behaviours = introCamera.GetComponents<Behaviour>();
        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this || !behaviour.enabled)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            if (typeName.Contains("Brain") || typeName.Contains("CameraFollow") || typeName.Contains("ThirdPersonCamera"))
            {
                behaviour.enabled = false;
                disabledCameraBehaviours.Add(behaviour);
            }
        }
    }

    private void RestoreCamera()
    {
        foreach (Behaviour behaviour in disabledCameraBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        disabledCameraBehaviours.Clear();

        if (introCamera != null && savedCameraPose)
        {
            introCamera.transform.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
        }
    }

    private void StartPlaying()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        if (playerBody != null)
        {
            playerBody.isKinematic = playerWasKinematic;
        }

        if (playerGameplay != null)
        {
            playerGameplay.enabled = gameplayWasEnabled;
            playerGameplay.ResetRunTimer();
        }

        instructionStartTime = Time.time;
        runStartTime = Time.time;
        state = GameIntroState.Playing;
    }

    private void Update()
    {
        if (state != GameIntroState.Playing || playerGameplay == null || playerGameplay.HasEnded)
        {
            return;
        }

        if (timeLimitSeconds > 0f && GetRemainingTime() <= 0f)
        {
            playerGameplay.LoseFromTimer();
        }
    }

    private void OnGUI()
    {
        ApplyGameFont();

        switch (state)
        {
            case GameIntroState.StartScreen:
                DrawStartScreen();
                break;
            case GameIntroState.Panning:
                DrawCenteredMessage("Escape to the trash can...");
                break;
            case GameIntroState.Playing:
                if (playerGameplay == null || !playerGameplay.HasEnded)
                {
                    DrawTimer();
                    DrawInstructions();
                    DrawSpongeHint();
                }
                break;
        }
    }

    private void ApplyGameFont()
    {
        if (playerGameplay != null && playerGameplay.gameFont != null)
        {
            GUI.skin.font = playerGameplay.gameFont;
        }
    }

    private void DrawStartScreen()
    {
        DrawDimBackground(0.65f);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 56,
            fontStyle = FontStyle.Bold
        };

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold
        };

        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 70f), titleText, titleStyle);
        DrawStartInstructions(new Rect(0f, Screen.height * 0.4f, Screen.width, 120f));

        Rect buttonRect = new Rect(
            Screen.width * 0.5f - 90f,
            Screen.height * 0.58f,
            180f,
            58f
        );

        if (GUI.Button(buttonRect, startButtonText, buttonStyle))
        {
            BeginIntroPan();
        }

        DrawBestTimeRecord(new Rect(0f, Screen.height * 0.68f, Screen.width, 40f));

        GUI.color = oldColor;
    }

    private void DrawBestTimeRecord(Rect rect)
    {
        float bestTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);
        if (bestTime <= 0f)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(rect, "Best time: " + FormatTime(bestTime), style);
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int wholeSeconds = Mathf.FloorToInt(seconds % 60f);
        int hundredths = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, wholeSeconds, hundredths);
    }

    private void DrawStartInstructions(Rect rect)
    {
        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 28,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(
            rect,
            objectiveText + "\n" + spongeText + "\n" + controlsText,
            bodyStyle
        );
    }

    private void DrawCenteredMessage(string message)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 28,
            fontStyle = FontStyle.Bold
        };

        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.Label(new Rect(0f, Screen.height * 0.08f, Screen.width, 50f), message, style);
        GUI.color = oldColor;
    }

    private void DrawInstructions()
    {
        if (instructionSeconds > 0f && Time.time - instructionStartTime > instructionSeconds)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(18, 18, 12, 12),
            wordWrap = true
        };

        string text = objectiveText + "\n" + spongeText + "\n" + controlsText;
        Vector2 size = style.CalcSize(new GUIContent(text));
        Rect rect = new Rect(
            20f,
            20f,
            Mathf.Min(Screen.width - 40f, Mathf.Max(360f, size.x + 48f)),
            116f
        );

        DrawBox(rect, 0.55f);

        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.Label(rect, text, style);
        GUI.color = oldColor;
    }

    private void DrawTimer()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(16, 16, 8, 8)
        };

        int remainingSeconds = Mathf.CeilToInt(GetRemainingTime());
        string text = string.Format(
            "{0:00}:{1:00}",
            remainingSeconds / 60,
            remainingSeconds % 60
        );

        Rect rect = new Rect(Screen.width - 180f, 20f, 160f, 52f);
        DrawBox(rect, 0.55f);

        Color oldColor = GUI.color;
        GUI.color = remainingSeconds <= 10 ? new Color(1f, 0.35f, 0.25f) : Color.white;
        GUI.Label(rect, text, style);
        GUI.color = oldColor;
    }

    private float GetRemainingTime()
    {
        return Mathf.Max(0f, timeLimitSeconds - (Time.time - runStartTime));
    }

    private void DrawSpongeHint()
    {
        if (playerBody == null || spongeTarget == null)
        {
            return;
        }

        float distance = Vector3.Distance(
            playerBody.position,
            spongeTarget.position
        );

        if (distance > spongeHintDistance)
        {
            return;
        }

        DrawBottomMessage(spongeHintText);
    }

    private void DrawBottomMessage(string message)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 28,
            fontStyle = FontStyle.Bold
        };

        Rect rect = new Rect(0f, Screen.height * 0.76f, Screen.width, 56f);
        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.Label(rect, message, style);
        GUI.color = oldColor;
    }

    private void DrawDimBackground(float alpha)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    private void DrawBox(Rect rect, float alpha)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }
}
