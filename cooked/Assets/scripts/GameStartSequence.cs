using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStartSequence : MonoBehaviour
{
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
    [SerializeField] private float overviewHeightMultiplier = 1.8f;
    [SerializeField] private float overviewDistanceMultiplier = 0.55f;
    [SerializeField] private float trashCanStopDistanceMultiplier = 1.8f;
    [SerializeField] private Vector3 startFocusOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 goalFocusOffset = new Vector3(0f, 3.2f, 0f);

    [Header("Instructions")]
    [SerializeField] private string titleText = "COOKED";
    [SerializeField] private string startButtonText = "PLAY";
    [SerializeField] private string objectiveText = "Reach the trash can without getting chopped!";
    [SerializeField] private string spongeText = "Use the sponge to jump toward freedom.";
    [SerializeField] private string controlsText = "Move: WASD / arrow keys";
    [SerializeField, Min(0f)] private float instructionSeconds = 14f;

    private readonly List<Behaviour> disabledCameraBehaviours = new List<Behaviour>();
    private GameIntroState state = GameIntroState.StartScreen;
    private Transform trashCanTarget;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private bool savedCameraPose;
    private bool playerWasKinematic;
    private bool gameplayWasEnabled;
    private float instructionStartTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfNeeded()
    {
        if (FindObjectOfType<GameStartSequence>() != null)
        {
            return;
        }

        new GameObject("Game Start Sequence").AddComponent<GameStartSequence>();
    }

    private void Awake()
    {
        FindSceneReferences();
        FreezePlayer();
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
        Vector3 endFocus = Vector3.Lerp(playerFocus, goalFocus, 0.72f);

        Vector3 startPosition =
            overviewFocus -
            pathDirection * (routeLength * overviewDistanceMultiplier) +
            side * (routeLength * 0.25f) +
            Vector3.up * (panHeight * overviewHeightMultiplier);

        Vector3 endPosition =
            goalFocus -
            pathDirection * (panDistance * trashCanStopDistanceMultiplier) +
            side * 5f +
            Vector3.up * (panHeight * 1.25f);

        float elapsed = 0f;
        while (elapsed < panDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / panDuration);
            Vector3 focus = Vector3.Lerp(overviewFocus, endFocus, t);

            introCamera.transform.position = Vector3.Lerp(startPosition, endPosition, t);
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
        state = GameIntroState.Playing;
    }

    private void OnGUI()
    {
        switch (state)
        {
            case GameIntroState.StartScreen:
                DrawStartScreen();
                break;
            case GameIntroState.Panning:
                DrawCenteredMessage("Find the trash can...");
                break;
            case GameIntroState.Playing:
                DrawInstructions();
                break;
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

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24
        };

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold
        };

        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 70f), titleText, titleStyle);
        GUI.Label(
            new Rect(0f, Screen.height * 0.4f, Screen.width, 110f),
            objectiveText + "\n" + spongeText + "\n" + controlsText,
            bodyStyle
        );

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

        GUI.color = oldColor;
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
            fontSize = 22,
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
