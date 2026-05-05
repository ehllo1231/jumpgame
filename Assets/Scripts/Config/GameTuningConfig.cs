using UnityEngine;

[CreateAssetMenu(fileName = "GameTuningConfig", menuName = "Merado/Game Tuning Config")]
public sealed class GameTuningConfig : ScriptableObject
{
    [Header("Player")]
    [SerializeField] private float playerMoveSpeed = 2.25f;
    [SerializeField] private float playerJumpForce = 7.2f;
    [SerializeField] private float playerGravityScale = 2f;
    [SerializeField] private float heldJumpForce = 24f;
    [SerializeField] private float maxJumpHoldTime = 0.22f;
    [SerializeField] private float jumpCutVelocityMultiplier = 0.45f;
    [SerializeField] private bool useMovementBounds;
    [SerializeField] private float playerMinX = -3.2f;
    [SerializeField] private float playerMaxX = 3.2f;
    [SerializeField] private float groundCheckRadius = 0.14f;
    [SerializeField] private float edgeCheckForwardDistance;
    [SerializeField] private float edgeCheckDownDistance = 0.35f;
    [SerializeField] private float ledgeLandingOverlapWidth = 0.14f;
    [SerializeField] private float ledgeLandingInset = 0.06f;
    [SerializeField] private float platformPassThroughLookAhead = 1f;
    [SerializeField] private float platformPassThroughExtraWidth = 0.15f;
    [SerializeField] private bool flipDirectionOnSamePlatformLanding = true;
    [SerializeField] private Vector2 playerVisualScale = Vector2.one;

    [Header("Camera")]
    [SerializeField] private Vector2 targetAspect = new Vector2(16f, 9f);
    [SerializeField] private float cameraOrthographicSize = 5f;

    [Header("Platforms")]
    [SerializeField] private int platformCount = 45;
    [SerializeField] private float firstPlatformY = 1.8f;
    [SerializeField] private Vector2 platformXRange = new Vector2(-2.6f, 2.6f);
    [SerializeField] private Vector2 platformVerticalSpacingRange = new Vector2(1.25f, 1.75f);
    [SerializeField] private Vector2 platformWidthRange = new Vector2(1.8f, 3f);
    [SerializeField] private float platformMaxHorizontalStep = 1.8f;
    [SerializeField] private int platformMinCountPerRow = 1;
    [SerializeField] private int platformMaxCountPerRow = 3;
    [SerializeField] private float platformAdditionalSameRowChance = 0.45f;
    [SerializeField] private float platformSameRowMinGap = 0.8f;
    [SerializeField] private float platformSameRowSpawnRadius = 8f;
    [SerializeField] private int platformSameRowPlacementAttempts = 20;
    [SerializeField] private float platformHeight = 0.25f;
    [SerializeField] private bool keepSpawningPlatforms = true;
    [SerializeField] private float platformSpawnAheadDistance = 18f;
    [SerializeField] private float platformCleanupBelowDistance = 30f;
    [SerializeField] private bool useRandomSeed;
    [SerializeField] private int randomSeed = 12345;

    [Header("Spikes")]
    [SerializeField] private int spikePlatformStartScore = 100;
    [SerializeField] private float spikePlatformChance = 0.25f;
    [SerializeField] private float spikeWidth = 1.2f;
    [SerializeField] private float spikeHeight = 0.35f;
    [SerializeField] private float spikeEdgePadding = 0.25f;
    [SerializeField] private float spikeToothWidth = 0.35f;
    [SerializeField] private float spikeMinHorizontalDistanceFromPreviousRow = 2.5f;
    [SerializeField] private Color spikeColor = new Color(0.9f, 0.12f, 0.08f);

    [Header("Lava")]
    [SerializeField] private float lavaRiseSpeed = 0.7f;
    [SerializeField] private float lavaAcceleration = 0.04f;
    [SerializeField] private float lavaMaxRiseSpeed = 2.2f;
    [SerializeField] private float lavaMaxDistanceBelowPlayer = 8f;
    [SerializeField] private Vector2 lavaSize = new Vector2(200f, 3f);
    [SerializeField] private bool lavaFollowCameraX = true;

    public float PlayerMoveSpeed => playerMoveSpeed;
    public float PlayerJumpForce => playerJumpForce;
    public float PlayerGravityScale => playerGravityScale;
    public float HeldJumpForce => heldJumpForce;
    public float MaxJumpHoldTime => maxJumpHoldTime;
    public float JumpCutVelocityMultiplier => jumpCutVelocityMultiplier;
    public bool UseMovementBounds => useMovementBounds;
    public float PlayerMinX => playerMinX;
    public float PlayerMaxX => playerMaxX;
    public float GroundCheckRadius => groundCheckRadius;
    public float EdgeCheckForwardDistance => edgeCheckForwardDistance;
    public float EdgeCheckDownDistance => edgeCheckDownDistance;
    public float LedgeLandingOverlapWidth => ledgeLandingOverlapWidth;
    public float LedgeLandingInset => ledgeLandingInset;
    public float PlatformPassThroughLookAhead => platformPassThroughLookAhead;
    public float PlatformPassThroughExtraWidth => platformPassThroughExtraWidth;
    public bool FlipDirectionOnSamePlatformLanding => flipDirectionOnSamePlatformLanding;
    public Vector2 PlayerVisualScale => playerVisualScale;

    public Vector2 TargetAspect => targetAspect;
    public float CameraOrthographicSize => cameraOrthographicSize;

    public int PlatformCount => platformCount;
    public float FirstPlatformY => firstPlatformY;
    public Vector2 PlatformXRange => platformXRange;
    public Vector2 PlatformVerticalSpacingRange => platformVerticalSpacingRange;
    public Vector2 PlatformWidthRange => platformWidthRange;
    public float PlatformMaxHorizontalStep => platformMaxHorizontalStep;
    public int PlatformMinCountPerRow => platformMinCountPerRow;
    public int PlatformMaxCountPerRow => platformMaxCountPerRow;
    public float PlatformAdditionalSameRowChance => platformAdditionalSameRowChance;
    public float PlatformSameRowMinGap => platformSameRowMinGap;
    public float PlatformSameRowSpawnRadius => platformSameRowSpawnRadius;
    public int PlatformSameRowPlacementAttempts => platformSameRowPlacementAttempts;
    public float PlatformHeight => platformHeight;
    public bool KeepSpawningPlatforms => keepSpawningPlatforms;
    public float PlatformSpawnAheadDistance => platformSpawnAheadDistance;
    public float PlatformCleanupBelowDistance => platformCleanupBelowDistance;
    public bool UseRandomSeed => useRandomSeed;
    public int RandomSeed => randomSeed;

    public int SpikePlatformStartScore => spikePlatformStartScore;
    public float SpikePlatformChance => spikePlatformChance;
    public float SpikeWidth => spikeWidth;
    public float SpikeHeight => spikeHeight;
    public float SpikeEdgePadding => spikeEdgePadding;
    public float SpikeToothWidth => spikeToothWidth;
    public float SpikeMinHorizontalDistanceFromPreviousRow => spikeMinHorizontalDistanceFromPreviousRow;
    public Color SpikeColor => spikeColor;

    public float LavaRiseSpeed => lavaRiseSpeed;
    public float LavaAcceleration => lavaAcceleration;
    public float LavaMaxRiseSpeed => lavaMaxRiseSpeed;
    public float LavaMaxDistanceBelowPlayer => lavaMaxDistanceBelowPlayer;
    public Vector2 LavaSize => lavaSize;
    public bool LavaFollowCameraX => lavaFollowCameraX;
}
