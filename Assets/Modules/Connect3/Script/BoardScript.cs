using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public struct MovingBrick
{
    public BrickScript brick;
    public Vector3 startPosition;
    public Vector3 targetPosition;
    public float startTime;
    public float duration;

    public MovingBrick(BrickScript brick, Vector3 startPos, Vector3 targetPos, float duration)
    {
        this.brick = brick;
        this.startPosition = startPos;
        this.targetPosition = targetPos;
        this.startTime = Time.time;
        this.duration = duration;
    }

    public bool IsComplete => Time.time >= startTime + duration;
    public float Progress => Mathf.Clamp01((Time.time - startTime) / duration);
}

public class BoardScript : MonoBehaviour
{
    #region Events

    /// <summary>
    /// Event fired when a match is found and bricks are removed.
    /// Parameters: (representativeBrick, matchCount, removedPositions, isFromBoardFill)
    /// </summary>
    public static event Action<BrickScript, int, Vector2Int[]> OnMatch;

    /// <summary>
    /// Event fired when the board is being reset due to no possible moves.
    /// </summary>
    public static event Action OnBoardReset;

    /// <summary>
    /// Event fired when a swap attempt fails (creates no match).
    /// Parameters: (brick1, brick2, position1, position2)
    /// </summary>
    public static event Action<BrickScript, BrickScript, Vector2Int, Vector2Int> OnFailedSwapAttempt;

    /*
    Example usage:

    void Start()
    {
        BoardScript.OnMatch += HandleMatch;
        BoardScript.OnBoardReset += HandleBoardReset;
        BoardScript.OnFailedSwapAttempt += HandleFailedSwap;
    }

    void HandleMatch(BrickScript brick, int count, Vector2Int[] positions, bool isFromBoardFill)
    {
        if (isFromBoardFill)
        {
            Debug.Log($"Match during board fill - ignoring: {count} {brick.Type} bricks!");
            return; // Ignore matches during board initialization/refill
        }
        Debug.Log($"Player-created match: {count} {brick.Type} bricks!");
        // Play sound effects, update score, etc.
    }

    void HandleBoardReset()
    {
        Debug.Log("Board is being reset!");
        // Play shuffle animation, update UI, etc.
    }

    void HandleFailedSwap(BrickScript brick1, BrickScript brick2, Vector2Int pos1, Vector2Int pos2)
    {
        Debug.Log($"Failed swap between {brick1.Type} and {brick2.Type}!");
        // Play negative feedback sound, shake animation, etc.
    }
    */

    #endregion

    private const int W = 7;
    private const int H = 7;
    private BrickScript[,] Board = new BrickScript[W, H + 1];

    [SerializeField] private float brickSize = 1f;
    [SerializeField] private float settlementDelay = 0.1f;
    [SerializeField] private float movementDuration = 0.3f;

    public static BoardScript Instance;

    private BrickScript selectedBrick = null;
    private bool isPlayerTurn = true;
    public bool isProcessingSwap = false;
    public bool isProcessingMatches = false;
    public bool isProcessingSettlement = false;
    public bool isTestingMoves = false;
    private bool boardInitialized = false;
    private bool isBoardFilling = false;

    // Movement tracking - simple list-based system
    private List<MovingBrick> movingBricks = new List<MovingBrick>();
    public bool HasActiveMovements => movingBricks.Count > 0;

    public void AddMovingBrick(BrickScript brick, Vector3 targetPosition, float duration)
    {
        if (brick != null)
        {
            // Remove any existing movements for this brick first
            movingBricks.RemoveAll(m => m.brick == brick);

            Vector3 startPos = brick.transform.position;
            MovingBrick movement = new MovingBrick(brick, startPos, targetPosition, duration);
            movingBricks.Add(movement);
        }
    }

