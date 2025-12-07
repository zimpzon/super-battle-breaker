using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameScript : MonoBehaviour
{
    public GameObject BallPrefab;
    public Transform BallSpawnPoint;
    [SerializeField] private float ballSpacing = 0.1f;
    [SerializeField] private float moveRange = 4f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float velocityMultiplier = 0.75f;

    private Vector3 startPosition;
    private float timeOffset;
    private Vector3 previousSpawnPointPosition;

    private void Start()
    {
        if (BallSpawnPoint != null)
        {
            startPosition = BallSpawnPoint.position;
            timeOffset = Random.Range(0f, 2f * Mathf.PI);
            previousSpawnPointPosition = BallSpawnPoint.position;
        }
    }

    private void OnEnable()
    {
        BoardScript.OnMatch += HandleMatch;
        BoardScript.OnBoardReset += HandleBoardReset;
        BoardScript.OnFailedSwapAttempt += HandleFailedSwap;
    }

    private void Update()
    {
        if (BallSpawnPoint != null)
        {
            previousSpawnPointPosition = BallSpawnPoint.position;
            float oscillation = Mathf.Sin(Time.time * moveSpeed + timeOffset);
            BallSpawnPoint.position = startPosition + Vector3.right * (oscillation * moveRange);
        }
    }

    private void OnDisable()
    {
        BoardScript.OnMatch -= HandleMatch;
        BoardScript.OnBoardReset -= HandleBoardReset;
        BoardScript.OnFailedSwapAttempt -= HandleFailedSwap;
    }

    private void HandleMatch(BrickScript representativeBrick, int matchCount, Vector2Int[] removedPositions)
    {
        if (BallPrefab != null && BallSpawnPoint != null && removedPositions.Length > 0)
        {
            SpawnBallsInPattern(removedPositions, representativeBrick);
        }
    }

    private void SpawnBallsInPattern(Vector2Int[] positions, BrickScript representativeBrick)
    {
        if (positions.Length == 0) return;

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

            // Clamp spawn position to stay within the movement range (4 units left/right of start)
            float clampedX = Mathf.Clamp(spawnPos.x, startPosition.x - moveRange, startPosition.x + moveRange);
            spawnPos.x = clampedX;

            GameObject ball = Instantiate(BallPrefab, spawnPos, Quaternion.identity, parent: transform);
            ball.GetComponent<SpriteRenderer>().color = Color.Lerp(representativeBrick.Color, Color.black, 0.5f);

            // Set light color to match brick color
            if (ball.TryGetComponent<Light2D>(out var ballLight))
            {
                ballLight.color = representativeBrick.Color;
            }

            // Add physics and set velocity
            if (ball.TryGetComponent<Rigidbody2D>(out var rb2d))
            {
                // Calculate spawn point's current velocity using derivative of sin function
                float currentVelocity = Mathf.Cos(Time.time * moveSpeed + timeOffset) * moveSpeed * moveRange;
                float horizontalVelocity = currentVelocity * velocityMultiplier;

                rb2d.linearVelocity = new Vector2(horizontalVelocity, 0f);
            }
            else if (ball.TryGetComponent<Rigidbody>(out var rb3d))
            {
                // Calculate spawn point's current velocity using derivative of sin function
                float currentVelocity = Mathf.Cos(Time.time * moveSpeed + timeOffset) * moveSpeed * moveRange;
                float horizontalVelocity = currentVelocity * velocityMultiplier;

                rb3d.linearVelocity = new Vector3(horizontalVelocity, 0f, 0f);
            }

            // Optional: set ball color to match the brick that was matched
            if (ball.TryGetComponent<Renderer>(out var renderer))
            {
                // You can access the representative brick's color if needed
                // renderer.material.color = representativeBrick.Color;
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
