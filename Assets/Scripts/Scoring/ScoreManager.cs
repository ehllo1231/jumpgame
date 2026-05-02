using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreManager : MonoBehaviour
{
    private const string BestScoreKey = "Merado.BestScore";

    [SerializeField] private PlayerController player;
    [SerializeField] private string playerTag = "Player";

    public static ScoreManager Instance { get; private set; }

    public int CurrentScore { get; private set; }
    public int BestScore { get; private set; }

    public event Action<int> ScoreChanged;
    public event Action<int> BestScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    private void Start()
    {
        EnsurePlayer();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged += HandleGameStateChanged;
        }

        ScoreChanged?.Invoke(CurrentScore);
        BestScoreChanged?.Invoke(BestScore);
    }

    private void Update()
    {
        if (!GameManager.IsGamePlaying)
        {
            return;
        }

        EnsurePlayer();
        UpdateScoreFromCurrentGround();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged -= HandleGameStateChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void UpdateScoreFromCurrentGround()
    {
        if (player == null || !player.IsGrounded || player.CurrentGround == null)
        {
            return;
        }

        var platformScore = player.CurrentGround.GetComponentInParent<PlatformScore>();
        var newScore = platformScore != null ? platformScore.Score : 0;

        if (newScore <= CurrentScore)
        {
            return;
        }

        CurrentScore = newScore;
        ScoreChanged?.Invoke(CurrentScore);
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            SaveBestScoreIfNeeded();
        }
    }

    private void SaveBestScoreIfNeeded()
    {
        if (CurrentScore <= BestScore)
        {
            return;
        }

        BestScore = CurrentScore;
        PlayerPrefs.SetInt(BestScoreKey, BestScore);
        PlayerPrefs.Save();
        BestScoreChanged?.Invoke(BestScore);
    }

    private void EnsurePlayer()
    {
        if (player != null)
        {
            return;
        }

        try
        {
            var playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null && playerObject.TryGetComponent<PlayerController>(out player))
            {
                return;
            }
        }
        catch (UnityException)
        {
            player = null;
        }

        player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
    }
}
