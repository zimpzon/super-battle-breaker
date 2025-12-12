using UnityEngine;

public class BallScript : MonoBehaviour
{
    [Header("Ball Type")]
    public BrickType ballType;

    private void OnDestroy()
    {
        var thisColor = GetComponent<SpriteRenderer>().color;
        ParticleScript.I.Emit(ParticleScript.I.BallDecayParticles, transform.position, 4, thisColor);
    }
}
