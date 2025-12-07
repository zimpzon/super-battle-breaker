using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BlockScript : MonoBehaviour
{
    public BrickType BlockType;

    void Start()
    {
        var brickPrefabs = FindAnyObjectByType<BrickPrefabs>();
        var match = brickPrefabs.Prefabs.Where(p => p.GetComponent<BrickScript>().Type == BlockType).First();
        var myColor = match.GetComponent<BrickScript>().Color;
        GetComponent<SpriteRenderer>().color = myColor;
        GetComponent<Light2D>().color = myColor;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        BallScript ball = collision.gameObject.GetComponent<BallScript>();
        if (ball != null && ball.ballType == BlockType)
        {
            GameScript.I.AddScore(1);
            GameScript.I.PlayBlockPopSound();
            StartScaleDestruction(0.3f);
        }
    }

    public void StartScaleDestruction(float duration = 0.3f)
    {
        StartCoroutine(ScaleDestructionCoroutine(duration));
    }

    private System.Collections.IEnumerator ScaleDestructionCoroutine(float duration)
    {
        Vector3 originalScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float scale = Mathf.Lerp(1f, 0f, progress);
            transform.localScale = originalScale * scale;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final scale is exactly zero
        transform.localScale = Vector3.zero;

        // Destroy the object
        Destroy(gameObject);
    }
}
