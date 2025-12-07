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

    bool isPlaying = false; // Start as false for initial start screen
    bool IsFirstStart = true; // Track if this is the first start

    public TMP_Text TextGameOver;
    public TMP_Text TextScore;

    [Header("Sound Effects")]
    public AudioSource audioSource;
    public AudioClip soundMatch3;
    public AudioClip soundMatchBig;
    public AudioClip soundBlockPop;
    public AudioClip soundGameOver;

    int score = 0;

    private Vector3 startPosition;
    private float timeOffset;
    private Vector3 previousSpawnPointPosition;

    public void GameOver()
    {
        TextGameOver.enabled = true;
        Time.timeScale = 0.00001f;
        isPlaying = false;
        TextGameOver.text = "GAME OVER\n<size=-10>PRESS SPACE TO BEGIN";

        // Play game over sound
        if (audioSource != null && soundGameOver != null)
        {
            audioSource.PlayOneShot(soundGameOver);
        }
    }

    void ShowInitialStartScreen()
    {
        TextGameOver.enabled = true;
        TextGameOver.text = "<size=-10>PRESS SPACE TO BEGIN";
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
    }

    private void Start()
    {
        startPosition = BallSpawnPoint.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        previousSpawnPointPosition = BallSpawnPoint.position;

        // Show initial start screen
        ShowInitialStartScreen();
    }

    void Update()
    {
        if (!isPlaying && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space pressed");
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

        previousSpawnPointPosition = BallSpawnPoint.position;
        float oscillation = Mathf.Sin(Time.time * moveSpeed + timeOffset);
        BallSpawnPoint.position = startPosition + Vector3.right * (oscillation * moveRange);
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

        // Start the board settlement
        BoardScript.Instance.StartCoroutine("SettleCo");
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

        // Reset board state before starting new settlement
        BoardScript.Instance.boardInitialized = false;
        BoardScript.Instance.StartCoroutine("SettleCo");
    }

    void UpdateScoreText()
    {
        TextScore.text = $"Score: {score}";
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreText();
    }

    public void PlayBlockPopSound()
    {
        if (audioSource != null && soundBlockPop != null)
        {
            audioSource.PlayOneShot(soundBlockPop);
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

    private void HandleMatch(BrickScript representativeBrick, int matchCount, Vector2Int[] removedPositions)
    {
        // Play match sound
        if (audioSource != null)
        {
            if (matchCount == 3 && soundMatch3 != null)
            {
                audioSource.PlayOneShot(soundMatch3);
            }
            else if (matchCount > 3 && soundMatchBig != null)
            {
                audioSource.PlayOneShot(soundMatchBig);
            }
        }

        if (BallPrefab != null && BallSpawnPoint != null && removedPositions.Length > 0)
        {
            SpawnBallsInPattern(removedPositions, representativeBrick);
        }
    }

    private void SpawnBallsInPattern(Vector2Int[] positions, BrickScript representativeBrick)
    {
        if (positions.Length == 0) return;

        // Determine how many balls to spawn per position based on match count
        int ballsPerPosition = 1;
        if (positions.Length >= 5)
        {
            ballsPerPosition = 3;
        }
        else if (positions.Length >= 4)
        {
            ballsPerPosition = 2;
        }

        // Find the center of the matched pattern
        Vector2 center = Vector2.zero;
        foreach (var pos in positions)
        {
            center += new Vector2(pos.x, pos.y);
        }
        center /= positions.Length;

        // Spawn balls relative to BallSpawnPoint, maintaining the match pattern
        Vector3 basePosition = BallSpawnPoint.position;

        foreach (var pos in positions)
        {
            // Calculate offset from pattern center
            Vector2 offset = new Vector2(pos.x, pos.y) - center;

            // Apply spacing and spawn at BallSpawnPoint + offset
            Vector3 spawnPos = basePosition + new Vector3(offset.x * ballSpacing, offset.y * ballSpacing, 0);

            // Spawn multiple balls with slight X offset
            for (int i = 0; i < ballsPerPosition; i++)
            {
                // Adjust X position by 0.05 per ball
                Vector3 adjustedSpawnPos = spawnPos + new Vector3(i * 0.2f, 0, 0);

                // Clamp spawn position to stay within the movement range (4 units left/right of start)
                float clampedX = Mathf.Clamp(adjustedSpawnPos.x, startPosition.x - moveRange, startPosition.x + moveRange);
                adjustedSpawnPos.x = clampedX;

                GameObject ball = Instantiate(BallPrefab, adjustedSpawnPos, Quaternion.identity, parent: transform);
                ball.GetComponent<SpriteRenderer>().color = Color.Lerp(representativeBrick.Color, Color.black, 0.5f);

                // Set ball type to match brick type
                BallScript ballScript = ball.GetComponent<BallScript>();
                ballScript.ballType = representativeBrick.Type;

                // Set light color to match brick color
                if (ball.TryGetComponent<Light2D>(out var ballLight))
                {
                    ballLight.color = representativeBrick.Color;
                }

                // Destroy ball after X seconds
                Destroy(ball, 10f);

                // Add physics and set velocity
                if (ball.TryGetComponent<Rigidbody2D>(out var rb2d))
                {
                    // Calculate spawn point's current velocity using derivative of sin function
                    float currentVelocity = Mathf.Cos(Time.time * moveSpeed + timeOffset) * moveSpeed * moveRange;
                    float horizontalVelocity = currentVelocity * ballLaunchHorzVelocityMultiplier;

                    rb2d.linearVelocity = new Vector2(horizontalVelocity, 0f);
                }
                else if (ball.TryGetComponent<Rigidbody>(out var rb3d))
                {
                    // Calculate spawn point's current velocity using derivative of sin function
                    float currentVelocity = Mathf.Cos(Time.time * moveSpeed + timeOffset) * moveSpeed * moveRange;
                    float horizontalVelocity = currentVelocity * ballLaunchHorzVelocityMultiplier;

                    rb3d.linearVelocity = new Vector3(horizontalVelocity, 0f, 0f);
                }
            }
        }
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
