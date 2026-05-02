using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlatformScore : MonoBehaviour
{
    [SerializeField] private int score;

    public int Score => score;

    public void SetScore(int newScore)
    {
        score = Mathf.Max(0, newScore);
    }
}
