using UnityEngine;
using System.Collections;

public class CustomerBehavior : MonoBehaviour
{
    [Header("Customer Config")]
    public DrinkType desiredDrink; // Loại đồ khách muốn
    public Animator animator;
    public Transform exitPoint; // Điểm đi ra khỏi quán
    public float walkSpeed = 2f;

    private bool isLeaving = false;
    private bool isSatisfied = false;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public bool CheckDrink(DrinkType received)
    {
        return received == desiredDrink;
    }

    public void OnReceiveCorrectDrink()
    {
        if (isLeaving) return;

        isSatisfied = true;
        Debug.Log($"{name} received the correct drink ({desiredDrink})!");

        // Phát animation vui vẻ, uống tiếp
        animator.SetTrigger("Happy");
        animator.SetBool("IsDrinking", true);
    }

    public void OnReceiveWrongDrink()
    {
        if (isLeaving) return;

        Debug.Log($"{name} received the WRONG drink!");
        StartCoroutine(AngryAndLeave());
    }

    private IEnumerator AngryAndLeave()
    {
        isLeaving = true;
        animator.SetTrigger("Angry");
        animator.SetBool("IsDrinking", false);

        // Chờ animation tức giận 1.5s
        yield return new WaitForSeconds(1.5f);

        // Đi ra khỏi quán
        if (exitPoint != null)
        {
            animator.SetTrigger("Walk");
            while (Vector3.Distance(transform.position, exitPoint.position) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, exitPoint.position, walkSpeed * Time.deltaTime);
                transform.LookAt(exitPoint.position);
                yield return null;
            }
        }

        Destroy(gameObject, 0.5f);
    }
}
