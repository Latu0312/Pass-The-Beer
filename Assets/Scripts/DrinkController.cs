using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DrinkController : MonoBehaviour
{
    public DrinkType drinkType; // Loại đồ uống của ly này
    private bool hasHit = false; // Tránh xử lý nhiều lần

    private void Awake()
    {
        // Đảm bảo collider được thiết lập đúng
        Collider col = GetComponent<Collider>();
        col.isTrigger = true; // Có thể bật thủ công trong Inspector
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (!other.CompareTag("Customer")) return;

        hasHit = true;

        // Kiểm tra xem khách có script CustomerController không
        CustomerController customer = other.GetComponent<CustomerController>();
        if (customer != null)
        {
            // Gọi xử lý "đồ uống trúng khách"
            customer.OnDrinkHit(this);
        }

        // Hủy cốc nước ngay sau khi trúng
        Destroy(gameObject, 0.1f);
    }
}
