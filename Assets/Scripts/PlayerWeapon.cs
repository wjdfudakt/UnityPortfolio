using System;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    private const string EquippedWeaponKey = "AimWeapons.v1.EquippedId";

    [Header("모델 생성 위치")]
    [SerializeField] private Transform weaponHolder;

    [Header("사용 가능한 총기 - 첫 번째가 기본 총기")]
    [SerializeField] private WeaponData[] weapons;

    private GameObject currentModel;

    // 반동이 없을 때의 WeaponHolder 자세
    private Vector3 restPosition;
    private Quaternion restRotation;
    private bool hasRestPose;

    // 목표 반동과 실제 화면에 적용되는 반동
    private float targetRecoilDistance;
    private float targetRecoilAngle;
    private float currentRecoilDistance;
    private float currentRecoilAngle;

    public WeaponData EquippedWeapon { get; private set; }

    public event Action<WeaponData> WeaponChanged;

    private void Awake()
    {
        if (weaponHolder == null)
            return;

        restPosition = weaponHolder.localPosition;
        restRotation = weaponHolder.localRotation;
        hasRestPose = true;
    }

    private void Start()
    {
        if (weaponHolder == null || weapons == null || weapons.Length == 0)
        {
            Debug.LogError(
                "PlayerWeapon의 Weapon Holder와 Weapons를 연결해 주세요.",
                this);
            return;
        }

        WeaponData initialWeapon = null;
        string savedId = PlayerPrefs.GetString(EquippedWeaponKey, "");

        foreach (WeaponData weapon in weapons)
        {
            if (weapon == null || weapon.ModelPrefab == null)
                continue;

            if (initialWeapon == null)
                initialWeapon = weapon;

            if (weapon.WeaponId == savedId)
            {
                initialWeapon = weapon;
                break;
            }
        }

        if (initialWeapon == null)
        {
            Debug.LogError("장착할 수 있는 총기 데이터가 없습니다.", this);
            return;
        }

        Equip(initialWeapon);
    }

    private void LateUpdate()
    {
        if (!hasRestPose ||
            weaponHolder == null ||
            EquippedWeapon == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        // 일시정지 중에는 반동도 멈춘다.
        if (deltaTime <= 0f)
            return;

        float kickSpeed = Mathf.Max(
            0.1f,
            EquippedWeapon.RecoilKickSpeed
        );

        float returnSpeed = Mathf.Max(
            0.1f,
            EquippedWeapon.RecoilReturnSpeed
        );

        // 프레임 간격을 반영한 보간 비율
        float kickBlend = 1f - Mathf.Exp(-kickSpeed * deltaTime);

        currentRecoilDistance = Mathf.Lerp(
            currentRecoilDistance,
            targetRecoilDistance,
            kickBlend
        );

        currentRecoilAngle = Mathf.Lerp(
            currentRecoilAngle,
            targetRecoilAngle,
            kickBlend
        );

        // 목표 반동을 점차 줄여 기본 자세로 복귀시킨다.
        float returnFactor = Mathf.Exp(-returnSpeed * deltaTime);

        targetRecoilDistance *= returnFactor;
        targetRecoilAngle *= returnFactor;

        // WeaponHolder의 부모인 PlayerCamera 기준으로 뒤로 이동한다.
        weaponHolder.localPosition =
            restPosition + Vector3.back * currentRecoilDistance;

        // 카메라 기준 X축으로 회전한 뒤 기존 회전을 적용한다.
        // WeaponHolder의 기존 Y축 180도 회전도 유지된다.
        weaponHolder.localRotation =
            Quaternion.Euler(-currentRecoilAngle, 0f, 0f)
            * restRotation;
    }

    public void PlayRecoil()
    {
        if (!hasRestPose ||
            weaponHolder == null ||
            EquippedWeapon == null)
        {
            return;
        }

        float distance = Mathf.Max(0f, EquippedWeapon.RecoilDistance);
        float angle = Mathf.Max(0f, EquippedWeapon.RecoilAngle);

        // 연사하면 남은 반동에 누적한다.
        // 과도하게 밀리지 않도록 목표 반동을 설정값의 2배로 제한한다.
        targetRecoilDistance = Mathf.Min(
            targetRecoilDistance + distance,
            distance * 2f
        );

        targetRecoilAngle = Mathf.Min(
            targetRecoilAngle + angle,
            angle * 2f
        );
    }

    public void ResetRecoil()
    {
        targetRecoilDistance = 0f;
        targetRecoilAngle = 0f;

        currentRecoilDistance = 0f;
        currentRecoilAngle = 0f;

        if (!hasRestPose || weaponHolder == null)
            return;

        weaponHolder.localPosition = restPosition;
        weaponHolder.localRotation = restRotation;
    }

    public bool Equip(WeaponData weapon)
    {
        if (weaponHolder == null || weapon == null ||
            weapon.ModelPrefab == null)
        {
            return false;
        }

        if (weapons == null || Array.IndexOf(weapons, weapon) < 0)
            return false;

        if (string.IsNullOrWhiteSpace(weapon.WeaponId))
        {
            Debug.LogError(
                "총기 데이터의 Weapon Id를 입력해 주세요.",
                weapon);
            return false;
        }

        if (EquippedWeapon == weapon && currentModel != null)
            return true;

        ResetRecoil();

        if (currentModel != null)
        {
            currentModel.SetActive(false);
            Destroy(currentModel);
        }

        currentModel = Instantiate(weapon.ModelPrefab, weaponHolder);

        Transform modelTransform = currentModel.transform;
        modelTransform.localPosition = weapon.ModelLocalPosition;
        modelTransform.localRotation =
            Quaternion.Euler(weapon.ModelLocalRotation);
        modelTransform.localScale = weapon.ModelLocalScale;

        PrepareModel(currentModel);

        EquippedWeapon = weapon;

        PlayerPrefs.SetString(EquippedWeaponKey, weapon.WeaponId);
        PlayerPrefs.Save();

        WeaponChanged?.Invoke(weapon);
        return true;
    }

    private void PrepareModel(GameObject model)
    {
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = ignoreRaycastLayer;
        }

        foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>(true))
        {
            modelCollider.enabled = false;
        }

        foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }
    }

    public void SetModelVisible(bool visible)
    {
        if (currentModel == null)
            return;

        if (currentModel.activeSelf != visible)
        {
            currentModel.SetActive(visible);
        }
    }

    private void OnDisable()
    {
        ResetRecoil();
    }
}