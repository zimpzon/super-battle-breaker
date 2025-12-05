using UnityEngine;

public class BrickScript : MonoBehaviour
{
    public BrickType Type;
    public Color Color;

    [HideInInspector]
    public int BoardX;
    [HideInInspector]
    public int BoardY;

    private bool isSelected = false;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && Color == default(Color))
        {
            Color = spriteRenderer.color;
        }
        originalColor = Color;
        UpdateSpriteColor();
    }

    void Update()
    {

    }

    public void SetBoardPosition(int x, int y)
    {
        BoardX = x;
        BoardY = y;
    }

    public void UpdateSpriteColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (spriteRenderer != null)
        {
            if (selected)
            {
                spriteRenderer.color = Color.Lerp(originalColor, Color.white, 0.5f);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        Collider2D existingCollider = GetComponent<Collider2D>();
        if (existingCollider == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.size = Vector2.one * 0.9f;
            Debug.Log($"Added BoxCollider2D to brick at {transform.position}");
        }
        else
        {
            existingCollider.isTrigger = false;
            if (existingCollider is BoxCollider2D box)
            {
                box.size = Vector2.one * 0.9f;
            }
            Debug.Log($"Found existing collider on brick at {transform.position}");
        }
    }

    void OnMouseDown()
    {
        Debug.Log($"OnMouseDown: Brick with BrickType {Type} at board coords ({BoardX},{BoardY}), world position {transform.position}");

        if (BoardScript.Instance != null)
        {
            BoardScript.Instance.OnBrickClicked(this);
        }
        else
        {
            Debug.Log("BoardScript.Instance is null!");
        }
    }

    public void MoveTo(Vector3 worldPosition, float duration = 0.3f)
    {
        StartCoroutine(MoveToCo(worldPosition, duration));
    }

    private System.Collections.IEnumerator MoveToCo(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
    }
}
