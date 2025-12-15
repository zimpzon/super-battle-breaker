using UnityEngine;
using UnityEngine.UI;

public class SkinListScript : MonoBehaviour
{
    public const int FirstUnclockValue = 200;
    public const int StepUnlockValue = 100;

    public static SkinListScript I;

    public SpriteRenderer DispenserSprite;

    public Image[] Skins;

    private void Awake()
    {
        I = this;
    }

    private void Update()
    {
        for (int i = 0; i < Skins.Length; i++)
        {
            var image = Skins[i];
            var skin = Skins[i].GetComponent<SkinScript>();

            bool isUnclocked = GameScript.I.BestScore >= FirstUnclockValue + skin.SkinId * StepUnlockValue || skin.SkinId == 0;
            image.color = isUnclocked ? Color.white : Color.black;
        }
    }

    public void SkinClicked(SkinScript skinScript, Image image = null)
    {
        bool isUnclocked = GameScript.I.BestScore >= FirstUnclockValue + skinScript.SkinId * StepUnlockValue || skinScript.SkinId == 0;
        if (!isUnclocked)
            return;

        Image targetImage = image ?? skinScript.GetComponent<Image>();
        if (targetImage == null || targetImage.sprite == null) return;

        DispenserSprite.sprite = targetImage.sprite;
    }
}
