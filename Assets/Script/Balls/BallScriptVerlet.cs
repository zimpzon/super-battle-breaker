using UnityEngine;

public class BallScriptVerlet : MonoBehaviour
{
    [Header("Initial Launch")]
    public Vector2 startDirection = new Vector2(1, 1);
    public float startForce = 5f;

    [Header("Physics Settings")]
    public float gravityDown = 9.81f;
    public float gravityUp = 9.81f;
    public float drag = 0.02f;
    public float bounciness = 0.8f;
    public float bouncePower = 1.0f;
    public float minBounceVelocity = 0.15f;
    public float groundAngleLimit = 45f;

    private Vector2 currentPosition;
    private Vector2 previousPosition;
    private CircleCollider2D circle;

    private float radius;
    private int wallMask;
    private int ballMask;

    void Start()
    {
        circle = GetComponent<CircleCollider2D>();
        radius = circle.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);

        wallMask = LayerMask.GetMask("StaticLevel");
        ballMask = LayerMask.GetMask("Ball");

        currentPosition = transform.position;
        Vector2 initialVelocity = startDirection.normalized * startForce;
        float initialDt = Time.fixedDeltaTime; // Use fixed timestep for consistent initialization
        previousPosition = currentPosition - initialVelocity * initialDt;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ------------------------------
        // 1) VERLET INTEGRATION
        // ------------------------------
        // Calculate directional gravity based on current velocity
        Vector2 currentVelocity = (currentPosition - previousPosition) / dt;
        float effectiveGravity = currentVelocity.y >= 0 ? -gravityUp : -gravityDown;

        Vector2 acceleration = new Vector2(0, effectiveGravity);
        Vector2 nextPosition = 2 * currentPosition - previousPosition + acceleration * dt * dt;

        Vector2 displacement = nextPosition - currentPosition;

        // ------------------------------
        // 2) WALL COLLISION SWEEP
        // ------------------------------
        if (displacement.sqrMagnitude > 0.0000001f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(
                currentPosition,
                radius,
                displacement.normalized,
                displacement.magnitude,
                wallMask
            );

            if (hit.collider != null)
            {
                HandleWallCollision(hit, dt);
                return;
            }
        }

        // Normal motion
        previousPosition = currentPosition;
        currentPosition = nextPosition;
        transform.position = currentPosition;

        // ------------------------------
        // 3) BALL–BALL COLLISIONS
        // ------------------------------
        Collider2D[] hits = Physics2D.OverlapCircleAll(currentPosition, radius, ballMask);

        foreach (var h in hits)
        {
            if (h == circle) continue;

            BallScriptVerlet other = h.GetComponent<BallScriptVerlet>();
            if (other != null)
                ResolveBallCollision(other, dt);
        }
    }

    // ============================================================
    // WALL COLLISION (SAFE VELOCITY-PRESERVING)
    // ============================================================
    private void HandleWallCollision(RaycastHit2D hit, float dt)
    {
        // Velocity BEFORE adjustment
        Vector2 velocity = (currentPosition - previousPosition) / dt;

        // Calculate proper ball center position at collision
        Vector2 displacement = velocity * dt;
        Vector2 hitPos = currentPosition + displacement.normalized * hit.distance;
        Vector2 correctedPos = hitPos + hit.normal * (radius + 0.0001f);

        // Safety check - prevent extreme positions
        if (correctedPos.magnitude > 100f)
        {
            Debug.LogWarning($"Ball position seems extreme: {correctedPos}, clamping");
            correctedPos = Vector2.ClampMagnitude(correctedPos, 20f);
        }

        currentPosition = correctedPos;

        // Slope detection
        bool isGround =
            hit.normal.y >= Mathf.Cos(groundAngleLimit * Mathf.Deg2Rad);

        if (isGround && velocity.magnitude < minBounceVelocity)
        {
            // STOP motion — zero velocity
            previousPosition = currentPosition;
        }
        else
        {
            // Bounce
            Vector2 reflected = Vector2.Reflect(velocity, hit.normal) * bounciness;
            reflected *= bouncePower;
            previousPosition = currentPosition - reflected * dt;
        }

        // Update object transform
        transform.position = currentPosition;
    }

    // ============================================================
    // BALL–BALL COLLISION (SAFE VERLET VERSION)
    // ============================================================
    private void ResolveBallCollision(BallScriptVerlet other, float dt)
    {
        Vector2 delta = currentPosition - other.currentPosition;
        float dist = delta.magnitude;
        float minDist = radius + other.radius;

        if (dist < 0.00001f) return;

        float penetration = minDist - dist;
        if (penetration <= 0) return;

        Vector2 normal = delta / dist;

        // --- Step 1: compute velocities BEFORE moving
        Vector2 v1 = (currentPosition - previousPosition) / dt;
        Vector2 v2 = (other.currentPosition - other.previousPosition) / dt;

        // --- Step 2: move positions (but don't update previous yet!)
        currentPosition += normal * (penetration * 0.5f);
        other.currentPosition -= normal * (penetration * 0.5f);

        // --- Step 3: compute new velocities
        float v1n = Vector2.Dot(v1, normal);
        float v2n = Vector2.Dot(v2, normal);

        float newV1n = v2n;
        float newV2n = v1n;

        Vector2 newV1 = v1 + (newV1n - v1n) * normal;
        Vector2 newV2 = v2 + (newV2n - v2n) * normal;

        newV1 *= bounciness;
        newV2 *= bounciness;

        // --- Step 4: Verlet velocity preservation
        previousPosition = currentPosition - newV1 * dt;
        other.previousPosition = other.currentPosition - newV2 * dt;

        // apply corrected positions visually
        transform.position = currentPosition;
        other.transform.position = other.currentPosition;
    }

    // ============================================================
    // PUBLIC HELPERS
    // ============================================================
    public Vector2 GetVelocity() =>
        (currentPosition - previousPosition) / Time.deltaTime;

    public void SetVelocity(Vector2 v)
    {
        previousPosition = currentPosition - v * Time.deltaTime;
    }

    public void AddForce(Vector2 f)
    {
        SetVelocity(GetVelocity() + f);
    }
}
