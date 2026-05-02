using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class LavaController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GameTuningConfig tuningConfig;

    [SerializeField] private float riseSpeed = 0.7f;
    [SerializeField] private string playerTag = "Player";

    public void SetTuningConfig(GameTuningConfig config)
    {
        tuningConfig = config;
        ApplyConfig();
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        ApplyConfig();
    }

    private void OnValidate()
    {
        ApplyConfig();
    }

    private void Update()
    {
        if (!GameManager.IsGamePlaying)
        {
            return;
        }

        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerGameOver(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTriggerGameOver(other);
    }

    private void TryTriggerGameOver(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Debug.Log("Game Over");
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        return HasTag(other, playerTag) || other.GetComponentInParent<PlayerController>() != null;
    }

    private static bool HasTag(Component target, string tagName)
    {
        try
        {
            return target.CompareTag(tagName);
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private void ApplyConfig()
    {
        if (tuningConfig == null)
        {
            return;
        }

        riseSpeed = tuningConfig.LavaRiseSpeed;
    }
}
