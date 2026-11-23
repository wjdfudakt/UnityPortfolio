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

    [Header("Memory Shooting")]
    [SerializeField] private Renderer memoryRenderer;

    [SerializeField]
    private Color memoryIdleColor =
        new Color(0.35f, 0.45f, 0.55f);

    [SerializeField]
    private Color memoryCueColor = Color.yellow;

    [SerializeField]
    private Color memoryCorrectColor = Color.green;

    [SerializeField, Min(0.01f)]
    private float memoryFeedbackDuration = 0.15f;

    private ScoreManager scoreManager;
    private bool isHit;

    private System.Action onTimeAttackHit;
    private System.Action onReactionHit;

    private MaterialPropertyBlock memoryPropertyBlock;
    private float memoryFeedbackRemaining;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    public TargetType Type => targetType;

    public bool IsForbidden =>
        targetType == TargetType.Forbidden;

    public bool IsMemoryTarget { get; private set; }

    public TargetSpawner MemoryOwner { get; private set; }

    public int MemoryIndex { get; private set; } = -1;

    private void Update()
    {
        if (!IsMemoryTarget || memoryFeedbackRemaining <= 0f)
            return;

        memoryFeedbackRemaining -= Time.deltaTime;

        if (memoryFeedbackRemaining <= 0f)
        {
            ApplyMemoryColor(memoryIdleColor);
        }
    }

    // 일반 라운드 타겟 초기화
    public void Initialize(ScoreManager manager, float lifetime)
    {
        scoreManager = manager;

        Destroy(gameObject, lifetime);
    }

    // 타임어택 타겟 초기화
    public void InitializeForTimeAttack(
        ScoreManager manager,
        System.Action onHit)
    {
        scoreManager = manager;
        onTimeAttackHit = onHit;
    }

    // 반응속도 타겟 초기화
    public void InitializeForReaction(System.Action onHit)
    {
        onReactionHit = onHit;
    }

    // 메모리 타겟으로 사용할 수 있는지 확인한다.
    public bool ValidateMemorySetup(out string error)
    {
        error = "";

        if (!gameObject.activeSelf || !enabled)
        {
            error = "메모리 타겟과 ShootingTarget을 활성화해 주세요.";
            return false;
        }

        if (targetType != TargetType.Static)
        {
            error = "메모리 타겟의 Target Type을 Static으로 설정해 주세요.";
            return false;
        }

        Renderer visual = memoryRenderer != null
            ? memoryRenderer
            : GetComponentInChildren<Renderer>();

        if (visual == null ||
            !visual.enabled ||
            visual.sharedMaterial == null)
        {
            error = "메모리 타겟의 Renderer와 Material을 확인해 주세요.";
            return false;
        }

        if (!visual.sharedMaterial.HasProperty(BaseColorId) &&
            !visual.sharedMaterial.HasProperty(ColorId))
        {
            error = "메모리 타겟의 Material은 _BaseColor 또는 _Color를 지원해야 합니다.";
            return false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider targetCollider in colliders)
        {
            bool canReceiveRaycast =
                (Physics.DefaultRaycastLayers &
                (1 << targetCollider.gameObject.layer)) != 0;

            if (targetCollider.enabled &&
                !targetCollider.isTrigger &&
                canReceiveRaycast)
            {
                return true;
            }
        }

        error = "메모리 타겟의 Collider를 활성화하고 Is Trigger를 꺼 주세요. Layer는 Default로 설정하세요.";
        return false;
    }

    // 메모리 타겟 초기화: 자동 제거 시간을 설정하지 않는다.
    public void InitializeForMemory(TargetSpawner owner, int index)
    {
        IsMemoryTarget = true;
        MemoryOwner = owner;
        MemoryIndex = index;

        isHit = false;
        scoreManager = null;
        onTimeAttackHit = null;
        onReactionHit = null;

        if (memoryRenderer == null)
        {
            memoryRenderer = GetComponentInChildren<Renderer>();
        }

        SetMemoryCue(false);
    }

    // 기억할 순서를 보여줄 때 호출한다.
    public void SetMemoryCue(bool highlighted)
    {
        if (!IsMemoryTarget)
            return;

        memoryFeedbackRemaining = 0f;

        ApplyMemoryColor(
            highlighted ? memoryCueColor : memoryIdleColor
        );
    }

    // 정답 타겟을 맞혔을 때 잠깐 초록색으로 표시한다.
    public void ShowMemoryCorrect()
    {
        if (!IsMemoryTarget)
            return;

        memoryFeedbackRemaining =
            Mathf.Max(0.01f, memoryFeedbackDuration);

        ApplyMemoryColor(memoryCorrectColor);
    }

    private void ApplyMemoryColor(Color color)
    {
        if (memoryRenderer == null)
            return;

        if (memoryPropertyBlock == null)
        {
            memoryPropertyBlock = new MaterialPropertyBlock();
        }

        // 공유 머티리얼을 직접 변경하지 않고 이 타겟의 색만 바꾼다.
        memoryRenderer.GetPropertyBlock(memoryPropertyBlock);

        memoryPropertyBlock.SetColor(BaseColorId, color);
        memoryPropertyBlock.SetColor(ColorId, color);

        memoryRenderer.SetPropertyBlock(memoryPropertyBlock);
    }

    public void Hit()
    {
        // 메모리 타겟의 정답 판정은 GameManager에서 처리한다.
        // 같은 타겟이 다시 등장할 수 있으므로 제거하지 않는다.
        if (IsMemoryTarget)
            return;

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