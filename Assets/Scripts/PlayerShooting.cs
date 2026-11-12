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
        if (gameManager == null || !gameManager.IsPlaying
            || !gameManager.CanLook || gameManager.IsTracking) return;
        if (Mouse.current == null || shootingCamera == null) return;
        if (Mouse.current.leftButton.wasPressedThisFrame && gameManager.TryBeginShot())
            Shoot();
    }

    private void Shoot()
    {
        if (shootingStats == null) return;
        shootingStats.RegisterShot();
        PlaySound(shotClip);
        Physics.SyncTransforms();
        Ray ray = shootingCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, shootingDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
        ShootingTarget target = hit.collider.GetComponentInParent<ShootingTarget>();
        // 현재 구역에서 생성한 타겟만 명중으로 처리한다.
        if (target == null || gameManager.ActiveSpawner == null
            || !gameManager.ActiveSpawner.OwnsTarget(target)) return;

        shootingStats.RegisterHit(target.Type);
        PlaySound(target.IsForbidden ? forbiddenHitClip : hitClip);
        if (crosshairUI != null) crosshairUI.ShowHit(target.IsForbidden);
        target.Hit();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    public void ResetFeedback()
    {
        if (audioSource != null) audioSource.Stop();
    }
}
