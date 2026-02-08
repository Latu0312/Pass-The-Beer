using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class CustomerController : MonoBehaviour
{
    public enum CustomerState { WalkingToSeat, WaitingForOrder, Reacting, Leaving }

    [Header("Current State")]
    public CustomerState state = CustomerState.WalkingToSeat;

    [Header("References")]
    public SeatPoint assignedPoint;
    public List<Transform> exitPoints = new List<Transform>();
    public Camera mainCamera;

    [Header("Order Config")]
    public string orderId;
    public float waitingTime = 20f;
    public Vector2 sitEnjoyTimeRange = new Vector2(5f, 20f);

    [Header("Animation Triggers (trùng Animator)")]
    public string walkTrigger = "Walk";
    public string sitTrigger = "Sit";
    public string angrySitTrigger = "AngrySit";
    public string angryIdleTrigger = "AngryIdle";
    public string idleTrigger = "Idle";
    public string happyIdleTrigger = "HappyIdle";

    [Header("Audio Settings")]
    public AudioSource audioSource;           // Kéo AudioSource SFX vào đây
    public AudioClip correctServeSFX;         // Âm thanh khi phục vụ đúng
    public AudioClip wrongServeSFX;           // Âm thanh khi phục vụ sai
    public AudioClip invalidHitSFX;           // Âm thanh khi bị ném ly sai thời điểm

    private float remainingWait;
    private NavMeshAgent agent;
    private Animator anim;
    private Action<CustomerController> onSeatVacated;
    private bool reachedExit = false;
    [SerializeField] public bool hasReceivedOrder = false;
    // Đã nhận đồ hay chưa

    public event Action<CustomerController> OnCustomerDestroyed;
    public float RemainingWait => remainingWait;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        if (audioSource == null)
        {
            var sfxManager = GameObject.Find("SFXGameManager");
            if (sfxManager != null)
                audioSource = sfxManager.GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        TriggerAnim(walkTrigger);
    }

    private void Update()
    {
        switch (state)
        {
            case CustomerState.WalkingToSeat:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                    ArrivedAtPoint();
                break;

            case CustomerState.WaitingForOrder:
                remainingWait -= Time.deltaTime;
                if (remainingWait <= 0f)
                    OnWaitTooLong();
                break;

            case CustomerState.Leaving:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f && !reachedExit)
                {
                    reachedExit = true;
                    StartCoroutine(DestroyAfterExit());
                }
                break;
        }
    }

    private IEnumerator DestroyAfterExit()
    {
        yield return new WaitForSeconds(0.3f);
        OnCustomerDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    public void Assign(SeatPoint point, string order, Action<CustomerController> callback)
    {
        assignedPoint = point;
        orderId = order;
        onSeatVacated = callback;

        assignedPoint.Occupy();
        agent.isStopped = false;
        agent.SetDestination(point.GetPosition());
        state = CustomerState.WalkingToSeat;

        TriggerAnim(walkTrigger);
    }

    private void ArrivedAtPoint()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        remainingWait = waitingTime;
        state = CustomerState.WaitingForOrder;

        OrderUIManager.Instance.RegisterOrder(this);

        if (assignedPoint.seatType == SeatPoint.SeatType.Counter && mainCamera != null)
            StartCoroutine(RotateTowardsCamera(mainCamera.transform));

        if (assignedPoint.seatType == SeatPoint.SeatType.Seat)
        {
            TriggerAnim(sitTrigger);
            agent.updatePosition = false;
            Vector3 pos = transform.position;
            pos.y = 0.52f;
            transform.position = pos;
        }
        else
        {
            TriggerAnim(idleTrigger);
        }
    }

    private IEnumerator RotateTowardsCamera(Transform cam)
    {
        Vector3 lookDir = cam.position - transform.position;
        lookDir.y = 0;
        Quaternion targetRot = Quaternion.LookRotation(lookDir);
        float t = 0f;
        while (t < 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
            t += Time.deltaTime * 2.5f;
            yield return null;
        }
        transform.rotation = targetRot;
    }

    // =========================== MAIN DRINK HIT ===========================
    public void OnDrinkHit(DrinkController drink)
    {
        // Nếu khách đang đi vào hoặc đang rời đi (trước/ sau khi order) -> chỉ phản ứng âm thanh
        if (state == CustomerState.WalkingToSeat || state == CustomerState.Leaving)
        {
            Debug.Log($"{name} bị ném ly trong lúc đi - không tính tiền!");
            PlayInvalidHitSound();
            return;
        }

        // Nếu khách đã nhận đồ xong rồi nhưng chưa rời -> không nhận thêm
        if (hasReceivedOrder)
        {
            Debug.Log($"{name} đã nhận đồ rồi mà vẫn bị ném ly!");
            PlayInvalidHitSound();
            return;
        }

        if (state == CustomerState.Reacting)
            return;

        // Xử lý ném ly trong lúc đang chờ order (bình thường)
        string receivedId = drink.drinkType.ToString();

        if (string.Equals(orderId, receivedId, StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"{name} nhận đúng đồ uống ({receivedId})!");
            OnSuccessfulServe();
        }
        else
        {
            Debug.Log($"{name} nhận sai đồ uống ({receivedId}) — cần {orderId}!");
            OnServeFailed();
        }
    }

    private void PlayInvalidHitSound()
    {
        if (audioSource != null && invalidHitSFX != null)
            audioSource.PlayOneShot(invalidHitSFX);
    }

    public void OnSuccessfulServe()
    {
        if (state == CustomerState.Leaving || state == CustomerState.Reacting) return;

        hasReceivedOrder = true; // Đánh dấu đã nhận đồ

        // ✅ Phát âm thanh phục vụ đúng
        if (audioSource != null && correctServeSFX != null)
            audioSource.PlayOneShot(correctServeSFX);

        OrderUIManager.Instance.UnregisterOrder(this);
        state = CustomerState.Reacting;

        if (assignedPoint.seatType == SeatPoint.SeatType.Seat)
        {
            GameManager.Instance.AddMoney(UnityEngine.Random.Range(50, 301));
            TriggerAnim(happyIdleTrigger);
            StartCoroutine(ReturnToSitAfterDelay(2f));
        }
        else
        {
            GameManager.Instance.AddMoney(UnityEngine.Random.Range(50, 301));
            StartCoroutine(CounterReactionAndLeave(happyIdleTrigger, 2f));
        }
    }

    public void OnServeFailed()
    {
        if (state == CustomerState.Leaving || state == CustomerState.Reacting) return;

        hasReceivedOrder = true; // Đánh dấu đã được phục vụ (dù sai)

        // ❌ Phát âm thanh phục vụ sai
        if (audioSource != null && wrongServeSFX != null)
            audioSource.PlayOneShot(wrongServeSFX);

        GameManager.Instance.LoseLife();
        OrderUIManager.Instance.UnregisterOrder(this);
        state = CustomerState.Reacting;

        if (assignedPoint.seatType == SeatPoint.SeatType.Seat)
            StartCoroutine(SeatAngryAndLeave());
        else
            StartCoroutine(CounterReactionAndLeave(angryIdleTrigger, 2f));
    }

    private void OnWaitTooLong()
    {
        if (state == CustomerState.Leaving || state == CustomerState.Reacting) return;

        Debug.Log($"{name} waited too long! Leaving angrily.");
        OrderUIManager.Instance.UnregisterOrder(this);
        GameManager.Instance.LoseLife();

        state = CustomerState.Reacting;

        if (assignedPoint.seatType == SeatPoint.SeatType.Seat)
            StartCoroutine(SeatAngryAndLeave());
        else
            StartCoroutine(CounterReactionAndLeave(angryIdleTrigger, 2f));
    }

    private IEnumerator SeatAngryAndLeave()
    {
        TriggerAnim(angrySitTrigger);
        yield return new WaitForSeconds(2f);

        TriggerAnim(idleTrigger);
        yield return new WaitForSeconds(0.5f);

        Leave();
    }

    private IEnumerator CounterReactionAndLeave(string emotionTrigger, float emotionDuration)
    {
        TriggerAnim(emotionTrigger);
        yield return new WaitForSeconds(emotionDuration);

        Leave();
    }

    private IEnumerator ReturnToSitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerAnim(sitTrigger);

        Vector3 pos = transform.position;
        pos.y = 0.52f;
        transform.position = pos;

        StartCoroutine(EnjoyDrinkAndLeave());
    }

    private IEnumerator EnjoyDrinkAndLeave()
    {
        float enjoyTime = UnityEngine.Random.Range(sitEnjoyTimeRange.x, sitEnjoyTimeRange.y);
        yield return new WaitForSeconds(enjoyTime);
        Leave();
    }

    private void Leave()
    {
        if (state == CustomerState.Leaving) return;

        state = CustomerState.Leaving;
        assignedPoint.Vacate();
        onSeatVacated?.Invoke(this);
        agent.updatePosition = true;

        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;

        TriggerAnim(walkTrigger);

        if (exitPoints.Count > 0)
        {
            Transform exit = exitPoints[UnityEngine.Random.Range(0, exitPoints.Count)];
            agent.isStopped = false;
            agent.SetDestination(exit.position);
        }
        else
        {
            StartCoroutine(DestroyAfterExit());
        }
    }

    private void TriggerAnim(string trigger)
    {
        if (anim == null || string.IsNullOrEmpty(trigger)) return;

        anim.ResetTrigger(walkTrigger);
        anim.ResetTrigger(sitTrigger);
        anim.ResetTrigger(angrySitTrigger);
        anim.ResetTrigger(angryIdleTrigger);
        anim.ResetTrigger(idleTrigger);
        anim.ResetTrigger(happyIdleTrigger);

        anim.SetTrigger(trigger);
    }

    public void ForceLeaveImmediately()
    {
        StopAllCoroutines();

        if (assignedPoint != null)
        {
            assignedPoint.Vacate();
            assignedPoint = null;
        }

        if (OrderUIManager.Instance != null)
            OrderUIManager.Instance.UnregisterOrder(this);

        OnCustomerDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
    

    public void ResetCustomers()
    {
        var customers = FindObjectsOfType<CustomerController>();
        foreach (var c in customers)
        {
            c.ForceLeaveImmediately();
        }
    }
    // =========================== AUTO SERVE ALL ===========================
  
    public static void AutoServeAllCustomers()
    {
        var allCustomers = GameObject.FindGameObjectsWithTag("Customer");
        if (allCustomers.Length == 0)
        {
            Debug.Log("❌ Không có khách nào trong scene.");
            return;
        }

        int servedCount = 0;

        foreach (var obj in allCustomers)
        {
            var c = obj.GetComponent<CustomerController>();
            if (c == null) continue;

            if (c.state == CustomerState.WaitingForOrder && !c.hasReceivedOrder)
            {
                c.hasReceivedOrder = true;
                c.state = CustomerState.Reacting;

                if (OrderUIManager.Instance != null)
                    OrderUIManager.Instance.UnregisterOrder(c);

                GameManager.Instance.AddMoney(UnityEngine.Random.Range(50, 301));

                if (c.audioSource && c.correctServeSFX)
                    c.audioSource.PlayOneShot(c.correctServeSFX);

                if (c.assignedPoint && c.assignedPoint.seatType == SeatPoint.SeatType.Seat)
                {
                    c.TriggerAnim(c.happyIdleTrigger);
                    c.StartCoroutine(c.ReturnToSitAfterDelay(2f));
                }
                else
                {
                    c.StartCoroutine(c.CounterReactionAndLeave(c.happyIdleTrigger, 2f));
                }

                servedCount++;
            }
        }

        Debug.Log($"🎯 AutoThrow: Đã phục vụ {servedCount} khách đang chờ order.");
    }



}
