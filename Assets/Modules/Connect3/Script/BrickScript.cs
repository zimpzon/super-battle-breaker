using UnityEngine;

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
    private float dragThreshold = 0.5f; // Minimum distance to register as drag

    void OnMouseDown()
    {
        if (BoardScript.Instance == null) return;

        mouseDownPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseDownPosition.z = 0; // Ensure Z is 0 for 2D
        hasDragged = false;

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

            // Find the adjacent brick in that direction
            int targetX = BoardX + (int)dragDirection.x;
            int targetY = BoardY + (int)dragDirection.y;

            //Debug.Log($"Target position: ({targetX},{targetY})");

            // Clear drag highlight
            SetSelected(false);

            // Trigger swap with adjacent brick
            if (BoardScript.Instance != null)
            {
                BoardScript.Instance.OnDragSwap(this, targetX, targetY);
            }
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
            //Debug.Log("Drag operation completed");
        }

        // Reset for next interaction
        hasDragged = false;
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
}
