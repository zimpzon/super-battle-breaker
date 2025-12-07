using UnityEngine;

public class BallLauncherScript : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameScript.I.GameOver();
    }
}
