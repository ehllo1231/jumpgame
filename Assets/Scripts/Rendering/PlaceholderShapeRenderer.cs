using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlaceholderShapeRenderer : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private int sortingOrder;

    private SpriteRenderer spriteRenderer;

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    public void Configure(Color newColor, int newSortingOrder = 0)
    {
        color = newColor;
        sortingOrder = newSortingOrder;
        Apply();
    }

    private void Apply()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = RuntimeSpriteUtility.SquareSprite;
        }

        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
    }
}
