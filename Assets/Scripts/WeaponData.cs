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

    public AudioClip ShotClip => shotClip;
    public AudioClip ReloadClip => reloadClip;
}