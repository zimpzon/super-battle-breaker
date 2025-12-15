using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameScript : MonoBehaviour
{
    public static GameScript I;

    public GameObject BallPrefab;

    public Transform BallSpawnPoint;
    [SerializeField] private float ballSpacing = 0.1f;
    [SerializeField] private float moveRange = 4f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float ballLaunchHorzVelocityMultiplier = 0.75f;

    public bool IsPlaying => isPlaying;
    public int BestScore => bestScore;

    bool isPlaying = false; // Start as false for initial start screen
    bool IsFirstStart = true; // Track if this is the first start

    public TMP_Text TextGameOver;
    public TMP_Text TextScore;

    [Header("Sound Effects")]
    public AudioSource audioSourceMusic;
    public AudioSource audioSourceSfx;
    public AudioClip soundMatch3;
    public AudioClip soundMatchBig;
    public AudioClip soundBlockPop;
    public AudioClip soundGameOver;
    public AudioClip music;

    int score = 0;
    int bestScore = 0;

    private Vector3 startPosition;
    private float timeOffset;
    private Vector3 previousSpawnPointPosition;
    private BlockBoardScript blockBoard;

    public void GameOver()
    {
        // Update best score if needed
        bool isNewBestScore = false;
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
            isNewBestScore = true;
        }

        TextGameOver.enabled = true;
        Time.timeScale = 0.00001f;
        isPlaying = false;

        var sb = new StringBuilder();
        sb.AppendLine("GAME OVER");
        if (isNewBestScore)
            sb.AppendLine("<size=-5><color=yellow>New best score!</color>");

        sb.AppendLine("<size=-10>PRESS SPACE TO BEGIN");
        sb.AppendLine();
        sb.AppendLine("<size=-25>Write your best score in the comments!");

        TextGameOver.text = sb.ToString();

        audioSourceSfx.PlayOneShot(soundGameOver);

        var stats = new Dictionary<string, int> { { "score", score }, { "best_score", bestScore } };
        Playfab.PlayerStat(stats);
    }

    void ShowInitialStartScreen()
    {
        TextGameOver.enabled = true;
        TextGameOver.text = "<size=-10>PRESS SPACE TO BEGIN\n\n<size=-25>Write your best score in the comments!";
        Time.timeScale = 0.00001f; // Keep normal time scale for input detection
    }

    void ClearAll()
    {
        // Clear all balls
        BallScript[] balls = FindObjectsOfType<BallScript>();
        foreach (BallScript ball in balls)
        {
            Destroy(ball.gameObject);
        }

        // Clear all bricks
        BrickScript[] bricks = FindObjectsOfType<BrickScript>();
        foreach (BrickScript brick in bricks)
        {
            Destroy(brick.gameObject);
        }

        // Clear all blocks
        BlockScript[] blocks = FindObjectsOfType<BlockScript>();
        foreach (BlockScript block in blocks)
        {
            Destroy(block.gameObject);
        }
    }

    private void Awake()
    {
        I = this;
        Playfab.Login();
    }

    private void Start()
    {
        audioSourceMusic.clip = music;
        audioSourceMusic.loop = true;
        audioSourceMusic.volume = 0.10f;
        audioSourceMusic.Play();
        audioSourceSfx.volume = 0.25f;

        // Load best score from PlayerPrefs
        bestScore = PlayerPrefs.GetInt("BestScore", 0);

        startPosition = BallSpawnPoint.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        previousSpawnPointPosition = BallSpawnPoint.position;

        // Find BlockBoardScript
        blockBoard = FindObjectOfType<BlockBoardScript>();

        // Show initial start screen
        ShowInitialStartScreen();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && isPlaying)
        {
            GameOver();
            return;
        }

        if (!isPlaying && Input.GetKeyDown(KeyCode.Space))
        {
            if (!IsFirstStart)
            {
                // First time starting
                StartNewGame();
            }
            else
            {
                // Restarting after game over
                RestartGame();
            }
        }

        // Always move the ball spawner, even during game over screens
        if (BallSpawnPoint != null)
        {
            float oscillation = Mathf.Sin(Time.unscaledTime * moveSpeed + timeOffset);
            BallSpawnPoint.position = startPosition + Vector3.right * (oscillation * moveRange);
        }
    }

    void StartNewGame()
    {
        IsFirstStart = false;
        isPlaying = true;
        TextGameOver.enabled = false;
        Time.timeScale = 1f;

        // Reset score
        score = 0;
        UpdateScoreText();

        // Reset block advancement
        if (blockBoard != null)
        {
            blockBoard.ResetAdvancement();
        }

        // Start the board settlement
        if (BoardScript.Instance != null)
        {
            BoardScript.Instance.StartSettlementLoop();
        }
    }

    void RestartGame()
    {
        ClearAll();
        isPlaying = true;
        TextGameOver.enabled = false;
        Time.timeScale = 1f;

        // Reset score
        score = 0;
        UpdateScoreText();

        // Reset block advancement
        if (blockBoard != null)
        {
            blockBoard.ResetAdvancement();
        }

        // Reset board state before starting new settlement
        if (BoardScript.Instance != null)
        {
            BoardScript.Instance.ResetBoardState();
            BoardScript.Instance.StartSettlementLoop();
        }
    }

    void UpdateScoreText()
    {
        TextScore.text = $"Score: {score}<color=#888888>\n<size=-4>Best: {GameScript.I.BestScore}";
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreText();
    }

    public void PlayBlockPopSound()
    {
        if (audioSourceMusic != null && soundBlockPop != null)
        {
            audioSourceMusic.PlayOneShot(soundBlockPop);
        }
    }

    private void OnEnable()
    {
        BoardScript.OnMatch += HandleMatch;
        BoardScript.OnBoardReset += HandleBoardReset;
        BoardScript.OnFailedSwapAttempt += HandleFailedSwap;
    }


    private void OnDisable()
    {
        BoardScript.OnMatch -= HandleMatch;
        BoardScript.OnBoardReset -= HandleBoardReset;
        BoardScript.OnFailedSwapAttempt -= HandleFailedSwap;
    }

    private void HandleMatch(BrickScript representativeBrick, int matchCount, Vector2Int[] removedPositions, bool isPlayerInitiated, int groupIndex)
    {
        // Play match sound
        if (audioSourceMusic != null)
        {
            if (matchCount == 3 && soundMatch3 != null)
            {
                audioSourceSfx.PlayOneShot(soundMatch3);
            }
            else if (matchCount > 3 && soundMatchBig != null)
            {
                CameraShake.I.Shake();
                audioSourceSfx.PlayOneShot(soundMatchBig);
            }
        }

        if (BallPrefab != null && BallSpawnPoint != null && removedPositions.Length > 0)
        {
            SpawnBallsInPattern(removedPositions, representativeBrick, groupIndex);
        }

        // Only trigger block advancement for player-initiated matches
        if (isPlayerInitiated)
        {
            TriggerBlockAdvancement();
        }
    }

    private void TriggerBlockAdvancement()
    {
        if (blockBoard != null)
        {
            blockBoard.AdvanceBlocks();
        }
    }

    private void SpawnBallsInPattern(Vector2Int[] positions, BrickScript representativeBrick, int groupIndex = 0)
    {
        if (positions.Length == 0) return;

        // For 4+ matches: spawn matched color balls + random color balls
        bool is4PlusMatch = positions.Length >= 4;

        // Find the center of the matched pattern
        Vector2 center = Vector2.zero;
        foreach (var pos in positions)
        {
            center += new Vector2(pos.x, pos.y);
        }
        center /= positions.Length;

        // Spawn balls relative to BallSpawnPoint, maintaining the match pattern
        Vector3 basePosition = BallSpawnPoint.position;

        // Apply group offset to space apart multiple match groups
        float groupSpacing = 0.1f; // Distance between match groups
        basePosition.x += groupIndex * groupSpacing;

        BrickType bonusType = BrickType.Undefined;
        Color bonusColor = representativeBrick.Color;
        if (is4PlusMatch)
        {
            bonusType = GetRandomAlternateBrickType(representativeBrick.Type);
            bonusColor = GetColorForBrickType(bonusType, representativeBrick.Color);
            if (bonusType == representativeBrick.Type || ColorsApproximatelyEqual(bonusColor, representativeBrick.Color))
            {
                bonusColor = GetRandomDistinctColor(representativeBrick.Color, representativeBrick.Type);
            }
        }

        foreach (var pos in positions)
        {
            // Calculate offset from pattern center
            Vector2 offset = new Vector2(pos.x, pos.y) - center;

            // Apply spacing and spawn at BallSpawnPoint + offset
            Vector3 spawnPos = basePosition + new Vector3(offset.x * ballSpacing, offset.y * ballSpacing, 0);

            // Always spawn one matched-color ball per cleared brick
            SpawnBallSet(spawnPos, 1, representativeBrick.Type, representativeBrick.Color, 0f);

            if (is4PlusMatch)
            {
                // Spawn random colored balls from other prefabs (not the matched color)
                SpawnBallSet(spawnPos, 1, bonusType, bonusColor, 0.2f);
            }
        }
    }

    private void SpawnBallSet(Vector3 spawnPos, int count, BrickType ballType, Color brickColor, float initialXOffset)
    {
        for (int i = 0; i < count; i++)
        {
            float offset = initialXOffset + (i * 0.2f);
            Vector3 adjustedSpawnPos = spawnPos + new Vector3(offset, 0, 0);

            // Clamp spawn position to stay within the movement range (4 units left/right of start)
            float clampedX = Mathf.Clamp(adjustedSpawnPos.x, startPosition.x - moveRange, startPosition.x + moveRange);
            adjustedSpawnPos.x = clampedX;

            SpawnBallInstance(adjustedSpawnPos, ballType, brickColor);
        }
    }

    private void SpawnBallInstance(Vector3 spawnPos, BrickType ballType, Color brickColor)
    {
        GameObject ball = Instantiate(BallPrefab, spawnPos, Quaternion.identity, parent: transform);
        Color ballColor = Color.Lerp(brickColor, Color.black, 0.2f);
        ball.GetComponent<SpriteRenderer>().color = ballColor;

        // Set ball type to match brick type
        BallScript ballScript = ball.GetComponent<BallScript>();
        ballScript.ballType = ballType;

        // Set light color to match brick color
        if (ball.TryGetComponent<Light2D>(out var ballLight))
        {
            ballLight.color = brickColor;
        }

        // Set trail renderer color darker than ball color
        if (ball.TryGetComponent<TrailRenderer>(out var trailRenderer))
        {
            Color trailColor = Color.Lerp(ballColor, Color.black, 0.5f);
            trailRenderer.startColor = trailColor;
            trailRenderer.endColor = trailColor;
        }

        // Destroy ball after X seconds
        Destroy(ball, 10f);

        // Add physics and set velocity
        if (ball.TryGetComponent<Rigidbody2D>(out var rb2d))
        {
            // Calculate instantaneous velocity from movement equation
            float currentVelocity = Mathf.Cos(Time.unscaledTime * moveSpeed + timeOffset) * moveSpeed * moveRange;
            Vector2 ballVelocity = new Vector2(currentVelocity * ballLaunchHorzVelocityMultiplier, 0f);

            rb2d.linearVelocity = ballVelocity;
        }
        else if (ball.TryGetComponent<Rigidbody>(out var rb3d))
        {
            // Calculate instantaneous velocity from movement equation
            float currentVelocity = Mathf.Cos(Time.unscaledTime * moveSpeed + timeOffset) * moveSpeed * moveRange;
            Vector3 ballVelocity = new Vector3(currentVelocity * ballLaunchHorzVelocityMultiplier, 0f, 0f);

            rb3d.linearVelocity = ballVelocity;
        }
    }

    private BrickType GetRandomBrickType()
    {
        GameObject randomPrefab = BrickPrefabs.I?.GetRandomBrickPrefab();
        if (randomPrefab != null)
        {
            BrickScript brick = randomPrefab.GetComponent<BrickScript>();
            if (brick != null && brick.Type != BrickType.Undefined)
            {
                return brick.Type;
            }
        }

        // Fallback to any valid type
        return BrickType.Type1;
    }

    private BrickType GetRandomAlternateBrickType(BrickType originalType)
    {
        if (BrickPrefabs.I == null || BrickPrefabs.I.Prefabs == null || BrickPrefabs.I.Prefabs.Count == 0)
        {
            return originalType;
        }

        List<BrickType> availableTypes = new List<BrickType>();
        for (int i = 0; i < BrickPrefabs.I.Prefabs.Count; i++)
        {
            if (i == 0) continue; // Skip index 0 per project convention
            var prefab = BrickPrefabs.I.Prefabs[i];
            if (prefab == null) continue;
            var brick = prefab.GetComponent<BrickScript>();
            if (brick == null) continue;

            if (brick.Type != BrickType.Undefined && brick.Type != originalType && !availableTypes.Contains(brick.Type))
            {
                availableTypes.Add(brick.Type);
            }
        }

        if (availableTypes.Count == 0)
        {
            return originalType;
        }

        int randomIndex = UnityEngine.Random.Range(0, availableTypes.Count);
        return availableTypes[randomIndex];
    }

    private Color GetColorForBrickType(BrickType brickType, Color fallbackColor)
    {
        if (BrickPrefabs.I != null && BrickPrefabs.I.Prefabs != null)
        {
            foreach (var prefab in BrickPrefabs.I.Prefabs)
            {
                if (prefab == null) continue;
                var brick = prefab.GetComponent<BrickScript>();
                if (brick != null && brick.Type == brickType)
                {
                    Color color = brick.Color;
                    return ColorsApproximatelyEqual(color, fallbackColor)
                        ? GetRandomDistinctColor(fallbackColor, brickType)
                        : color;
                }
            }
        }

        return GetRandomDistinctColor(fallbackColor, brickType);
    }

    private Color GetRandomDistinctColor(Color avoidColor, BrickType avoidType = BrickType.Undefined)
    {
        List<Color> availableColors = GetPrefabColorsExcluding(avoidColor, avoidType);
        if (availableColors.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, availableColors.Count);
            return availableColors[index];
        }

        // No alternate prefab colors found; fall back to the original color
        return avoidColor;
    }

    private List<Color> GetPrefabColorsExcluding(Color avoidColor, BrickType avoidType)
    {
        List<Color> colors = new List<Color>();
        if (BrickPrefabs.I == null || BrickPrefabs.I.Prefabs == null) return colors;

        for (int i = 0; i < BrickPrefabs.I.Prefabs.Count; i++)
        {
            if (i == 0) continue; // Skip index 0 per project convention
            var prefab = BrickPrefabs.I.Prefabs[i];
            if (prefab == null) continue;
            var brick = prefab.GetComponent<BrickScript>();
            if (brick == null) continue;
            if (brick.Type == BrickType.Undefined || brick.Type == avoidType) continue;

            Color color = brick.Color;
            if (ColorsApproximatelyEqual(color, avoidColor)) continue;

            if (!colors.Contains(color))
            {
                colors.Add(color);
            }
        }

        return colors;
    }

    private bool ColorsApproximatelyEqual(Color a, Color b)
    {
        const float tolerance = 0.01f;
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }

    private void HandleBoardReset()
    {
        Debug.Log("Board reset - no moves available!");
        // Add your board reset handling logic here
    }

    private void HandleFailedSwap(BrickScript brick1, BrickScript brick2, Vector2Int pos1, Vector2Int pos2)
    {
        Debug.Log($"Failed swap attempt between {brick1.Type} at {pos1} and {brick2.Type} at {pos2}");
        // Add your failed swap handling logic here (negative feedback, etc.)
    }
}
