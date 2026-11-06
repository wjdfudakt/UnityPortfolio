using UnityEngine;

public enum TargetType
{
    Static,
    Moving,
    Forbidden
}

public class ShootingTarget : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private TargetType targetType;

    private ScoreManager scoreManager;
    private bool isHit;

    private System.Action onTimeAttackHit;
    private System.Action onReactionHit;

    public TargetType Type => targetType;

    public bool IsForbidden =>
        targetType == TargetType.Forbidden;

    public void Initialize(ScoreManager manager, float lifetime)
    {
        scoreManager = manager;

        Destroy(gameObject, lifetime);
    }

    public void InitializeForTimeAttack(
        ScoreManager manager,
        System.Action onHit)
    {
        scoreManager = manager;
        onTimeAttackHit = onHit;
    }

    public void InitializeForReaction(System.Action onHit)
    {
        onReactionHit = onHit;
    }

    public void Hit()
    {
        if (isHit)
            return;

        if (onReactionHit != null)
        {
            isHit = true;

            gameObject.SetActive(false);
            Destroy(gameObject);

            onReactionHit();
            return;
        }

        if (scoreManager == null)
            return;

        isHit = true;

        gameObject.SetActive(false);
        Destroy(gameObject);

        if (onTimeAttackHit != null)
        {
            scoreManager.AddScore(1);

            onTimeAttackHit();
            return;
        }

        scoreManager.AddScore(scoreValue);

        if (IsForbidden)
        {
            Debug.Log("금지 표적 명중! 감점: " + (-scoreValue));
        }
        else
        {
            Debug.Log("표적 명중! 획득 점수: " + scoreValue);
        }
    }
}