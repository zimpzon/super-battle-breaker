using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    public static ParticleScript I;

    public ParticleSystem BallDecayParticles;

    public void Emit(ParticleSystem particleSystem, Vector3 pos, int count, Color? color)
    {
        particleSystem.transform.position = pos;
        var main = particleSystem.main;
        if (color != null)
            main.startColor = color.Value;

        particleSystem.Emit(count);
    }

    private void Awake()
    {
        I = this;
    }
}
