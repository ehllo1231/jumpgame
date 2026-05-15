using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class PlayerController : MonoBehaviour
{
    private const string FrictionlessMaterialName = "Player Frictionless Material";

    [Header("Config")]
    [SerializeField] private GameTuningConfig tuningConfig;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float jumpForce = 7.2f;
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private float heldJumpForce = 24f;
    [SerializeField] private float maxJumpHoldTime = 0.22f;
    [SerializeField] private float jumpCutVelocityMultiplier = 0.45f;
    [SerializeField] private int maxAirJumps = 1;
    [SerializeField] private float airJumpForce = 6.2f;
    [SerializeField] private float airJumpMaxHoldTime = 0.1f;
    [SerializeField] private float airJumpCutVelocityMultiplier = 0.6f;
    [SerializeField] private float airJumpHorizontalBoostMultiplier = 1.25f;
    [SerializeField] private float airJumpBoostDuration = 0.18f;
    [SerializeField] private bool keepAirMomentum = true;
    [SerializeField] private bool useFrictionlessMaterial = true;
    [SerializeField] private bool useMovementBounds;
    [SerializeField] private float minX = -3.2f;
    [SerializeField] private float maxX = 3.2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer = Physics2D.DefaultRaycastLayers;

    [Header("Edge Check")]
    [SerializeField] private bool flipAtPlatformEdges = true;
    [SerializeField] private float edgeCheckForwardDistance;
    [SerializeField] private float edgeCheckDownDistance = 0.35f;
    [SerializeField] private float ledgeLandingOverlapWidth = 0.14f;
    [SerializeField] private bool flipDirectionOnSamePlatformLanding = true;

    [Header("Platform Pass Through")]
    [SerializeField] private float platformPassThroughLookAhead = 1f;
    [SerializeField] private float platformPassThroughExtraWidth = 0.15f;
    [SerializeField] private string platformTag = "Platform";

    private enum JumpKind
    {
        Ground,
        Air
    }

    private Rigidbody2D body;
    private Collider2D playerCollider;
    private IJumpInput jumpInput;
    private int moveDirection = 1;
    private bool isGrounded;
    private Collider2D currentGround;
    private bool jumpQueued;
    private JumpKind queuedJumpKind;
    private bool isJumpHeld;
    private bool jumpReleaseQueued;
    private bool isVariableJumpActive;
    private JumpKind activeVariableJumpKind;
    private float jumpHoldTimer;
    private int airJumpsRemaining;
    private bool isAirJumpBoostActive;
    private float airJumpBoostTimer;
    private bool isJumpPassThroughActive;
    private bool wasGrounded;
    private bool isWaitingForJumpLanding;
    private Collider2D jumpStartGround;
    private PlatformScore jumpStartPlatformScore;
    private readonly HashSet<Collider2D> ignoredPlatforms = new HashSet<Collider2D>();
    private readonly List<Collider2D> platformsToRestore = new List<Collider2D>();
    private static PhysicsMaterial2D frictionlessMaterial;

    public bool IsGrounded => isGrounded;
    public Collider2D CurrentGround => currentGround;

    public void SetTuningConfig(GameTuningConfig config)
    {
        tuningConfig = config;
        ApplyConfig();
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        jumpInput = FindJumpInput();

        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ApplyPhysicsMaterial();
        ApplyConfig();
        ResetAirJumps();

        if (groundCheck == null)
        {
            var groundCheckObject = new GameObject("GroundCheck");
            groundCheckObject.transform.SetParent(transform);
            groundCheckObject.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            groundCheck = groundCheckObject.transform;
        }
    }

    private void Start()
    {
        if (jumpInput == null)
        {
            jumpInput = FindJumpInput();
        }

        if (jumpInput == null)
        {
            Debug.LogWarning($"{nameof(PlayerController)} needs a component that implements {nameof(IJumpInput)}.", this);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged += HandleGameStateChanged;
            HandleGameStateChanged(GameManager.Instance.State);
        }
    }

    private void OnValidate()
    {
        ApplyConfig();
    }

    private void OnDestroy()
    {
        RestoreAllIgnoredPlatforms();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged -= HandleGameStateChanged;
        }
    }

    private void Update()
    {
        if (!GameManager.IsGamePlaying)
        {
            return;
        }

        if (jumpInput != null)
        {
            if (jumpInput.JumpPressedThisFrame)
            {
                TryQueueJump();
            }

            isJumpHeld = jumpInput.JumpHeld;

            if (jumpInput.JumpReleasedThisFrame)
            {
                jumpReleaseQueued = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!GameManager.IsGamePlaying)
        {
            return;
        }

        UpdatePlatformPassThrough();
        UpdateAirJumpBoostTimer();
        wasGrounded = isGrounded;
        isGrounded = TryGetGround(out currentGround);

        if (isGrounded)
        {
            ResetAirJumps();
            StopAirJumpBoost();
        }

        var flippedOnLanding = false;
        if (!wasGrounded && isGrounded)
        {
            flippedOnLanding = HandleLanding(currentGround);
        }

        if (isGrounded && !flippedOnLanding && TryHandleLedgeLanding(currentGround))
        {
            ApplyHorizontalMovement();
        }
        else if (isGrounded && !flippedOnLanding && TryFlipDirection(currentGround))
        {
            ApplyHorizontalMovement();
        }

        if (isGrounded || keepAirMomentum)
        {
            ApplyHorizontalMovement();
        }

        if (jumpQueued)
        {
            PerformQueuedJump();
            jumpQueued = false;
        }

        ApplyVariableJump();
    }

    private void TryQueueJump()
    {
        if (TryGetGround(out _))
        {
            QueueJump(JumpKind.Ground);
            return;
        }

        if (CanAirJump())
        {
            QueueJump(JumpKind.Air);
        }
    }

    private void QueueJump(JumpKind jumpKind)
    {
        queuedJumpKind = jumpKind;
        jumpQueued = true;
    }

    private bool CanAirJump()
    {
        return GetMaxAirJumps() > 0 && airJumpsRemaining > 0;
    }

    private bool TryGetGround(out Collider2D ground)
    {
        if (IsMovingUpward())
        {
            ground = null;
            return false;
        }

        var hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
        if (TryFindValidGround(hits, out ground))
        {
            return true;
        }

        if (playerCollider == null)
        {
            ground = null;
            return false;
        }

        var bounds = playerCollider.bounds;
        var checkSize = new Vector2(bounds.size.x, groundCheckRadius * 2f);
        var checkCenter = new Vector2(bounds.center.x, bounds.min.y - groundCheckRadius * 0.5f);
        hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, groundLayer);

        return TryFindValidGround(hits, out ground);
    }

    private bool TryFindValidGround(Collider2D[] hits, out Collider2D ground)
    {
        foreach (var hit in hits)
        {
            if (IsValidGround(hit) && !IsIgnoringPlatform(hit) && IsGroundBelowPlayer(hit))
            {
                ground = hit;
                return true;
            }
        }

        ground = null;
        return false;
    }

    private bool IsGroundBelowPlayer(Collider2D hit)
    {
        if (playerCollider == null)
        {
            return false;
        }

        return hit.bounds.max.y <= playerCollider.bounds.min.y + groundCheckRadius * 2f;
    }

    private void ApplyConfig()
    {
        if (tuningConfig == null)
        {
            return;
        }

        moveSpeed = tuningConfig.PlayerMoveSpeed;
        jumpForce = tuningConfig.PlayerJumpForce;
        gravityScale = tuningConfig.PlayerGravityScale;
        heldJumpForce = tuningConfig.HeldJumpForce;
        maxJumpHoldTime = tuningConfig.MaxJumpHoldTime;
        jumpCutVelocityMultiplier = tuningConfig.JumpCutVelocityMultiplier;
        maxAirJumps = tuningConfig.MaxAirJumps;
        airJumpForce = tuningConfig.AirJumpForce;
        airJumpMaxHoldTime = tuningConfig.AirJumpMaxHoldTime;
        airJumpCutVelocityMultiplier = tuningConfig.AirJumpCutVelocityMultiplier;
        airJumpHorizontalBoostMultiplier = tuningConfig.AirJumpHorizontalBoostMultiplier;
        airJumpBoostDuration = tuningConfig.AirJumpBoostDuration;
        useMovementBounds = tuningConfig.UseMovementBounds;
        minX = tuningConfig.PlayerMinX;
        maxX = tuningConfig.PlayerMaxX;
        groundCheckRadius = tuningConfig.GroundCheckRadius;
        edgeCheckForwardDistance = tuningConfig.EdgeCheckForwardDistance;
        edgeCheckDownDistance = tuningConfig.EdgeCheckDownDistance;
        ledgeLandingOverlapWidth = tuningConfig.LedgeLandingOverlapWidth;
        flipDirectionOnSamePlatformLanding = tuningConfig.FlipDirectionOnSamePlatformLanding;
        platformPassThroughLookAhead = tuningConfig.PlatformPassThroughLookAhead;
        platformPassThroughExtraWidth = tuningConfig.PlatformPassThroughExtraWidth;

        if (body != null)
        {
            body.gravityScale = Mathf.Max(0f, gravityScale);
        }

        ClampAirJumpState();
    }

    private void ApplyPhysicsMaterial()
    {
        if (!useFrictionlessMaterial || playerCollider == null)
        {
            return;
        }

        playerCollider.sharedMaterial = GetFrictionlessMaterial();
    }

    private static PhysicsMaterial2D GetFrictionlessMaterial()
    {
        if (frictionlessMaterial != null)
        {
            return frictionlessMaterial;
        }

        frictionlessMaterial = new PhysicsMaterial2D(FrictionlessMaterialName)
        {
            friction = 0f,
            bounciness = 0f
        };

        return frictionlessMaterial;
    }

    private IJumpInput FindJumpInput()
    {
        var behaviours = GetComponents<MonoBehaviour>();

        foreach (var behaviour in behaviours)
        {
            if (behaviour is IJumpInput input)
            {
                return input;
            }
        }

        return null;
    }

    private bool TryFlipDirection(Collider2D ground)
    {
        if (flipAtPlatformEdges && ground != null)
        {
            if (HasReachedPlatformEdge(ground))
            {
                FlipDirection();
                return true;
            }

            return false;
        }

        if (useMovementBounds && HasReachedMovementBound())
        {
            FlipDirection();
            return true;
        }

        return false;
    }

    private bool HasReachedPlatformEdge(Collider2D ground)
    {
        if (ground == null || playerCollider == null)
        {
            return false;
        }

        var playerBounds = playerCollider.bounds;
        var groundBounds = ground.bounds;
        var edgeLookAhead = GetEdgeLookAhead();

        if (moveDirection < 0)
        {
            return playerBounds.min.x - edgeLookAhead <= groundBounds.min.x;
        }

        if (moveDirection > 0)
        {
            return playerBounds.max.x + edgeLookAhead >= groundBounds.max.x;
        }

        return false;
    }

    private float GetEdgeLookAhead()
    {
        var movementSpeed = Mathf.Max(Mathf.Abs(body.linearVelocity.x), Mathf.Abs(moveSpeed));
        var velocityStep = movementSpeed * Time.fixedDeltaTime;
        var configuredDistance = Mathf.Max(0f, edgeCheckForwardDistance);
        return Mathf.Max(configuredDistance, velocityStep);
    }

    private bool HasReachedMovementBound()
    {
        if (moveDirection < 0 && transform.position.x <= minX)
        {
            return true;
        }

        return moveDirection > 0 && transform.position.x >= maxX;
    }

    private bool TryHandleLedgeLanding(Collider2D ground)
    {
        if (IsMovingUpward() || !flipAtPlatformEdges || ground == null || !IsPassThroughPlatform(ground))
        {
            return false;
        }

        var playerBounds = playerCollider.bounds;
        var groundBounds = ground.bounds;
        var overlapMin = Mathf.Max(playerBounds.min.x, groundBounds.min.x);
        var overlapMax = Mathf.Min(playerBounds.max.x, groundBounds.max.x);
        var overlapWidth = overlapMax - overlapMin;

        if (overlapWidth <= 0f || overlapWidth > ledgeLandingOverlapWidth)
        {
            return false;
        }

        if (playerBounds.center.x >= groundBounds.center.x)
        {
            moveDirection = -1;
            return true;
        }

        moveDirection = 1;
        return true;
    }

    private bool IsValidGround(Collider2D hit)
    {
        return hit != null
            && !hit.isTrigger
            && !hit.transform.IsChildOf(transform);
    }

    private bool IsMovingUpward()
    {
        return body != null && body.linearVelocity.y > 0f;
    }

    private void UpdatePlatformPassThrough()
    {
        if (playerCollider == null)
        {
            return;
        }

        if (!isJumpPassThroughActive)
        {
            return;
        }

        if (body.linearVelocity.y >= 0f)
        {
            IgnoreOverlappingAndNearbyPlatforms();
            return;
        }

        RestorePlatformsThatAreNoLongerOverlapping();

        if (ignoredPlatforms.Count == 0)
        {
            isJumpPassThroughActive = false;
        }
    }

    private void IgnoreOverlappingAndNearbyPlatforms()
    {
        var bounds = playerCollider.bounds;
        var upwardStep = body.linearVelocity.y * Time.fixedDeltaTime + 0.05f;
        var upwardLookAhead = Mathf.Max(0.05f, Mathf.Max(platformPassThroughLookAhead, upwardStep));
        var queryCenter = bounds.center + Vector3.up * (upwardLookAhead * 0.5f);
        var querySize = new Vector2(bounds.size.x + Mathf.Max(0f, platformPassThroughExtraWidth), bounds.size.y + upwardLookAhead);
        var hits = Physics2D.OverlapBoxAll(queryCenter, querySize, 0f, groundLayer);

        foreach (var hit in hits)
        {
            if (IsPassThroughPlatform(hit) && IsPlatformInJumpPath(hit))
            {
                IgnorePlatform(hit);
            }
        }
    }

    private bool IsPlatformInJumpPath(Collider2D platform)
    {
        if (playerCollider == null || platform == null)
        {
            return false;
        }

        if (IsJumpStartGround(platform))
        {
            return false;
        }

        var playerFeetY = playerCollider.bounds.min.y + groundCheckRadius * 2f;
        return platform.bounds.max.y > playerFeetY;
    }

    private void RestorePlatformsThatAreNoLongerOverlapping()
    {
        platformsToRestore.Clear();

        foreach (var platform in ignoredPlatforms)
        {
            if (platform == null || !playerCollider.bounds.Intersects(platform.bounds))
            {
                platformsToRestore.Add(platform);
            }
        }

        foreach (var platform in platformsToRestore)
        {
            RestorePlatform(platform);
        }

        platformsToRestore.Clear();
    }

    private void IgnorePlatform(Collider2D platform)
    {
        if (ignoredPlatforms.Add(platform))
        {
            Physics2D.IgnoreCollision(playerCollider, platform, true);
        }
    }

    private void RestorePlatform(Collider2D platform)
    {
        ignoredPlatforms.Remove(platform);

        if (platform != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platform, false);
        }
    }

    private void RestoreAllIgnoredPlatforms()
    {
        platformsToRestore.Clear();
        platformsToRestore.AddRange(ignoredPlatforms);

        foreach (var platform in platformsToRestore)
        {
            RestorePlatform(platform);
        }

        platformsToRestore.Clear();
    }

    private bool IsPassThroughPlatform(Collider2D hit)
    {
        return IsValidGround(hit) && HasTag(hit, platformTag);
    }

    private bool IsIgnoringPlatform(Collider2D hit)
    {
        return hit != null && ignoredPlatforms.Contains(hit);
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

    private float GetColliderHalfHeight()
    {
        return playerCollider != null ? playerCollider.bounds.extents.y : 0.5f;
    }

    private void ApplyHorizontalMovement()
    {
        var velocity = body.linearVelocity;
        velocity.x = moveDirection * GetCurrentMoveSpeed();
        body.linearVelocity = velocity;
    }

    private float GetCurrentMoveSpeed()
    {
        var speed = moveSpeed;
        if (isAirJumpBoostActive)
        {
            speed *= Mathf.Max(1f, airJumpHorizontalBoostMultiplier);
        }

        return speed;
    }

    private void PerformQueuedJump()
    {
        if (queuedJumpKind == JumpKind.Air && !isGrounded)
        {
            if (CanAirJump())
            {
                PerformAirJump();
            }

            return;
        }

        PerformGroundJump();
    }

    private void PerformGroundJump()
    {
        ResetAirJumps();
        jumpStartGround = currentGround;
        jumpStartPlatformScore = GetPlatformScore(currentGround);
        isWaitingForJumpLanding = jumpStartGround != null;
        StartVariableJump(JumpKind.Ground);

        var velocity = body.linearVelocity;
        velocity.y = jumpForce;
        body.linearVelocity = velocity;
        isGrounded = false;
        currentGround = null;

        StartPlatformPassThrough();
    }

    private void PerformAirJump()
    {
        airJumpsRemaining = Mathf.Max(0, airJumpsRemaining - 1);
        StartVariableJump(JumpKind.Air);
        StartAirJumpBoost();

        var velocity = body.linearVelocity;
        velocity.x = moveDirection * GetCurrentMoveSpeed();
        velocity.y = GetAirJumpVerticalVelocity(velocity.y);
        body.linearVelocity = velocity;
        isGrounded = false;
        currentGround = null;

        StartPlatformPassThrough();
    }

    private float GetAirJumpVerticalVelocity(float currentVelocityY)
    {
        return Mathf.Max(currentVelocityY, Mathf.Max(0f, airJumpForce));
    }

    private void StartVariableJump(JumpKind jumpKind)
    {
        activeVariableJumpKind = jumpKind;
        isVariableJumpActive = true;
        jumpHoldTimer = 0f;
        isJumpHeld = jumpInput != null && jumpInput.JumpHeld;
        jumpReleaseQueued = false;
    }

    private void StartPlatformPassThrough()
    {
        isJumpPassThroughActive = true;
        IgnoreOverlappingAndNearbyPlatforms();
    }

    private void UpdateAirJumpBoostTimer()
    {
        if (!isAirJumpBoostActive)
        {
            return;
        }

        airJumpBoostTimer -= Time.fixedDeltaTime;
        if (airJumpBoostTimer <= 0f)
        {
            StopAirJumpBoost();
        }
    }

    private void StartAirJumpBoost()
    {
        airJumpBoostTimer = Mathf.Max(0f, airJumpBoostDuration);
        isAirJumpBoostActive = airJumpBoostTimer > 0f;
    }

    private void StopAirJumpBoost()
    {
        isAirJumpBoostActive = false;
        airJumpBoostTimer = 0f;
    }

    private void ResetAirJumps()
    {
        airJumpsRemaining = GetMaxAirJumps();
    }

    private void ClampAirJumpState()
    {
        airJumpsRemaining = Mathf.Clamp(airJumpsRemaining, 0, GetMaxAirJumps());
        airJumpBoostTimer = Mathf.Max(0f, airJumpBoostTimer);
        isAirJumpBoostActive = isAirJumpBoostActive && airJumpBoostTimer > 0f;
    }

    private int GetMaxAirJumps()
    {
        return Mathf.Max(0, maxAirJumps);
    }

    private bool HandleLanding(Collider2D landedGround)
    {
        if (!isWaitingForJumpLanding)
        {
            return false;
        }

        isWaitingForJumpLanding = false;

        if (flipDirectionOnSamePlatformLanding && IsJumpStartGround(landedGround))
        {
            FlipDirection();
            ClearJumpStartGround();
            return true;
        }

        ClearJumpStartGround();
        return false;
    }

    private bool IsJumpStartGround(Collider2D landedGround)
    {
        if (landedGround == null)
        {
            return false;
        }

        if (landedGround == jumpStartGround)
        {
            return true;
        }

        var landedPlatformScore = GetPlatformScore(landedGround);
        return landedPlatformScore != null && landedPlatformScore == jumpStartPlatformScore;
    }

    private static PlatformScore GetPlatformScore(Collider2D ground)
    {
        return ground != null ? ground.GetComponentInParent<PlatformScore>() : null;
    }

    private void ClearJumpStartGround()
    {
        jumpStartGround = null;
        jumpStartPlatformScore = null;
    }

    private void ApplyVariableJump()
    {
        if (!isVariableJumpActive)
        {
            return;
        }

        if (jumpReleaseQueued)
        {
            CutJumpShort();
            return;
        }

        if (!isJumpHeld || jumpHoldTimer >= GetActiveMaxJumpHoldTime() || body.linearVelocity.y <= 0f)
        {
            isVariableJumpActive = false;
            return;
        }

        jumpHoldTimer += Time.fixedDeltaTime;
        body.AddForce(Vector2.up * heldJumpForce, ForceMode2D.Force);
    }

    private void CutJumpShort()
    {
        jumpReleaseQueued = false;
        isVariableJumpActive = false;

        if (body.linearVelocity.y <= 0f)
        {
            return;
        }

        var velocity = body.linearVelocity;
        velocity.y *= GetActiveJumpCutVelocityMultiplier();
        body.linearVelocity = velocity;
    }

    private float GetActiveMaxJumpHoldTime()
    {
        return activeVariableJumpKind == JumpKind.Air
            ? Mathf.Max(0f, airJumpMaxHoldTime)
            : Mathf.Max(0f, maxJumpHoldTime);
    }

    private float GetActiveJumpCutVelocityMultiplier()
    {
        return activeVariableJumpKind == JumpKind.Air
            ? Mathf.Clamp01(airJumpCutVelocityMultiplier)
            : Mathf.Clamp01(jumpCutVelocityMultiplier);
    }

    private void FlipDirection()
    {
        moveDirection *= -1;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (body == null)
        {
            return;
        }

        if (state == GameState.GameOver)
        {
            isJumpPassThroughActive = false;
            isVariableJumpActive = false;
            StopAirJumpBoost();
            isWaitingForJumpLanding = false;
            ClearJumpStartGround();
            jumpReleaseQueued = false;
            RestoreAllIgnoredPlatforms();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            return;
        }

        body.simulated = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.yellow;
        var bounds = playerCollider != null ? playerCollider.bounds : new Bounds(transform.position, Vector3.one);
        var frontX = moveDirection > 0 ? bounds.max.x : bounds.min.x;
        var origin = new Vector2(frontX + moveDirection * edgeCheckForwardDistance, bounds.center.y);
        Gizmos.DrawLine(origin, origin + Vector2.down * (GetColliderHalfHeight() + edgeCheckDownDistance));
    }
}
