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
            Destroy(gameObject);
        }
    }
}
