using System.Collections.Generic;
using UnityEngine;

public class BlockBoardScript : MonoBehaviour
{
    public Transform BlockSpawnCenter;
    public GameObject[] BlockPrefabList;
    private float blockSpacing = 1f;
    private float activeScrollSpeed = 0.1f;
    private float waitingScrollSpeed = 0.02f;

    private Rect detectionRect;
    private int blockLayerMask;
    private int pendingAdvancement = 2;

    void Start()
    {
        // Create detection rect that covers the spawn row area
        Vector3 centerPosition = BlockSpawnCenter.position;
        float rectWidth = 8 * blockSpacing; // 9 blocks with spacing
        float rectHeight = 0.5f; // Block height
        detectionRect = new Rect(centerPosition.x - rectWidth / 2, centerPosition.y - rectHeight / 2, rectWidth, rectHeight);

        // Get Block layer mask
        blockLayerMask = LayerMask.GetMask("Block");

        // Don't spawn initial row - wait for game to start
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameScript.I.IsPlaying) return;

        // Control block movement based on pending advancement
        UpdateBlockMovement();

        // Check if any blocks are in the detection rect
        Collider2D hit = Physics2D.OverlapArea(
            new Vector2(detectionRect.xMin, detectionRect.yMin),
            new Vector2(detectionRect.xMax, detectionRect.yMax),
            blockLayerMask
        );

        bool spawnRowEmpty = hit == null;
        if (spawnRowEmpty)
        {
            SpawnRow();
            pendingAdvancement--;
        }
    }

    public void SpawnRow()
    {
        Vector3 centerPosition = BlockSpawnCenter.position;

        // Spawn 9 blocks horizontally centered around BlockSpawnCenter
        for (int i = 0; i < 9; i++)
        {
            // Calculate position offset from center (-4 to +4)
            float xOffset = (i - 4) * blockSpacing;
            Vector3 spawnPosition = centerPosition + new Vector3(xOffset, 0, 0);

            // Choose random block prefab
            GameObject randomPrefab = BlockPrefabList[Random.Range(1, BlockPrefabList.Length)]; // start from 1, skipping the first. Bricks do the same.

            // Spawn the block
            GameObject block = Instantiate(randomPrefab, spawnPosition, Quaternion.identity, transform);

            // Start with no movement - will be controlled by UpdateBlockMovement
            Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void AdvanceBlocks()
    {
        pendingAdvancement = 1;
    }

    public void ResetAdvancement()
    {
        pendingAdvancement = 2;
    }

    private void UpdateBlockMovement()
    {
        // Find all blocks in the scene
        Rigidbody2D[] allBlocks = FindObjectsOfType<Rigidbody2D>();

        foreach (Rigidbody2D block in allBlocks)
        {
            // Check if this is a block (on the Block layer)
            if (block.gameObject.layer == LayerMask.NameToLayer("Block"))
            {
                // Use active speed when advancing, otherwise waiting speed
                block.linearVelocity = pendingAdvancement > 0
                    ? Vector2.up * activeScrollSpeed
                    : Vector2.up * waitingScrollSpeed;
            }
        }
    }

    private void SetBlockVelocity(Vector2 velocity)
    {
        Rigidbody2D[] allBlocks = FindObjectsOfType<Rigidbody2D>();

        foreach (Rigidbody2D block in allBlocks)
        {
            if (block.gameObject.layer == LayerMask.NameToLayer("Block"))
            {
                block.linearVelocity = velocity;
            }
        }
    }

    private bool HasAnyBlocks()
    {
        Rigidbody2D[] allBlocks = FindObjectsOfType<Rigidbody2D>();

        foreach (Rigidbody2D block in allBlocks)
        {
            if (block.gameObject.layer == LayerMask.NameToLayer("Block"))
            {
                return true;
            }
        }
        return false;
    }
}
