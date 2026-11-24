using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField, Min(0.5f)]
    private float autoSaveInterval = 5f;

    // UI에서 구독할 이벤트
    public event Action<AchievementData> AchievementUnlocked;
    public event Action ProgressChanged;

    private const string SavePrefix = "AimAchievements.v1.";

    private readonly List<AchievementData> achievements =
        new List<AchievementData>();

    private readonly Dictionary<string, AchievementData> dataById =
        new Dictionary<string, AchievementData>();

    private readonly Dictionary<string, ProgressState> progressById =
        new Dictionary<string, ProgressState>();

    private IReadOnlyList<AchievementData> readOnlyAchievements;

    // 도전과제별 저장 상태
    private class ProgressState
    {
        public float value;
        public bool hasValue;
        public bool unlocked;
    }

    // 게임을 종료해도 유지되는 기록
    private int totalTargetHits;
    private int completedModeMask;

    // 한 번 입장한 동안 유지되는 기록
    private bool sessionActive;
    private GameMode sessionMode;
    private int sessionForbiddenHits;
    private int sessionMemoryOrderErrors;

    // 현재 라운드 또는 측정 1회의 기록
    private bool attemptActive;
    private int attemptRound;
    private int attemptShots;
    private int attemptValidHits;

    private int reactionMeasurementCount;
    private double reactionTotalMilliseconds;

    // 저장 및 UI 갱신 상태
    private bool initialized;
    private bool saveDirty;
    private bool notifyPending;
    private float nextSaveTime;

    public IReadOnlyList<AchievementData> Achievements =>
        readOnlyAchievements;

    public int TotalCount => achievements.Count;

    public int TotalTargetHits => totalTargetHits;

    public int UnlockedCount
    {
        get
        {
            int count = 0;

            foreach (ProgressState state in progressById.Values)
            {
                if (state.unlocked)
                    count++;
            }

            return count;
        }
    }

    public float CompletionPercent =>
        TotalCount > 0
            ? (float)UnlockedCount / TotalCount * 100f
            : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        BuildDefinitions();

        readOnlyAchievements = achievements.AsReadOnly();

        LoadProgress();

        initialized = true;

        nextSaveTime =
            Time.unscaledTime + Mathf.Max(0.5f, autoSaveInterval);
    }

    private void Update()
    {
        if (!initialized)
            return;

        // 여러 기록이 같은 프레임에 변경되어도
        // 목록 UI 갱신 이벤트는 한 번만 보낸다.
        if (notifyPending)
        {
            notifyPending = false;
            ProgressChanged?.Invoke();
        }

        if (saveDirty && Time.unscaledTime >= nextSaveTime)
        {
            SaveProgress();
        }
    }

    // --------------------------------------------------
    // 도전과제 목록
    // ID는 저장에 사용하므로 출시 후 임의로 변경하지 않는다.
    // --------------------------------------------------

    private void BuildDefinitions()
    {
        achievements.Clear();
        dataById.Clear();
        progressById.Clear();

        // 공통: 4개

        Add(
            "common_first",
            "첫 발걸음",
            "아무 모드나 1회 완료",
            AchievementCategory.Common,
            AchievementGrade.Bronze,
            AchievementCondition.CompleteAnyMode,
            1f
        );

        Add(
            "common_perfect_accuracy",
            "백발백중",
            "20발 이상 사격하고 명중률 100%로 1회 완료",
            AchievementCategory.Common,
            AchievementGrade.Gold,
            AchievementCondition.CompleteWithPerfectAccuracy,
            1f,
            minimumShots: 20,
            minimumAccuracy: 100f
        );

        Add(
            "common_total_hits",
            "숙련된 사수",
            "누적 타겟 1,000개 명중",
            AchievementCategory.Common,
            AchievementGrade.Silver,
            AchievementCondition.TotalTargetHits,
            1000f
        );

        Add(
            "common_all_modes",
            "만능 사수",
            "모든 모드 1회 완료",
            AchievementCategory.Common,
            AchievementGrade.Silver,
            AchievementCondition.CompleteAllModes,
            5f
        );

        // 라운드: 4개

        Add(
            "round_clear_5",
            "준비 운동",
            "5라운드 클리어",
            AchievementCategory.Round,
            AchievementGrade.Bronze,
            AchievementCondition.ClearRound,
            5f
        );

        Add(
            "round_clear_15",
            "한계 돌파",
            "15라운드 클리어",
            AchievementCategory.Round,
            AchievementGrade.Silver,
            AchievementCondition.ClearRound,
            15f
        );

        Add(
            "round_clear_30",
            "끝없는 도전",
            "30라운드 클리어",
            AchievementCategory.Round,
            AchievementGrade.Platinum,
            AchievementCondition.ClearRound,
            30f
        );

        Add(
            "round_clean_10",
            "신중한 사수",
            "한 번 입장한 동안 금지 타겟 명중 없이 1~10라운드 클리어",
            AchievementCategory.Round,
            AchievementGrade.Gold,
            AchievementCondition.ClearRoundWithoutForbiddenHits,
            10f
        );

        // 타임어택: 3개

        Add(
            "timeattack_hits_30",
            "속사",
            "한 게임에서 타겟 30개 명중",
            AchievementCategory.TimeAttack,
            AchievementGrade.Bronze,
            AchievementCondition.TimeAttackHits,
            30f
        );

        Add(
            "timeattack_hits_50",
            "쏟아지는 탄환",
            "한 게임에서 타겟 50개 명중",
            AchievementCategory.TimeAttack,
            AchievementGrade.Gold,
            AchievementCondition.TimeAttackHits,
            50f
        );

        Add(
            "timeattack_accurate_40",
            "속도와 정확성",
            "한 게임을 40개 이상 명중하고 명중률 95% 이상으로 완료",
            AchievementCategory.TimeAttack,
            AchievementGrade.Gold,
            AchievementCondition.TimeAttackHitsWithAccuracy,
            40f,
            minimumAccuracy: 95f
        );

        // 반응속도: 3개
        // 목표값의 단위는 밀리초(ms)다.

        Add(
            "reaction_single_300",
            "빠른 손",
            "단일 반응속도 300ms 이하 달성",
            AchievementCategory.ReactionTime,
            AchievementGrade.Bronze,
            AchievementCondition.SingleReactionTime,
            300f
        );

        Add(
            "reaction_single_200",
            "찰나의 순간",
            "단일 반응속도 200ms 이하 달성",
            AchievementCategory.ReactionTime,
            AchievementGrade.Gold,
            AchievementCondition.SingleReactionTime,
            200f
        );

        Add(
            "reaction_average_250",
            "흔들림 없는 집중력",
            "5회 이상 측정한 테스트를 평균 반응속도 250ms 이하로 완료",
            AchievementCategory.ReactionTime,
            AchievementGrade.Gold,
            AchievementCondition.AverageReactionTime,
            250f,
            minimumMeasurements: 5
        );

        // 트래킹: 3개
        // 목표값의 단위는 초다.

        Add(
            "tracking_survive_30",
            "놓치지 않는다",
            "30초 생존",
            AchievementCategory.Tracking,
            AchievementGrade.Bronze,
            AchievementCondition.TrackingSurvivalTime,
            30f
        );

        Add(
            "tracking_survive_60",
            "집요한 추적자",
            "60초 생존",
            AchievementCategory.Tracking,
            AchievementGrade.Gold,
            AchievementCondition.TrackingSurvivalTime,
            60f
        );

        Add(
            "tracking_hold_10",
            "완벽한 추적",
            "10초 동안 연속 조준 유지",
            AchievementCategory.Tracking,
            AchievementGrade.Gold,
            AchievementCondition.TrackingContinuousAimTime,
            10f
        );

        // 기억력: 3개

        Add(
            "memory_clear_5",
            "좋은 기억력",
            "기억력 모드 5라운드 클리어",
            AchievementCategory.Memory,
            AchievementGrade.Bronze,
            AchievementCondition.ClearMemoryRound,
            5f
        );

        Add(
            "memory_clear_10",
            "사진 같은 기억",
            "기억력 모드 10라운드 클리어",
            AchievementCategory.Memory,
            AchievementGrade.Silver,
            AchievementCondition.ClearMemoryRound,
            10f
        );

        Add(
            "memory_clean_10",
            "완벽한 기억",
            "한 번 입장한 동안 순서 오류 없이 기억력 1~10라운드 클리어",
            AchievementCategory.Memory,
            AchievementGrade.Gold,
            AchievementCondition.ClearMemoryRoundWithoutOrderErrors,
            10f
        );
    }

    private void Add(
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
        AchievementData data = new AchievementData(
            id,
            title,
            description,
            category,
            grade,
            condition,
            targetValue,
            minimumShots,
            minimumMeasurements,
            minimumAccuracy
        );

        achievements.Add(data);
        dataById.Add(id, data);
        progressById.Add(id, new ProgressState());
    }

    // --------------------------------------------------
    // UI에서 조회할 정보
    // --------------------------------------------------

    public AchievementData GetAchievement(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        dataById.TryGetValue(id, out AchievementData data);
        return data;
    }

    public bool IsUnlocked(string id)
    {
        return !string.IsNullOrEmpty(id)
            && progressById.TryGetValue(id, out ProgressState state)
            && state.unlocked;
    }

    public bool HasProgress(string id)
    {
        return !string.IsNullOrEmpty(id)
            && progressById.TryGetValue(id, out ProgressState state)
            && state.hasValue;
    }

    public float GetProgress(string id)
    {
        if (string.IsNullOrEmpty(id))
            return 0f;

        if (!progressById.TryGetValue(id, out ProgressState state))
            return 0f;

        return state.value;
    }

    // --------------------------------------------------
    // 입장·라운드 시작·퇴장
    // --------------------------------------------------

    public void BeginSession(GameMode mode)
    {
        if (!initialized)
            return;

        SaveProgress();

        sessionMode = mode;
        sessionActive = true;
        attemptActive = false;

        // 재입장할 때만 초기화한다.
        sessionForbiddenHits = 0;
        sessionMemoryOrderErrors = 0;
    }

    public void BeginAttempt(int round)
    {
        if (!initialized || !sessionActive)
            return;

        attemptActive = true;
        attemptRound = Mathf.Max(1, round);

        attemptShots = 0;
        attemptValidHits = 0;

        reactionMeasurementCount = 0;
        reactionTotalMilliseconds = 0d;

        // 재도전과 자동 라운드 진행에서는
        // sessionForbiddenHits와 sessionMemoryOrderErrors를 유지한다.
    }

    public void EndSession()
    {
        if (!initialized)
            return;

        attemptActive = false;
        sessionActive = false;

        SaveProgress();
    }

    // --------------------------------------------------
    // 사격 및 명중 기록
    // --------------------------------------------------

    public void RecordShot()
    {
        if (!CanRecord() || sessionMode == GameMode.Tracking)
            return;

        if (attemptShots < int.MaxValue)
        {
            attemptShots++;
        }
    }

    public void RecordHit(TargetType type)
    {
        if (!CanRecord() || sessionMode == GameMode.Tracking)
            return;

        if (type == TargetType.Forbidden)
        {
            if (sessionMode == GameMode.Round &&
                sessionForbiddenHits < int.MaxValue)
            {
                sessionForbiddenHits++;
            }

            // 금지 타겟은 유효 명중과 누적 명중에서 제외한다.
            return;
        }

        if (attemptValidHits < int.MaxValue)
        {
            attemptValidHits++;
        }

        if (totalTargetHits < int.MaxValue)
        {
            totalTargetHits++;
        }

        MarkDirty();

        ReportProgress(
            AchievementCondition.TotalTargetHits,
            totalTargetHits
        );

        if (sessionMode == GameMode.TimeAttack)
        {
            ReportProgress(
                AchievementCondition.TimeAttackHits,
                attemptValidHits
            );
        }
    }

    public void RecordMemoryOrderError()
    {
        if (!CanRecord() || sessionMode != GameMode.Memory)
            return;

        if (sessionMemoryOrderErrors < int.MaxValue)
        {
            sessionMemoryOrderErrors++;
        }
    }

    // --------------------------------------------------
    // 반응속도 및 트래킹 기록
    // --------------------------------------------------

    public void RecordReaction(float milliseconds)
    {
        if (!CanRecord() ||
            sessionMode != GameMode.ReactionTime ||
            !IsFinite(milliseconds) ||
            milliseconds <= 0f)
        {
            return;
        }

        reactionMeasurementCount++;
        reactionTotalMilliseconds += milliseconds;

        ReportProgress(
            AchievementCondition.SingleReactionTime,
            milliseconds
        );
    }

    public void RecordTracking(
        float survivalSeconds,
        float consecutiveAimSeconds)
    {
        if (!CanRecord() || sessionMode != GameMode.Tracking)
            return;

        if (IsFinite(survivalSeconds) && survivalSeconds >= 0f)
        {
            ReportProgress(
                AchievementCondition.TrackingSurvivalTime,
                survivalSeconds
            );
        }

        if (IsFinite(consecutiveAimSeconds) &&
            consecutiveAimSeconds >= 0f)
        {
            ReportProgress(
                AchievementCondition.TrackingContinuousAimTime,
                consecutiveAimSeconds
            );
        }
    }

    // --------------------------------------------------
    // 한 라운드 또는 한 번의 측정 종료
    // completed:
    // 라운드·기억력은 클리어 시 true
    // 타임어택·반응속도는 정상 측정 종료 시 true
    // 트래킹은 생존 종료 시 true
    // 취소할 때는 이 함수를 호출하지 않고 EndSession을 호출한다.
    // --------------------------------------------------

    public void FinishAttempt(bool completed)
    {
        if (!CanRecord())
            return;

        // 같은 결과가 두 번 집계되지 않도록 한다.
        attemptActive = false;

        if (!completed)
        {
            SaveProgress();
            return;
        }

        completedModeMask |= GetModeBit(sessionMode);
        MarkDirty();

        ReportProgress(
            AchievementCondition.CompleteAnyMode,
            1f
        );

        ReportProgress(
            AchievementCondition.CompleteAllModes,
            CountCompletedModes()
        );

        CheckPerfectAccuracy();

        switch (sessionMode)
        {
            case GameMode.Round:
                ReportProgress(
                    AchievementCondition.ClearRound,
                    attemptRound
                );

                if (sessionForbiddenHits == 0)
                {
                    ReportProgress(
                        AchievementCondition.ClearRoundWithoutForbiddenHits,
                        attemptRound
                    );
                }
                break;

            case GameMode.TimeAttack:
                CheckTimeAttackAccuracy();
                break;

            case GameMode.ReactionTime:
                CheckAverageReaction();
                break;

            case GameMode.Memory:
                ReportProgress(
                    AchievementCondition.ClearMemoryRound,
                    attemptRound
                );

                if (sessionMemoryOrderErrors == 0)
                {
                    ReportProgress(
                        AchievementCondition.ClearMemoryRoundWithoutOrderErrors,
                        attemptRound
                    );
                }
                break;
        }

        SaveProgress();
    }

    private void CheckPerfectAccuracy()
    {
        foreach (AchievementData data in achievements)
        {
            if (data.Condition !=
                AchievementCondition.CompleteWithPerfectAccuracy)
            {
                continue;
            }

            if (attemptShots >= data.MinimumShots &&
                attemptShots > 0 &&
                attemptValidHits == attemptShots)
            {
                UpdateProgress(data, 1f);
            }
        }
    }

    private void CheckTimeAttackAccuracy()
    {
        float accuracy = attemptShots > 0
            ? (float)attemptValidHits / attemptShots * 100f
            : 0f;

        foreach (AchievementData data in achievements)
        {
            if (data.Condition !=
                AchievementCondition.TimeAttackHitsWithAccuracy)
            {
                continue;
            }

            if (accuracy >= data.MinimumAccuracy)
            {
                UpdateProgress(data, attemptValidHits);
            }
        }
    }

    private void CheckAverageReaction()
    {
        if (reactionMeasurementCount <= 0)
            return;

        float average = (float)(
            reactionTotalMilliseconds / reactionMeasurementCount
        );

        foreach (AchievementData data in achievements)
        {
            if (data.Condition !=
                AchievementCondition.AverageReactionTime)
            {
                continue;
            }

            if (reactionMeasurementCount >= data.MinimumMeasurements)
            {
                UpdateProgress(data, average);
            }
        }
    }

    // --------------------------------------------------
    // 진행도 갱신과 달성 판정
    // --------------------------------------------------

    private bool CanRecord()
    {
        return initialized && sessionActive && attemptActive;
    }

    private void ReportProgress(
        AchievementCondition condition,
        float value)
    {
        foreach (AchievementData data in achievements)
        {
            if (data.Condition == condition)
            {
                UpdateProgress(data, value);
            }
        }
    }

    private void UpdateProgress(AchievementData data, float value)
    {
        if (!IsFinite(value) || value < 0f)
            return;

        ProgressState state = progressById[data.Id];

        // 달성한 과제는 다시 알림을 발생시키지 않는다.
        if (state.unlocked)
            return;

        if (!data.IsLowerBetter)
        {
            value = Mathf.Min(value, data.TargetValue);
        }

        bool improved =
            !state.hasValue ||
            (data.IsLowerBetter
                ? value < state.value
                : value > state.value);

        if (!improved)
            return;

        state.value = value;
        state.hasValue = true;

        MarkDirty();

        bool achieved = data.IsLowerBetter
            ? state.value <= data.TargetValue
            : state.value >= data.TargetValue;

        if (!achieved)
            return;

        state.unlocked = true;

        // 달성 기록은 알림을 보내기 전에 즉시 저장한다.
        SaveProgress();

        Debug.Log(
            "도전과제 달성: " + data.Title,
            this
        );

        AchievementUnlocked?.Invoke(data);
    }

    private void MarkDirty()
    {
        saveDirty = true;
        notifyPending = true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // 각 모드의 완료 여부를 정수 하나에 기록한다.
    private static int GetModeBit(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Round:
                return 1 << 0;

            case GameMode.TimeAttack:
                return 1 << 1;

            case GameMode.ReactionTime:
                return 1 << 2;

            case GameMode.Tracking:
                return 1 << 3;

            case GameMode.Memory:
                return 1 << 4;

            default:
                return 0;
        }
    }

    private int CountCompletedModes()
    {
        int count = 0;

        for (int i = 0; i < 5; i++)
        {
            if ((completedModeMask & (1 << i)) != 0)
            {
                count++;
            }
        }

        return count;
    }

    // --------------------------------------------------
    // 저장과 불러오기
    // --------------------------------------------------

    private void LoadProgress()
    {
        totalTargetHits = Mathf.Max(
            0,
            PlayerPrefs.GetInt(SavePrefix + "totalHits", 0)
        );

        completedModeMask =
            PlayerPrefs.GetInt(SavePrefix + "completedModes", 0)
            & 31;

        foreach (AchievementData data in achievements)
        {
            string key = SavePrefix + data.Id;
            ProgressState state = progressById[data.Id];

            state.value = PlayerPrefs.GetFloat(
                key + ".value",
                0f
            );

            state.hasValue = PlayerPrefs.GetInt(
                key + ".hasValue",
                0
            ) == 1;

            state.unlocked = PlayerPrefs.GetInt(
                key + ".unlocked",
                0
            ) == 1;

            if (!IsFinite(state.value) || state.value < 0f)
            {
                state.value = 0f;
                state.hasValue = false;
            }
        }

        // 불러올 때는 기존 달성 알림을 다시 보내지 않는다.
        saveDirty = false;
    }

    public void SaveProgress()
    {
        if (!initialized || !saveDirty)
            return;

        PlayerPrefs.SetInt(
            SavePrefix + "totalHits",
            totalTargetHits
        );

        PlayerPrefs.SetInt(
            SavePrefix + "completedModes",
            completedModeMask
        );

        foreach (AchievementData data in achievements)
        {
            string key = SavePrefix + data.Id;
            ProgressState state = progressById[data.Id];

            PlayerPrefs.SetFloat(
                key + ".value",
                state.value
            );

            PlayerPrefs.SetInt(
                key + ".hasValue",
                state.hasValue ? 1 : 0
            );

            PlayerPrefs.SetInt(
                key + ".unlocked",
                state.unlocked ? 1 : 0
            );
        }

        PlayerPrefs.Save();

        saveDirty = false;

        nextSaveTime =
            Time.unscaledTime + Mathf.Max(0.5f, autoSaveInterval);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveProgress();
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            SaveProgress();
        }
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SaveProgress();
        Instance = null;
    }
}