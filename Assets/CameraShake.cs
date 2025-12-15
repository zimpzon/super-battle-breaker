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
        transform.DOShakePosition(0.3f, 0.2f);
    }
}
