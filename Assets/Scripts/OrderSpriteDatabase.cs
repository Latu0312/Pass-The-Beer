using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Order/OrderSpriteDatabase")]
public class OrderSpriteDatabase : ScriptableObject
{
    [System.Serializable]
    public class OrderSpritePair { public string id; public Sprite sprite; }
    [Header("Order Sprite List")] public List<OrderSpritePair> spritePairs = new List<OrderSpritePair>();


    private static Dictionary<string, Sprite> spriteMap;


    public void Initialize()
    {
        spriteMap = new Dictionary<string, Sprite>();
        foreach (var p in spritePairs) if (!spriteMap.ContainsKey(p.id)) spriteMap.Add(p.id, p.sprite);
    }


    public static Sprite GetSpriteForId(string id)
    {
        if (spriteMap != null && spriteMap.ContainsKey(id)) return spriteMap[id];
        return null;
    }
}