using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardScript : MonoBehaviour
{
    private const int W = 8;
    private const int H = 8;

    private BrickScript[,] Board = new BrickScript[W, H + 1];

    [SerializeField] private float brickSize = 1f;
    [SerializeField] private float settlementDelay = 0.1f;
    [SerializeField] private float movementDuration = 0.3f;

    public static BoardScript Instance;

    private BrickScript selectedBrick = null;
    private bool isPlayerTurn = true;
    private bool isProcessingSwap = false;
    private bool isProcessingMatches = false;
    private bool boardInitialized = false;

    [Header("UI")]
    public UnityEngine.UI.Text notificationText;

    void Start()
    {
        Instance = this;
        StartCoroutine(SettleCo());
    }

    void Update()
    {

    }

    bool TryFindEmptySquare(out Vector2Int emptySquare)
    {
        emptySquare = Vector2Int.zero;

        for (int y = 1; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (Board[x, y] == null)
                {
                    emptySquare = new Vector2Int(x, y);
                    return true;
                }
            }
        }
        return false;
    }

    BrickScript SpawnBrick(int x)
    {
        if (Board[x, 0] != null)
            return null;

        GameObject prefab = BrickPrefabs.I?.GetRandomBrickPrefab();
        if (prefab == null)
            return null;

        GameObject newBrickObj = Instantiate(prefab, GetWorldPosition(x, 0), Quaternion.identity);
        BrickScript newBrick = newBrickObj.GetComponent<BrickScript>();

        if (newBrick != null)
        {
            newBrick.SetBoardPosition(x, 0);

            if (newBrick.Type == BrickType.Undefined)
            {
                BrickType[] validTypes = { BrickType.Type1, BrickType.Type2, BrickType.Type3, BrickType.Type4, BrickType.Type5, BrickType.Type6, BrickType.Type7 };
                newBrick.Type = validTypes[Random.Range(0, validTypes.Length)];
            }

            Board[x, 0] = newBrick;
        }

        return newBrick;
    }

    void MoveBrick(BrickScript brick, int newX, int newY)
    {
        if (brick == null)
            return;

        Board[brick.BoardX, brick.BoardY] = null;
        Board[newX, newY] = brick;

        brick.SetBoardPosition(newX, newY);
        brick.MoveTo(GetWorldPosition(newX, newY));
    }

    Vector3 GetWorldPosition(int x, int y)
    {
        // Use negative Z to ensure bricks are in front of background, with slight depth variation
        float z = -0.1f - (y * 0.001f);
        return new Vector3(x * brickSize, -y * brickSize, z);
    }

    List<BrickScript> FindAllMatches()
    {
        List<BrickScript> matches = new List<BrickScript>();
        bool[,] processed = new bool[W, H + 1];

        for (int y = 1; y <= H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (Board[x, y] != null && !processed[x, y])
                {
                    List<BrickScript> horizontalMatch = FindHorizontalMatch(x, y);
                    if (horizontalMatch.Count >= 3)
                    {
                        foreach (var brick in horizontalMatch)
                        {
                            if (!matches.Contains(brick) && brick.BoardX >= 0 && brick.BoardX < W && brick.BoardY >= 0 && brick.BoardY <= H)
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
                            if (!matches.Contains(brick) && brick.BoardX >= 0 && brick.BoardX < W && brick.BoardY >= 0 && brick.BoardY <= H)
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

        if (startBrick == null) return match;

        int leftX = startX;
        while (leftX > 0 && Board[leftX - 1, startY] != null && BrickTypesMatch(startBrick, Board[leftX - 1, startY]))
        {
            leftX--;
        }

        for (int x = leftX; x < W; x++)
        {
            BrickScript currentBrick = Board[x, startY];
            if (currentBrick != null && BrickTypesMatch(startBrick, currentBrick))
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

        if (startBrick == null) return match;

        int topY = startY;
        while (topY > 1 && Board[startX, topY - 1] != null && BrickTypesMatch(startBrick, Board[startX, topY - 1]))
        {
            topY--;
        }

        for (int y = topY; y <= H; y++)
        {
            BrickScript currentBrick = Board[startX, y];
            if (currentBrick != null && BrickTypesMatch(startBrick, currentBrick))
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
        if (brick1 == null || brick2 == null) return false;
        return brick1.Type == brick2.Type && brick1.Type != BrickType.Undefined;
    }

    bool HasPossibleMoves()
    {
        for (int y = 1; y <= H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (Board[x, y] != null)
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

    bool CanSwapCreateMatch(int x1, int y1, int x2, int y2)
    {
        if (x2 < 0 || x2 >= W || y2 < 1 || y2 > H) return false;
        if (Board[x1, y1] == null || Board[x2, y2] == null) return false;

        BrickScript brick1 = Board[x1, y1];
        BrickScript brick2 = Board[x2, y2];

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

        for (int y = H; y >= 1; y--)
        {
            for (int x = 0; x < W; x++)
            {
                SpawnBrick(x);
                yield return new WaitForSeconds(0.02f);
            }

            bool anyMovement = true;
            while (anyMovement)
            {
                anyMovement = false;
                for (int col = 0; col < W; col++)
                {
                    for (int row = H; row >= 1; row--)
                    {
                        if (Board[col, row] == null && Board[col, 0] != null)
                        {
                            BrickScript movingBrick = Board[col, 0];
                            Board[col, row] = movingBrick;
                            Board[col, 0] = null;

                            movingBrick.SetBoardPosition(col, row);
                            movingBrick.MoveTo(GetWorldPosition(col, row), movementDuration);

                            anyMovement = true;
                            break;
                        }
                    }
                }
                yield return new WaitForSeconds(movementDuration + 0.05f);
            }
        }

        List<BrickScript> immediateMatches = FindAllMatches();
        if (immediateMatches.Count > 0)
        {
            StartCoroutine(ProcessMatchesCo());
        }
        else
        {
            ShowNotification("New board ready!", 2f);
        }
    }


    public void OnBrickClicked(BrickScript clickedBrick)
    {
        Debug.Log($"Brick clicked at ({clickedBrick.BoardX}, {clickedBrick.BoardY})");

        if (!isPlayerTurn)
        {
            Debug.Log("Not player turn - ignoring click");
            return;
        }
        if (isProcessingSwap)
        {
            Debug.Log("Processing swap - ignoring click");
            return;
        }
        if (isProcessingMatches)
        {
            Debug.Log("Processing matches - ignoring click");
            return;
        }
        if (clickedBrick.BoardY == 0)
        {
            Debug.Log("Clicked on spawn row - ignoring click");
            return;
        }
        if (!boardInitialized)
        {
            Debug.Log("Board not initialized - ignoring click");
            return;
        }

        if (selectedBrick == null)
        {
            Debug.Log("Selecting first brick");
            selectedBrick = clickedBrick;
            selectedBrick.SetSelected(true);
        }
        else if (selectedBrick == clickedBrick)
        {
            Debug.Log("Deselecting brick");
            selectedBrick.SetSelected(false);
            selectedBrick = null;
        }
        else if (IsAdjacent(selectedBrick, clickedBrick))
        {
            Debug.Log($"Adjacent bricks - starting swap between ({selectedBrick.BoardX},{selectedBrick.BoardY}) and ({clickedBrick.BoardX},{clickedBrick.BoardY})");
            StartCoroutine(SwapBricksCo(selectedBrick, clickedBrick));
        }
        else
        {
            Debug.Log("Not adjacent - selecting new brick");
            selectedBrick.SetSelected(false);
            selectedBrick = clickedBrick;
            selectedBrick.SetSelected(true);
        }
    }

    bool IsAdjacent(BrickScript brick1, BrickScript brick2)
    {
        int dx = Mathf.Abs(brick1.BoardX - brick2.BoardX);
        int dy = Mathf.Abs(brick1.BoardY - brick2.BoardY);
        bool adjacent = (dx == 1 && dy == 0) || (dx == 0 && dy == 1);

        Debug.Log($"Adjacency check: Brick1({brick1.BoardX},{brick1.BoardY}) vs Brick2({brick2.BoardX},{brick2.BoardY}) -> dx={dx}, dy={dy}, adjacent={adjacent}");

        return adjacent;
    }

    IEnumerator SwapBricksCo(BrickScript brick1, BrickScript brick2)
    {
        isProcessingSwap = true;
        brick1.SetSelected(false);
        selectedBrick = null;

        Vector3 pos1 = GetWorldPosition(brick1.BoardX, brick1.BoardY);
        Vector3 pos2 = GetWorldPosition(brick2.BoardX, brick2.BoardY);

        int originalBrick1X = brick1.BoardX;
        int originalBrick1Y = brick1.BoardY;
        int originalBrick2X = brick2.BoardX;
        int originalBrick2Y = brick2.BoardY;

        Board[brick1.BoardX, brick1.BoardY] = brick2;
        Board[brick2.BoardX, brick2.BoardY] = brick1;

        brick1.SetBoardPosition(brick2.BoardX, brick2.BoardY);
        brick2.SetBoardPosition(originalBrick1X, originalBrick1Y);

        brick1.MoveTo(pos2, movementDuration);
        brick2.MoveTo(pos1, movementDuration);

        yield return new WaitForSeconds(movementDuration);

        List<BrickScript> matches = FindAllMatches();

        if (matches.Count == 0)
        {
            Debug.Log($"No matches created - reverting swap");

            Board[originalBrick2X, originalBrick2Y] = brick2;
            Board[originalBrick1X, originalBrick1Y] = brick1;

            brick1.SetBoardPosition(originalBrick1X, originalBrick1Y);
            brick2.SetBoardPosition(originalBrick2X, originalBrick2Y);

            brick1.MoveTo(pos1, movementDuration);
            brick2.MoveTo(pos2, movementDuration);

            yield return new WaitForSeconds(movementDuration);

            ShowNotification("No match created!", 1.5f);
        }
        else
        {
            Debug.Log($"Matches created - processing {matches.Count} matches");
            StartCoroutine(ProcessMatchesCo());
        }

        isProcessingSwap = false;
    }

    IEnumerator ProcessMatchesCo()
    {
        isProcessingMatches = true;

        while (true)
        {
            List<BrickScript> matches = FindAllMatches();
            if (matches.Count == 0)
            {
                break;
            }


            foreach (BrickScript match in matches)
            {
                Board[match.BoardX, match.BoardY] = null;
                Destroy(match.gameObject);
            }

            yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(SettleAllColumnsCo());

            yield return new WaitForSeconds(0.3f);
        }

        isProcessingMatches = false;

        yield return new WaitForSeconds(1f);

        if (!HasPossibleMoves())
        {
            StartCoroutine(ClearAndRefillBoardCo());
        }
    }

    IEnumerator SettleAllColumnsCo()
    {
        bool anyMovement = true;
        while (anyMovement)
        {
            anyMovement = false;

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
                                Board[x, y] = movingBrick;
                                Board[x, searchY] = null;

                                movingBrick.SetBoardPosition(x, y);
                                movingBrick.MoveTo(GetWorldPosition(x, y), movementDuration);

                                anyMovement = true;
                                break;
                            }
                        }
                    }
                }

                for (int y = H; y >= 1; y--)
                {
                    if (Board[x, y] == null)
                    {
                        if (Board[x, 0] != null)
                        {
                            BrickScript movingBrick = Board[x, 0];
                            Board[x, y] = movingBrick;
                            Board[x, 0] = null;

                            movingBrick.SetBoardPosition(x, y);
                            movingBrick.MoveTo(GetWorldPosition(x, y), movementDuration);

                            anyMovement = true;
                        }
                        else
                        {
                            SpawnBrick(x);
                            anyMovement = true;
                        }
                        break;
                    }
                }
            }

            if (anyMovement)
            {
                yield return new WaitForSeconds(movementDuration + settlementDelay);
            }
        }
    }

    IEnumerator SettleCo()
    {
        if (!boardInitialized)
        {
            Debug.Log("Initializing board...");
            for (int y = H; y >= 1; y--)
            {
                for (int x = 0; x < W; x++)
                {
                    SpawnBrick(x);
                    yield return new WaitForSeconds(0.02f);
                }

                bool anyMovement = true;
                while (anyMovement)
                {
                    anyMovement = false;
                    for (int col = 0; col < W; col++)
                    {
                        for (int row = H; row >= 1; row--)
                        {
                            if (Board[col, row] == null && Board[col, 0] != null)
                            {
                                BrickScript movingBrick = Board[col, 0];
                                Board[col, 0] = null;
                                Board[col, row] = movingBrick;
                                movingBrick.SetBoardPosition(col, row);
                                movingBrick.MoveTo(GetWorldPosition(col, row), movementDuration);
                                anyMovement = true;
                                break;
                            }
                        }
                    }
                    yield return new WaitForSeconds(movementDuration + 0.05f);
                }
            }
            boardInitialized = true;

            yield return new WaitForSeconds(0.3f);

            List<BrickScript> initialMatches = FindAllMatches();
            if (initialMatches.Count > 0)
            {
                StartCoroutine(ProcessMatchesCo());
            }
            else if (!HasPossibleMoves())
            {
                StartCoroutine(ClearAndRefillBoardCo());
            }
        }

        while (true)
        {
            if (isProcessingSwap || isProcessingMatches)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
