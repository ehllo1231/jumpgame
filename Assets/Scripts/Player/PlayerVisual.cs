using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameTuningConfig tuningConfig;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private bool loadWalkFramesFromResources = true;
    [SerializeField] private string walkFramesResourcePath = "Image/char";
    [SerializeField] private float generatedSpritePixelsPerUnit = 550f;
    [SerializeField] private Vector2 visualScale = Vector2.one;
    [SerializeField] private bool fitVisualToColliderHeight = true;
    [SerializeField] private bool alignVisualBottomToCollider = true;
    [SerializeField] private float visualGroundOffset;

    [Header("Animation")]
    [SerializeField] private float walkFramesPerSecond = 8f;
    [SerializeField] private bool spriteFacesRight = true;
    [SerializeField] private float flipVelocityThreshold = 0.05f;
    [SerializeField] private int sortingOrder = 2;

    private int currentFrameIndex;
    private float frameTimer;
    private Sprite fallbackJumpSprite;

    public void SetTuningConfig(GameTuningConfig config)
    {
        tuningConfig = config;
        ApplyConfig();
        ApplyVisualScale();
    }

    private void Awake()
    {
        CacheComponents();
        LoadConfigIfNeeded();
        ApplyConfig();
        LoadDefaultSpritesIfNeeded();
        ApplyRendererDefaults();
        ApplyVisualScale();
        SetFrame(0);
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        ApplyFlip();

        if (playerController != null && !playerController.IsGrounded)
        {
            SetSprite(GetJumpSprite());
            return;
        }

        AnimateWalk();
    }

    private void OnValidate()
    {
        CacheComponents();
        ApplyConfig();
        ApplyRendererDefaults();
        ApplyVisualScale();
    }

    private void CacheComponents()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
        }

        EnsureVisualRoot();
        EnsureVisualRenderer();
        DisablePlaceholderRenderer();
    }

    private void EnsureVisualRoot()
    {
        if (visualRoot != null)
        {
            return;
        }

        var existingVisual = transform.Find("Visual");
        if (existingVisual != null)
        {
            visualRoot = existingVisual;
            return;
        }

        visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;
    }

    private void EnsureVisualRenderer()
    {
        if (spriteRenderer != null && spriteRenderer.transform == visualRoot)
        {
            return;
        }

        spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void DisablePlaceholderRenderer()
    {
        var rootRenderer = GetComponent<SpriteRenderer>();
        if (rootRenderer != null && rootRenderer != spriteRenderer)
        {
            rootRenderer.enabled = false;
        }
    }

    private void LoadDefaultSpritesIfNeeded()
    {
        if (!loadWalkFramesFromResources || HasWalkFrames())
        {
            return;
        }

        var textures = Resources.LoadAll<Texture2D>(walkFramesResourcePath);
        Array.Sort(textures, (left, right) => string.CompareOrdinal(left.name, right.name));

        if (textures.Length == 0)
        {
            Debug.LogWarning($"{nameof(PlayerVisual)} could not find walk frame textures at Resources/{walkFramesResourcePath}.", this);
            return;
        }

        walkFrames = new Sprite[textures.Length];
        for (var i = 0; i < textures.Length; i++)
        {
            walkFrames[i] = CreateSprite(textures[i]);
        }
    }

    private void LoadConfigIfNeeded()
    {
        if (tuningConfig == null)
        {
            tuningConfig = Resources.Load<GameTuningConfig>("GameTuningConfig");
        }
    }

    private void ApplyConfig()
    {
        if (tuningConfig == null)
        {
            return;
        }

        visualScale = tuningConfig.PlayerVisualScale;
    }

    private Sprite CreateSprite(Texture2D texture)
    {
        var rect = new Rect(0f, 0f, texture.width, texture.height);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), Mathf.Max(1f, generatedSpritePixelsPerUnit));
    }

    private void ApplyRendererDefaults()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = sortingOrder;
    }

    private void ApplyVisualScale()
    {
        if (visualRoot == null)
        {
            return;
        }

        var sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        if (fitVisualToColliderHeight && playerCollider != null && sprite != null)
        {
            ApplyColliderRelativeVisualScale(sprite);
            AlignVisualBottom(sprite);
            return;
        }

        visualRoot.localScale = new Vector3(Mathf.Max(0.01f, visualScale.x), Mathf.Max(0.01f, visualScale.y), 1f);
        AlignVisualBottom(sprite);
    }

    private void ApplyColliderRelativeVisualScale(Sprite sprite)
    {
        var parentScale = GetSafeParentScale();
        var colliderLocalHeight = GetColliderLocalBounds().size.y;
        var targetLocalHeight = Mathf.Max(0.01f, colliderLocalHeight * Mathf.Max(0.01f, visualScale.y));
        var spriteLocalHeight = Mathf.Max(0.0001f, sprite.bounds.size.y);
        var uniformWorldScale = targetLocalHeight * parentScale.y / spriteLocalHeight;
        var horizontalScaleMultiplier = Mathf.Max(0.01f, visualScale.x) / Mathf.Max(0.01f, visualScale.y);

        // Compensate for the player's non-uniform physics scale so the sprite keeps its source aspect ratio.
        visualRoot.localScale = new Vector3(uniformWorldScale * horizontalScaleMultiplier / parentScale.x, uniformWorldScale / parentScale.y, 1f);
    }

    private void AlignVisualBottom(Sprite sprite)
    {
        if (!alignVisualBottomToCollider || playerCollider == null || visualRoot == null || sprite == null)
        {
            return;
        }

        var colliderBottomLocal = GetColliderLocalBounds().min.y;
        var localPosition = visualRoot.localPosition;
        localPosition.y = colliderBottomLocal - visualRoot.localScale.y * sprite.bounds.min.y + visualGroundOffset;
        visualRoot.localPosition = localPosition;
    }

    private Vector2 GetSafeParentScale()
    {
        var scale = transform.lossyScale;
        return new Vector2(Mathf.Max(0.0001f, Mathf.Abs(scale.x)), Mathf.Max(0.0001f, Mathf.Abs(scale.y)));
    }

    private Bounds GetColliderLocalBounds()
    {
        // Local collider data avoids visual jitter from Rigidbody interpolation and world bounds during jumps.
        if (playerCollider is BoxCollider2D boxCollider)
        {
            return new Bounds(boxCollider.offset, boxCollider.size);
        }

        if (playerCollider is CapsuleCollider2D capsuleCollider)
        {
            return new Bounds(capsuleCollider.offset, capsuleCollider.size);
        }

        if (playerCollider is CircleCollider2D circleCollider)
        {
            var size = Vector2.one * circleCollider.radius * 2f;
            return new Bounds(circleCollider.offset, size);
        }

        var parentScale = GetSafeParentScale();
        var localSize = new Vector3(playerCollider.bounds.size.x / parentScale.x, playerCollider.bounds.size.y / parentScale.y, 0f);
        var localCenter = transform.InverseTransformPoint(playerCollider.bounds.center);
        return new Bounds(localCenter, localSize);
    }

    private void AnimateWalk()
    {
        if (!HasWalkFrames())
        {
            return;
        }

        var frameDuration = 1f / Mathf.Max(1f, walkFramesPerSecond);
        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrameIndex = (currentFrameIndex + 1) % walkFrames.Length;
        }

        SetFrame(currentFrameIndex);
    }

    private void SetFrame(int frameIndex)
    {
        if (!HasWalkFrames())
        {
            return;
        }

        currentFrameIndex = Mathf.Clamp(frameIndex, 0, walkFrames.Length - 1);
        fallbackJumpSprite = walkFrames[currentFrameIndex];
        SetSprite(walkFrames[currentFrameIndex]);
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null || sprite == null)
        {
            return;
        }

        if (spriteRenderer.sprite == sprite)
        {
            return;
        }

        spriteRenderer.sprite = sprite;
        ApplyVisualScale();
    }

    private Sprite GetJumpSprite()
    {
        if (jumpSprite != null)
        {
            return jumpSprite;
        }

        return fallbackJumpSprite != null ? fallbackJumpSprite : GetFirstWalkFrame();
    }

    private Sprite GetFirstWalkFrame()
    {
        return HasWalkFrames() ? walkFrames[0] : null;
    }

    private bool HasWalkFrames()
    {
        return walkFrames != null && walkFrames.Length > 0 && walkFrames[0] != null;
    }

    private void ApplyFlip()
    {
        if (body == null || spriteRenderer == null)
        {
            return;
        }

        var horizontalVelocity = body.linearVelocity.x;
        if (Mathf.Abs(horizontalVelocity) < flipVelocityThreshold)
        {
            return;
        }

        var isMovingRight = horizontalVelocity > 0f;
        spriteRenderer.flipX = spriteFacesRight ? !isMovingRight : isMovingRight;
    }
}
