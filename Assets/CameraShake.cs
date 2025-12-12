using DG.Tweening;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake I;

    private void Awake()
    {
        I = this;
    }

    public void Shake()
    {
        transform.DOKill();
        transform.DOShakePosition(0.1f, 0.1f);
    }
}
