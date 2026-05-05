using UnityEngine;

public static class MvpRuntimeBootstrap
{
    private const string ConfigResourcePath = "GameTuningConfig";
    private const float DefaultPlayerGravityScale = 2f;
    private static readonly Vector2 StartPlatformPosition = Vector2.zero;
    private static readonly Vector2 StartPlatformSize = new Vector2(4f, 0.35f);
    private static readonly Vector2 PlayerSize = new Vector2(0.6f, 0.8f);
    private static readonly Vector2 DefaultLavaSize = new Vector2(200f, 3f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePlayableMvpIfSceneIsEmpty()
    {
        if (Object.FindAnyObjectByType<PlayerController>() != null)
        {
            EnsureRuntimeSupportObjects();
            return;
        }

        BuildRuntimeScene();
        EnsureRuntimeSupportObjects();
    }

    public static void RestartRuntimeScene()
    {
        DestroyRuntimeObject(Object.FindAnyObjectByType<PlayerController>());
        DestroyRuntimeObject(Object.FindAnyObjectByType<PlatformSpawner>());
        DestroyRuntimeObject(Object.FindAnyObjectByType<LavaController>());
        DestroyRuntimeObject(Object.FindAnyObjectByType<ScoreManager>());
        DestroyRuntimeObject(Object.FindAnyObjectByType<GameManager>());

        var platforms = GameObject.Find("Platforms");
        if (platforms != null)
        {
            DestroyGameObject(platforms);
        }

        BuildRuntimeScene();
        EnsureRuntimeSupportObjects();
    }

    private static void BuildRuntimeScene()
    {
        var tuningConfig = Resources.Load<GameTuningConfig>(ConfigResourcePath);

        new GameObject("GameManager").AddComponent<GameManager>();

        var platformsParent = new GameObject("Platforms").transform;
        var startPlatform = CreateBox("StartPlatform", StartPlatformPosition, StartPlatformSize, new Color(0.2f, 0.55f, 0.38f), "Platform", platformsParent);
        AssignPlatformScore(startPlatform, 0);

        var player = CreateBox("Player", GetStandingPosition(StartPlatformPosition, StartPlatformSize, PlayerSize), PlayerSize, new Color(0.2f, 0.55f, 0.95f), "Player", null, 2);
        var playerBody = player.AddComponent<Rigidbody2D>();
        playerBody.gravityScale = GetPlayerGravityScale(tuningConfig);
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        player.AddComponent<KeyboardTouchJumpInput>();
        player.AddComponent<PlayerController>().SetTuningConfig(tuningConfig);
        player.AddComponent<PlayerVisual>().SetTuningConfig(tuningConfig);

        var lava = CreateBox("Lava", new Vector2(0f, -4.6f), GetLavaSize(tuningConfig), new Color(1f, 0.2f, 0.05f), "Lava", null, 1);
        lava.GetComponent<BoxCollider2D>().isTrigger = true;
        lava.AddComponent<LavaController>().SetTuningConfig(tuningConfig);

        new GameObject("PlatformSpawner").AddComponent<PlatformSpawner>().SetTuningConfig(tuningConfig);

        var cameraObject = GetOrCreateMainCamera();
        cameraObject.transform.position = new Vector3(0f, 2.2f, -10f);

        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        var cameraFollow = cameraObject.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = cameraObject.AddComponent<CameraFollow>();
        }

        cameraFollow.SetTarget(player.transform);
        cameraFollow.SetFollowDownward(true);
    }

    private static void EnsureRuntimeSupportObjects()
    {
        if (Object.FindAnyObjectByType<GameManager>() == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        if (Object.FindAnyObjectByType<ScoreManager>() == null)
        {
            new GameObject("ScoreManager").AddComponent<ScoreManager>();
        }

        if (Object.FindAnyObjectByType<GameUiController>() == null)
        {
            new GameObject("GameUI").AddComponent<GameUiController>();
        }

        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null && player.GetComponent<PlayerVisual>() == null)
        {
            player.gameObject.AddComponent<PlayerVisual>().SetTuningConfig(Resources.Load<GameTuningConfig>(ConfigResourcePath));
        }
    }

    private static void DestroyRuntimeObject(Component component)
    {
        if (component == null)
        {
            return;
        }

        DestroyGameObject(component.gameObject);
    }

    private static void DestroyGameObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Object.DestroyImmediate(target);
    }

    private static GameObject GetOrCreateMainCamera()
    {
        var camera = Camera.main;
        if (camera != null)
        {
            return camera.gameObject;
        }

        var cameraObject = new GameObject("Main Camera");
        TrySetTag(cameraObject, "MainCamera");
        cameraObject.AddComponent<Camera>();
        return cameraObject;
    }

    private static Vector2 GetLavaSize(GameTuningConfig tuningConfig)
    {
        return tuningConfig != null ? tuningConfig.LavaSize : DefaultLavaSize;
    }

    private static float GetPlayerGravityScale(GameTuningConfig tuningConfig)
    {
        return tuningConfig != null ? tuningConfig.PlayerGravityScale : DefaultPlayerGravityScale;
    }

    private static Vector2 GetStandingPosition(Vector2 platformPosition, Vector2 platformSize, Vector2 playerSize)
    {
        return new Vector2(platformPosition.x, platformPosition.y + platformSize.y * 0.5f + playerSize.y * 0.5f);
    }

    private static GameObject CreateBox(string name, Vector2 position, Vector2 size, Color color, string tagName, Transform parent = null, int sortingOrder = 0)
    {
        var target = new GameObject(name);
        target.transform.SetParent(parent);
        target.transform.position = position;
        RuntimeSpriteUtility.EnsureSpriteRenderer(target, color, size, sortingOrder);
        target.AddComponent<PlaceholderShapeRenderer>().Configure(color, sortingOrder);
        target.AddComponent<BoxCollider2D>().size = Vector2.one;
        TrySetTag(target, tagName);
        return target;
    }

    private static void AssignPlatformScore(GameObject platform, int score)
    {
        if (platform == null)
        {
            return;
        }

        var platformScore = platform.GetComponent<PlatformScore>();
        if (platformScore == null)
        {
            platformScore = platform.AddComponent<PlatformScore>();
        }

        platformScore.SetScore(score);
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
}
