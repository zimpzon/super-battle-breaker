using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkinScript : MonoBehaviour, IPointerClickHandler
{
    public int SkinId;

    public void OnPointerClick(PointerEventData eventData)
    {
        Image image = GetComponent<Image>();
        SkinListScript.I?.SkinClicked(this, image);
    }
}
