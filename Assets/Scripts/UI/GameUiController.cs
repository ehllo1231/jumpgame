using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class GameUiController : MonoBehaviour
{
    [Header("Score HUD")]
    [SerializeField] private Color scoreTextColor = Color.white;
    [SerializeField] private Color scoreBackgroundColor = new Color(0f, 0f, 0f, 0.45f);

    [Header("Game Over Panel")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color panelColor = new Color(0.08f, 0.09f, 0.1f, 0.95f);
    [SerializeField] private Color buttonColor = new Color(1f, 0.45f, 0.08f, 1f);

    private const float UiPlaneDistance = 10f;
    private const int OverlaySortingOrder = 900;
    private const int PanelSortingOrder = 910;
    private const int TextSortingOrder = 920;

    private readonly Vector2 scoreBackgroundSize = new Vector2(2.55f, 0.58f);
    private readonly Vector2 buttonSize = new Vector2(2.8f, 0.7f);

    private ScoreManager scoreManager;
    private GameManager boundGameManager;
    private Camera targetCamera;
    private Transform uiRoot;
    private GameObject gameOverRoot;
    private SpriteRenderer scoreBackground;
    private SpriteRenderer overlayRenderer;
    private SpriteRenderer panelRenderer;
    private SpriteRenderer buttonRenderer;
    private TextMesh scoreText;
    private TextMesh titleText;
    private TextMesh finalScoreText;
    private TextMesh bestScoreText;
    private TextMesh restartText;
    private Rect restartButtonLocalRect;
    private bool isGameOverVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Object.FindAnyObjectByType<GameUiController>() == null)
        {
            new GameObject("GameUI").AddComponent<GameUiController>();
        }
    }

    private void Start()
    {
        BindScoreManager();
        EnsureGameManagerBinding();
        RefreshGameOverVisibility();
        EnsureWorldUi();
    }

    private void Update()
    {
        BindScoreManager();
        EnsureGameManagerBinding();
        RefreshGameOverVisibility();

        if (isGameOverVisible && WasRestartPressed())
        {
            RestartGame();
        }
    }

    private void LateUpdate()
    {
        EnsureWorldUi();
        UpdateLayout();
        UpdateText();
        UpdateVisibility();
    }

    private void OnDestroy()
    {
        if (boundGameManager != null)
        {
            boundGameManager.StateChanged -= HandleGameStateChanged;
        }
    }

    private void BindScoreManager()
    {
        if (scoreManager != null)
        {
            return;
        }

        scoreManager = ScoreManager.Instance;

        if (scoreManager == null)
        {
            scoreManager = Object.FindAnyObjectByType<ScoreManager>();
        }
    }

    private void EnsureGameManagerBinding()
    {
        if (boundGameManager == GameManager.Instance)
        {
            return;
        }

        if (boundGameManager != null)
        {
            boundGameManager.StateChanged -= HandleGameStateChanged;
        }

        boundGameManager = GameManager.Instance;

        if (boundGameManager != null)
        {
            boundGameManager.StateChanged += HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        isGameOverVisible = state == GameState.GameOver;
    }

    private void RefreshGameOverVisibility()
    {
        isGameOverVisible = boundGameManager != null && boundGameManager.State == GameState.GameOver;
    }

    private void EnsureWorldUi()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        }

        if (targetCamera == null)
        {
            return;
        }

        if (uiRoot != null)
        {
            return;
        }

        uiRoot = new GameObject("WorldGameUI").transform;
        uiRoot.SetParent(targetCamera.transform, false);
        uiRoot.localPosition = Vector3.zero;
        uiRoot.localRotation = Quaternion.identity;
        uiRoot.localScale = Vector3.one;

        scoreBackground = CreateRectangle("ScoreBackground", uiRoot, scoreBackgroundColor, PanelSortingOrder);
        scoreText = CreateText("ScoreText", uiRoot, scoreTextColor, TextAnchor.UpperLeft, TextAlignment.Left, TextSortingOrder);

        gameOverRoot = new GameObject("GameOverRoot");
        gameOverRoot.transform.SetParent(uiRoot, false);

        overlayRenderer = CreateRectangle("GameOverOverlay", gameOverRoot.transform, overlayColor, OverlaySortingOrder);
        panelRenderer = CreateRectangle("GameOverPanel", gameOverRoot.transform, panelColor, PanelSortingOrder);
        buttonRenderer = CreateRectangle("RestartButton", gameOverRoot.transform, buttonColor, PanelSortingOrder + 1);

        titleText = CreateText("TitleText", gameOverRoot.transform, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, TextSortingOrder);
        finalScoreText = CreateText("FinalScoreText", gameOverRoot.transform, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, TextSortingOrder);
        bestScoreText = CreateText("BestScoreText", gameOverRoot.transform, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, TextSortingOrder);
        restartText = CreateText("RestartText", gameOverRoot.transform, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, TextSortingOrder + 1);
        gameOverRoot.SetActive(isGameOverVisible);
    }

    private void UpdateLayout()
    {
        if (targetCamera == null || uiRoot == null)
        {
            return;
        }

        var height = targetCamera.orthographicSize * 2f;
        var width = height * targetCamera.aspect;
        var left = -width * 0.5f;
        var top = height * 0.5f;
        var scale = GetWorldUiScale();
        var margin = 0.32f * scale;
        var scoreSize = scoreBackgroundSize * scale;

        SetRectangle(scoreBackground, new Vector2(scoreSize.x, scoreSize.y), new Vector3(left + margin + scoreSize.x * 0.5f, top - margin - scoreSize.y * 0.5f, UiPlaneDistance));
        SetText(scoreText, new Vector3(left + margin + 0.18f * scale, top - margin - 0.1f * scale, UiPlaneDistance), 0.055f * scale, 64);

        if (gameOverRoot == null)
        {
            return;
        }

        gameOverRoot.transform.localPosition = Vector3.zero;

        SetRectangle(overlayRenderer, new Vector2(width, height), new Vector3(0f, 0f, UiPlaneDistance));

        var panelSize = new Vector2(Mathf.Min(width - 0.7f * scale, 5.8f * scale), Mathf.Min(height - 0.7f * scale, 4.35f * scale));
        SetRectangle(panelRenderer, panelSize, new Vector3(0f, 0f, UiPlaneDistance));

        SetText(titleText, new Vector3(0f, 1.35f * scale, UiPlaneDistance), 0.07f * scale, 72);
        SetText(finalScoreText, new Vector3(0f, 0.45f * scale, UiPlaneDistance), 0.052f * scale, 64);
        SetText(bestScoreText, new Vector3(0f, -0.15f * scale, UiPlaneDistance), 0.047f * scale, 60);

        var scaledButtonSize = buttonSize * scale;
        var buttonCenter = new Vector3(0f, -1.35f * scale, UiPlaneDistance);
        SetRectangle(buttonRenderer, scaledButtonSize, buttonCenter);
        SetText(restartText, buttonCenter + Vector3.up * 0.03f * scale, 0.05f * scale, 60);
        restartButtonLocalRect = new Rect(
            buttonCenter.x - scaledButtonSize.x * 0.5f,
            buttonCenter.y - scaledButtonSize.y * 0.5f,
            scaledButtonSize.x,
            scaledButtonSize.y);
    }

    private void UpdateText()
    {
        var currentScore = GetCurrentScore();
        var bestScore = GetBestScore();

        if (scoreText != null)
        {
            scoreText.text = $"Score {currentScore}";
        }

        if (titleText != null)
        {
            titleText.text = "GAME OVER";
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Score: {currentScore}";
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = $"Best: {bestScore}";
        }

        if (restartText != null)
        {
            restartText.text = "Restart";
        }
    }

    private void UpdateVisibility()
    {
        if (gameOverRoot != null && gameOverRoot.activeSelf != isGameOverVisible)
        {
            gameOverRoot.SetActive(isGameOverVisible);
        }
    }

    private int GetCurrentScore()
    {
        return scoreManager != null ? scoreManager.CurrentScore : 0;
    }

    private int GetBestScore()
    {
        return scoreManager != null ? scoreManager.BestScore : 0;
    }

    private bool WasRestartPressed()
    {
        if (targetCamera == null)
        {
            return false;
        }

        if (!TryGetPointerDownScreenPosition(out var screenPosition))
        {
            return false;
        }

        var worldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, UiPlaneDistance));
        var localPosition = targetCamera.transform.InverseTransformPoint(worldPosition);
        return restartButtonLocalRect.Contains(new Vector2(localPosition.x, localPosition.y));
    }

    private void RestartGame()
    {
        MvpRuntimeBootstrap.RestartRuntimeScene();
        scoreManager = null;
        boundGameManager = null;
        isGameOverVisible = false;
        EnsureWorldUi();
    }

    private static bool TryGetPointerDownScreenPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }
        }

        screenPosition = default;
        return false;
