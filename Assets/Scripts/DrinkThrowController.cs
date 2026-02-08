using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using static CameraPanController;

[RequireComponent(typeof(LineRenderer))]
public class DrinkThrowController : MonoBehaviour
{
    [Header("Drink Settings")]
    public GameObject[] drinkPrefabs;
    public OrderSpriteDatabase orderSpriteDatabase;
    public Transform spawnPoint;
    public Transform trajectorySpawnPoint;
    public float maxThrowForce = 14f;
    public float minThrowForce = 3f;
    public float dragMultiplier = 0.02f;

    [Header("Swipe Settings")]
    public float swipeThreshold = 100f;
    public float maxThrowAngle = 90f;

    [Header("UI Settings")]
    public Image leftDrinkIcon;
    public Image rightDrinkIcon;
    public Slider powerBar;
    public Button leftButton;
    public Button rightButton;

    [Header("Power Bar Visual Effects")]
    public Color lowPowerColor = new Color(0.2f, 1f, 0.2f);
    public Color midPowerColor = new Color(1f, 0.9f, 0.1f);
    public Color highPowerColor = new Color(1f, 0.3f, 0.1f);
    public float colorLerpSpeed = 8f;
    public float shakeAmplitude = 8f;
    public float shakeFrequency = 15f;
    public float maxScaleBump = 1.15f;

    private Image powerBarFill;
    private RectTransform powerBarRect;
    private Vector3 initialBarPosition;
    private Vector3 initialBarScale;
    private Color currentColor;

    [Header("Trajectory Settings")]
    public GameObject trajectoryDotPrefab;
    public int trajectoryDotCount = 15;
    public float dotSpacing = 0.1f;

    private List<GameObject> trajectoryDots = new List<GameObject>();
    private int currentDrinkIndex = 0;
    private GameObject currentDrink;
    private Vector2 startTouchPos, currentTouchPos;
    private bool isDragging = false;
    private bool isAimingThrow = false;
    private LineRenderer lineRenderer;
    private Camera mainCam;
    private float currentForce = 0f;
    private float shakeTimer = 0f;

    [Header("Audio Settings")]
    public AudioSource audioSource;         // Kéo Audio Source vào đây
    public AudioClip throwSound;            // Kéo loại âm thanh khi ném ly

    void Start()
    {
        mainCam = Camera.main;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        if (orderSpriteDatabase != null)
            orderSpriteDatabase.Initialize();
        SpawnDrink();
        UpdateDrinkIcons();

        if (leftButton != null) leftButton.onClick.AddListener(PreviousDrink);
        if (rightButton != null) rightButton.onClick.AddListener(NextDrink);

        if (powerBar != null)
        {
            powerBar.value = 0;
            powerBar.gameObject.SetActive(false);
            powerBarFill = powerBar.fillRect?.GetComponent<Image>();
            powerBarRect = powerBar.GetComponent<RectTransform>();
            if (powerBarFill != null)
                powerBarFill.color = lowPowerColor;
            currentColor = lowPowerColor;
            if (powerBarRect != null)
            {
                initialBarPosition = powerBarRect.anchoredPosition;
                initialBarScale = powerBarRect.localScale;
            }
        }

        CreateTrajectoryDots();
    }

    void Update()
    {
        if (CameraPanController.IsDraggingCamera)
            return;

        if (Time.time - CameraPanController.LastCameraStopTime < 0.1f && CameraPanController.LastCameraStopTime > 0f)
            return;

        HandleTouchInput();

        if (isAimingThrow)
            UpdateDragVisualization();

        if (currentDrink != null)
        {
            currentDrink.transform.position = spawnPoint.position;
            currentDrink.transform.rotation = spawnPoint.rotation;
        }
    }

