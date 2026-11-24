using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField]
    private Color lockedBackground =
        new Color32(37, 50, 70, 255);

    [SerializeField]
    private Color unlockedBackground =
        new Color32(30, 65, 55, 255);

    [SerializeField]
    private Color lockedTextColor =
        new Color32(190, 195, 205, 255);

    [SerializeField]
    private Color unlockedTextColor =
        new Color32(120, 230, 160, 255);

    private AchievementData data;
    private AchievementManager manager;

    public AchievementData Data => data;

    // 목록을 생성할 때 표시할 도전과제 정보를 전달한다.
    public void Bind(
        AchievementData achievement,
        AchievementManager achievementManager)
    {
        data = achievement;
        manager = achievementManager;

        Refresh();
    }

    // 진행도가 변경되거나 창을 열었을 때 호출한다.
    public void Refresh()
    {
        if (data == null || manager == null)
            return;

        bool unlocked = manager.IsUnlocked(data.Id);

        if (titleText != null)
        {
            titleText.text = data.Title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.Description;
        }

        if (statusText != null)
        {
            statusText.text = unlocked
                ? "달성 완료"
                : "미달성";

            statusText.color = unlocked
                ? unlockedTextColor
                : lockedTextColor;
        }

        if (progressText != null)
        {
            progressText.text = BuildProgressText();

            progressText.color = unlocked
                ? unlockedTextColor
                : lockedTextColor;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = unlocked
                ? unlockedBackground
                : lockedBackground;
        }
    }

    private string BuildProgressText()
    {
        bool hasProgress = manager.HasProgress(data.Id);
        float value = manager.GetProgress(data.Id);
        float target = data.TargetValue;

        // 반응속도는 숫자가 작을수록 좋은 기록이다.
        // 기록이 없으면 0ms로 오해하지 않도록 미측정으로 표시한다.
        if (data.IsLowerBetter)
        {
            if (!hasProgress)
            {
                return "미측정";
            }

            return value.ToString("F1")
                + " / ≤"
                + target.ToString("0.#")
                + " ms";
        }

        // 트래킹 시간은 소수점 첫째 자리까지 표시한다.
        if (data.Condition ==
                AchievementCondition.TrackingSurvivalTime ||
            data.Condition ==
                AchievementCondition.TrackingContinuousAimTime)
        {
            return value.ToString("F1")
                + " / "
                + target.ToString("0.#")
                + " s";
        }

        // 명중 수, 라운드 수, 완료 여부 등은 정수로 표시한다.
        return value.ToString("0")
            + " / "
            + target.ToString("0");
    }
}