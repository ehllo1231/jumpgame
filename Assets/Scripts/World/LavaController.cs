using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class LavaController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GameTuningConfig tuningConfig;

    [SerializeField] private float initialRiseSpeed = 0.7f;
    [SerializeField] private float acceleration = 0.04f;
    [SerializeField] private float maxRiseSpeed = 2.2f;
    [SerializeField] private Vector2 size = new Vector2(200f, 3f);
    [SerializeField] private bool followCameraX = true;
    [SerializeField] private string playerTag = "Player";

    private Collider2D lavaCollider;
    private BoxCollider2D boxCollider;
    private float currentRiseSpeed;

    public void SetTuningConfig(GameTuningConfig config)
    {
        tuningConfig = config;
        CacheComponents();
        ApplyConfig();
        ApplyColliderSettings();
        ApplySize();
        ResetSpeed();
    }

    private void Awake()
    {
        CacheComponents();
        ApplyColliderSettings();
        ApplyConfig();
        ApplySize();
        ResetSpeed();
    }

    private void OnValidate()
    {
        CacheComponents();
        ApplyColliderSettings();
        ApplyConfig();
        ApplySize();
    }

    private void Update()
    {
        if (!GameManager.IsGamePlaying)
        {
            return;
        }

        currentRiseSpeed = Mathf.Min(currentRiseSpeed + acceleration * Time.deltaTime, maxRiseSpeed);

        var position = transform.position;
        position.y += currentRiseSpeed * Time.deltaTime;

        if (followCameraX && Camera.main != null)
        {
            position.x = Camera.main.transform.position.x;
        }

        transform.position = position;
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

        initialRiseSpeed = tuningConfig.LavaRiseSpeed;
        acceleration = tuningConfig.LavaAcceleration;
        maxRiseSpeed = tuningConfig.LavaMaxRiseSpeed;
        size = tuningConfig.LavaSize;
        followCameraX = tuningConfig.LavaFollowCameraX;
    }

    private void ResetSpeed()
    {
        maxRiseSpeed = Mathf.Max(initialRiseSpeed, maxRiseSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        currentRiseSpeed = Mathf.Min(initialRiseSpeed, maxRiseSpeed);
    }

    private void CacheComponents()
    {
        lavaCollider = GetComponent<Collider2D>();
        boxCollider = lavaCollider as BoxCollider2D;
    }

    private void ApplyColliderSettings()
    {
        if (lavaCollider != null)
        {
            lavaCollider.isTrigger = true;
        }
    }

    private void ApplySize()
    {
        var safeSize = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(0.1f, size.y));
        transform.localScale = new Vector3(safeSize.x, safeSize.y, transform.localScale.z == 0f ? 1f : transform.localScale.z);

        if (boxCollider == null)
        {
            return;
        }

        boxCollider.offset = Vector2.zero;
        boxCollider.size = Vector2.one;
    }
}
