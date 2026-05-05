using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public sealed class PlatformSpikeHazard : MonoBehaviour
{
    private const string ColorPropertyName = "_Color";
    private const float PlatformLocalTopY = 0.5f;
    private const int SortingOrder = 3;

    private static Material sharedMaterial;

    [SerializeField] private Vector2 platformSize = Vector2.one;
    [SerializeField] private float width = 1.2f;
    [SerializeField] private float height = 0.35f;
    [SerializeField] private float horizontalOffset;
    [SerializeField] private float toothWidth = 0.35f;
    [SerializeField] private Color color = new Color(0.9f, 0.12f, 0.08f);
    [SerializeField] private string playerTag = "Player";

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private PolygonCollider2D spikeCollider;
    private MaterialPropertyBlock propertyBlock;

    public void Configure(
        Vector2 newPlatformSize,
        float newWidth,
        float newHeight,
        float newHorizontalOffset,
        float newToothWidth,
        Color newColor,
        string newPlayerTag)
    {
        platformSize = newPlatformSize;
        width = newWidth;
        height = newHeight;
        horizontalOffset = newHorizontalOffset;
        toothWidth = newToothWidth;
        color = newColor;
        playerTag = string.IsNullOrEmpty(newPlayerTag) ? playerTag : newPlayerTag;

        CacheComponents();
        Apply();
    }

    private void Awake()
    {
        CacheComponents();
        Apply();
    }

    private void OnValidate()
    {
        CacheComponents();
        Apply();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerGameOver(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTriggerGameOver(other);
    }

    private void CacheComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        spikeCollider = GetComponent<PolygonCollider2D>();
    }

    private void Apply()
    {
        if (meshFilter == null || meshRenderer == null || spikeCollider == null)
        {
            return;
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        var safePlatformSize = new Vector2(Mathf.Max(0.01f, platformSize.x), Mathf.Max(0.01f, platformSize.y));
        var localWidth = Mathf.Clamp(Mathf.Max(0.01f, width) / safePlatformSize.x, 0.01f, 1f);
        var localHeight = Mathf.Max(0.01f, height) / safePlatformSize.y;
        var localToothWidth = Mathf.Max(0.01f, toothWidth) / safePlatformSize.x;
        var localOffset = Mathf.Clamp(horizontalOffset / safePlatformSize.x, -0.5f + localWidth * 0.5f, 0.5f - localWidth * 0.5f);

        BuildSpikeShape(localOffset, localWidth, localHeight, localToothWidth);
        ApplyRendererSettings();
    }

    private void BuildSpikeShape(float localOffset, float localWidth, float localHeight, float localToothWidth)
    {
        var toothCount = Mathf.Max(1, Mathf.CeilToInt(localWidth / localToothWidth));
        var actualToothWidth = localWidth / toothCount;
        var stripLeft = localOffset - localWidth * 0.5f;
        var vertices = new Vector3[toothCount * 3];
        var triangles = new int[toothCount * 3];

        spikeCollider.isTrigger = true;
        spikeCollider.pathCount = toothCount;

        for (var i = 0; i < toothCount; i++)
        {
            var left = stripLeft + actualToothWidth * i;
            var right = i == toothCount - 1 ? stripLeft + localWidth : left + actualToothWidth;
            var center = (left + right) * 0.5f;
            var vertexIndex = i * 3;

            vertices[vertexIndex] = new Vector3(left, PlatformLocalTopY, 0f);
            vertices[vertexIndex + 1] = new Vector3(right, PlatformLocalTopY, 0f);
            vertices[vertexIndex + 2] = new Vector3(center, PlatformLocalTopY + localHeight, 0f);

            triangles[vertexIndex] = vertexIndex;
            triangles[vertexIndex + 1] = vertexIndex + 1;
            triangles[vertexIndex + 2] = vertexIndex + 2;

            spikeCollider.SetPath(i, new[]
            {
                new Vector2(left, PlatformLocalTopY),
                new Vector2(right, PlatformLocalTopY),
                new Vector2(center, PlatformLocalTopY + localHeight)
            });
        }

        var mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "Platform Spike Mesh",
                hideFlags = HideFlags.HideAndDontSave
            };
            meshFilter.sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void ApplyRendererSettings()
    {
        meshRenderer.sharedMaterial = GetSharedMaterial();
        meshRenderer.sortingOrder = SortingOrder;

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ColorPropertyName, color);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private static Material GetSharedMaterial()
    {
        if (sharedMaterial != null)
        {
            return sharedMaterial;
        }

        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        sharedMaterial = new Material(shader)
        {
            name = "Runtime Spike Hazard Material",
            color = Color.white,
            hideFlags = HideFlags.HideAndDontSave
        };

        return sharedMaterial;
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
            return;
        }

        Debug.Log("Game Over");
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
}
