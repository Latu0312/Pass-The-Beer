using UnityEngine;

public class OrderSystemInitializer : MonoBehaviour
{
    [Header("Database Asset")]
    public OrderSpriteDatabase database;

    private void Awake()
    {
        if (database != null)
        {
            database.Initialize();
            Debug.Log("Order Sprite Database initialized successfully!");
        }
        else
        {
            Debug.LogError("Missing OrderSpriteDatabase reference!");
        }
    }
}
