using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Danh sách vật phẩm nâng cấp")]
    public List<UpgradeItem> upgradeItems = new List<UpgradeItem>();

    [Header("Tiền người chơi")]
    public int playerCoins;
    public Text coinText;

    private void OnEnable()
    {
        playerCoins = PlayerPrefs.GetInt("TotalMoney", 0);
        if (coinText != null)
            coinText.text = playerCoins.ToString("N0");
    }

    private void Start()
    {
        // Lấy số tiền hiện tại từ cùng key với GameManager
        playerCoins = PlayerPrefs.GetInt("TotalMoney", 0);
        coinText.text = playerCoins.ToString("N0");

        // Kiểm tra trạng thái đã mua của vật phẩm
        foreach (var item in upgradeItems)
        {
            item.isPurchased = PlayerPrefs.GetInt(item.itemID, 0) == 1;
            item.buyButton.onClick.AddListener(() => BuyItem(item));

            if (item.isPurchased)
                item.buyButton.interactable = false;
        }
    }

    public void BuyItem(UpgradeItem item)
    {
        if (playerCoins >= item.price)
        {
            playerCoins -= item.price;
            PlayerPrefs.SetInt("TotalMoney", playerCoins); // <-- đồng bộ key
            PlayerPrefs.SetInt(item.itemID, 1);
            PlayerPrefs.Save();

            coinText.text = playerCoins.ToString("N0");
            item.buyButton.interactable = false;

            Debug.Log($"Đã mua vật phẩm: {item.itemID}");
        }
        else
        {
            Debug.Log("Không đủ tiền để mua vật phẩm này!");
        }
    }
}
