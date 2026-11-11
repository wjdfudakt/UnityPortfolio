using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    public enum MovementMode
    {
        Horizontal,
        Vertical,
        RandomAxis
    }

    [Header("Movement")]
    [SerializeField]
    private MovementMode movementMode = MovementMode.RandomAxis;

    [SerializeField, Min(0f)]
    private float moveSpeed = 2f;

    [SerializeField, Min(0f)]
    private float moveRange = 0.8f;

    private Vector3 negativeEnd;
    private Vector3 positiveEnd;

    private bool movingPositive;

    private void Start()
    {
        Vector3 centerPosition = transform.position;

        Vector3 moveAxis;

        switch (movementMode)
        {
            case MovementMode.Horizontal:
                moveAxis = Vector3.right;
                break;

            case MovementMode.Vertical:
                moveAxis = Vector3.up;
                break;

            default:
                moveAxis = Random.value < 0.5f
                    ? Vector3.right
                    : Vector3.up;
                break;
        }

        negativeEnd = centerPosition - moveAxis * moveRange;
        positiveEnd = centerPosition + moveAxis * moveRange;

        float randomOffset = Random.Range(-moveRange, moveRange);

        transform.position =
            centerPosition + moveAxis * randomOffset;

        movingPositive = Random.value < 0.5f;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f || moveSpeed <= 0f || moveRange <= 0f)
            return;

        Vector3 destination = movingPositive
            ? positiveEnd
            : negativeEnd;

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            moveSpeed * Time.deltaTime
        );

        if (transform.position == destination)
        {
            movingPositive = !movingPositive;
        }
    }
}