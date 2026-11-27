using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class AmmoUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerShooting playerShooting;

    [Header("텍스트")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text reloadText;

    [Header("색상")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField]
    private Color emptyColor =
        new Color(1f, 0.35f, 0.35f);
    [SerializeField]
    private Color reloadingColor =
        new Color(1f, 0.8f, 0.25f);

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // 표시 전용 UI이므로 마우스 입력을 막지 않는다.
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
    }

    private void LateUpdate()
    {
        bool visible =
            gameManager != null &&
            playerShooting != null &&
            gameManager.IsPlaying &&
            !gameManager.IsTracking &&
            !gameManager.IsAchievementsOpen &&
            playerShooting.MagazineSize > 0;

        canvasGroup.alpha = visible ? 1f : 0f;

        if (!visible)
            return;

        int current = playerShooting.CurrentAmmo;
        int capacity = playerShooting.MagazineSize;

        if (ammoText != null)
        {
            ammoText.text = $"{current} / {capacity}";
            ammoText.color = current > 0 ? normalColor : emptyColor;
        }

        if (reloadText == null)
            return;

        if (playerShooting.IsReloading)
        {
            reloadText.text =
                $"재장전 중... {playerShooting.ReloadRemaining:F1}초";

            reloadText.color = reloadingColor;
        }
        else if (current <= 0)
        {
            reloadText.text = "탄약 없음 · R : 재장전";
            reloadText.color = emptyColor;
        }
        else if (current < capacity)
        {
            reloadText.text = "R : 재장전";
            reloadText.color = normalColor;
        }
        else
        {
            reloadText.text = "";
            reloadText.color = normalColor;
        }
    }
}