using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementListUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AchievementManager achievementManager;
    [SerializeField] private AchievementItemUI itemPrefab;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_Text summaryText;

    private readonly List<AchievementItemUI> items =
        new List<AchievementItemUI>();

    private AchievementManager subscribedManager;

    private bool started;
    private bool listCreated;

    // -1은 전체, 0~5는 AchievementCategory의 순서다.
    private int selectedCategory = -1;

    private void Start()
    {
        started = true;
        PrepareAndRefresh();
    }

    private void OnEnable()
    {
        // 처음 활성화할 때는 Start에서 처리한다.
        if (!started)
            return;

        PrepareAndRefresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void PrepareAndRefresh()
    {
        if (!CreateListIfNeeded())
            return;

        Subscribe();

        ApplyFilter();
        RefreshList();
        ScrollToTop();
    }

    private bool CreateListIfNeeded()
    {
        if (listCreated)
            return true;

        if (achievementManager == null)
        {
            achievementManager = AchievementManager.Instance;
        }

        if (achievementManager == null ||
            itemPrefab == null ||
            contentRoot == null ||
            scrollRect == null ||
            summaryText == null)
        {
            Debug.LogError(
                "AchievementListUI의 References를 모두 연결해 주세요.",
                this
            );

            return false;
        }

        if (achievementManager.Achievements == null)
        {
            Debug.LogError(
                "AchievementManager가 아직 초기화되지 않았습니다.",
                this
            );

            return false;
        }

        foreach (AchievementData data in achievementManager.Achievements)
        {
            AchievementItemUI item = Instantiate(
                itemPrefab,
                contentRoot,
                false
            );

            item.name = "Achievement_" + data.Id;

            item.Bind(data, achievementManager);
            item.gameObject.SetActive(true);

            items.Add(item);
        }

        listCreated = true;
        return true;
    }

    private void Subscribe()
    {
        if (subscribedManager == achievementManager)
            return;

        Unsubscribe();

        subscribedManager = achievementManager;

        if (subscribedManager != null)
        {
            subscribedManager.ProgressChanged += RefreshList;
        }
    }

    private void Unsubscribe()
    {
        if (subscribedManager != null)
        {
            subscribedManager.ProgressChanged -= RefreshList;
        }

        subscribedManager = null;
    }

    // 저장된 기록과 현재 진행도를 화면에 반영한다.
    public void RefreshList()
    {
        if (!listCreated || achievementManager == null)
            return;

        foreach (AchievementItemUI item in items)
        {
            if (item != null && item.gameObject.activeSelf)
            {
                item.Refresh();
            }
        }

        // 분류 필터와 관계없이 전체 달성률을 표시한다.
        if (summaryText != null)
        {
            summaryText.text =
                achievementManager.UnlockedCount
                + " / "
                + achievementManager.TotalCount
                + "   ("
                + achievementManager.CompletionPercent.ToString("F1")
                + "%)";
        }
    }

    // 전체 보기 버튼에서 호출한다.
    public void ShowAll()
    {
        SetFilter(-1);
    }

    // 분류 버튼에서 정수 값을 전달한다.
    // 0: 공통
    // 1: 라운드
    // 2: 타임어택
    // 3: 반응속도
    // 4: 트래킹
    // 5: 기억력
    public void SetFilter(int categoryIndex)
    {
        if (categoryIndex < -1 ||
            categoryIndex > (int)AchievementCategory.Memory)
        {
            return;
        }

        selectedCategory = categoryIndex;

        if (!isActiveAndEnabled)
            return;

        if (!CreateListIfNeeded())
            return;

        Subscribe();

        ApplyFilter();
        RefreshList();
        ScrollToTop();
    }

    private void ApplyFilter()
    {
        foreach (AchievementItemUI item in items)
        {
            if (item == null || item.Data == null)
                continue;

            bool visible =
                selectedCategory == -1 ||
                (int)item.Data.Category == selectedCategory;

            item.gameObject.SetActive(visible);
        }
    }

    private void ScrollToTop()
    {
        if (scrollRect == null ||
            contentRoot == null ||
            !gameObject.activeInHierarchy)
        {
            return;
        }

        // 항목 개수에 따른 Content 높이를 먼저 반영한다.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}