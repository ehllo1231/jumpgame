using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private bool startPlayingOnAwake = true;

    public GameState State { get; private set; } = GameState.Playing;
    public bool IsPlaying => State == GameState.Playing;

    public event Action<GameState> StateChanged;

    public static bool IsGamePlaying => Instance == null || Instance.IsPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        State = startPlayingOnAwake ? GameState.Playing : GameState.GameOver;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartGame()
    {
        SetState(GameState.Playing);
    }

    public void GameOver()
    {
        if (State == GameState.GameOver)
        {
            return;
        }

        SetState(GameState.GameOver);
        Debug.Log("Game Over");
    }

    private void SetState(GameState newState)
    {
        if (State == newState)
        {
            return;
        }

        State = newState;
        StateChanged?.Invoke(State);
    }
}
