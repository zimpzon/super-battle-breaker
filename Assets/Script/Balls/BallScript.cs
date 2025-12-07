using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BallScript : MonoBehaviour
{
    [Header("Initial Launch Settings")]
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

    private Vector2 velocity;
    private float radius;
    private CircleCollider2D circle;
    private Light2D ballLight;

    private int wallMask;
    private int ballMask;

    void Start()
    {
        circle = GetComponent<CircleCollider2D>();
        radius = circle.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);

        wallMask = LayerMask.GetMask("StaticLevel");
        ballMask = LayerMask.GetMask("Ball");

        velocity = startDirection.normalized * startForce;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector2 pos = transform.position;

        // Apply directional gravity
        float effectiveGravity = velocity.y >= 0 ? gravityUp : gravityDown;
        velocity.y -= effectiveGravity * dt;

        // Apply drag (air resistance)
        velocity *= (1f - drag * dt);

        Vector2 displacement = velocity * dt;

        // -------------------------------------------
        // 1) WALL COLLISION (sweep)
        // -------------------------------------------
        RaycastHit2D wallHit = Physics2D.CircleCast(pos, radius, displacement.normalized, displacement.magnitude, wallMask);

        if (wallHit.collider != null)
        {
            Vector2 hitPos = pos + displacement.normalized * wallHit.distance;
            transform.position = hitPos + wallHit.normal * (radius + 0.001f);

            velocity = Vector2.Reflect(velocity, wallHit.normal) * bounciness;

            float angle = Vector2.Angle(wallHit.normal, Vector2.up);
            if (velocity.magnitude < minBounceVelocity && angle < groundAngleLimit)
                velocity = Vector2.zero;

            return; // Important: do NOT move forward after wall hit
        }

        // No wall hit → move freely
        transform.position = pos + displacement;

        // -------------------------------------------
        // 2) BALL–BALL COLLISION (simple overlap)
        // -------------------------------------------
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, ballMask);

        foreach (var h in hits)
        {
            if (h == circle) continue;

            BallScript other = h.GetComponent<BallScript>();
            if (other == null) continue;

            ResolveBallCollision(other);
        }
    }

    private void ResolveBallCollision(BallScript other)
    {
        Vector2 posA = transform.position;
        Vector2 posB = other.transform.position;

        Vector2 delta = posA - posB;
        float dist = delta.magnitude;

        if (dist <= 0.0001f) return; // overlapping too deep

        Vector2 normal = delta / dist;

        float penetration = (radius + other.radius) - dist;
        if (penetration > 0)
        {
            // Separate both balls equally
            transform.position = posA + normal * (penetration / 2f);
            other.transform.position = posB - normal * (penetration / 2f);

            // Basic elastic 1D collision along normal
            float vA = Vector2.Dot(velocity, normal);
            float vB = Vector2.Dot(other.velocity, normal);

            float temp = vA;
            vA = vB;
            vB = temp;

            velocity += (vA - Vector2.Dot(velocity, normal)) * normal;
            other.velocity += (vB - Vector2.Dot(other.velocity, normal)) * normal;
        }
    }

    public Vector2 GetVelocity() => velocity;
    public void SetVelocity(Vector2 v) => velocity = v;
    public void AddForce(Vector2 f) => velocity += f;
}
