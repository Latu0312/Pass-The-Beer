using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraPanController : MonoBehaviour
{
    [Header("Pan Config")]
    public float panSpeed = 3f;
    public float centerAngle = -90f;
    public float leftLimit = -113f;
    public float rightLimit = -45f;
    public float returnSpeed = 2.5f;

    [Header("Input Config")]
    [Range(0f, 1f)] public float centerRegionWidth = 0.4f;

    [Header("Camera Tilt")]
    public float tiltX = 21f;

    [Header("Double Tap Config")]
    public float doubleTapTime = 0.3f;
    public float smoothResetSpeed = 5f;

    [System.Serializable]
    public class AnchoredObject
    {
        public Transform obj;
        public float distance = 2f;
        public Vector3 offset;
    }

    [Header("Anchored Objects")]
    public List<AnchoredObject> anchoredObjects = new();

    [Header("UI Buttons that should block camera drag")]
    public List<Button> uiButtons = new(); // Kéo các button cần chặn xoay vào đây

    public static bool IsCameraLocked = false;
    public static bool IsDraggingCamera = false;
    public static bool IsTouchingDrink = false;
    public static float LastCameraStopTime = 0f;

    private float currentAngle;
    private float targetAngle;
    private float lastTapTime = 0f;
    private bool isResetting = false;
    private static bool IsPointerOverUIButton = false;

    private void Awake()
    {
        // Reset tất cả trạng thái static
        IsCameraLocked = false;
        IsDraggingCamera = false;
        IsTouchingDrink = false;
        LastCameraStopTime = 0f;
    }

    private void OnEnable()
    {
        IsCameraLocked = false;
        IsDraggingCamera = false;
        IsTouchingDrink = false;
        LastCameraStopTime = 0f;

        RegisterUIButtonEvents();
    }

    private void OnDisable()
    {
        UnregisterUIButtonEvents();
        IsDraggingCamera = false;
    }

    private void Start()
    {
        StartCoroutine(EnableCameraAfterDelay());
        currentAngle = centerAngle;
        targetAngle = centerAngle;
        transform.rotation = Quaternion.Euler(tiltX, centerAngle, 0f);
    }

    private void Update()
    {
        DetectDoubleTap();

        // Nếu đang chạm ly, bị khoá, hoặc đang nhấn UI
        if (IsCameraLocked || IsTouchingDrink || IsPointerOverUIButton)
        {
            IsDraggingCamera = false;
            return;
        }

        if (!isResetting)
            HandleInput();

        ApplyRotation();
        UpdateAnchoredObjects();
    }

    private void DetectDoubleTap()
    {
        
        if (IsPointerOverUIButton)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastTapTime <= doubleTapTime)
                StartCoroutine(SmoothResetToCenter());

            lastTapTime = Time.time;
        }
    }


    private void HandleInput()
    {
        if (Input.GetMouseButton(0))
        {
            IsDraggingCamera = true;

            float mouseX = Input.mousePosition.x / Screen.width;
            float leftBoundary = 0.5f - (centerRegionWidth / 2f);
            float rightBoundary = 0.5f + (centerRegionWidth / 2f);

            if (mouseX < leftBoundary)
                targetAngle = Mathf.Lerp(targetAngle, leftLimit, Time.deltaTime * panSpeed);
            else if (mouseX > rightBoundary)
                targetAngle = Mathf.Lerp(targetAngle, rightLimit, Time.deltaTime * panSpeed);
        }
        else
        {
            if (IsDraggingCamera)
            {
                IsDraggingCamera = false;
                LastCameraStopTime = Time.time;
            }

            targetAngle = Mathf.Lerp(targetAngle, centerAngle, Time.deltaTime * returnSpeed);
        }
    }

    private void ApplyRotation()
    {
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * panSpeed);
        transform.rotation = Quaternion.Euler(tiltX, currentAngle, 0f);
    }

    private void UpdateAnchoredObjects()
    {
        foreach (var anchored in anchoredObjects)
        {
            if (anchored.obj == null) continue;
            Vector3 forwardPos = transform.position + transform.forward * anchored.distance;
            Vector3 offsetWorld =
                transform.right * anchored.offset.x +
                transform.up * anchored.offset.y +
                transform.forward * anchored.offset.z;

            anchored.obj.position = forwardPos + offsetWorld;
        }
    }

    private IEnumerator SmoothResetToCenter()
    {
        isResetting = true;

        while (Mathf.Abs(currentAngle - centerAngle) > 0.1f)
        {
            targetAngle = Mathf.Lerp(targetAngle, centerAngle, Time.deltaTime * smoothResetSpeed);
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * smoothResetSpeed);
            transform.rotation = Quaternion.Euler(tiltX, currentAngle, 0f);
            UpdateAnchoredObjects();
            yield return null;
        }

        targetAngle = centerAngle;
        currentAngle = centerAngle;
        transform.rotation = Quaternion.Euler(tiltX, centerAngle, 0f);
        UpdateAnchoredObjects();

        isResetting = false;
    }

    private IEnumerator EnableCameraAfterDelay()
    {
        enabled = false;
        yield return new WaitForSeconds(2f);
        enabled = true;
    }

    // ================================
    // UI BUTTON HANDLING
    // ================================
    private void RegisterUIButtonEvents()
    {
        foreach (var btn in uiButtons)
        {
            if (btn == null) continue;
            EventTrigger trigger = btn.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            AddUITriggerEvent(trigger, EventTriggerType.PointerDown, (_) => IsPointerOverUIButton = true);
            AddUITriggerEvent(trigger, EventTriggerType.PointerUp, (_) => IsPointerOverUIButton = false);
            AddUITriggerEvent(trigger, EventTriggerType.PointerExit, (_) => IsPointerOverUIButton = false);
        }
    }

    private void UnregisterUIButtonEvents()
    {
        IsPointerOverUIButton = false;
    }

    private void AddUITriggerEvent(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => action(data));
        trigger.triggers.Add(entry);
    }
}
