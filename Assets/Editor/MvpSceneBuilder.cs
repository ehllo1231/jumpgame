using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MvpSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/VerticalClimbMvp.unity";
    private const string ConfigAssetPath = "Assets/Resources/GameTuningConfig.asset";
    private const float DefaultPlayerGravityScale = 2f;
    private static readonly Vector2 StartPlatformPosition = Vector2.zero;
    private static readonly Vector2 StartPlatformSize = new Vector2(4f, 0.35f);
    private static readonly Vector2 PlayerSize = new Vector2(0.6f, 0.8f);
    private static readonly Vector2 DefaultLavaSize = new Vector2(200f, 3f);

    [MenuItem("Tools/Merado/Create Vertical Climb MVP Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureTag("Player");
        EnsureTag("Platform");
        EnsureTag("Lava");
        var tuningConfig = LoadOrCreateConfig();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("ScoreManager").AddComponent<ScoreManager>();
        new GameObject("GameUI").AddComponent<GameUiController>();

        var platformsParent = new GameObject("Platforms").transform;
        var startPlatform = CreateBox("StartPlatform", StartPlatformPosition, StartPlatformSize, new Color(0.2f, 0.55f, 0.38f), "Platform", platformsParent);
        AssignPlatformScore(startPlatform, 0);

        var player = CreateBox("Player", GetStandingPosition(StartPlatformPosition, StartPlatformSize, PlayerSize), PlayerSize, new Color(0.2f, 0.55f, 0.95f), "Player", null, 2);
        var playerBody = player.AddComponent<Rigidbody2D>();
        playerBody.gravityScale = GetPlayerGravityScale(tuningConfig);
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        player.AddComponent<KeyboardTouchJumpInput>();
        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform);
        groundCheck.localPosition = new Vector3(0f, -0.55f, 0f);
        var playerController = player.AddComponent<PlayerController>();
        playerController.SetTuningConfig(tuningConfig);
        SetObject(playerController, "groundCheck", groundCheck);
        player.AddComponent<PlayerVisual>().SetTuningConfig(tuningConfig);

        var lava = CreateBox("Lava", new Vector2(0f, -4.6f), GetLavaSize(tuningConfig), new Color(1f, 0.2f, 0.05f), "Lava", null, 1);
        lava.GetComponent<BoxCollider2D>().isTrigger = true;
        lava.AddComponent<LavaController>().SetTuningConfig(tuningConfig);

        var spawner = new GameObject("PlatformSpawner");
        var platformSpawner = spawner.AddComponent<PlatformSpawner>();
        platformSpawner.SetTuningConfig(tuningConfig);
        SetObject(platformSpawner, "platformParent", platformsParent);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 2.2f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        var cameraFollow = cameraObject.AddComponent<CameraFollow>();
        cameraFollow.SetTarget(player.transform);
        cameraFollow.SetFollowDownward(true);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeObject = player;
        Debug.Log($"Created MVP scene at {ScenePath}");
    }

    [MenuItem("Tools/Merado/Select Game Tuning Config")]
    public static void SelectGameTuningConfig()
    {
        Selection.activeObject = LoadOrCreateConfig();
        EditorGUIUtility.PingObject(Selection.activeObject);
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
            Debug.LogWarning($"Tag '{tagName}' is not defined. The MVP scene will still run, but add the tag for cleaner setup.", target);
        }
    }

    private static void EnsureTag(string tagName)
    {
        var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets == null || tagManagerAssets.Length == 0)
        {
            return;
        }

        var tagManager = new SerializedObject(tagManagerAssets[0]);
        var tags = tagManager.FindProperty("tags");

        for (var i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }

    private static GameTuningConfig LoadOrCreateConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<GameTuningConfig>(ConfigAssetPath);
        if (config != null)
        {
            return config;
        }

        Directory.CreateDirectory("Assets/Resources");
        config = ScriptableObject.CreateInstance<GameTuningConfig>();
        AssetDatabase.CreateAsset(config, ConfigAssetPath);
        AssetDatabase.SaveAssets();
        return config;
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        var property = GetProperty(target, propertyName);
        property.objectReferenceValue = value;
        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SerializedProperty GetProperty(Object target, string propertyName)
    {
        var serializedObject = new SerializedObject(target);
        var property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            throw new MissingReferenceException($"Serialized property '{propertyName}' was not found on {target.name}.");
        }

        return property;
    }
}
