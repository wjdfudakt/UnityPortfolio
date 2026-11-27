using UnityEngine;

public enum WeaponType
{
    Pistol,
    AssaultRifle,
    SniperRifle,
    Shotgun
}

public enum WeaponFireMode
{
    SemiAutomatic,
    Automatic
}

[CreateAssetMenu(
    fileName = "WeaponData",
    menuName = "Aim Trainer/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string weaponId;
    [SerializeField] private string displayName;

    [SerializeField, TextArea(2, 4)]
    private string description;

    [SerializeField] private WeaponType weaponType;
    [SerializeField] private WeaponFireMode fireMode;

    [Header("총기 모델")]
    [SerializeField] private GameObject modelPrefab;

    // WeaponHolder를 기준으로 적용할 위치, 회전, 크기.
    [SerializeField] private Vector3 modelLocalPosition;
    [SerializeField] private Vector3 modelLocalRotation;
    [SerializeField] private Vector3 modelLocalScale = Vector3.one;

    [Header("사격 설정")]
    [SerializeField, Min(0.01f)]
    private float fireInterval = 0.2f;

    [SerializeField, Min(1f)]
    private float shootingDistance = 100f;

    [Header("탄창 및 재장전")]
    [SerializeField, Min(1)]
    private int magazineSize = 12;

    [SerializeField, Min(0.1f)]
    private float reloadDuration = 1.2f;

    [Header("산탄 설정")]
    [SerializeField, Min(1)]
    private int pelletsPerShot = 1;

    [SerializeField, Range(0f, 30f)]
    private float spreadAngle = 0f;

    [Header("총기 모델 반동")]
    [Tooltip("발사 시 카메라 쪽으로 밀리는 거리")]
    [SerializeField, Min(0f)]
    private float recoilDistance = 0.035f;

    [Tooltip("발사 시 총구가 위로 들리는 각도")]
    [SerializeField, Range(0f, 45f)]
    private float recoilAngle = 6f;

    [Tooltip("반동 자세를 향해 움직이는 속도. 클수록 빠르게 튄다.")]
    [SerializeField, Min(0.1f)]
    private float recoilKickSpeed = 35f;

    [Tooltip("기본 자세로 돌아오는 속도. 클수록 빠르게 복귀한다.")]
    [SerializeField, Min(0.1f)]
    private float recoilReturnSpeed = 12f;

    [Header("카메라 반동")]
    [Tooltip("한 발 발사할 때 실제 조준 방향이 위로 올라가는 각도")]
    [SerializeField, Range(0f, 15f)]
    private float cameraRecoilPitch = 1.2f;

    [Tooltip("반동 각도가 적용되는 속도. 클수록 빠르게 올라간다.")]
    [SerializeField, Min(0.1f)]
    private float cameraRecoilSpeed = 25f;

    [Tooltip("사격을 멈춘 뒤 반동이 돌아오는 속도")]
    [SerializeField, Min(0.1f)]
    private float cameraRecoilReturnSpeed = 8f;

    [Tooltip("마지막 발사 후 반동 회복을 시작하기까지의 시간")]
    [SerializeField, Min(0f)]
    private float cameraRecoilReturnDelay = 0.12f;

    [Tooltip("연사로 누적될 수 있는 최대 카메라 반동 각도")]
    [SerializeField, Range(0f, 45f)]
    private float cameraRecoilMaxAngle = 10f;

    [Header("확대 조준")]
    [SerializeField] private bool canUseScope;

    [Tooltip("조준 중 카메라 시야각. 작을수록 더 크게 확대된다.")]
    [SerializeField, Range(5f, 90f)]
    private float scopeFieldOfView = 20f;

    [Tooltip("일반 시야와 확대 시야 사이의 전환 속도")]
    [SerializeField, Min(0.1f)]
    private float scopeTransitionSpeed = 12f;

    [Tooltip("조준 중 마우스 감도 배율")]
    [SerializeField, Range(0.05f, 1f)]
    private float scopeSensitivityMultiplier = 0.35f;

    [Header("효과음")]
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip reloadClip;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public string Description => description;

    public WeaponType Type => weaponType;
    public WeaponFireMode FireMode => fireMode;

    public GameObject ModelPrefab => modelPrefab;
    public Vector3 ModelLocalPosition => modelLocalPosition;
    public Vector3 ModelLocalRotation => modelLocalRotation;
    public Vector3 ModelLocalScale => modelLocalScale;

    public float FireInterval => fireInterval;
    public float ShootingDistance => shootingDistance;

    public int MagazineSize => magazineSize;
    public float ReloadDuration => reloadDuration;

    public int PelletsPerShot => pelletsPerShot;
    public float SpreadAngle => spreadAngle;

    public float RecoilDistance => recoilDistance;
    public float RecoilAngle => recoilAngle;
    public float RecoilKickSpeed => recoilKickSpeed;
    public float RecoilReturnSpeed => recoilReturnSpeed;

    public float CameraRecoilPitch => cameraRecoilPitch;
    public float CameraRecoilSpeed => cameraRecoilSpeed;
    public float CameraRecoilReturnSpeed => cameraRecoilReturnSpeed;
    public float CameraRecoilReturnDelay => cameraRecoilReturnDelay;
    public float CameraRecoilMaxAngle => cameraRecoilMaxAngle;

    public bool CanUseScope => canUseScope;
    public float ScopeFieldOfView => scopeFieldOfView;
    public float ScopeTransitionSpeed => scopeTransitionSpeed;
    public float ScopeSensitivityMultiplier => scopeSensitivityMultiplier;

    public AudioClip ShotClip => shotClip;
    public AudioClip ReloadClip => reloadClip;
}