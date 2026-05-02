using UnityEngine;

[CreateAssetMenu(fileName = "GameTuningConfig", menuName = "Merado/Game Tuning Config")]
public sealed class GameTuningConfig : ScriptableObject
{
    [Header("Player")]
    [SerializeField] private float playerMoveSpeed = 2.25f;
    [SerializeField] private float playerJumpForce = 7.2f;
    [SerializeField] private float heldJumpForce = 24f;
    [SerializeField] private float maxJumpHoldTime = 0.22f;
    [SerializeField] private float jumpCutVelocityMultiplier = 0.45f;
    [SerializeField] private float playerMinX = -3.2f;
    [SerializeField] private float playerMaxX = 3.2f;
    [SerializeField] private float groundCheckRadius = 0.14f;
    [SerializeField] private float edgeCheckForwardDistance = 0.03f;
    [SerializeField] private float edgeCheckDownDistance = 0.35f;
    [SerializeField] private float ledgeLandingOverlapWidth = 0.14f;
    [SerializeField] private float ledgeLandingInset = 0.06f;

    [Header("Platforms")]
    [SerializeField] private int platformCount = 45;
    [SerializeField] private float firstPlatformY = 1.8f;
    [SerializeField] private Vector2 platformXRange = new Vector2(-2.6f, 2.6f);
    [SerializeField] private Vector2 platformVerticalSpacingRange = new Vector2(1.25f, 1.75f);
    [SerializeField] private Vector2 platformWidthRange = new Vector2(1.8f, 3f);
    [SerializeField] private float platformMaxHorizontalStep = 1.8f;
    [SerializeField] private float platformHeight = 0.25f;
    [SerializeField] private bool useRandomSeed;
    [SerializeField] private int randomSeed = 12345;

    [Header("Lava")]
    [SerializeField] private float lavaRiseSpeed = 0.7f;

    public float PlayerMoveSpeed => playerMoveSpeed;
    public float PlayerJumpForce => playerJumpForce;
    public float HeldJumpForce => heldJumpForce;
    public float MaxJumpHoldTime => maxJumpHoldTime;
    public float JumpCutVelocityMultiplier => jumpCutVelocityMultiplier;
    public float PlayerMinX => playerMinX;
    public float PlayerMaxX => playerMaxX;
    public float GroundCheckRadius => groundCheckRadius;
    public float EdgeCheckForwardDistance => edgeCheckForwardDistance;
    public float EdgeCheckDownDistance => edgeCheckDownDistance;
    public float LedgeLandingOverlapWidth => ledgeLandingOverlapWidth;
    public float LedgeLandingInset => ledgeLandingInset;

    public int PlatformCount => platformCount;
    public float FirstPlatformY => firstPlatformY;
    public Vector2 PlatformXRange => platformXRange;
    public Vector2 PlatformVerticalSpacingRange => platformVerticalSpacingRange;
    public Vector2 PlatformWidthRange => platformWidthRange;
    public float PlatformMaxHorizontalStep => platformMaxHorizontalStep;
    public float PlatformHeight => platformHeight;
    public bool UseRandomSeed => useRandomSeed;
    public int RandomSeed => randomSeed;

    public float LavaRiseSpeed => lavaRiseSpeed;
}
