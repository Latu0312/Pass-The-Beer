using UnityEngine;

public class SeatPoint : MonoBehaviour
{
    public enum SeatType { Seat, Counter }

    [Header("Seat Config")]
    public SeatType seatType = SeatType.Seat;

    [Tooltip("Vị trí cụ thể để khách đứng/ngồi")]
    public Transform seatTransform;

    [HideInInspector] public bool isOccupied = false;

    private void Reset()
    {
        seatTransform = transform;
    }

    public void Occupy() => isOccupied = true;
    public void Vacate() => isOccupied = false;
    public Vector3 GetPosition() => seatTransform != null ? seatTransform.position : transform.position;
}
