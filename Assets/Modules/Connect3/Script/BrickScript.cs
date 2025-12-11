using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class BrickScript : MonoBehaviour
{
    public BrickType Type;
    public Color Color;

    [HideInInspector]
    public int BoardX = -1; // Initialize to -1 to indicate "not set"
    [HideInInspector]
    public int BoardY = -1;

    private bool isSelected = false;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;
    private bool isBeingDestroyed = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && Color == default(Color))
        {
            Color = spriteRenderer.color;
            GetComponent<Light2D>().color = Color;
        }
        originalColor = Color;
        UpdateSpriteColor();
    }

    void Update()
    {

    }

    public void SetBoardPosition(int x, int y)
    {
        // Debug log if a brick is changing columns during settlement (but not during swaps, testing, or initial spawn)
        if (BoardX != -1 && BoardX != x && BoardScript.Instance != null &&
            BoardScript.Instance.isProcessingMatches &&
            !BoardScript.Instance.isProcessingSwap &&
            !BoardScript.Instance.isTestingMoves)
        {
            Debug.LogError($"HORIZONTAL MOVEMENT BUG: Brick moved from column {BoardX} to {x}! Y: {BoardY} to {y}");
        }

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

    private Vector3 mouseDownPosition;
    private bool hasDragged = false;
    private int pendingTargetX = -1;
    private int pendingTargetY = -1;
    private float dragThreshold = 0.5f; // Minimum distance to register as drag

    void OnMouseDown()
    {
        if (BoardScript.Instance == null) return;

        mouseDownPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseDownPosition.z = 0; // Ensure Z is 0 for 2D
        hasDragged = false;
        pendingTargetX = -1;
        pendingTargetY = -1;

        // Debug.Log($"=== MOUSE DOWN ===");
        //Debug.Log($"Mouse down on brick at ({BoardX},{BoardY})");
        //Debug.Log($"Mouse down position: {mouseDownPosition}");
    }

    void OnMouseDrag()
    {
        if (BoardScript.Instance == null) return;

        Vector3 currentMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currentMousePos.z = 0;

        Vector3 dragDelta = currentMousePos - mouseDownPosition;
        float dragDistance = dragDelta.magnitude;

        // Show visual feedback during drag
        if (dragDistance > 0.2f && !hasDragged)
        {
            // Highlight this brick during drag
            SetSelected(true);
        }

        // Only process drag if we've moved enough and haven't already dragged
        if (dragDistance >= dragThreshold && !hasDragged)
        {
            hasDragged = true;

            // Determine drag direction (up, down, left, right)
            Vector2 dragDirection = GetDragDirection(dragDelta);

            // Debug.Log($"=== DRAG DETECTED ===");
            //Debug.Log($"Drag distance: {dragDistance:F2}, direction: {dragDirection}");

            // Store the target position for potential swap on mouse up
            pendingTargetX = BoardX + (int)dragDirection.x;
            pendingTargetY = BoardY + (int)dragDirection.y;

            //Debug.Log($"Target position: ({pendingTargetX},{pendingTargetY})");

            // Clear drag highlight
            SetSelected(false);
        }
    }

    void OnMouseUp()
    {
        if (BoardScript.Instance == null) return;

        // Clear any drag highlight
        if (hasDragged)
        {
            SetSelected(false);
        }

        if (!hasDragged)
        {
            // This was a click, not a drag - use original click behavior
            // Debug.Log($"=== CLICK DETECTED ===");
            //Debug.Log($"GameObject: {gameObject.name}");
            //Debug.Log($"BrickType: {Type}");
            //Debug.Log($"Board coords: ({BoardX},{BoardY})");
            //Debug.Log($"World position: {transform.position}");

            BoardScript.Instance.OnBrickClicked(this);
        }
        else
        {
            // Drag completed - execute the swap if we have a valid target
            //Debug.Log("Drag operation completed");
            if (pendingTargetX != -1 && pendingTargetY != -1 && BoardScript.Instance != null)
            {
                BoardScript.Instance.OnDragSwap(this, pendingTargetX, pendingTargetY);
            }
        }

        // Reset for next interaction
        hasDragged = false;
        pendingTargetX = -1;
        pendingTargetY = -1;
    }

    private Vector2 GetDragDirection(Vector3 dragDelta)
    {
        // Determine primary direction based on largest component
        if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y))
        {
            // Horizontal drag - X direction is correct
            return dragDelta.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            // Vertical drag - FLIP Y direction because board coordinates are inverted
            // World Y increases upward, but BoardY increases downward on screen
            // Dragging up (positive world Y) should target lower board Y (up on screen) = -1 in Y
            // Dragging down (negative world Y) should target higher board Y (down on screen) = +1 in Y
            return dragDelta.y > 0 ? new Vector2(0, -1) : new Vector2(0, 1);
        }
    }

    public void MoveTo(Vector3 worldPosition, float duration = 0.2f)
    {
        // Check if this brick has been destroyed
        if (this == null || gameObject == null)
        {
            return;
        }

        // Register this movement with the BoardScript
        if (BoardScript.Instance != null)
        {
            BoardScript.Instance.AddMovingBrick(this, worldPosition, duration);
        }
    }

    public void StartScaleDestruction(float duration = 0.3f)
    {
        if (isBeingDestroyed) return;

        isBeingDestroyed = true;

        // Set sorting order behind other bricks
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = -10;
        }

        StartCoroutine(ScaleDestructionCoroutine(duration));
    }

    private IEnumerator ScaleDestructionCoroutine(float duration)
    {
        Vector3 originalScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float scale = Mathf.Lerp(1f, 0f, progress);
            transform.localScale = originalScale * scale;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final scale is exactly zero
        transform.localScale = Vector3.zero;

        // Destroy the object
        Destroy(gameObject);
    }
}
