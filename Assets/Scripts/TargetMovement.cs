using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float moveRange = 3f;

    private Vector3 leftPosition;
    private Vector3 rightPosition;

    private bool movingRight;

    private void Start()
    {
        Vector3 centerPosition = transform.position;

        leftPosition = centerPosition + Vector3.left * moveRange;
        rightPosition = centerPosition + Vector3.right * moveRange;

        float randomOffset = Random.Range(-moveRange, moveRange);

        transform.position =
            centerPosition + Vector3.right * randomOffset;

        movingRight = Random.value < 0.5f;
    }

    private void Update()
    {
        Vector3 destination;

        if (movingRight)
            destination = rightPosition;
        else
            destination = leftPosition;

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, destination) < 0.01f)
            movingRight = !movingRight;
    }
}