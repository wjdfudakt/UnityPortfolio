using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("카메라")]
    [SerializeField] private Camera shootingCamera;

    [Header("게임 연결")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CrosshairUI crosshairUI;
    [SerializeField] private ShootingStats shootingStats;
    [SerializeField] private PlayerWeapon playerWeapon;

    [Header("효과음")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip forbiddenHitClip;

    [Header("실행 중 상태 확인용 - 직접 수정하지 않음")]
    [SerializeField] private int currentAmmo;
    [SerializeField] private bool isReloading;
    [SerializeField] private float reloadRemaining;

    private WeaponData currentWeapon;
    private float nextShotTime;

    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => isReloading;
    public float ReloadRemaining => reloadRemaining;

    public int MagazineSize =>
        currentWeapon != null
            ? Mathf.Max(1, currentWeapon.MagazineSize)
            : 0;

    private void Awake()
    {
        FindPlayerWeapon();
    }

    public void Configure(GameManager manager, Camera camera)
    {
        ResetFeedback();

        gameManager = manager;
        shootingCamera = camera;

        playerWeapon = shootingCamera != null
            ? shootingCamera.GetComponentInParent<PlayerWeapon>()
            : null;

        currentWeapon = null;
        currentAmmo = 0;

        FindPlayerWeapon();
        SyncEquippedWeapon();
    }

    private void FindPlayerWeapon()
    {
        if (playerWeapon != null)
            return;

        if (shootingCamera != null)
        {
            playerWeapon =
                shootingCamera.GetComponentInParent<PlayerWeapon>();
        }

        if (playerWeapon == null)
        {
            playerWeapon = GetComponentInParent<PlayerWeapon>();
        }
    }

    private void SyncEquippedWeapon()
    {
        FindPlayerWeapon();

        WeaponData equippedWeapon = playerWeapon != null
            ? playerWeapon.EquippedWeapon
            : null;

        if (currentWeapon == equippedWeapon)
            return;

        // 총기가 변경되면 이전 재장전을 취소하고 새 탄창을 채운다.
        ResetFeedback();

        currentWeapon = equippedWeapon;
        currentAmmo = MagazineSize;
    }

    private void Update()
    {
        // 허브에서도 총기 장착 상태를 반영한다.
        SyncEquippedWeapon();

        if (gameManager == null ||
            !gameManager.IsPlaying ||
            !gameManager.CanLook ||
            gameManager.IsTracking)
        {
            return;
        }

        if (currentWeapon == null || shootingCamera == null)
            return;

        // 일시정지 중에는 위에서 반환하므로 재장전 시간도 멈춘다.
        UpdateReload();

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            // 재장전을 시작한 프레임에는 사격하지 않는다.
            if (TryStartReload())
                return;
        }

        if (isReloading)
            return;

        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (!gameManager.IsMemory && shootingStats == null)
            return;

        bool automatic =
            currentWeapon.FireMode == WeaponFireMode.Automatic;

        // 반응속도 측정에서는 새 클릭만 인정한다.
        if (gameManager.IsReactionTime)
        {
            automatic = false;
        }

        bool wantsToShoot = automatic
            ? mouse.leftButton.isPressed
            : mouse.leftButton.wasPressedThisFrame;

        if (!wantsToShoot ||
            Time.time < nextShotTime ||
            currentAmmo <= 0)
        {
            return;
        }

        if (!gameManager.TryBeginShot())
            return;

        nextShotTime =
            Time.time + Mathf.Max(0.01f, currentWeapon.FireInterval);

        // 명중으로 라운드가 끝날 수 있으므로 먼저 탄약과 반동을 처리한다.
        currentAmmo--;

        if (playerWeapon != null)
        {
            playerWeapon.PlayRecoil();
        }

        if (gameManager.Player != null)
        {
            gameManager.Player.AddCameraRecoil(
                currentWeapon.CameraRecoilPitch,
                currentWeapon.CameraRecoilSpeed,
                currentWeapon.CameraRecoilReturnSpeed,
                currentWeapon.CameraRecoilReturnDelay,
                currentWeapon.CameraRecoilMaxAngle
            );
        }

        Shoot(currentWeapon);
    }

    private bool TryStartReload()
    {
        if (currentWeapon == null ||
            isReloading ||
            currentAmmo >= MagazineSize)
        {
            return false;
        }

        isReloading = true;
        reloadRemaining = Mathf.Max(
            0.1f,
            currentWeapon.ReloadDuration
        );

        PlaySound(currentWeapon.ReloadClip);
        return true;
    }

    private void UpdateReload()
    {
        if (!isReloading)
            return;

        reloadRemaining = Mathf.Max(
            0f,
            reloadRemaining - Time.deltaTime
        );

        if (reloadRemaining > 0f)
            return;

        // 예비 탄약은 무제한이므로 탄창을 최대치까지 채운다.
        currentAmmo = MagazineSize;
        isReloading = false;
    }

    public void CancelReload()
    {
        if (isReloading && audioSource != null)
        {
            audioSource.Stop();
        }

        isReloading = false;
        reloadRemaining = 0f;
    }

    public void ResetForAttempt()
    {
        SyncEquippedWeapon();
        ResetFeedback();

        // 모드 시작, 다음 라운드, 재도전 시 호출한다.
        currentAmmo = MagazineSize;
    }

    private void Shoot(WeaponData weapon)
    {
        bool isMemory = gameManager.IsMemory;

        // 기존 게임 결과에 사용하는 통계
        if (!isMemory)
        {
            shootingStats.RegisterShot();
        }

        // 실제로 허용된 발사만 기록한다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.RecordShot();
        }

        AudioClip currentShotClip =
            weapon.ShotClip != null ? weapon.ShotClip : shotClip;

        PlaySound(currentShotClip);

        Physics.SyncTransforms();

        Ray ray = shootingCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        ShootingTarget target = null;

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            weapon.ShootingDistance,
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

        // Hit()에서 라운드가 끝날 수 있으므로 먼저 기록한다.
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
            // 빈 공간이나 벽을 맞혀도 순서 진행은 유지한다.
            // 실제 발사는 했으므로 탄약은 소모된다.
            gameManager.SubmitMemoryShot(null);
            return;
        }

        // 정답 명중 기록은 GameManager에서 처리한다.
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
        CancelReload();
        nextShotTime = 0f;

        if (playerWeapon != null)
        {
            playerWeapon.ResetRecoil();
        }

        if (gameManager != null && gameManager.Player != null)
        {
            gameManager.Player.ResetCameraRecoil();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void OnDisable()
    {
        ResetFeedback();
    }
}