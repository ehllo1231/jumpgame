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
    [SerializeField] private float maxDistanceBelowPlayer = 8f;
    [SerializeField] private Vector2 size = new Vector2(200f, 3f);
    [SerializeField] private bool followCameraX = true;
    [SerializeField] private string playerTag = "Player";

    private Collider2D lavaCollider;
    private BoxCollider2D boxCollider;
    private Transform playerTransform;
    private Collider2D playerCollider;
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
        SanitizeConfigValues();
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
        position.y = ClampToMaxPlayerDistance(position.y);

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
        maxDistanceBelowPlayer = tuningConfig.LavaMaxDistanceBelowPlayer;
        size = tuningConfig.LavaSize;
        followCameraX = tuningConfig.LavaFollowCameraX;
    }

    private void ResetSpeed()
    {
        SanitizeConfigValues();
        currentRiseSpeed = Mathf.Min(initialRiseSpeed, maxRiseSpeed);
    }

    private void SanitizeConfigValues()
    {
        maxRiseSpeed = Mathf.Max(initialRiseSpeed, maxRiseSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        maxDistanceBelowPlayer = Mathf.Max(0f, maxDistanceBelowPlayer);
    }

    private float ClampToMaxPlayerDistance(float proposedY)
    {
        if (maxDistanceBelowPlayer <= 0f)
        {
            return proposedY;
        }

        CachePlayer();
        if (playerTransform == null)
        {
            return proposedY;
        }

        var minimumLavaTopY = GetPlayerBottomY() - maxDistanceBelowPlayer;
        var proposedLavaTopY = GetProjectedLavaTopY(proposedY);
        if (proposedLavaTopY >= minimumLavaTopY)
        {
            return proposedY;
        }

        return proposedY + minimumLavaTopY - proposedLavaTopY;
    }

    private void CachePlayer()
    {
        if (playerTransform != null)
        {
            return;
        }

        var playerObject = FindPlayerObjectByTag();
        if (playerObject == null)
        {
            var playerController = Object.FindAnyObjectByType<PlayerController>();
            playerObject = playerController != null ? playerController.gameObject : null;
        }

        if (playerObject == null)
        {
            return;
        }

        playerTransform = playerObject.transform;
        playerCollider = playerObject.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            playerCollider = playerObject.GetComponentInChildren<Collider2D>();
        }
    }

    private GameObject FindPlayerObjectByTag()
    {
        try
        {
            return GameObject.FindGameObjectWithTag(playerTag);
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private float GetPlayerBottomY()
    {
        if (playerCollider != null && playerCollider.enabled)
        {
            return playerCollider.bounds.min.y;
        }

        return playerTransform.position.y;
    }

    private float GetProjectedLavaTopY(float proposedY)
    {
        if (lavaCollider != null)
        {
            return lavaCollider.bounds.max.y + proposedY - transform.position.y;
        }

        return proposedY + Mathf.Max(0.1f, size.y) * 0.5f;
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
