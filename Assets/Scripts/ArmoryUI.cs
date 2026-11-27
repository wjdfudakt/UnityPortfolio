using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArmoryUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text equippedText;

    [Header("같은 번호에 같은 총기의 데이터와 버튼을 연결")]
    [SerializeField] private WeaponData[] weapons;
    [SerializeField] private Button[] weaponButtons;

    private PlayerWeapon playerWeapon;

    private void OnEnable()
    {
        RefreshUI();
    }

    private void LateUpdate()
    {
        RefreshUI();
    }

    private void FindPlayerWeapon()
    {
        if (playerWeapon != null)
            return;

        if (gameManager == null || gameManager.Player == null)
            return;

        // 실행 중 생성된 플레이어에서 장착 컴포넌트를 찾는다.
        playerWeapon =
            gameManager.Player.GetComponent<PlayerWeapon>();
    }

    public void EquipWeapon(int index)
    {
        if (!isActiveAndEnabled ||
            gameManager == null ||
            !gameManager.CanChangeWeapon)
        {
            return;
        }

        if (weapons == null || index < 0 || index >= weapons.Length)
            return;

        FindPlayerWeapon();

        if (playerWeapon == null || weapons[index] == null)
            return;

        // 모델 교체와 마지막 장착 총기 저장은 PlayerWeapon에서 처리한다.
        if (playerWeapon.Equip(weapons[index]))
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        FindPlayerWeapon();

        WeaponData equippedWeapon = playerWeapon != null
            ? playerWeapon.EquippedWeapon
            : null;

        if (equippedText != null)
        {
            equippedText.text = equippedWeapon != null
                ? "현재 장착: " + equippedWeapon.DisplayName
                : "장착된 총기 없음";
        }

        if (weaponButtons == null)
            return;

        bool canChange =
            gameManager != null &&
            gameManager.CanChangeWeapon &&
            playerWeapon != null;

        for (int i = 0; i < weaponButtons.Length; i++)
        {
            Button button = weaponButtons[i];

            if (button == null)
                continue;

            WeaponData weapon =
                weapons != null && i < weapons.Length
                    ? weapons[i]
                    : null;

            // 이미 장착한 총기의 버튼은 다시 누를 필요가 없다.
            button.interactable =
                canChange &&
                weapon != null &&
                weapon.ModelPrefab != null &&
                weapon != equippedWeapon;
        }
    }
}