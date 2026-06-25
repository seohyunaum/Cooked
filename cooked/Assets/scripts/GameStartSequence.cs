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
        StoryComic,
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
    [SerializeField, Min(0.1f)] private float panDuration = 6f;
    [SerializeField] private float panHeight = 7f;
    [SerializeField] private float panDistance = 9f;
    [SerializeField] private float overviewHeightMultiplier = 2.8f;
    [SerializeField] private float overviewDistanceMultiplier = 0.55f;
    [SerializeField] private float trashCanStopDistanceMultiplier = 1.8f;
    [SerializeField] private float minimumCameraClearanceHeight = 40f;
    [SerializeField] private float arcHeight = 12f;
    [SerializeField, Range(0.55f, 0.98f)] private float fallStartProgress = 0.98f;
    [SerializeField, Min(0f)] private float overviewDuration = 4f;
    [SerializeField] private float overviewRotationDegrees = 80f;
    [SerializeField] private Vector3 startFocusOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 goalFocusOffset = new Vector3(0f, 3.2f, 0f);

    [Header("Instructions")]
    [SerializeField] private string titleText = "COOKED";
    [SerializeField] private string startButtonText = "PLAY";
    [SerializeField] private string exitButtonText = "EXIT";
    [SerializeField] private string objectiveText = "Run to the trashcan to flee the human!";
    [SerializeField] private string knifeText = "If you're too slow a knife will chop you.";
    [SerializeField] private string spongeText = "Is that a sponge in the sink...?";
    [SerializeField] private string controlsText = "Move: WASD / arrow keys\nCamera: right click + drag mouse";
    [SerializeField, Min(0f)] private float instructionSeconds = 0f;

    [Header("Story Slideshow")]
    [SerializeField] private Texture2D[] storyPanels;
    [SerializeField] private string[] storyResourcePaths =
    {
        "StoryScenes/scene1",
        "StoryScenes/scene2",
        "StoryScenes/scene3"
    };
    [SerializeField, Min(0.1f)] private float storySecondsPerPanel = 7f;
    [SerializeField, Min(0f)] private float storyFadeSeconds = 0.75f;
    [SerializeField] private string storyResourceFolder = "StoryScenes";
    [SerializeField] private string storySkipText = "Press Space to skip";

    [Header("Music")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip storyBGM;
    [SerializeField] private AudioClip normalBGM;
    [SerializeField, Range(0f, 1f)] private float storyBGMVolume = 0.7f;
    [SerializeField] private float normalBGMVolume = -1f;

    [Header("Timer")]
    [SerializeField] private float timeLimitSeconds = 120f;

    [Header("Sponge Hint")]
    [SerializeField] private string spongeHintText = "Use the sponge!";
    [SerializeField] private float spongeHintDistance = 5f;

    private readonly List<Behaviour> disabledCameraBehaviours = new List<Behaviour>();
    private static GameStartSequence instance;
    private static bool storyPlayedThisSession;
    private GameIntroState state = GameIntroState.StartScreen;
    private Transform trashCanTarget;
    private Transform spongeTarget;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private bool savedCameraPose;
    private bool playerWasKinematic;
    private bool gameplayWasEnabled;
    private bool storyClockStarted;
    private float normalBGMSceneVolume = -1f;
    private float instructionStartTime;
    private float runStartTime;
    private float storyStartTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        instance = null;
        storyPlayedThisSession = false;
    }

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
        LoadStoryPanelsIfNeeded();

        if (!storyPlayedThisSession && storyPanels != null && storyPanels.Length > 0)
        {
            storyClockStarted = false;
            storyStartTime = 0f;
            state = GameIntroState.StoryComic;
            PlayStoryBGM();
        }
        else
        {
            state = GameIntroState.StartScreen;
            PlayNormalBGM();
        }
    }

    private void ClearSceneReferences()
    {
        playerController = null;
        playerGameplay = null;
        playerBody = null;
        introCamera = null;
        bgmSource = null;
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

        if (bgmSource == null)
        {
            bgmSource = FindNamedAudioSource("BGM");
        }

        if (normalBGM == null && bgmSource != null)
        {
            normalBGM = bgmSource.clip;
        }

        if (bgmSource != null && normalBGMSceneVolume < 0f)
        {
            normalBGMSceneVolume = bgmSource.volume;
        }

        if (storyBGM == null)
        {
            AudioSource storySource = FindNamedAudioSource("StoryBGM");
            if (storySource != null)
            {
                storyBGM = storySource.clip;
                storySource.Stop();
            }
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

    private AudioSource FindNamedAudioSource(string objectName)
    {
        GameObject sourceObject = GameObject.Find(objectName);
        return sourceObject != null ? sourceObject.GetComponent<AudioSource>() : null;
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

        KnifeFollow knife = FindObjectOfType<KnifeFollow>();
        if (knife != null)
        {
            knife.ResetSpawnDelay();
        }

        instructionStartTime = Time.time;
        runStartTime = Time.time;
        state = GameIntroState.Playing;
    }

    private void Update()
    {
        if (state == GameIntroState.StoryComic)
        {
            UpdateStoryComic();
            return;
        }

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
            case GameIntroState.StoryComic:
                DrawStoryComic();
                break;
            case GameIntroState.StartScreen:
                DrawStartScreen();
                break;
            case GameIntroState.Panning:
                DrawPanningMessage("Escape to the trash can...");
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

    private void LoadStoryPanelsIfNeeded()
    {
        if (storyResourcePaths != null && storyResourcePaths.Length > 0)
        {
            List<Texture2D> loadedPanels = new List<Texture2D>();

            for (int i = 0; i < storyResourcePaths.Length; i++)
            {
                Texture2D panel = Resources.Load<Texture2D>(storyResourcePaths[i]);
                if (panel != null)
                {
                    loadedPanels.Add(panel);
                }
            }

            if (loadedPanels.Count > 0)
            {
                storyPanels = loadedPanels.ToArray();
                return;
            }
        }

        if (storyPanels != null && storyPanels.Length > 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(storyResourceFolder))
        {
            return;
        }

        storyPanels = Resources.LoadAll<Texture2D>(storyResourceFolder);
        System.Array.Sort(storyPanels, (a, b) => string.CompareOrdinal(a.name, b.name));
    }

    private void UpdateStoryComic()
    {
        if (!storyClockStarted)
        {
            return;
        }

        if (
            Input.GetKeyDown(KeyCode.Space) ||
            Time.unscaledTime - storyStartTime >= GetStoryDuration()
        )
        {
            FinishStoryComic();
        }
    }

    private void FinishStoryComic()
    {
        storyPlayedThisSession = true;
        state = GameIntroState.StartScreen;
        PlayNormalBGM();
    }

    private void PlayStoryBGM()
    {
        PlayBGM(storyBGM, storyBGMVolume);
    }

    private void PlayNormalBGM()
    {
        float volume =
            normalBGMVolume >= 0f
                ? normalBGMVolume
                : normalBGMSceneVolume;

        PlayBGM(normalBGM, volume);
    }

    private void PlayBGM(AudioClip clip, float volume)
    {
        if (bgmSource == null || clip == null)
        {
            return;
        }

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = Mathf.Clamp01(volume);

        if (bgmSource.clip != clip)
        {
            bgmSource.Stop();
            bgmSource.clip = clip;
        }

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    private float GetStoryDuration()
    {
        return GetStoryPanelDuration() * storyPanels.Length;
    }

    private void DrawStoryComic()
    {
        DrawDimBackground(1f);

        if (!storyClockStarted)
        {
            storyStartTime = Time.unscaledTime;
            storyClockStarted = true;
        }

        float elapsed = Time.unscaledTime - storyStartTime;
        int panelIndex = Mathf.Clamp(
            Mathf.FloorToInt(elapsed / GetStoryPanelDuration()),
            0,
            storyPanels.Length - 1
        );

        DrawStorySlides(elapsed, panelIndex);
        DrawStorySkipPrompt();
    }

    private float GetStoryPanelDuration()
    {
        return Mathf.Max(0.1f, storySecondsPerPanel);
    }

    private void DrawStorySlides(float elapsed, int panelIndex)
    {
        float panelElapsed = elapsed - panelIndex * GetStoryPanelDuration();
        float transitionSeconds = GetStoryTransitionSeconds();

        if (panelIndex > 0 && transitionSeconds > 0f && panelElapsed < transitionSeconds)
        {
            float transitionProgress = Mathf.Clamp01(panelElapsed / transitionSeconds);
            DrawStorySlide(storyPanels[panelIndex - 1], 1f - transitionProgress);
            DrawStorySlide(storyPanels[panelIndex], transitionProgress);
            return;
        }

        DrawStorySlide(storyPanels[panelIndex], 1f);
    }

    private float GetStoryTransitionSeconds()
    {
        return Mathf.Min(
            storyFadeSeconds,
            GetStoryPanelDuration() * 0.45f
        );
    }

    private void DrawStorySlide(Texture2D panel, float alpha)
    {
        if (panel == null)
        {
            return;
        }

        Rect safeRect = GetSafeRect(72f);
        float padding = Mathf.Min(safeRect.width, safeRect.height) * 0.025f;
        Rect panelRect = GetFittedStoryPanelRect(
            panel,
            safeRect.x + padding,
            safeRect.y + padding,
            safeRect.width - padding * 2f,
            safeRect.height - padding * 2f
        );

        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(panelRect, panel, ScaleMode.ScaleToFit);
        GUI.color = oldColor;
    }

    private Rect GetFittedStoryPanelRect(
        Texture2D texture,
        float x,
        float y,
        float maxWidth,
        float maxHeight
    )
    {
        float textureAspect = (float)texture.width / texture.height;
        float boundsAspect = maxWidth / maxHeight;

        if (textureAspect > boundsAspect)
        {
            float height = maxWidth / textureAspect;
            return new Rect(x, y + (maxHeight - height) * 0.5f, maxWidth, height);
        }

        float width = maxHeight * textureAspect;
        return new Rect(x + (maxWidth - width) * 0.5f, y, width, maxHeight);
    }

    private void DrawStorySkipPrompt()
    {
        GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = ScaleFont(30, 24, 56),
            fontStyle = FontStyle.Bold
        };

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        Rect safeRect = GetSafeRect(90f);
        float promptHeight = ScaleLength(70f);
        GUI.Label(
            new Rect(
                safeRect.x,
                safeRect.yMax - promptHeight - ScaleLength(54f),
                safeRect.width,
                promptHeight
            ),
            storySkipText,
            promptStyle
        );
        GUI.color = oldColor;
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
            fontSize = ScaleFont(62, 48, 104),
            fontStyle = FontStyle.Bold
        };

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = ScaleFont(28, 24, 52),
            fontStyle = FontStyle.Bold
        };

        Color oldColor = GUI.color;
        GUI.color = Color.white;
        Rect safeRect = GetSafeRect(20f);
        GUI.Label(
            new Rect(safeRect.x, safeRect.y + safeRect.height * 0.18f, safeRect.width, ScaleLength(92f)),
            titleText,
            titleStyle
        );
        DrawStartInstructions(
            new Rect(safeRect.x, safeRect.y + safeRect.height * 0.31f, safeRect.width, ScaleLength(190f))
        );

        float buttonWidth = ScaleLength(170f);
        float buttonHeight = ScaleLength(62f);
        float buttonGap = ScaleLength(24f);
        float buttonY = safeRect.y + safeRect.height * 0.70f;
        float totalButtonWidth = buttonWidth * 2f + buttonGap;
        Rect playButtonRect = new Rect(
            safeRect.center.x - totalButtonWidth * 0.5f,
            buttonY,
            buttonWidth,
            buttonHeight
        );
        Rect exitButtonRect = new Rect(
            playButtonRect.xMax + buttonGap,
            buttonY,
            buttonWidth,
            buttonHeight
        );

        if (GUI.Button(playButtonRect, startButtonText, buttonStyle))
        {
            BeginIntroPan();
        }

        if (GUI.Button(exitButtonRect, exitButtonText, buttonStyle))
        {
            ExitGame();
        }

        DrawBestTimeRecord(
            new Rect(safeRect.x, safeRect.y + safeRect.height * 0.82f, safeRect.width, ScaleLength(50f))
        );

        GUI.color = oldColor;
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
            fontSize = ScaleFont(28, 22, 48),
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
            fontSize = ScaleFont(28, 24, 48),
            fontStyle = FontStyle.Bold
        };

        GUI.Label(
            rect,
            objectiveText + "\n" +  knifeText + "\n" + spongeText + "\n" + controlsText,
            bodyStyle
        );
    }

    private void DrawCenteredMessage(string message)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = ScaleFont(34, 28, 60),
            fontStyle = FontStyle.Bold
        };

        Color oldColor = GUI.color;
        GUI.color = Color.white;
        Rect safeRect = GetSafeRect(20f);
        GUI.Label(
            new Rect(safeRect.x, safeRect.y + safeRect.height * 0.08f, safeRect.width, ScaleLength(58f)),
            message,
            style
        );
        GUI.color = oldColor;
    }

    private void DrawPanningMessage(string message)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = ScaleFont(52, 40, 92),
            fontStyle = FontStyle.Bold
        };

        Color oldColor = GUI.color;
        GUI.color = Color.white;
        Rect safeRect = GetSafeRect(20f);
        GUI.Label(
            new Rect(
                safeRect.x,
                safeRect.y + safeRect.height * 0.08f,
                safeRect.width,
                ScaleLength(100f)
            ),
            message,
            style
        );
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
            fontSize = ScaleFont(26, 22, 44),
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(
                Mathf.RoundToInt(ScaleLength(14f)),
                Mathf.RoundToInt(ScaleLength(14f)),
                Mathf.RoundToInt(ScaleLength(10f)),
                Mathf.RoundToInt(ScaleLength(10f))
            ),
            wordWrap = true
        };

        string text = objectiveText + "\n" +  knifeText + "\n" + spongeText + "\n" + controlsText;
        Vector2 size = style.CalcSize(new GUIContent(text));
        float paddingWidth = ScaleLength(36f);
        Rect safeRect = GetSafeRect(20f);
        Rect rect = new Rect(
            safeRect.x,
            safeRect.y,
            Mathf.Min(safeRect.width, Mathf.Max(ScaleLength(380f), size.x + paddingWidth)),
            ScaleLength(142f)
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
            fontSize = ScaleFont(40, 32, 70),
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(
                Mathf.RoundToInt(ScaleLength(16f)),
                Mathf.RoundToInt(ScaleLength(16f)),
                Mathf.RoundToInt(ScaleLength(8f)),
                Mathf.RoundToInt(ScaleLength(8f))
            )
        };

        int remainingSeconds = Mathf.CeilToInt(GetRemainingTime());
        string text = string.Format(
            "{0:00}:{1:00}",
            remainingSeconds / 60,
            remainingSeconds % 60
        );

        float timerWidth = ScaleLength(220f);
        Rect safeRect = GetSafeRect(20f);
        Rect rect = new Rect(safeRect.xMax - timerWidth, safeRect.y, timerWidth, ScaleLength(76f));
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
            fontSize = ScaleFont(36, 30, 68),
            fontStyle = FontStyle.Bold
        };

        Rect safeRect = GetSafeRect(32f);
        Rect rect = new Rect(
            safeRect.x,
            safeRect.y + safeRect.height * 0.64f,
            safeRect.width,
            ScaleLength(86f)
        );
        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.Label(rect, message, style);
        GUI.color = oldColor;
    }

    private int ScaleFont(float baseSize, int minSize, int maxSize)
    {
        return Mathf.RoundToInt(Mathf.Clamp(baseSize * GetScreenScale(), minSize, maxSize));
    }

    private float ScaleLength(float baseLength)
    {
        return baseLength * GetScreenScale();
    }

    private float GetScreenScale()
    {
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        return Mathf.Clamp(scale * 1.18f, 1f, 2.05f);
    }

    private Rect GetSafeRect(float extraMargin)
    {
        Rect safeRect = Screen.safeArea;
        if (safeRect.width <= 0f || safeRect.height <= 0f)
        {
            safeRect = new Rect(0f, 0f, Screen.width, Screen.height);
        }

        float margin = ScaleLength(extraMargin);
        safeRect.xMin = Mathf.Min(safeRect.xMin + margin, Screen.width * 0.5f);
        safeRect.xMax = Mathf.Max(safeRect.xMax - margin, Screen.width * 0.5f);
        safeRect.yMin = Mathf.Min(safeRect.yMin + margin, Screen.height * 0.5f);
        safeRect.yMax = Mathf.Max(safeRect.yMax - margin, Screen.height * 0.5f);
        return safeRect;
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
