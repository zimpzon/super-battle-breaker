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
    }

    void OnMouseDown()
    {
        Debug.Log($"=== CLICK DETECTED ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"BrickType: {Type}");
        Debug.Log($"Board coords: ({BoardX},{BoardY})");
        Debug.Log($"World position: {transform.position}");
        Debug.Log($"Mouse position: {Input.mousePosition}");
        Debug.Log($"World mouse position: {Camera.main.ScreenToWorldPoint(Input.mousePosition)}");

        if (BoardScript.Instance != null)
        {
            BoardScript.Instance.OnBrickClicked(this);
        }
        else
        {
            Debug.LogError("BoardScript.Instance is null!");
        }
    }

    private Coroutine currentMovement;

    public void MoveTo(Vector3 worldPosition, float duration = 0.2f)
    {
        // Check if this brick has been destroyed
        if (this == null || gameObject == null)
        {
            return;
        }

        // Stop any existing movement animation
        if (currentMovement != null)
        {
            try
            {
                StopCoroutine(currentMovement);
                Debug.Log($"Stopped existing movement for brick at ({BoardX},{BoardY})");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to stop coroutine on destroyed brick: {e.Message}");
            }
        }

        try
        {
            currentMovement = StartCoroutine(MoveToCo(worldPosition, duration));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to start movement coroutine on destroyed brick: {e.Message}");
        }
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
        currentMovement = null; // Clear reference when animation completes
    }
}
