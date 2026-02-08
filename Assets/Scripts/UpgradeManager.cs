using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    [Header("Danh sách vật phẩm nâng cấp")]
    public List<UpgradeItem> upgradeItems = new List<UpgradeItem>();

    [Header("Tham chiếu gameplay")]
    public GameManager gameManager;  // chứa isImmune & isDoubleCoin

    private void Awake()
    {
        Debug.Log("🧩 [UpgradeManager] Awake() khởi tạo!");
    }

    private void Start()
    {
        Debug.Log("🧠 [UpgradeManager] Start() đang chạy...");

        foreach (var item in upgradeItems)
        {
            if (item == null)
            {
                Debug.LogWarning("⚠️ Một phần tử upgradeItems bị null!");
                continue;
            }

            int savedValue = PlayerPrefs.GetInt(item.itemID, 0);
            item.isPurchased = (savedValue == 1);

            Debug.Log($"[UpgradeManager] itemID={item.itemID}, savedValue={savedValue}, isPurchased={item.isPurchased}");

            // Ép bật button nếu vật phẩm đã mua
            if (item.useButton != null)
            {
                item.useButton.gameObject.SetActive(item.isPurchased);
                item.useButton.interactable = true;
                item.useButton.onClick.RemoveAllListeners();
                item.useButton.onClick.AddListener(() => UseUpgrade(item));
            }

            // Ẩn overlay lúc đầu
            if (item.cooldownOverlay != null)
                item.cooldownOverlay.gameObject.SetActive(false);
        }
    }

    private void UseUpgrade(UpgradeItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("⚠️ Item bị null trong UseUpgrade!");
            return;
        }

        if (item.isOnCooldown)
        {
            Debug.Log($"⏳ {item.itemID} đang hồi chiêu!");
            return;
        }

        item.isOnCooldown = true;

        // Hiển thị overlay
        if (item.cooldownOverlay != null)
        {
            item.cooldownOverlay.fillAmount = 0;
            item.cooldownOverlay.gameObject.SetActive(true);
        }

        // Gọi hiệu ứng dựa theo ID
        switch (item.itemID)
        {
            case "AutoThrow":
                StartCoroutine(AutoThrowEffect());
                break;
            case "NoDamage":
                StartCoroutine(NoDamageEffect());
                break;
            case "DoubleCoin":
                StartCoroutine(DoubleCoinEffect());
                break;
            default:
                Debug.LogWarning($"[UpgradeManager] Không tìm thấy itemID: {item.itemID}");
                break;
        }

        // Hồi chiêu
        StartCoroutine(CooldownEffect(item, item.cooldownTime));
    }

    private IEnumerator CooldownEffect(UpgradeItem item, float cooldownTime)
    {
        float elapsed = 0f;
        item.useButton.interactable = false;

        while (elapsed < cooldownTime)
        {
            elapsed += Time.deltaTime;
            if (item.cooldownOverlay != null)
                item.cooldownOverlay.fillAmount = elapsed / cooldownTime;
            yield return null;
        }

        if (item.cooldownOverlay != null)
        {
            item.cooldownOverlay.fillAmount = 0;
            item.cooldownOverlay.gameObject.SetActive(false);
        }

        item.useButton.interactable = true;
        item.isOnCooldown = false;
    }

    // ======================= EFFECTS =======================
    private IEnumerator AutoThrowEffect()
    {
        Debug.Log("🎯 AutoThrow kích hoạt!");
        var customers = GameObject.FindGameObjectsWithTag("Customer");

        if (customers.Length == 0)
        {
            Debug.Log("⚠️ Không tìm thấy khách nào trong scene!");
            yield break;
        }

        int servedCount = 0;
        foreach (var c in customers)
        {
            CustomerController controller = c.GetComponent<CustomerController>();
            if (controller != null && controller.enabled && controller.gameObject.activeInHierarchy)
            {
                if (controller.state == CustomerController.CustomerState.WaitingForOrder)
                {
                    controller.OnSuccessfulServe();
                    servedCount++;
                }
            }
        }

        Debug.Log($"✅ AutoThrow hoàn tất — {servedCount} khách được phục vụ!");
        yield return null;
    }

    private IEnumerator NoDamageEffect()
    {
        Debug.Log("🛡️ Bắt đầu miễn sát thương 10s...");
        gameManager.isImmune = true;
        yield return new WaitForSeconds(10f);
        gameManager.isImmune = false;
        Debug.Log("🛡️ Hết miễn sát thương.");
    }

    private IEnumerator DoubleCoinEffect()
    {
        Debug.Log("💰 Nhân đôi coin 10s...");
        gameManager.isDoubleCoin = true;
        yield return new WaitForSeconds(10f);
        gameManager.isDoubleCoin = false;
        Debug.Log("💰 Hết hiệu ứng nhân đôi coin.");
    }

    public void ResetAllUpgrades()
    {
        foreach (var item in upgradeItems)
        {
            if (item == null) continue;

            item.isOnCooldown = false;

            if (item.cooldownOverlay != null)
            {
                item.cooldownOverlay.fillAmount = 0;
                item.cooldownOverlay.gameObject.SetActive(false);
            }

            if (item.useButton != null)
            {
                item.useButton.interactable = true;
            }
        }
        Debug.Log("🧩 UpgradeManager: reset toàn bộ cooldown và trạng thái vật phẩm.");
    }

}
