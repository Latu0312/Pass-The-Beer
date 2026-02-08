using UnityEngine;
using System.Collections.Generic;

public class OrderUIManager : MonoBehaviour
{
    public static OrderUIManager Instance { get; private set; }

    [Header("UI Config")]
    [Tooltip("Prefab của UI hiển thị món gọi (Canvas World Space)")]
    public GameObject orderUIPrefab;

    private Dictionary<CustomerController, OrderUI> activeUIs = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterOrder(CustomerController c)
    {
        if (activeUIs.ContainsKey(c)) return;

        // Tạo UI trong không gian thế giới (không cần parent canvas)
        var uiObj = Instantiate(orderUIPrefab);
        var ui = uiObj.GetComponent<OrderUI>();
        ui.BindToCustomer(c);

        activeUIs.Add(c, ui);
    }

    public void UnregisterOrder(CustomerController c)
    {
        if (!activeUIs.ContainsKey(c)) return;
        Destroy(activeUIs[c].gameObject);
        activeUIs.Remove(c);
    }
}
