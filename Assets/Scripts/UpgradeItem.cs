using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UpgradeItem
{
    [Header("Thông tin vật phẩm")]
    public string itemID;            // ID duy nhất, ví dụ: "AutoThrow", "NoDamage", "DoubleCoin"
    public int price;                // Giá tiền
    public Button buyButton;         // Nút mua trong Shop
    public Button useButton;         // Nút kích hoạt trong Gameplay (ẩn/hiện tùy trạng thái)
    public Image cooldownOverlay;    // Ảnh mờ hiển thị thời gian hồi chiêu

    [HideInInspector] public bool isPurchased;
    [HideInInspector] public bool isOnCooldown;

    [Header("Cấu hình nâng cấp")]
    public float cooldownTime = 20f;

}
