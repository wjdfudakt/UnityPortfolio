using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private Camera shootingCamera;
    [SerializeField] private float shootingDistance = 100f;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private CrosshairUI crosshairUI;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip forbiddenHitClip;

    [SerializeField] private ShootingStats shootingStats;

    public void Configure(GameManager manager, Camera camera)
    {
        gameManager = manager;
        shootingCamera = camera;
    }

    private void Update()
    {
        if (gameManager == null ||
            !gameManager.IsPlaying ||
            !gameManager.CanLook ||
            gameManager.IsTracking)
        {
            return;
        }

        if (Mouse.current == null || shootingCamera == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            gameManager.TryBeginShot())
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        bool isMemory = gameManager.IsMemory;

        // 기존 게임 결과에 사용하는 통계
        if (!isMemory)
        {
            if (shootingStats == null)
                return;

            shootingStats.RegisterShot();
        }

        // 실제로 허용된 사격만 도전과제 발사 수에 기록한다.
        // 메모리 모드의 빗나간 사격도 포함한다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.RecordShot();
        }

        PlaySound(shotClip);

        Physics.SyncTransforms();

        Ray ray = shootingCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        ShootingTarget target = null;

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            shootingDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            target = hit.collider.GetComponentInParent<ShootingTarget>();
        }

        if (isMemory)
        {
            HandleMemoryShot(target);
            return;
        }

        // 현재 구역에서 생성한 타겟만 명중으로 처리한다.
        if (target == null ||
            gameManager.ActiveSpawner == null ||
            !gameManager.ActiveSpawner.OwnsTarget(target))
        {
            return;
        }

        shootingStats.RegisterHit(target.Type);

        // Hit()에서 라운드가 끝날 수 있으므로
        // 도전과제 명중 기록을 먼저 반영한다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.RecordHit(target.Type);
        }

        PlaySound(
            target.IsForbidden ? forbiddenHitClip : hitClip
        );

        if (crosshairUI != null)
        {
            crosshairUI.ShowHit(target.IsForbidden);
        }

        target.Hit();
    }

    private void HandleMemoryShot(ShootingTarget target)
    {
        TargetSpawner activeSpawner = gameManager.ActiveSpawner;

        bool hitMemoryTarget =
            target != null &&
            target.isActiveAndEnabled &&
            target.IsMemoryTarget &&
            activeSpawner != null &&
            target.MemoryOwner == activeSpawner &&
            activeSpawner.OwnsTarget(target);

        if (!hitMemoryTarget)
        {
            // 빈 공간이나 벽을 맞히면 진행을 유지한다.
            gameManager.SubmitMemoryShot(null);
            return;
        }

        // 메모리 모드의 정답 명중 기록은
        // GameManager의 정답 판정 안에서 연결한다.
        // 여기에서 중복으로 RecordHit을 호출하지 않는다.
        bool correct = gameManager.SubmitMemoryShot(target);

        if (correct)
        {
            PlaySound(hitClip);

            if (crosshairUI != null)
            {
                crosshairUI.ShowHit(false);
            }
        }
        else
        {
            PlaySound(forbiddenHitClip);

            if (crosshairUI != null)
            {
                crosshairUI.ShowHit(true);
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void ResetFeedback()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}