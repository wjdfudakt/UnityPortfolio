using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ModeEntrance : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameMode mode;
    [SerializeField] private string displayName = "Round";
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private Transform playPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(0.1f)] private float enterDuration = 2f;

    private BoxCollider area;
    private float dwellTime;

    public GameMode Mode => mode;
    public TargetSpawner Spawner => spawner;
    public Transform PlayPoint => playPoint;
    public Transform ExitPoint => exitPoint;

    private void Awake()
    {
        area = GetComponent<BoxCollider>();
        area.isTrigger = true;
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.CanEnterZone
            || gameManager.Player == null || !area.enabled)
        {
            ResetDwell();
            return;
        }

        // 플레이어 중심이 입장 상자 안에 있는지 매 프레임 확인한다.
        // 순간이동 후 Trigger Exit 이벤트가 누락되어도 대기 상태가 남지 않는다.
        Vector3 localCenter = transform.InverseTransformPoint(gameManager.Player.CenterPosition);
        bool inside = new Bounds(area.center, area.size).Contains(localCenter);
        if (!inside)
        {
            ResetDwell();
            return;
        }

        dwellTime += Time.deltaTime;
        float duration = Mathf.Max(0.1f, enterDuration);
        gameManager.ShowEntryPrompt(this,
            displayName + "  " + Mathf.Max(0f, duration - dwellTime).ToString("F1") + " s");
        if (dwellTime < duration) return;

        dwellTime = 0f;
        gameManager.TryEnterZone(this);
    }

    private void ResetDwell()
    {
        dwellTime = 0f;
        if (gameManager != null) gameManager.ClearEntryPrompt(this);
    }

    private void OnDisable()
    {
        ResetDwell();
    }
}
