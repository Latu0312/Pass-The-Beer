using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CustomerController;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Customer Prefabs")]
    public List<GameObject> customerPrefabs = new List<GameObject>();

    [Header("Spawn & Exit Points")]
    public List<Transform> spawnPoints = new List<Transform>();
    public List<Transform> exitPoints = new List<Transform>();

    [Header("Seats & Counters")]
    public List<SeatPoint> seatPoints = new List<SeatPoint>();

    [Header("Timing Config")]
    public float waveDelay = 5f;
    public float customerSpawnInterval = 0.5f;
    public bool autoSpawn = true;

    [Header("Wave Config (Dynamic Difficulty)")]
    public Vector2Int initialWaveCount = new Vector2Int(2, 4);
    public int maxSeats = 16;
    public int spawnIncreasePerWave = 1;
    public int reduceWaitAfterWaves = 3;
    public Vector2 reduceWaitRandomRange = new Vector2(3, 5);

    [Header("Customer Wait Time")]
    public float initialWaitTime = 45f;
    public float minWaitTime = 20f;
    public float waitDecreaseStep = 10f;
    public float randomWaitOffset = 5f;

    [Header("Spawn Ratio")]
    [Range(0f, 1f)] public float counterSpawnChance = 0.4f;

    [Header("Camera Reference")]
    public Camera mainCamera;

    [Header("Audio Settings")]
    public AudioSource audioSource;      // AudioSource để phát âm thanh spawn
    public AudioClip spawnSound;         // Âm thanh khi spawn khách

    private List<CustomerController> activeCustomers = new();
    private bool hasStartedTiming = false;

    private int currentWave = 0;
    private int nextWaitReductionWave = 3;
    private Vector2Int currentWaveCount;
    private float currentWaitTime;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        ResetDifficulty();

        // ✅ Nếu AudioSource chưa được gán, thử tìm đối tượng SFXGameManager
        if (audioSource == null)
        {
            var sfxManager = GameObject.Find("SFXGameManager");
            if (sfxManager != null)
                audioSource = sfxManager.GetComponent<AudioSource>();
        }

        if (autoSpawn)
            StartCoroutine(SpawnLoop());
    }

    // =============== CORE SPAWN LOOP ===============
    private IEnumerator SpawnLoop()
    {
        while (autoSpawn)
        {
            yield return new WaitUntil(() => activeCustomers.Count == 0);

            currentWave++;
            int spawnCount = Random.Range(currentWaveCount.x, currentWaveCount.y + 1);
            spawnCount = Mathf.Min(spawnCount, maxSeats - CountOccupiedSeats());

            Debug.Log($"[Wave {currentWave}] Spawning {spawnCount} customers. Wait = {currentWaitTime}s");

            yield return StartCoroutine(SpawnWave(spawnCount));
            yield return new WaitForSeconds(waveDelay);

            // Cập nhật độ khó
            if (currentWave >= nextWaitReductionWave)
            {
                currentWaitTime = Mathf.Max(minWaitTime, currentWaitTime - waitDecreaseStep);
                currentWaveCount = new Vector2Int(
                    Mathf.Min(currentWaveCount.x + spawnIncreasePerWave, maxSeats),
                    Mathf.Min(currentWaveCount.y + spawnIncreasePerWave, maxSeats)
                );
                nextWaitReductionWave = currentWave + Random.Range((int)reduceWaitRandomRange.x, (int)reduceWaitRandomRange.y + 1);
            }
        }
    }

    private IEnumerator SpawnWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (AllSeatsOccupied()) break;
            SpawnCustomer();
            yield return new WaitForSeconds(customerSpawnInterval);
        }
    }

    // =============== SPAWN LOGIC ===============
    private void SpawnCustomer()
    {
        if (!hasStartedTiming)
        {
            GameManager.Instance.StartTiming();
            hasStartedTiming = true;
        }

        var freeSeat = GetRandomFreeSeat();
        if (freeSeat == null || customerPrefabs.Count == 0)
            return;

        var prefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
        var spawnPoint = spawnPoints.Count > 0
            ? spawnPoints[Random.Range(0, spawnPoints.Count)]
            : transform;

        GameObject go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        CustomerController cust = go.GetComponent<CustomerController>();

        if (cust == null)
        {
            Debug.LogError("Prefab khách thiếu CustomerController!");
            Destroy(go);
            return;
        }

        // ✅ Phát âm thanh khi spawn khách
        if (audioSource != null && spawnSound != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }

        string order = OrderDatabase.GetRandomOrderId();

        float randomWait = Random.Range(currentWaitTime - randomWaitOffset, currentWaitTime + randomWaitOffset);
        randomWait = Mathf.Clamp(randomWait, minWaitTime, initialWaitTime);
        cust.waitingTime = randomWait;

        cust.Assign(freeSeat, order, OnCustomerLeft);
        cust.exitPoints = exitPoints;
        cust.mainCamera = mainCamera;
        cust.OnCustomerDestroyed += HandleCustomerDestroyed;

        activeCustomers.Add(cust);
    }

    // =============== HELPER FUNCTIONS ===============
    private bool AllSeatsOccupied()
    {
        foreach (var seat in seatPoints)
            if (!seat.isOccupied)
                return false;
        return true;
    }

    private int CountOccupiedSeats()
    {
        int count = 0;
        foreach (var seat in seatPoints)
            if (seat.isOccupied)
                count++;
        return count;
    }

    private SeatPoint GetRandomFreeSeat()
    {
        bool chooseCounter = Random.value < counterSpawnChance;
        SeatPoint chosen = null;

        if (chooseCounter)
        {
            chosen = GetRandomFreeSeatByType(SeatPoint.SeatType.Counter) ??
                     GetRandomFreeSeatByType(SeatPoint.SeatType.Seat);
        }
        else
        {
            chosen = GetRandomFreeSeatByType(SeatPoint.SeatType.Seat) ??
                     GetRandomFreeSeatByType(SeatPoint.SeatType.Counter);
        }

        return chosen;
    }

    private SeatPoint GetRandomFreeSeatByType(SeatPoint.SeatType type)
    {
        List<SeatPoint> freeSeats = new();
        foreach (var s in seatPoints)
            if (!s.isOccupied && s.seatType == type)
                freeSeats.Add(s);

        if (freeSeats.Count == 0) return null;
        return freeSeats[Random.Range(0, freeSeats.Count)];
    }

    private void HandleCustomerDestroyed(CustomerController c)
    {
        activeCustomers.Remove(c);
    }

    private void OnCustomerLeft(CustomerController c)
    {
        // callback khi khách bắt đầu rời đi
    }
    

    // =============== DIFFICULTY RESET ===============
    public void ResetDifficulty()
    {
        StopAllCoroutines();
        activeCustomers.Clear();
        currentWave = 0;
        nextWaitReductionWave = 3;
        currentWaitTime = initialWaitTime;
        currentWaveCount = initialWaveCount;
        hasStartedTiming = false;

        var customers = FindObjectsOfType<CustomerController>();
        foreach (var c in customers)
            c.ForceLeaveImmediately();

        if (autoSpawn)
            StartCoroutine(SpawnLoop());
    }
}
