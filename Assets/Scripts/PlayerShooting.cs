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

    private void Update()
    {
        if (GameSettings.SelectedMode == GameMode.Tracking)
            return;

        if (gameManager == null || !gameManager.IsPlaying)
            return;

        if (Mouse.current == null || shootingCamera == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (gameManager.TryBeginShot())
            {
                Shoot();
            }
        }
    }

    private void Shoot()
    {
        shootingStats.RegisterShot();

        PlaySound(shotClip);

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray = shootingCamera.ScreenPointToRay(
            mousePosition
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            shootingDistance))
        {
            ShootingTarget target =
                hit.collider.GetComponentInParent<ShootingTarget>();

            if (target == null)
                return;

            if (target.IsForbidden)
            {
                PlaySound(forbiddenHitClip);
            }
            else
            {
                PlaySound(hitClip);
            }

            shootingStats.RegisterHit(target.Type);

            target.Hit();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}