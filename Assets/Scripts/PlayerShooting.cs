using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private Camera shootingCamera;
    [SerializeField] private float shootingDistance = 100f;

    private void Update()
    {
        if (Mouse.current == null || shootingCamera == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = shootingCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, shootingDistance))
        {
            ShootingTarget target = hit.collider.GetComponent<ShootingTarget>();

            if (target != null)
            {
                target.Hit();
            }
        }
    }
}