#else
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        for (var i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        screenPosition = default;
        return false;
#endif
    }

    private static SpriteRenderer CreateRectangle(string name, Transform parent, Color color, int sortingOrder)
    {
        var target = new GameObject(name);
        target.transform.SetParent(parent, false);

        var spriteRenderer = target.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = RuntimeSpriteUtility.SquareSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;

        return spriteRenderer;
    }

    private static void SetRectangle(SpriteRenderer spriteRenderer, Vector2 size, Vector3 localPosition)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.transform.localPosition = localPosition;
        spriteRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private static TextMesh CreateText(string name, Transform parent, Color color, TextAnchor anchor, TextAlignment alignment, int sortingOrder)
    {
        var target = new GameObject(name);
        target.transform.SetParent(parent, false);

        var textMesh = target.AddComponent<TextMesh>();
        textMesh.anchor = anchor;
        textMesh.alignment = alignment;
        textMesh.color = color;

        var renderer = target.GetComponent<MeshRenderer>();
        renderer.sortingOrder = sortingOrder;

        return textMesh;
    }

    private static void SetText(TextMesh textMesh, Vector3 localPosition, float characterSize, int fontSize)
    {
        if (textMesh == null)
        {
            return;
        }

        textMesh.transform.localPosition = localPosition;
        textMesh.transform.localRotation = Quaternion.identity;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = fontSize;
    }

    private float GetWorldUiScale()
    {
        return targetCamera != null ? Mathf.Clamp(targetCamera.orthographicSize / 5f, 0.75f, 1.5f) : 1f;
    }
}
