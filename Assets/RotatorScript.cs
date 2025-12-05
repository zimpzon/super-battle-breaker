using UnityEngine;

public class RotatorScript : MonoBehaviour
{
    public float Speed = 1.0f;

    private void Update()
    {
        transform.Rotate(Vector3.forward, Time.time * Speed);
    }
}
