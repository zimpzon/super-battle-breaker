using System.Collections.Generic;
using UnityEngine;

public class BlockBoardScript : MonoBehaviour
{
    public Transform BlockSpawnCenter;
    public GameObject[] BlockPrefabList;
    [SerializeField] private float blockSpacing = 1f;
    [SerializeField] private float scrollSpeed = 3f;

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

        // Don't spawn blocks while board is busy processing
        if (BoardScript.Instance != null &&
            (BoardScript.Instance.isProcessingSwap ||
             BoardScript.Instance.isProcessingMatches ||
             BoardScript.Instance.isProcessingSettlement))
        {
            return;
        }

        // Control block movement based on pending advancement
        UpdateBlockMovement();

        // Check if any blocks are in the detection rect
        Collider2D hit = Physics2D.OverlapArea(
            new Vector2(detectionRect.xMin, detectionRect.yMin),
            new Vector2(detectionRect.xMax, detectionRect.yMax),
            blockLayerMask
        );

        // If no blocks detected, and we have pending advancement, spawn new row and decrement counter
        if (hit == null && pendingAdvancement > 0)
        {
            SpawnRow();
            pendingAdvancement--;
        }
        // If no blocks and no pending advancement, spawn initial row (for game start)
        else if (hit == null && pendingAdvancement == 0 && !HasAnyBlocks())
        {
            SpawnRow();
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
                // Set movement based on pending advancement counter
                if (pendingAdvancement > 0)
                {
                    block.linearVelocity = Vector2.up * scrollSpeed;
                }
                else
                {
                    block.linearVelocity = Vector2.zero;
                }
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