    void HandleTouchInput()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.CompareTag("Drink"))
                {
                    CameraPanController.IsTouchingDrink = true;
                    startTouchPos = Input.mousePosition;
                    currentTouchPos = startTouchPos;
                    isDragging = true;
                    isAimingThrow = false;
                }
                else
                {
                    CameraPanController.IsTouchingDrink = false;
                    return;
                }
            }
            else
            {
                CameraPanController.IsTouchingDrink = false;
                return;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (!isDragging && !CameraPanController.IsDraggingCamera)
            {
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Drink"))
                {
                    CameraPanController.IsTouchingDrink = true;
                    startTouchPos = Input.mousePosition;
                    currentTouchPos = startTouchPos;
                    isDragging = true;
                    isAimingThrow = false;
                }
            }

            if (isDragging)
            {
                currentTouchPos = Input.mousePosition;
                Vector2 swipe = currentTouchPos - startTouchPos;

                if (swipe.y < -30f)
                {
                    isAimingThrow = true;
                    if (powerBar != null && !powerBar.gameObject.activeSelf)
                        powerBar.gameObject.SetActive(true);
                    SetDotsActive(true);
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                DetectSwipeAndThrow(Input.mousePosition);
                ResetDragState();
            }

            CameraPanController.IsTouchingDrink = false;
        }
#endif
    }

    void ResetDragState()
    {
        isDragging = false;
        isAimingThrow = false;
        SetDotsActive(false);
        currentForce = 0f;
        shakeTimer = 0f;

        if (powerBar != null)
        {
            powerBar.value = 0;
            powerBar.gameObject.SetActive(false);
            if (powerBarFill != null)
                powerBarFill.color = lowPowerColor;
            currentColor = lowPowerColor;

            if (powerBarRect != null)
            {
                powerBarRect.anchoredPosition = initialBarPosition;
                powerBarRect.localScale = initialBarScale;
            }
        }
    }

    void DetectSwipeAndThrow(Vector2 endTouchPos)
    {
        Vector2 swipe = endTouchPos - startTouchPos;

        if (swipe.y < -swipeThreshold)
        {
            float force = currentForce;

            Vector3 screenDir = new Vector3(-swipe.x, 0, -swipe.y).normalized;
            Vector3 worldDir = mainCam.transform.TransformDirection(screenDir);

            float angle = Vector3.Angle(mainCam.transform.forward, worldDir);
            if (angle > maxThrowAngle / 2f) return;

            worldDir.y = Mathf.Clamp(worldDir.y + 0.2f, 0.2f, 0.8f);
            ThrowDrink(force, worldDir.normalized);
        }
    }

    void UpdateDragVisualization()
    {
        Vector2 dragVector = (Vector2)currentTouchPos - startTouchPos;
        float dragDistance = dragVector.magnitude;

        if (dragVector.y > 10f && currentForce > 0f)
        {
            currentForce = Mathf.Lerp(currentForce, 0f, Time.deltaTime * 4f);
            if (powerBar != null)
            {
                float normalizedForce = Mathf.InverseLerp(0, maxThrowForce, currentForce);
                powerBar.value = normalizedForce;

                if (powerBarFill != null)
                {
                    currentColor = Color.Lerp(currentColor, lowPowerColor, Time.deltaTime * colorLerpSpeed);
                    powerBarFill.color = currentColor;
                }

                if (currentForce <= minThrowForce * 0.8f)
                {
                    ResetDragState();
                    return;
                }
            }

            if (currentForce < minThrowForce * 1.2f)
                SetDotsActive(false);

            return;
        }

        if (dragVector.y > -10f) return;

        float rawForce = dragDistance * dragMultiplier;
        currentForce = Mathf.Clamp(rawForce, minThrowForce, maxThrowForce);
        float normalized = Mathf.InverseLerp(0, maxThrowForce, currentForce);

        if (powerBar != null)
        {
            powerBar.value = normalized;
            Color targetColor;
            if (normalized < 0.5f)
                targetColor = Color.Lerp(lowPowerColor, midPowerColor, normalized * 2f);
            else
                targetColor = Color.Lerp(midPowerColor, highPowerColor, (normalized - 0.5f) * 2f);

            if (powerBarFill != null)
            {
                currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorLerpSpeed);
                powerBarFill.color = currentColor;
            }

            if (powerBarRect != null)
            {
                shakeTimer += Time.deltaTime * shakeFrequency * Mathf.Lerp(0.5f, 1.5f, normalized);
                float shakeAmount = Mathf.Sin(shakeTimer) * shakeAmplitude * normalized;
                Vector3 shakePos = initialBarPosition + new Vector3(shakeAmount, 0, 0);
                powerBarRect.anchoredPosition = Vector3.Lerp(powerBarRect.anchoredPosition, shakePos, Time.deltaTime * 15f);
                float targetScale = Mathf.Lerp(1f, maxScaleBump, normalized);
                powerBarRect.localScale = Vector3.Lerp(powerBarRect.localScale, initialBarScale * targetScale, Time.deltaTime * 10f);
            }
        }

        Vector3 screenDir = new Vector3(-dragVector.x, 0, -dragVector.y).normalized;
        Vector3 worldDir = mainCam.transform.TransformDirection(screenDir);
        float angle = Vector3.Angle(mainCam.transform.forward, worldDir);
        if (angle > maxThrowAngle / 2f)
        {
            SetDotsActive(false);
            return;
        }

        float heightFactor = Mathf.Lerp(0.3f, 0.9f, currentForce / maxThrowForce);
        worldDir.y += heightFactor * 0.5f;

        Vector3 start = trajectorySpawnPoint != null ? trajectorySpawnPoint.position : spawnPoint.position;
        UpdateTrajectoryDots(start, (worldDir + Vector3.up * heightFactor) * currentForce);
    }

    void ThrowDrink(float force, Vector3 dir)
    {
        if (currentDrink == null) return;
        IsCameraLocked = true;

        // --- PHÁT ÂM THANH KHI NÉM LY ---
        if (audioSource != null && throwSound != null)
        {
            audioSource.PlayOneShot(throwSound);
        }

        currentDrink.transform.parent = null;
        Rigidbody rb = currentDrink.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        float heightFactor = Mathf.Lerp(0.3f, 0.9f, force / maxThrowForce);
        rb.AddForce((dir + Vector3.up * heightFactor) * force, ForceMode.Impulse);

        currentDrink = null;
        Invoke(nameof(SpawnDrink), 1f);
        Invoke(nameof(UnlockCamera), 1f);
    }

    void UnlockCamera() => IsCameraLocked = false;

    void CreateTrajectoryDots()
    {
        Vector3 startPos = trajectorySpawnPoint != null ? trajectorySpawnPoint.position : spawnPoint.position;
        for (int i = 0; i < trajectoryDotCount; i++)
        {
            GameObject dot = Instantiate(trajectoryDotPrefab, startPos, Quaternion.identity);
            dot.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 0.1f, (float)i / trajectoryDotCount);
            dot.SetActive(false);
            trajectoryDots.Add(dot);
        }
    }

    void UpdateTrajectoryDots(Vector3 startPos, Vector3 velocity)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        for (int i = 0; i < trajectoryDots.Count; i++)
        {
            float t = i * dotSpacing;
            Vector3 pos = startPos + velocity * t + 0.5f * Physics.gravity * (t * t);
            trajectoryDots[i].transform.position = pos;
        }
    }

    void SetDotsActive(bool active)
    {
        foreach (var dot in trajectoryDots)
            if (dot != null) dot.SetActive(active);
    }

    void SpawnDrink()
    {
        if (currentDrink != null) Destroy(currentDrink);
        currentDrink = Instantiate(drinkPrefabs[currentDrinkIndex], spawnPoint.position, Quaternion.identity, spawnPoint);
        currentDrink.transform.localPosition = Vector3.zero;

        Rigidbody rb = currentDrink.GetComponent<Rigidbody>();
        if (rb == null) rb = currentDrink.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void NextDrink()
    {
        currentDrinkIndex = (currentDrinkIndex + 1) % drinkPrefabs.Length;
        SpawnDrink();
        UpdateDrinkIcons();
    }

    void PreviousDrink()
    {
        currentDrinkIndex--;
        if (currentDrinkIndex < 0) currentDrinkIndex = drinkPrefabs.Length - 1;
        SpawnDrink();
        UpdateDrinkIcons();
    }

    void UpdateDrinkIcons()
    {
        if (drinkPrefabs.Length < 2 || orderSpriteDatabase == null) return;

        int leftIndex = (currentDrinkIndex - 1 + drinkPrefabs.Length) % drinkPrefabs.Length;
        int rightIndex = (currentDrinkIndex + 1) % drinkPrefabs.Length;

        string leftId = GetDrinkId(drinkPrefabs[leftIndex]);
        string rightId = GetDrinkId(drinkPrefabs[rightIndex]);

        if (leftDrinkIcon != null)
            leftDrinkIcon.sprite = OrderSpriteDatabase.GetSpriteForId(leftId);

        if (rightDrinkIcon != null)
            rightDrinkIcon.sprite = OrderSpriteDatabase.GetSpriteForId(rightId);
    }

    string GetDrinkId(GameObject prefab)
    {
        var idComp = prefab.GetComponent<DrinkIdentifier>();
        return idComp != null ? idComp.drinkId : "";
    }
}
