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
    [SerializeField] private float platformHeight = 0.25f;
    [SerializeField] private bool useRandomSeed;
    [SerializeField] private int randomSeed = 12345;

    [Header("Presentation")]
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private Transform platformParent;
    [SerializeField] private Color fallbackPlatformColor = new Color(0.22f, 0.55f, 0.42f);

    private readonly List<GameObject> spawnedPlatforms = new List<GameObject>();
    private System.Random random;

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

    private void OnValidate()
    {
        ApplyConfig();
    }

    [ContextMenu("Generate Platforms")]
    public void Generate()
    {
        ApplyConfig();
        ClearSpawnedPlatforms();

        random = useRandomSeed ? new System.Random(randomSeed) : new System.Random();

        var x = 0f;
        var y = firstPlatformY;
        for (var i = 0; i < platformCount; i++)
        {
            x = ClampToRange(x + Range(new Vector2(-maxHorizontalStep, maxHorizontalStep)), xRange);
            var width = Range(platformWidthRange);
            var size = new Vector2(width, platformHeight);
            var position = new Vector2(x, y);

            SpawnPlatform(position, size, $"Platform {i + 1:00}");
            y += Range(verticalSpacingRange);
        }
    }

    private void SpawnPlatform(Vector2 position, Vector2 size, string platformName)
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

    private float Range(Vector2 range)
    {
        var min = Mathf.Min(range.x, range.y);
        var max = Mathf.Max(range.x, range.y);
        return Mathf.Lerp(min, max, (float)random.NextDouble());
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
        if (tuningConfig == null)
        {
            return;
        }

        platformCount = tuningConfig.PlatformCount;
        firstPlatformY = tuningConfig.FirstPlatformY;
        xRange = tuningConfig.PlatformXRange;
        verticalSpacingRange = tuningConfig.PlatformVerticalSpacingRange;
        platformWidthRange = tuningConfig.PlatformWidthRange;
        maxHorizontalStep = tuningConfig.PlatformMaxHorizontalStep;
        platformHeight = tuningConfig.PlatformHeight;
        useRandomSeed = tuningConfig.UseRandomSeed;
        randomSeed = tuningConfig.RandomSeed;
    }
}
