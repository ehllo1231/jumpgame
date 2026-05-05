using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlatformSpawner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GameTuningConfig tuningConfig;

    [Header("Generation")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private int platformCount = 35;
    [SerializeField] private float firstPlatformY = 1.8f;
    [SerializeField] private Vector2 xRange = new Vector2(-2.6f, 2.6f);
    [SerializeField] private Vector2 verticalSpacingRange = new Vector2(1.25f, 1.75f);
    [SerializeField] private Vector2 platformWidthRange = new Vector2(1.8f, 3f);
    [SerializeField] private float maxHorizontalStep = 1.8f;
    [SerializeField] private int minPlatformsPerRow = 1;
    [SerializeField] private int maxPlatformsPerRow = 3;
    [Range(0f, 1f)]
    [SerializeField] private float additionalSameRowChance = 0.45f;
    [SerializeField] private float sameRowMinGap = 0.8f;
    [SerializeField] private float sameRowSpawnRadius = 8f;
    [SerializeField] private int sameRowPlacementAttempts = 20;
    [SerializeField] private float platformHeight = 0.25f;
    [SerializeField] private bool keepSpawningPlatforms = true;
    [SerializeField] private float spawnAheadDistance = 18f;
    [SerializeField] private float cleanupBelowDistance = 30f;
    [SerializeField] private bool useRandomSeed;
    [SerializeField] private int randomSeed = 12345;

    [Header("Spikes")]
    [SerializeField] private int spikePlatformStartScore = 100;
    [Range(0f, 1f)]
    [SerializeField] private float spikePlatformChance = 0.25f;
    [SerializeField] private float spikeWidth = 1.2f;
    [SerializeField] private float spikeHeight = 0.35f;
    [SerializeField] private float spikeEdgePadding = 0.25f;
    [SerializeField] private float spikeToothWidth = 0.35f;
    [SerializeField] private float spikeMinHorizontalDistanceFromPreviousRow = 2.5f;
    [SerializeField] private Color spikeColor = new Color(0.9f, 0.12f, 0.08f);

    [Header("Presentation")]
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private Transform platformParent;
    [SerializeField] private Color fallbackPlatformColor = new Color(0.22f, 0.55f, 0.42f);

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    private readonly List<GameObject> spawnedPlatforms = new List<GameObject>();
    private readonly List<PlatformPlacement> currentRowPlacements = new List<PlatformPlacement>();
    private readonly List<float> previousRowSpikeXs = new List<float>();
    private readonly List<float> currentRowSpikeXs = new List<float>();
    private readonly List<Vector2> candidateSpikeOffsetRanges = new List<Vector2>();
    private System.Random random;
    private float lastPlatformX;
    private float nextPlatformY;
    private int nextPlatformIndex;

    private struct PlatformPlacement
    {
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public bool HasSpike { get; }
        public float SpikeWidth { get; }
        public float SpikeHorizontalOffset { get; }
        public float Left => Position.x - Size.x * 0.5f;
        public float Right => Position.x + Size.x * 0.5f;

        public PlatformPlacement(Vector2 position, Vector2 size, bool hasSpike = false, float spikeWidth = 0f, float spikeHorizontalOffset = 0f)
        {
            Position = position;
            Size = size;
            HasSpike = hasSpike;
            SpikeWidth = spikeWidth;
            SpikeHorizontalOffset = spikeHorizontalOffset;
        }

        public PlatformPlacement WithSpike(float spikeWidth, float spikeHorizontalOffset)
        {
            return new PlatformPlacement(Position, Size, true, spikeWidth, spikeHorizontalOffset);
        }
    }

    public void SetTuningConfig(GameTuningConfig config)
    {
        tuningConfig = config;
        ApplyConfig();
    }

    private void Start()
    {
        ApplyConfig();

        if (generateOnStart)
        {
            Generate();
        }
    }

    private void Update()
    {
        if (!keepSpawningPlatforms || !GameManager.IsGamePlaying)
        {
            return;
        }

        EnsureTarget();

        if (target == null)
        {
            return;
        }

        EnsurePlatformsAhead(target.position.y + spawnAheadDistance);
        CleanupPlatformsBelow(target.position.y - cleanupBelowDistance);
    }

    private void OnValidate()
    {
        ApplyConfig();
    }

    [ContextMenu("Generate Platforms")]
    public void Generate()
    {
        ApplyConfig();
        ClearSpawnedPlatforms();
        ResetGenerator();
        GeneratePlatforms(platformCount);
    }

    private void GeneratePlatforms(int count)
    {
        for (var i = 0; i < count; i++)
        {
            SpawnNextPlatformRow();
        }
    }

    private void ResetGenerator()
    {
        random = useRandomSeed ? new System.Random(randomSeed) : new System.Random();
        lastPlatformX = 0f;
        nextPlatformY = firstPlatformY;
        nextPlatformIndex = 1;
        previousRowSpikeXs.Clear();
        currentRowSpikeXs.Clear();
    }

    private void EnsurePlatformsAhead(float requiredTopY)
    {
        if (random == null)
        {
            ResetGenerator();
        }

        while (nextPlatformY <= requiredTopY)
        {
            SpawnNextPlatformRow();
        }
    }

    private void SpawnNextPlatformRow()
    {
        currentRowPlacements.Clear();
        currentRowSpikeXs.Clear();
        var targetPlatformCount = GetPlatformCountForRow();
        var rowIndex = nextPlatformIndex;
        var mainPlatform = CreateMainPlatformPlacement(rowIndex);

        currentRowPlacements.Add(mainPlatform);
        SpawnRowPlatform(mainPlatform, rowIndex, 0, targetPlatformCount);

        for (var i = 1; i < targetPlatformCount; i++)
        {
            if (!TryCreateAdditionalPlatformPlacement(mainPlatform.Position.x, rowIndex, out var placement))
            {
                continue;
            }

            currentRowPlacements.Add(placement);
            SpawnRowPlatform(placement, rowIndex, i, targetPlatformCount);
        }

        previousRowSpikeXs.Clear();
        previousRowSpikeXs.AddRange(currentRowSpikeXs);
        nextPlatformIndex++;
        nextPlatformY += Range(verticalSpacingRange);
    }

    private PlatformPlacement CreateMainPlatformPlacement(int rowIndex)
    {
        var sourceX = lastPlatformX;
        var shouldHaveSpike = ShouldAttemptSpike(rowIndex);
        var attempts = Mathf.Max(1, sameRowPlacementAttempts);
        var fallback = CreateMainPlatformPlacementCandidate(sourceX);

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var candidate = attempt == 0 ? fallback : CreateMainPlatformPlacementCandidate(sourceX);
            if (TryConfigureSpike(candidate, shouldHaveSpike, out var placement))
            {
                lastPlatformX = placement.Position.x;
                return placement;
            }
        }

        if (shouldHaveSpike && TryCreateForcedSpikeMainPlacement(sourceX, out var forcedPlacement))
        {
            lastPlatformX = forcedPlacement.Position.x;
            return forcedPlacement;
        }

        lastPlatformX = fallback.Position.x;
        return fallback;
    }

    private PlatformPlacement CreateMainPlatformPlacementCandidate(float sourceX)
    {
        var x = ClampToRange(sourceX + Range(new Vector2(-maxHorizontalStep, maxHorizontalStep)), xRange);
        var size = new Vector2(Range(platformWidthRange), platformHeight);
        return new PlatformPlacement(new Vector2(x, nextPlatformY), size);
    }

    private bool TryCreateForcedSpikeMainPlacement(float sourceX, out PlatformPlacement placement)
    {
        var step = Mathf.Abs(maxHorizontalStep);
        var width = Mathf.Max(platformWidthRange.x, platformWidthRange.y);
        var size = new Vector2(width, platformHeight);
        var leftCandidate = new PlatformPlacement(new Vector2(ClampToRange(sourceX - step, xRange), nextPlatformY), size);
        var rightCandidate = new PlatformPlacement(new Vector2(ClampToRange(sourceX + step, xRange), nextPlatformY), size);

        if (TryConfigureSpike(leftCandidate, true, out placement))
        {
            return true;
        }

        return TryConfigureSpike(rightCandidate, true, out placement);
    }

    private void SpawnRowPlatform(PlatformPlacement placement, int rowIndex, int slotIndex, int rowPlatformCount)
    {
        var platformName = GetPlatformName(rowIndex, slotIndex, rowPlatformCount);
        var platform = SpawnPlatform(placement.Position, placement.Size, platformName);
        AssignPlatformScore(platform, rowIndex);
        AttachSpikeIfNeeded(platform, placement);
    }

    private int GetPlatformCountForRow()
    {
        var count = minPlatformsPerRow;
        while (count < maxPlatformsPerRow && Chance(additionalSameRowChance))
        {
            count++;
        }

        return count;
    }

    private bool TryCreateAdditionalPlatformPlacement(float anchorX, int rowIndex, out PlatformPlacement placement)
    {
        var spawnRange = GetSameRowSpawnRange(anchorX);
        var shouldHaveSpike = ShouldAttemptSpike(rowIndex);

        for (var attempt = 0; attempt < sameRowPlacementAttempts; attempt++)
        {
            var size = new Vector2(Range(platformWidthRange), platformHeight);
            var position = new Vector2(Range(spawnRange), nextPlatformY);
            var candidate = new PlatformPlacement(position, size);

            if (!HasRequiredGap(candidate))
            {
                continue;
            }

            if (TryConfigureSpike(candidate, shouldHaveSpike, out placement))
            {
                return true;
            }
        }

        placement = default(PlatformPlacement);
        return false;
    }

    private Vector2 GetSameRowSpawnRange(float anchorX)
    {
        var min = Mathf.Min(xRange.x, xRange.y);
        var max = Mathf.Max(xRange.x, xRange.y);
        var radius = Mathf.Max(0f, sameRowSpawnRadius);

        min = Mathf.Max(min, anchorX - radius);
        max = Mathf.Min(max, anchorX + radius);

        if (min > max)
        {
            return new Vector2(anchorX, anchorX);
        }

        return new Vector2(min, max);
    }

    private bool HasRequiredGap(PlatformPlacement candidate)
    {
        for (var i = 0; i < currentRowPlacements.Count; i++)
        {
            var existing = currentRowPlacements[i];
            var separated = candidate.Right + sameRowMinGap <= existing.Left || candidate.Left - sameRowMinGap >= existing.Right;
            if (!separated)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetPlatformName(int rowIndex, int slotIndex, int rowPlatformCount)
    {
        return rowPlatformCount > 1 ? $"Platform {rowIndex:00}-{slotIndex + 1}" : $"Platform {rowIndex:00}";
    }

    private GameObject SpawnPlatform(Vector2 position, Vector2 size, string platformName)
    {
        var parent = platformParent != null ? platformParent : transform;
        GameObject platform;

        if (platformPrefab != null)
        {
            platform = Instantiate(platformPrefab, position, Quaternion.identity, parent);
            platform.name = platformName;
            platform.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
        else
        {
            platform = new GameObject(platformName);
            platform.transform.SetParent(parent);
            platform.transform.position = position;
            RuntimeSpriteUtility.EnsureSpriteRenderer(platform, fallbackPlatformColor, size);
        }

        if (!platform.TryGetComponent<Collider2D>(out _))
        {
            platform.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        TrySetTag(platform, "Platform");
        spawnedPlatforms.Add(platform);
        return platform;
    }

    private void AttachSpikeIfNeeded(GameObject platform, PlatformPlacement placement)
    {
        if (platform == null || !placement.HasSpike)
        {
            return;
        }

        var spikeObject = new GameObject("Spike Hazard");
        spikeObject.transform.SetParent(platform.transform, false);

        var spike = spikeObject.AddComponent<PlatformSpikeHazard>();
        spike.Configure(
            placement.Size,
            placement.SpikeWidth,
            spikeHeight,
            placement.SpikeHorizontalOffset,
            spikeToothWidth,
            spikeColor,
            targetTag);

        currentRowSpikeXs.Add(placement.Position.x + placement.SpikeHorizontalOffset);
    }

    private bool ShouldAttemptSpike(int platformScore)
    {
        return platformScore >= spikePlatformStartScore
            && spikePlatformChance > 0f
            && spikeWidth > 0f
            && spikeHeight > 0f
            && Chance(spikePlatformChance);
    }

    private bool TryConfigureSpike(PlatformPlacement candidate, bool shouldHaveSpike, out PlatformPlacement placement)
    {
        placement = candidate;
        if (!shouldHaveSpike)
        {
            return true;
        }

        if (candidate.Size.x <= 0f)
        {
            return false;
        }

        var effectiveSpikeWidth = GetEffectiveSpikeWidth(candidate.Size.x);
        if (!TryGetSpikeHorizontalOffset(candidate.Position.x, candidate.Size.x, effectiveSpikeWidth, out var horizontalOffset))
        {
            return false;
        }

        placement = candidate.WithSpike(effectiveSpikeWidth, horizontalOffset);
        return true;
    }

    private float GetEffectiveSpikeWidth(float platformWidth)
    {
        var maxWidth = Mathf.Max(0f, platformWidth - spikeEdgePadding * 2f);
        if (maxWidth <= 0f)
        {
            maxWidth = Mathf.Max(0f, platformWidth);
        }

        return Mathf.Min(spikeWidth, maxWidth);
    }

    private bool TryGetSpikeHorizontalOffset(float platformCenterX, float platformWidth, float effectiveSpikeWidth, out float horizontalOffset)
    {
        var maxOffset = platformWidth * 0.5f - spikeEdgePadding - effectiveSpikeWidth * 0.5f;
        if (maxOffset <= 0f)
        {
            horizontalOffset = 0f;
            return IsFarEnoughFromPreviousRowSpikes(platformCenterX);
        }

        candidateSpikeOffsetRanges.Clear();
        candidateSpikeOffsetRanges.Add(new Vector2(-maxOffset, maxOffset));

        for (var i = 0; i < previousRowSpikeXs.Count; i++)
        {
            var blockedMin = previousRowSpikeXs[i] - spikeMinHorizontalDistanceFromPreviousRow - platformCenterX;
            var blockedMax = previousRowSpikeXs[i] + spikeMinHorizontalDistanceFromPreviousRow - platformCenterX;
            RemoveBlockedSpikeOffsetRange(blockedMin, blockedMax);
        }

        return TryPickSpikeOffset(out horizontalOffset);
    }

    private void RemoveBlockedSpikeOffsetRange(float blockedMin, float blockedMax)
    {
        for (var i = candidateSpikeOffsetRanges.Count - 1; i >= 0; i--)
        {
            var range = candidateSpikeOffsetRanges[i];
            if (blockedMax <= range.x || blockedMin >= range.y)
            {
                continue;
            }

            var hasLeftRange = blockedMin > range.x;
            var hasRightRange = blockedMax < range.y;

            if (hasLeftRange && hasRightRange)
            {
                candidateSpikeOffsetRanges[i] = new Vector2(range.x, blockedMin);
                candidateSpikeOffsetRanges.Add(new Vector2(blockedMax, range.y));
                continue;
            }

            if (hasLeftRange)
            {
                candidateSpikeOffsetRanges[i] = new Vector2(range.x, blockedMin);
                continue;
            }

            if (hasRightRange)
            {
                candidateSpikeOffsetRanges[i] = new Vector2(blockedMax, range.y);
                continue;
            }

            candidateSpikeOffsetRanges.RemoveAt(i);
        }
    }

    private bool TryPickSpikeOffset(out float horizontalOffset)
    {
        var totalLength = 0f;
        for (var i = 0; i < candidateSpikeOffsetRanges.Count; i++)
        {
            var range = candidateSpikeOffsetRanges[i];
            totalLength += Mathf.Max(0f, range.y - range.x);
        }

        if (totalLength <= 0f)
        {
            horizontalOffset = 0f;
            return false;
        }

        var target = (float)random.NextDouble() * totalLength;
        for (var i = 0; i < candidateSpikeOffsetRanges.Count; i++)
        {
            var range = candidateSpikeOffsetRanges[i];
            var length = Mathf.Max(0f, range.y - range.x);
            if (target > length)
            {
                target -= length;
                continue;
            }

            horizontalOffset = Mathf.Lerp(range.x, range.y, length > 0f ? target / length : 0f);
            return true;
        }

        var fallbackRange = candidateSpikeOffsetRanges[candidateSpikeOffsetRanges.Count - 1];
        horizontalOffset = fallbackRange.y;
        return true;
    }

    private bool IsFarEnoughFromPreviousRowSpikes(float spikeWorldX)
    {
        for (var i = 0; i < previousRowSpikeXs.Count; i++)
        {
            if (Mathf.Abs(spikeWorldX - previousRowSpikeXs[i]) < spikeMinHorizontalDistanceFromPreviousRow)
            {
                return false;
            }
        }

        return true;
    }

    private static void AssignPlatformScore(GameObject platform, int score)
    {
        if (platform == null)
        {
            return;
        }

        if (!platform.TryGetComponent(out PlatformScore platformScore))
        {
            platformScore = platform.AddComponent<PlatformScore>();
        }

        platformScore.SetScore(score);
    }

    private void ClearSpawnedPlatforms()
    {
        foreach (var platform in spawnedPlatforms)
        {
            if (platform == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(platform);
            }
            else
            {
                DestroyImmediate(platform);
            }
        }

        spawnedPlatforms.Clear();
    }

    private void CleanupPlatformsBelow(float cutoffY)
    {
        if (cleanupBelowDistance <= 0f)
        {
            return;
        }

        for (var i = spawnedPlatforms.Count - 1; i >= 0; i--)
        {
            var platform = spawnedPlatforms[i];

            if (platform == null)
            {
                spawnedPlatforms.RemoveAt(i);
                continue;
            }

            if (platform.transform.position.y >= cutoffY)
            {
                continue;
            }

            spawnedPlatforms.RemoveAt(i);
            Destroy(platform);
        }
    }

    private void EnsureTarget()
    {
        if (target != null)
        {
            return;
        }

        try
        {
            var targetObject = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObject != null)
            {
                target = targetObject.transform;
                return;
            }
        }
        catch (UnityException)
        {
            target = null;
        }

        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            target = player.transform;
        }
    }

    private float Range(Vector2 range)
    {
        var min = Mathf.Min(range.x, range.y);
        var max = Mathf.Max(range.x, range.y);
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private bool Chance(float probability)
    {
        return random.NextDouble() <= Mathf.Clamp01(probability);
    }

    private static float ClampToRange(float value, Vector2 range)
    {
        var min = Mathf.Min(range.x, range.y);
        var max = Mathf.Max(range.x, range.y);
        return Mathf.Clamp(value, min, max);
    }

    private static void TrySetTag(GameObject target, string tagName)
    {
        try
        {
            target.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Tag '{tagName}' is not defined. Add it in Project Settings > Tags and Layers.", target);
        }
    }

    private void ApplyConfig()
    {
        if (tuningConfig != null)
        {
            platformCount = tuningConfig.PlatformCount;
            firstPlatformY = tuningConfig.FirstPlatformY;
            xRange = tuningConfig.PlatformXRange;
            verticalSpacingRange = tuningConfig.PlatformVerticalSpacingRange;
            platformWidthRange = tuningConfig.PlatformWidthRange;
            maxHorizontalStep = tuningConfig.PlatformMaxHorizontalStep;
            minPlatformsPerRow = tuningConfig.PlatformMinCountPerRow;
            maxPlatformsPerRow = tuningConfig.PlatformMaxCountPerRow;
            additionalSameRowChance = tuningConfig.PlatformAdditionalSameRowChance;
            sameRowMinGap = tuningConfig.PlatformSameRowMinGap;
            sameRowSpawnRadius = tuningConfig.PlatformSameRowSpawnRadius;
            sameRowPlacementAttempts = tuningConfig.PlatformSameRowPlacementAttempts;
            platformHeight = tuningConfig.PlatformHeight;
            keepSpawningPlatforms = tuningConfig.KeepSpawningPlatforms;
            spawnAheadDistance = tuningConfig.PlatformSpawnAheadDistance;
            cleanupBelowDistance = tuningConfig.PlatformCleanupBelowDistance;
            useRandomSeed = tuningConfig.UseRandomSeed;
            randomSeed = tuningConfig.RandomSeed;
            spikePlatformStartScore = tuningConfig.SpikePlatformStartScore;
            spikePlatformChance = tuningConfig.SpikePlatformChance;
            spikeWidth = tuningConfig.SpikeWidth;
            spikeHeight = tuningConfig.SpikeHeight;
            spikeEdgePadding = tuningConfig.SpikeEdgePadding;
            spikeToothWidth = tuningConfig.SpikeToothWidth;
            spikeMinHorizontalDistanceFromPreviousRow = tuningConfig.SpikeMinHorizontalDistanceFromPreviousRow;
            spikeColor = tuningConfig.SpikeColor;
        }

        SanitizeGenerationSettings();
    }

    private void SanitizeGenerationSettings()
    {
        platformCount = Mathf.Max(0, platformCount);
        minPlatformsPerRow = Mathf.Max(1, minPlatformsPerRow);
        maxPlatformsPerRow = Mathf.Max(minPlatformsPerRow, maxPlatformsPerRow);
        additionalSameRowChance = Mathf.Clamp01(additionalSameRowChance);
        sameRowMinGap = Mathf.Max(0f, sameRowMinGap);
        sameRowSpawnRadius = Mathf.Max(0f, sameRowSpawnRadius);
        sameRowPlacementAttempts = Mathf.Max(1, sameRowPlacementAttempts);
        platformHeight = Mathf.Max(0.01f, platformHeight);
        spikePlatformStartScore = Mathf.Max(0, spikePlatformStartScore);
        spikePlatformChance = Mathf.Clamp01(spikePlatformChance);
        spikeWidth = Mathf.Max(0f, spikeWidth);
        spikeHeight = Mathf.Max(0f, spikeHeight);
        spikeEdgePadding = Mathf.Max(0f, spikeEdgePadding);
        spikeToothWidth = Mathf.Max(0.01f, spikeToothWidth);
        spikeMinHorizontalDistanceFromPreviousRow = Mathf.Max(0f, spikeMinHorizontalDistanceFromPreviousRow);
    }
}
