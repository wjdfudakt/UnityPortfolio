using System;
using UnityEngine;

// 도전과제 목록의 분류
public enum AchievementCategory
{
    Common,
    Round,
    TimeAttack,
    ReactionTime,
    Tracking,
    Memory
}

// 도전과제의 등급
public enum AchievementGrade
{
    Bronze,
    Silver,
    Gold,
    Platinum
}

// 어떤 기록으로 달성 여부를 판정할지 구분한다.
public enum AchievementCondition
{
    // 공통
    CompleteAnyMode,
    CompleteWithPerfectAccuracy,
    TotalTargetHits,
    CompleteAllModes,

    // 라운드
    ClearRound,
    ClearRoundWithoutForbiddenHits,

    // 타임어택
    TimeAttackHits,
    TimeAttackHitsWithAccuracy,

    // 반응속도
    SingleReactionTime,
    AverageReactionTime,

    // 트래킹
    TrackingSurvivalTime,
    TrackingContinuousAimTime,

    // 기억력
    ClearMemoryRound,
    ClearMemoryRoundWithoutOrderErrors
}

// 도전과제 한 개의 고정 정보를 담는다.
// 진행도와 달성 여부는 AchievementManager에서 별도로 관리한다.
[Serializable]
public class AchievementData
{
    [SerializeField] private string id;
    [SerializeField] private string title;

    [SerializeField, TextArea(2, 4)]
    private string description;

    [SerializeField] private AchievementCategory category;
    [SerializeField] private AchievementGrade grade;
    [SerializeField] private AchievementCondition condition;

    [SerializeField] private float targetValue;

    // 복합 조건에 사용하는 추가 기준
    [SerializeField] private int minimumShots;
    [SerializeField] private int minimumMeasurements;
    [SerializeField] private float minimumAccuracy;

    public string Id => id;
    public string Title => title;
    public string Description => description;

    public AchievementCategory Category => category;
    public AchievementGrade Grade => grade;
    public AchievementCondition Condition => condition;

    public float TargetValue => targetValue;

    public int MinimumShots => minimumShots;
    public int MinimumMeasurements => minimumMeasurements;
    public float MinimumAccuracy => minimumAccuracy;

    // 반응속도 과제는 목표값 이하일 때 달성한다.
    // 다른 수치 과제는 목표값 이상일 때 달성한다.
    public bool IsLowerBetter =>
        condition == AchievementCondition.SingleReactionTime ||
        condition == AchievementCondition.AverageReactionTime;

    public AchievementData(
        string id,
        string title,
        string description,
        AchievementCategory category,
        AchievementGrade grade,
        AchievementCondition condition,
        float targetValue,
        int minimumShots = 0,
        int minimumMeasurements = 0,
        float minimumAccuracy = 0f)
    {
        this.id = id;
        this.title = title;
        this.description = description;

        this.category = category;
        this.grade = grade;
        this.condition = condition;

        this.targetValue = Mathf.Max(0f, targetValue);

        this.minimumShots = Mathf.Max(0, minimumShots);
        this.minimumMeasurements = Mathf.Max(
            0,
            minimumMeasurements
        );

        this.minimumAccuracy = Mathf.Clamp(
            minimumAccuracy,
            0f,
            100f
        );
    }
}