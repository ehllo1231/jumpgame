using UnityEngine;

public static class RuntimeSpriteUtility
{
    private static Sprite squareSprite;

    public static Sprite SquareSprite
    {
        get
        {
            if (squareSprite != null)
            {
                return squareSprite;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Runtime Square Texture",
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            squareSprite.name = "Runtime Square Sprite";
            squareSprite.hideFlags = HideFlags.HideAndDontSave;

            return squareSprite;
        }
    }

    public static SpriteRenderer EnsureSpriteRenderer(GameObject target, Color color, Vector2 size, int sortingOrder = 0)
    {
        var spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = target.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = SquareSprite;
        }

        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        target.transform.localScale = new Vector3(size.x, size.y, 1f);

        return spriteRenderer;
    }
}
