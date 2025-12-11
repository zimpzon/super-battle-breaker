using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "skins-sprite-list", menuName = "Sprite List")]
public class SpriteListScriptableObject : ScriptableObject
{
    [SerializeField]
    public List<Sprite> sprites = new List<Sprite>();
}