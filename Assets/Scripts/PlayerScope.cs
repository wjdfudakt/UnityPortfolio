using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-75)]
public class PlayerScope : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerShooting playerShooting;

    [Header("조준경 UI")]
    [SerializeField] private GameObject scopeOverlay;
    [SerializeField] private CanvasGroup normalCrosshairGroup;

    private HubPlayerController player;
    private PlayerWeapon playerWeapon;
    private Camera viewCamera;
    private WeaponData lastWeapon;

    private float normalFieldOfView;
    private float scopeBlend;

    private bool crosshairHidden;
    private float savedCrosshairAlpha = 1f;

    public bool IsAiming { get; private set; }
    public float ScopeBlend => scopeBlend;

    private void Awake()
    {
        ResetScope();
    }

    private void Update()
    {
        if (!BindPlayer())
        {
            ResetScope();
            return;
        }

        WeaponData weapon = playerWeapon.EquippedWeapon;

        if (weapon != lastWeapon)
        {
            ResetScope();
            lastWeapon = weapon;
        }

        if (!CanUseScope(weapon) || Mouse.current == null)
        {
            ResetScope();
            return;
        }

        IsAiming = Mouse.current.rightButton.isPressed;

        float targetBlend = IsAiming ? 1f : 0f;

        float speed = Mathf.Max(
            0.1f,
            weapon.ScopeTransitionSpeed
        );

        float blendFactor =
            1f - Mathf.Exp(-speed * Time.deltaTime);

        scopeBlend = Mathf.Lerp(
            scopeBlend,
            targetBlend,
            blendFactor
        );

        if (Mathf.Abs(scopeBlend - targetBlend) < 0.001f)
        {
            scopeBlend = targetBlend;
        }

        float scopedFieldOfView = Mathf.Min(
            normalFieldOfView,
            Mathf.Clamp(weapon.ScopeFieldOfView, 5f, 90f)
        );

        viewCamera.fieldOfView = Mathf.Lerp(
            normalFieldOfView,
            scopedFieldOfView,
            scopeBlend
        );

        float sensitivityMultiplier = Mathf.Lerp(
            1f,
            Mathf.Clamp(
                weapon.ScopeSensitivityMultiplier,
                0.05f,
                1f
            ),
            scopeBlend
        );

        player.SetLookSensitivityMultiplier(sensitivityMultiplier);

        // 확대를 해제하는 중에도 원래 시야로 돌아올 때까지
        // 조준경 화면을 유지한다.
        SetScopeVisuals(IsAiming || scopeBlend > 0f);
    }

    private void LateUpdate()
    {
        // 사격 처리로 결과창이 열리거나 재장전이 시작된 경우,
        // 같은 프레임 안에 조준경을 해제한다.
        if (playerWeapon == null ||
            playerWeapon.EquippedWeapon != lastWeapon ||
            !CanUseScope(lastWeapon))
        {
            ResetScope();
            return;
        }

        SetScopeVisuals(IsAiming || scopeBlend > 0f);
    }

    private bool CanUseScope(WeaponData weapon)
    {
        return gameManager != null &&
               player != null &&
               viewCamera != null &&
               scopeOverlay != null &&
               gameManager.CanLook &&
               (
                   gameManager.CanMove ||
                   (
                       gameManager.IsPlaying &&
                       !gameManager.IsTracking
                   )
               ) &&
               weapon != null &&
               weapon.CanUseScope &&
               playerShooting != null &&
               !playerShooting.IsReloading;
    }

    private bool BindPlayer()
    {
        HubPlayerController nextPlayer =
            gameManager != null ? gameManager.Player : null;

        if (nextPlayer != player)
        {
            ResetScope();

            player = nextPlayer;
            playerWeapon = null;
            viewCamera = null;
            lastWeapon = null;

            if (player != null)
            {
                playerWeapon = player.GetComponent<PlayerWeapon>();
                viewCamera = player.ViewCamera;

                if (viewCamera != null)
                {
                    normalFieldOfView = viewCamera.fieldOfView;
                }
            }
        }

        return player != null &&
               playerWeapon != null &&
               viewCamera != null;
    }

    private void SetScopeVisuals(bool visible)
    {
        if (scopeOverlay != null &&
            scopeOverlay.activeSelf != visible)
        {
            scopeOverlay.SetActive(visible);
        }

        if (playerWeapon != null)
        {
            playerWeapon.SetModelVisible(!visible);
        }

        if (normalCrosshairGroup == null)
            return;

        if (visible)
        {
            if (!crosshairHidden)
            {
                savedCrosshairAlpha = normalCrosshairGroup.alpha;
                crosshairHidden = true;
            }

            normalCrosshairGroup.alpha = 0f;
        }
        else if (crosshairHidden)
        {
            normalCrosshairGroup.alpha = savedCrosshairAlpha;
            crosshairHidden = false;
        }
    }

    public void ResetScope()
    {
        IsAiming = false;
        scopeBlend = 0f;

        if (viewCamera != null)
        {
            viewCamera.fieldOfView = normalFieldOfView;
        }

        if (player != null)
        {
            player.SetLookSensitivityMultiplier(1f);
        }

        SetScopeVisuals(false);
    }

    private void OnDisable()
    {
        ResetScope();
    }
}