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

        // 일반 모드의 통계는 ShootingStats에서 기록한다.
        // 메모리 모드의 통계는 GameManager에서 기록한다.
        if (!isMemory)
        {
            if (shootingStats == null)
                return;

            shootingStats.RegisterShot();
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

        // 메모리 모드는 빗나간 사격도 판정해야 한다.
        if (isMemory)
        {
            HandleMemoryShot(target);
            return;
        }

        // 일반 모드는 현재 구역에서 생성한 타겟만 처리한다.
        if (target == null ||
            gameManager.ActiveSpawner == null ||
            !gameManager.ActiveSpawner.OwnsTarget(target))
        {
            return;
        }

        shootingStats.RegisterHit(target.Type);

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
        // null이면 허공 또는 타겟이 아닌 물체에 맞은 것이다.
        // 다른 구역의 타겟인지도 GameManager에서 검사한다.
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
            // 오답 또는 빗나감에는 기존 금지 타겟 효과음을 사용한다.
            PlaySound(forbiddenHitClip);

            if (crosshairUI != null)
            {
                crosshairUI.ShowHit(true);
            }
        }

        // 메모리 타겟에는 Hit()를 호출하지 않는다.
        // 타겟을 유지한 채 GameManager가 정답 순서를 관리한다.
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