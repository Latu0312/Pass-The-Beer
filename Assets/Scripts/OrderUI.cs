using UnityEngine;
using UnityEngine.UI;

public class OrderUI : MonoBehaviour
{
    [Header("UI References")]
    public Image orderImage;
    public Image timerBar;
    public Text orderText;

    [Header("Position Config")]
    [Tooltip("Độ lệch so với đầu khách")]
    public Vector3 offset = new Vector3(0.5f, 1.8f, 0f);
    [Tooltip("Tốc độ xoay theo camera")]
    public float faceCameraSpeed = 10f;

    private CustomerController target;
    private Transform camTransform;

    private void Start()
    {
        camTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (target == null) return;

        // Cập nhật vị trí UI theo đầu khách
        transform.position = target.transform.position + offset;

        // Hướng về camera nhưng chỉ xoay theo trục Y (ngang)
        Vector3 lookDir = camTransform.position - transform.position;
        lookDir.y = 0; // cố định không nghiêng lên xuống
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(-lookDir); // mặt về camera
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * faceCameraSpeed);
        }

        // Cập nhật thanh thời gian
        UpdateTimerVisual();
    }

    public void BindToCustomer(CustomerController c)
    {
        target = c;

        if (orderText) orderText.text = c.orderId;
        if (orderImage) orderImage.sprite = OrderSpriteDatabase.GetSpriteForId(c.orderId);
    }

    private void UpdateTimerVisual()
    {
        if (target == null) return;

        float t = Mathf.Clamp01(target.RemainingWait / target.waitingTime);
        if (timerBar) timerBar.fillAmount = t;

        Color color;
        if (t > 0.6f) color = Color.Lerp(Color.yellow, Color.green, (t - 0.6f) / 0.4f);
        else if (t > 0.3f) color = Color.Lerp(Color.red, Color.yellow, (t - 0.3f) / 0.3f);
        else color = Color.Lerp(Color.black, Color.red, t / 0.3f);

        if (timerBar) timerBar.color = color;
    }
}