    // Safely move a brick in the Board array, clearing any previous assignments
    private void MoveBrickInArray(BrickScript brick, int fromX, int fromY, int toX, int toY)
    {
        // First, clear the brick from its previous position in the array
        ClearBrickFromArray(brick);

        // Now assign it to the new position
        Board[toX, toY] = brick;
        if (fromX >= 0 && fromY >= 0)
        {
            Board[fromX, fromY] = null;
        }
    }

    // Clear a brick from all positions in the Board array (fixes corruption)
    private void ClearBrickFromArray(BrickScript brick)
    {
        if (brick == null) return;

        for (int y = 0; y <= H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (Board[x, y] == brick)
                {
                    Board[x, y] = null;
                }
            }
        }
    }

    // Safely swap two bricks in the Board array
    private void SwapBricksInArray(BrickScript brick1, int x1, int y1, BrickScript brick2, int x2, int y2)
    {
        // Clear both bricks from any previous positions
        ClearBrickFromArray(brick1);
        ClearBrickFromArray(brick2);

        // Now assign them to their new positions
        Board[x1, y1] = brick2;
        Board[x2, y2] = brick1;
    }

    public IEnumerator WaitForAllMovements()
    {
        // Wait for all movements to complete
        while (HasActiveMovements)
        {
            yield return null;
        }
    }

    public Transform Upperleft;

    [Header("UI")]
    public UnityEngine.UI.Text notificationText;

    void Start()
    {
        Instance = this;
        StartCoroutine(SettleCo());
    }

    void Update()
    {
        // Process moving bricks
        for (int i = movingBricks.Count - 1; i >= 0; i--)
        {
            MovingBrick movement = movingBricks[i];

            // Check if brick is still valid
            if (movement.brick == null || movement.brick.gameObject == null)
            {
                movingBricks.RemoveAt(i);
                continue;
            }

            // Update position based on progress
            float progress = movement.Progress;
            movement.brick.transform.position = Vector3.Lerp(movement.startPosition, movement.targetPosition, progress);

            // Remove if complete
            if (movement.IsComplete)
            {
                movement.brick.transform.position = movement.targetPosition;
                movingBricks.RemoveAt(i);
            }
        }

        // Debug key to show current state WITHOUT resetting
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log($"STATE CHECK - isProcessingSwap={isProcessingSwap}, isProcessingMatches={isProcessingMatches}, isProcessingSettlement={isProcessingSettlement}, activeMovements={movingBricks.Count}");
        }
    }

    BrickScript SpawnBrick(int x)
    {
        if (Board[x, 0] != null)
            return null;

        GameObject prefab = BrickPrefabs.I?.GetRandomBrickPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("SpawnBrick: No prefab available!");
            return null;
        }

        GameObject newBrickObj = Instantiate(prefab, GetWorldPosition(x, 0), Quaternion.identity, parent: Upperleft.transform);
        BrickScript newBrick = newBrickObj.GetComponent<BrickScript>();

        if (newBrick != null)
        {
            newBrick.BoardX = -1;
            newBrick.BoardY = -1;
            newBrick.SetBoardPosition(x, 0);

            if (newBrick.Type == BrickType.Undefined)
            {
                BrickType[] validTypes = {
                    BrickType.Type1, BrickType.Type2, BrickType.Type3, BrickType.Type4,
                    BrickType.Type5, BrickType.Type6, BrickType.Type7
                };
                newBrick.Type = validTypes[UnityEngine.Random.Range(0, validTypes.Length)];
            }

            Board[x, 0] = newBrick;
        }

        return newBrick;
    }

    Vector3 GetWorldPosition(int x, int y)
    {
        float z = -0.1f - (y * 0.001f);
        return new Vector3(x * brickSize + Upperleft.position.x, -y * brickSize + Upperleft.position.y, z);
    }

    List<BrickScript> FindAllMatches()
    {
        List<BrickScript> matches = new List<BrickScript>();
        bool[,] processed = new bool[W, H + 1];

        for (int y = 1; y <= H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (Board[x, y] != null && IsValidBrick(Board[x, y]) && !processed[x, y])
                {
                    List<BrickScript> horizontalMatch = FindHorizontalMatch(x, y);
                    if (horizontalMatch.Count >= 3)
                    {
                        foreach (var brick in horizontalMatch)
                        {
                            if (brick != null && IsValidBrick(brick) && !matches.Contains(brick) &&
                                brick.BoardX >= 0 && brick.BoardX < W && brick.BoardY >= 0 && brick.BoardY <= H)
                            {
                                matches.Add(brick);
                                processed[brick.BoardX, brick.BoardY] = true;
                            }
                        }
                    }

                    List<BrickScript> verticalMatch = FindVerticalMatch(x, y);
                    if (verticalMatch.Count >= 3)
                    {
                        foreach (var brick in verticalMatch)
                        {
                            if (brick != null && IsValidBrick(brick) && !matches.Contains(brick) &&
                                brick.BoardX >= 0 && brick.BoardX < W && brick.BoardY >= 0 && brick.BoardY <= H)
                            {
                                matches.Add(brick);
                                processed[brick.BoardX, brick.BoardY] = true;
                            }
                        }
                    }
                }
            }
        }

        return matches;
    }

    List<BrickScript> FindHorizontalMatch(int startX, int startY)
    {
        List<BrickScript> match = new List<BrickScript>();
        BrickScript startBrick = Board[startX, startY];

        if (startBrick == null || !IsValidBrick(startBrick))
            return match;

        int leftX = startX;
        while (leftX > 0 && Board[leftX - 1, startY] != null &&
               IsValidBrick(Board[leftX - 1, startY]) &&
               BrickTypesMatch(startBrick, Board[leftX - 1, startY]))
        {
            leftX--;
        }

        for (int x = leftX; x < W; x++)
        {
            BrickScript currentBrick = Board[x, startY];
            if (currentBrick != null && IsValidBrick(currentBrick) && BrickTypesMatch(startBrick, currentBrick))
            {
                match.Add(currentBrick);
            }
            else
            {
                break;
            }
        }

        return match;
    }

    List<BrickScript> FindVerticalMatch(int startX, int startY)
    {
        List<BrickScript> match = new List<BrickScript>();
        BrickScript startBrick = Board[startX, startY];

        if (startBrick == null || !IsValidBrick(startBrick))
            return match;

        int topY = startY;
        while (topY > 1 && Board[startX, topY - 1] != null &&
               IsValidBrick(Board[startX, topY - 1]) &&
               BrickTypesMatch(startBrick, Board[startX, topY - 1]))
        {
            topY--;
        }

        for (int y = topY; y <= H; y++)
        {
            BrickScript currentBrick = Board[startX, y];
            if (currentBrick != null && IsValidBrick(currentBrick) && BrickTypesMatch(startBrick, currentBrick))
            {
                match.Add(currentBrick);
            }
            else
            {
                break;
            }
        }

        return match;
    }

    bool BrickTypesMatch(BrickScript brick1, BrickScript brick2)
    {
        if (brick1 == null || brick2 == null)
            return false;

        if (!IsValidBrick(brick1) || !IsValidBrick(brick2))
            return false;

        return brick1.Type == brick2.Type && brick1.Type != BrickType.Undefined;
    }

    bool IsValidBrick(BrickScript brick)
    {
        return brick != null && brick.gameObject != null;
    }

    bool HasCurrentMatches()
    {
        List<BrickScript> currentMatches = FindAllMatches();
        return currentMatches.Count > 0;
    }

    bool HasPossibleMoves()
    {
        for (int y = 1; y <= H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (Board[x, y] != null && IsValidBrick(Board[x, y]))
                {
                    if (CanSwapCreateMatch(x, y, x + 1, y) ||
                        CanSwapCreateMatch(x, y, x - 1, y) ||
                        CanSwapCreateMatch(x, y, x, y + 1) ||
                        CanSwapCreateMatch(x, y, x, y - 1))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    bool ShouldClearBoard()
    {
        return !HasCurrentMatches() && !HasPossibleMoves();
    }

    void CleanupNullBricks()
    {
        for (int y = 0; y <= H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (Board[x, y] != null && !IsValidBrick(Board[x, y]))
                {
                    Board[x, y] = null;
                }
            }
        }
    }

    bool CanSwapCreateMatch(int x1, int y1, int x2, int y2)
    {
        if (x2 < 0 || x2 >= W || y2 < 1 || y2 > H)
            return false;

        if (Board[x1, y1] == null || Board[x2, y2] == null)
            return false;

        if (!IsValidBrick(Board[x1, y1]) || !IsValidBrick(Board[x2, y2]))
            return false;

        BrickScript brick1 = Board[x1, y1];
        BrickScript brick2 = Board[x2, y2];

        if (brick1 == null || brick2 == null)
            return false;

        isTestingMoves = true;

        Board[x1, y1] = brick2;
        Board[x2, y2] = brick1;
        brick1.SetBoardPosition(x2, y2);
        brick2.SetBoardPosition(x1, y1);

        List<BrickScript> matches = FindAllMatches();
        bool hasMatches = matches.Count > 0;

        Board[x1, y1] = brick1;
        Board[x2, y2] = brick2;
        brick1.SetBoardPosition(x1, y1);
        brick2.SetBoardPosition(x2, y2);

        isTestingMoves = false;

        return hasMatches;
    }

    void ShowNotification(string message, float duration = 3f)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            StartCoroutine(HideNotificationCo(duration));
        }
    }

    IEnumerator HideNotificationCo(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    IEnumerator ClearAndRefillBoardCo()
    {
        if (isProcessingSettlement)
        {
            //Debug.Log("Settlement in progress, skipping board clear");
            yield break;
        }

        isProcessingSettlement = true;
        isBoardFilling = true;

        // Fire board reset event
        OnBoardReset?.Invoke();

        ShowNotification("No moves available!\nShuffling board...", 3f);

        for (int y = 1; y <= H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (Board[x, y] != null)
                {
                    Destroy(Board[x, y].gameObject);
                    Board[x, y] = null;
                }
            }
        }

        for (int x = 0; x < W; x++)
        {
            if (Board[x, 0] != null)
            {
                Destroy(Board[x, 0].gameObject);
                Board[x, 0] = null;
            }
        }

        yield return new WaitForSeconds(0.5f);

        for (int x = 0; x < W; x++)
        {
            for (int y = H; y >= 1; y--)
            {
                SpawnBrick(x);

                if (Board[x, 0] != null)
                {
                    BrickScript movingBrick = Board[x, 0];
                    Board[x, y] = movingBrick;
                    Board[x, 0] = null;
                    movingBrick.SetBoardPosition(x, y);
                    movingBrick.MoveTo(GetWorldPosition(x, y), movementDuration);
                }
            }
        }

        yield return StartCoroutine(WaitForAllMovements());

        List<BrickScript> immediateMatches = FindAllMatches();
        if (immediateMatches.Count > 0)
        {
            if (!isProcessingMatches)
            {
                StartCoroutine(ProcessMatchesCo());
            }
        }
        else
        {
            ShowNotification("New board ready!", 2f);
        }

        isProcessingSettlement = false;
        isBoardFilling = false;
        //Debug.Log("=== BOARD CLEAR/REFILL COMPLETE - SETTLEMENT FLAG CLEARED ===");
    }

    public void OnDragSwap(BrickScript sourceBrick, int targetX, int targetY)
    {
        //Debug.Log($"Drag swap requested from ({sourceBrick.BoardX},{sourceBrick.BoardY}) to ({targetX},{targetY})");
        //Debug.Log($"State check - isPlayerTurn: {isPlayerTurn}, isProcessingSwap: {isProcessingSwap}, isProcessingMatches: {isProcessingMatches}");

        if (!isPlayerTurn)
        {
            //Debug.Log("Not player turn - ignoring drag");
            return;
        }

        if (isProcessingSwap)
        {
            //Debug.Log("Processing swap - ignoring drag");
            return;
        }

        if (isProcessingMatches)
        {
            //Debug.Log("Processing matches - ignoring drag");
            return;
        }

        if (sourceBrick == null || !IsValidBrick(sourceBrick))
        {
            //Debug.Log("Source brick is invalid - ignoring drag");
            return;
        }

        if (sourceBrick.BoardY == 0)
        {
            //Debug.Log("Dragged from spawn row - ignoring drag");
            return;
        }

        if (HasActiveMovements)
        {
            //Debug.Log("Bricks still moving - ignoring drag");
            return;
        }

        if (!boardInitialized)
        {
            //Debug.Log("Board not initialized - ignoring drag");
            return;
        }

        if (targetX < 0 || targetX >= W || targetY < 1 || targetY > H)
        {
            Debug.Log($"Target position ({targetX},{targetY}) is out of bounds");
            return;
        }

        BrickScript targetBrick = Board[targetX, targetY];
        if (targetBrick == null || !IsValidBrick(targetBrick))
        {
            Debug.Log($"No valid brick at target position ({targetX},{targetY})");
            return;
        }

        //Debug.Log($"Valid drag swap - starting swap between ({sourceBrick.BoardX},{sourceBrick.BoardY}) and ({targetX},{targetY})");

        if (selectedBrick != null)
        {
            selectedBrick.SetSelected(false);
            selectedBrick = null;
        }

        StartCoroutine(SwapBricksCo(sourceBrick, targetBrick));
    }

    public void OnBrickClicked(BrickScript clickedBrick)
    {
        //Debug.Log($"Brick clicked at ({clickedBrick.BoardX}, {clickedBrick.BoardY})");

        if (!isPlayerTurn)
        {
            //Debug.Log("Not player turn - ignoring click");
            return;
        }

        if (isProcessingSwap)
        {
            //Debug.Log("Processing swap - ignoring click");
            return;
        }

        if (isProcessingMatches)
        {
            //Debug.Log("Processing matches - ignoring click");
            return;
        }

        if (HasActiveMovements)
        {
            //Debug.Log("Bricks still moving - ignoring click");
            return;
        }

        if (clickedBrick == null || !IsValidBrick(clickedBrick))
        {
            //Debug.Log("Clicked brick is invalid - ignoring click");
            return;
        }

        if (clickedBrick.BoardY == 0)
        {
            //Debug.Log("Clicked on spawn row - ignoring click");
            return;
        }

        if (!boardInitialized)
        {
            //Debug.Log("Board not initialized - ignoring click");
            return;
        }

        if (selectedBrick == null)
        {
            //Debug.Log("Selecting first brick");
            selectedBrick = clickedBrick;
            selectedBrick.SetSelected(true);
        }
        else if (selectedBrick == clickedBrick)
        {
            //Debug.Log("Deselecting brick");
            selectedBrick.SetSelected(false);
            selectedBrick = null;
        }
        else if (IsAdjacent(selectedBrick, clickedBrick))
        {
            //Debug.Log($"Adjacent bricks - starting swap between ({selectedBrick.BoardX},{selectedBrick.BoardY}) and ({clickedBrick.BoardX},{clickedBrick.BoardY})");
            StartCoroutine(SwapBricksCo(selectedBrick, clickedBrick));
        }
        else
        {
            //Debug.Log("Not adjacent - selecting new brick");
            selectedBrick.SetSelected(false);
            selectedBrick = clickedBrick;
            selectedBrick.SetSelected(true);
        }
    }

    bool IsAdjacent(BrickScript brick1, BrickScript brick2)
    {
        if (brick1 == null || brick2 == null)
            return false;

        if (!IsValidBrick(brick1) || !IsValidBrick(brick2))
            return false;

        int dx = Mathf.Abs(brick1.BoardX - brick2.BoardX);
        int dy = Mathf.Abs(brick1.BoardY - brick2.BoardY);
        bool adjacent = (dx == 1 && dy == 0) || (dx == 0 && dy == 1);

        return adjacent;
    }

    IEnumerator SwapBricksCo(BrickScript brick1, BrickScript brick2)
    {
        //Debug.Log("=== STARTING SWAP ===");
        isProcessingSwap = true;

        if (selectedBrick != null)
        {
            selectedBrick.SetSelected(false);
            selectedBrick = null;
        }

        if (!IsValidBrick(brick1) || !IsValidBrick(brick2))
        {
            Debug.LogWarning("One or both bricks became invalid during swap!");
            isProcessingSwap = false;
            yield break;
        }

        Vector3 pos1 = GetWorldPosition(brick1.BoardX, brick1.BoardY);
        Vector3 pos2 = GetWorldPosition(brick2.BoardX, brick2.BoardY);

        int originalBrick1X = brick1.BoardX;
        int originalBrick1Y = brick1.BoardY;
        int originalBrick2X = brick2.BoardX;
        int originalBrick2Y = brick2.BoardY;

        SwapBricksInArray(brick1, originalBrick1X, originalBrick1Y, brick2, originalBrick2X, originalBrick2Y);
        brick1.SetBoardPosition(originalBrick2X, originalBrick2Y);
        brick2.SetBoardPosition(originalBrick1X, originalBrick1Y);

        brick1.MoveTo(pos2, movementDuration);
        brick2.MoveTo(pos1, movementDuration);

        yield return StartCoroutine(WaitForAllMovements());

        List<BrickScript> matches = FindAllMatches();

        if (matches.Count == 0)
        {
            //Debug.Log($"No matches created - reverting swap");

            // Fire failed swap attempt event
            Vector2Int pos1Vec = new Vector2Int(originalBrick1X, originalBrick1Y);
            Vector2Int pos2Vec = new Vector2Int(originalBrick2X, originalBrick2Y);
            OnFailedSwapAttempt?.Invoke(brick1, brick2, pos1Vec, pos2Vec);

            SwapBricksInArray(brick1, originalBrick2X, originalBrick2Y, brick2, originalBrick1X, originalBrick1Y);
            brick1.SetBoardPosition(originalBrick1X, originalBrick1Y);
            brick2.SetBoardPosition(originalBrick2X, originalBrick2Y);

            if (IsValidBrick(brick1))
            {
                brick1.MoveTo(pos1, movementDuration);
            }

            if (IsValidBrick(brick2))
            {
                brick2.MoveTo(pos2, movementDuration);
            }

            yield return StartCoroutine(WaitForAllMovements());

            ShowNotification("No match created!", 1.5f);
        }
        else
        {
            //Debug.Log($"Matches created - processing {matches.Count} matches");

            if (!isProcessingMatches)
            {
                StartCoroutine(ProcessMatchesCo());
            }
        }

        isProcessingSwap = false;
        //Debug.Log("=== SWAP COMPLETE - isProcessingSwap now FALSE ===");
    }

    IEnumerator ProcessMatchesCo()
    {
        if (isProcessingMatches)
        {
            Debug.LogWarning("ProcessMatchesCo already running, skipping duplicate call");
            yield break;
        }

        isProcessingMatches = true;

        while (true)
        {
            List<BrickScript> matches = FindAllMatches();

            if (matches.Count == 0)
            {
                break;
            }

            // Fire match events before destroying bricks
            if (matches.Count > 0)
            {
                // Get positions of all matched bricks
                Vector2Int[] removedPositions = new Vector2Int[matches.Count];
                for (int i = 0; i < matches.Count; i++)
                {
                    if (matches[i] != null)
                    {
                        removedPositions[i] = new Vector2Int(matches[i].BoardX, matches[i].BoardY);
                    }
                }

                // Fire event for first valid brick (as representative of the match)
                BrickScript representativeBrick = null;
                foreach (var match in matches)
                {
                    if (match != null && IsValidBrick(match))
                    {
                        representativeBrick = match;
                        break;
                    }
                }

                if (representativeBrick != null)
                {
                    OnMatch?.Invoke(representativeBrick, matches.Count, removedPositions);
                }
            }

            foreach (BrickScript match in matches)
            {
                if (match != null && IsValidBrick(match) &&
                    match.BoardX >= 0 && match.BoardX < W &&
                    match.BoardY >= 0 && match.BoardY <= H)
                {
                    Board[match.BoardX, match.BoardY] = null;
                    Destroy(match.gameObject);
                }
            }

            CleanupNullBricks();

            yield return new WaitForSeconds(0.1f);

            yield return StartCoroutine(SettleAllColumnsCo());
        }

        isProcessingMatches = false;

        yield return new WaitForSeconds(0.2f);

        if (HasCurrentMatches())
        {
            //Debug.Log("Found immediate matches after processing, continuing...");
            StartCoroutine(ProcessMatchesCo());
        }
        else if (ShouldClearBoard())
        {
            Debug.Log("No current matches and no possible moves - clearing and refilling board");
            yield return StartCoroutine(ClearAndRefillBoardCo());
        }
    }

    IEnumerator SettleAllColumnsCo()
    {
        if (isProcessingSettlement)
        {
            //Debug.Log("Settlement already in progress, skipping");
            yield break;
        }

        isProcessingSettlement = true;
        //Debug.Log("=== STARTING SETTLEMENT ===");

        bool anyMovement = true;
        int settlementIterations = 0;
        int maxSettlementIterations = 50;

        while (anyMovement && settlementIterations < maxSettlementIterations)
        {
            settlementIterations++;
            anyMovement = false;

            // Track bricks that have been moved in this settlement pass
            HashSet<BrickScript> movedBricksThisPass = new HashSet<BrickScript>();

            for (int x = 0; x < W; x++)
            {
                for (int y = H; y >= 1; y--)
                {
                    if (Board[x, y] == null)
                    {
                        for (int searchY = y - 1; searchY >= 0; searchY--)
                        {
                            if (Board[x, searchY] != null)
                            {
                                BrickScript movingBrick = Board[x, searchY];

                                if (IsValidBrick(movingBrick) && !movedBricksThisPass.Contains(movingBrick))
                                {
                                    MoveBrickInArray(movingBrick, x, searchY, x, y);
                                    movingBrick.SetBoardPosition(x, y);
                                    movingBrick.MoveTo(GetWorldPosition(x, y), movementDuration);
                                    movedBricksThisPass.Add(movingBrick);
                                    anyMovement = true;
                                }
                                else
                                {
                                    Board[x, searchY] = null;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            if (anyMovement)
            {
                yield return StartCoroutine(WaitForAllMovements());
            }
        }

        if (settlementIterations >= maxSettlementIterations)
        {
            Debug.LogError("Settlement exceeded max iterations! Possible infinite loop prevented.");
        }

        bool stillNeedsSettlement = true;
        int spawnIterations = 0;
        int maxSpawnIterations = 100;

        while (stillNeedsSettlement && spawnIterations < maxSpawnIterations)
        {
            spawnIterations++;
            stillNeedsSettlement = false;

            for (int x = 0; x < W; x++)
            {
                if (Board[x, 0] == null)
                {
                    bool hasEmptySpace = false;
                    for (int y = 1; y <= H; y++)
                    {
                        if (Board[x, y] == null)
                        {
                            hasEmptySpace = true;
                            break;
                        }
                    }

                    if (hasEmptySpace)
                    {
                        BrickScript spawned = SpawnBrick(x);
                        if (spawned != null)
                        {
                            stillNeedsSettlement = true;
                        }
                    }
                }
            }

            bool bricksMoving = true;
            int movementIterations = 0;
            int maxMovementIterations = 50;

            while (bricksMoving && movementIterations < maxMovementIterations)
            {
                movementIterations++;
                bricksMoving = false;

                // Track bricks moved in this inner settlement pass
                HashSet<BrickScript> movedBricksThisInnerPass = new HashSet<BrickScript>();

                for (int x = 0; x < W; x++)
                {
                    for (int y = H; y >= 1; y--)
                    {
                        if (Board[x, y] == null)
                        {
                            for (int searchY = y - 1; searchY >= 0; searchY--)
                            {
                                if (Board[x, searchY] != null)
                                {
                                    BrickScript movingBrick = Board[x, searchY];

                                    if (IsValidBrick(movingBrick) && !movedBricksThisInnerPass.Contains(movingBrick))
                                    {
                                        MoveBrickInArray(movingBrick, x, searchY, x, y);
                                        movingBrick.SetBoardPosition(x, y);
                                        movingBrick.MoveTo(GetWorldPosition(x, y), movementDuration);
                                        movedBricksThisInnerPass.Add(movingBrick);
                                        bricksMoving = true;
                                    }
                                    else
                                    {
                                        Board[x, searchY] = null;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

                if (bricksMoving)
                {
                    yield return StartCoroutine(WaitForAllMovements());
                }
            }

            if (movementIterations >= maxMovementIterations)
            {
                Debug.LogError("Movement settlement exceeded max iterations!");
            }

            yield return null;
        }

        if (spawnIterations >= maxSpawnIterations)
        {
            Debug.LogError("Spawn iterations exceeded max! Possible infinite loop prevented.");
        }

        yield return StartCoroutine(WaitForAllMovements());

        //Debug.Log("=== SETTLEMENT COMPLETE - CHECKING FOR MATCHES ===");

        yield return null;

        if (HasCurrentMatches())
        {
            //Debug.Log("Found matches after settlement, processing...");
            if (!isProcessingMatches)
            {
                StartCoroutine(ProcessMatchesCo());
            }
        }
        else if (ShouldClearBoard())
        {
            Debug.Log("No current matches and no possible moves after settlement - clearing and refilling board");
            isProcessingSettlement = false; // Clear flag before calling ClearAndRefillBoardCo
            yield return StartCoroutine(ClearAndRefillBoardCo());
            yield break; // Exit early since ClearAndRefillBoardCo will handle its own flag clearing
        }
        else
        {
            //Debug.Log("Settlement complete - no new matches");
        }

        isProcessingSettlement = false;
        //Debug.Log("=== SETTLEMENT FLAG CLEARED ===");
    }

    IEnumerator SettleCo()
    {
        if (!boardInitialized)
        {
            Debug.Log("Initializing board...");
            isBoardFilling = true;

            for (int x = 0; x < W; x++)
            {
                for (int y = H; y >= 1; y--)
                {
                    SpawnBrick(x);

                    if (Board[x, 0] != null)
                    {
                        BrickScript movingBrick = Board[x, 0];
                        Board[x, y] = movingBrick;
                        Board[x, 0] = null;
                        movingBrick.SetBoardPosition(x, y);
                        movingBrick.MoveTo(GetWorldPosition(x, y), movementDuration);
                    }
                }
            }

            yield return StartCoroutine(WaitForAllMovements());

            boardInitialized = true;

            yield return new WaitForSeconds(0.1f);

            if (HasCurrentMatches())
            {
                //Debug.Log("Found matches in initial board, processing...");
                if (!isProcessingMatches)
                {
                    StartCoroutine(ProcessMatchesCo());
                }
            }
            else if (ShouldClearBoard())
            {
                Debug.Log("Initial board has no matches and no possible moves - reshuffling");
                yield return StartCoroutine(ClearAndRefillBoardCo());
            }

            isBoardFilling = false;
        }

        while (true)
        {
            if (isProcessingSwap || isProcessingMatches)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}