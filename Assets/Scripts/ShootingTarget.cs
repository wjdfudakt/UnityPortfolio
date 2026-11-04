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

    public TargetType Type
    {
        get
        {
            return targetType;
        }
    }

    private ScoreManager scoreManager;
    private bool isHit = false;

    public bool IsForbidden
    {
        get
        {
            return targetType == TargetType.Forbidden;
        }
    }

    public void Initialize(ScoreManager manager, float lifetime)
    {
        scoreManager = manager;

        Destroy(gameObject, lifetime);
    }

    public void Hit()
    {
        if (isHit || scoreManager == null)
        {
            return;
        }

        isHit = true;

        scoreManager.AddScore(scoreValue);

        if (IsForbidden)
        {
            Debug.Log("금지 표적 명중! 감점: " + (-scoreValue));
        }
        else
        {
            Debug.Log("표적 명중! 획득 점수: " + scoreValue);
        }

        Destroy(gameObject);
    }
}