using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    [Header("Hub")]
    [SerializeField] private HubPlayerController playerPrefab;
    [SerializeField] private Transform hubSpawnPoint;
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private CrosshairUI crosshairUI;
    [SerializeField] private TMP_Text entryText;

    [Header("Achievement UI")]
    [SerializeField] private GameObject achievementsPanel;

    public bool IsAchievementsOpen =>
        achievementsPanel != null &&
        achievementsPanel.activeSelf;

    [Header("Game Duration")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float timeAttackDuration = 60f;

    [Header("Managers")]
    [SerializeField] private TargetSpawner targetSpawner;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private ShootingStats shootingStats;

    [Header("Game UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text roundText;

    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text resultStatsText;
    [SerializeField] private TMP_Text bestScoreText;

    [SerializeField] private GameObject nextRoundButton;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private TMP_Text restartButtonText;

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject exitModeButton;

    [Header("Round Settings")]
    [SerializeField] private int firstTargetScore = 100;
    [SerializeField] private int scoreIncreasePerRound = 50;

    [Header("Round Progression")]
    [SerializeField, Min(1)]
    private int resultEveryNRounds = 5;

    [SerializeField, Min(0f)]
    private float nextRoundDelay = 1f;

    private bool autoNextRoundPending;
    private float autoNextRoundTime;

    [Header("Round Penalty")]
    [SerializeField, Min(1)]
    private int maxForbiddenHits = 3;

    [Header("Tracking")]
    [SerializeField] private Camera trackingCamera;
    [SerializeField] private TMP_Text trackingProgressText;

    [SerializeField, Min(0.1f)]
    private float trackingStartSpeed = 2f;

    [SerializeField, Min(0f)]
    private float trackingAcceleration = 0.1f;

    [SerializeField, Min(0.1f)]
    private float trackingLoseDuration = 3f;

    [SerializeField, Min(0.1f)]
    private float trackingRecoveryDuration = 1f;

    [Header("Reaction Test")]
    [SerializeField, Min(1)]
    private int reactionAttempts = 10;

    [SerializeField, Min(0.1f)]
    private float minReactionWait = 1f;

    [SerializeField, Min(0.1f)]
    private float maxReactionWait = 3f;

    [SerializeField, Min(0.1f)]
    private float reactionFeedbackDuration = 0.8f;

    [Header("Memory Shooting")]
    [SerializeField, Min(1)]
    private int memoryFirstSequenceLength = 3;

    [SerializeField, Min(0.1f)]
    private float memoryPrepareDuration = 1f;

    [SerializeField, Min(0.1f)]
    private float memoryCueDuration = 0.6f;

    [SerializeField, Min(0.1f)]
    private float memoryGapDuration = 0.25f;

    [SerializeField, Min(0.1f)]
    private float memoryFirstAnswerDuration = 15f;

    [SerializeField, Min(0f)]
    private float memoryExtraSecondsPerRound = 2f;

    // 허브 진행 정보
    private HubPlayerController player;
    private ModeEntrance activeEntrance;
    private ModeEntrance promptOwner;

    private bool hasSession;
    private bool lastRunCompleted;
    private bool initialized;

    // 반응속도 모드
    private enum ReactionState
    {
        None,
        Waiting,
        TargetVisible,
        Feedback,
        Complete
    }

    private ReactionState reactionState;

    private double reactionNextTime;
    private double reactionSpawnTime;
    private int reactionSpawnFrame;

    private int completedAttempts;
    private int earlyClicks;

    private double totalReactionSeconds;

    private double fastestReactionSeconds =
        double.PositiveInfinity;

    // 트래킹 모드
    private Transform trackingTarget;

    private Vector3 trackingMinPosition;
    private Vector3 trackingMaxPosition;

    private Vector2 trackingPhase;
    private Vector2 trackingDirection;

    private float trackingElapsedTime;
    private float trackingSuccessTime;
    private float trackingCurrentSpeed;
    private float trackingLostTime;
    private float trackingHoldTime;

    // 도전과제용 연속 조준 시간.
    // 기존 trackingHoldTime은 회복 시간까지만 증가하므로 별도로 측정한다.
    private float trackingConsecutiveAimTime;

    // 메모리 모드
    private enum MemoryState
    {
        None,
        Preparing,
        Showing,
        Gap,
        Answering,
        Complete
    }

    private MemoryState memoryState;

    private ShootingTarget[] memoryTargets;
    private int[] memorySequence;

    private int memoryShowingIndex;
    private int memoryCorrectCount;
    private int memoryShotCount;
    private int memoryAnswerStartFrame = -1;

    private float memoryPhaseRemaining;
    private float memoryAnswerDuration;
    private float memoryAnswerStartTime;
    private float memoryAnswerEndTime;
    private float memoryAnswerElapsed;

    private string memoryFailureReason = "";

    // 최고 기록 저장 키
    private const string BestScoreKey =
        "ShootingRange_BestScore";

    private const string TimeAttackBestHitsKey =
        "ShootingRange_TimeAttack_BestHits";

    private const string ReactionBestKey =
        "ShootingRange_Reaction_BestAverageMs_";

    // 공통 진행 상태
    private bool isPaused;
    private bool hasEnded;

    private int resumeFrame = -1;
    private float endTime;

    private static int roundIndex;
    private bool roundCleared;

    public HubPlayerController Player => player;

    public TargetSpawner ActiveSpawner =>
        hasSession ? targetSpawner : null;

    public bool InputReady =>
        initialized &&
        Application.isFocused &&
        Time.frameCount > resumeFrame;

    public bool CanLook =>
        InputReady &&
        !isPaused &&
        !IsAchievementsOpen &&
        (!hasSession || !hasEnded) &&
        Cursor.lockState == CursorLockMode.Locked;

    public bool CanMove => CanLook && !hasSession;

    public bool CanEnterZone => CanMove;

    public bool IsRoundMode =>
        GameSettings.SelectedMode == GameMode.Round;

    public bool IsTimeAttack =>
        GameSettings.SelectedMode == GameMode.TimeAttack;

    public bool IsReactionTime =>
        GameSettings.SelectedMode == GameMode.ReactionTime;

    public bool IsTracking =>
        GameSettings.SelectedMode == GameMode.Tracking;

    public bool IsMemory =>
        GameSettings.SelectedMode == GameMode.Memory;

    public int CurrentRound => roundIndex + 1;

    private int TargetScore =>
        firstTargetScore + roundIndex * scoreIncreasePerRound;

    private int MemorySequenceLength =>
        memorySequence == null ? 0 : memorySequence.Length;

    public bool IsPlaying
    {
        get
        {
            return hasSession
                && InputReady
                && !hasEnded
                && !isPaused
                && !autoNextRoundPending
                && (
                    IsReactionTime ||
                    IsTracking ||
                    IsMemory ||
                    Time.time < endTime
                );
        }
    }

    public bool CanAnswerMemory =>
        IsMemory &&
        IsPlaying &&
        memoryState == MemoryState.Answering &&
        Time.frameCount > memoryAnswerStartFrame &&
        Time.time < memoryAnswerEndTime;

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (achievementsPanel != null)
        {
            achievementsPanel.SetActive(false);
        }

        hasSession = false;
        hasEnded = false;
        isPaused = false;

        roundIndex = 0;
        targetSpawner = null;

        ResetMemoryState();

        if (!ValidateRequiredReferences())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            enabled = false;
            return;
        }

        maxForbiddenHits = Mathf.Max(1, maxForbiddenHits);
        reactionAttempts = Mathf.Max(1, reactionAttempts);
        minReactionWait = Mathf.Max(0.1f, minReactionWait);

        maxReactionWait = Mathf.Max(
            minReactionWait,
            maxReactionWait
        );

        reactionFeedbackDuration = Mathf.Max(
            0.1f,
            reactionFeedbackDuration
        );

        resultPanel.SetActive(false);
        pausePanel.SetActive(false);

        if (nextRoundButton != null)
            nextRoundButton.SetActive(false);

        if (exitModeButton != null)
            exitModeButton.SetActive(false);

        if (entryText != null)
            entryText.gameObject.SetActive(false);

        SetModeHUD(false);

        player = Instantiate(playerPrefab);
        player.Initialize(this);
        player.Teleport(hubSpawnPoint);

        trackingCamera = player.ViewCamera;
        playerShooting.Configure(this, trackingCamera);

        initialized = true;
        resumeFrame = Time.frameCount;

        ApplyCursor();
    }

    private bool ValidateRequiredReferences()
    {
        string missing = "";

        void Check(UnityEngine.Object reference, string fieldName)
        {
            if (reference == null)
            {
                missing += "- " + fieldName + "\n";
            }
        }

        Check(playerPrefab, "Hub → Player Prefab");

        if (playerPrefab != null)
        {
            Check(
                playerPrefab.ViewCamera,
                "Player 프리팹 → HubPlayerController → View Camera"
            );
        }

        Check(hubSpawnPoint, "Hub → Hub Spawn Point");
        Check(playerShooting, "Hub → Player Shooting");

        Check(scoreManager, "Managers → Score Manager");
        Check(shootingStats, "Managers → Shooting Stats");

        Check(timeText, "Game UI → Time Text");
        Check(roundText, "Game UI → Round Text");

        Check(
            trackingProgressText,
            "Tracking → Tracking Progress Text"
        );

        Check(resultPanel, "Result UI → Result Panel");
        Check(resultTitle, "Result UI → Result Title");
        Check(finalScoreText, "Result UI → Final Score Text");
        Check(resultStatsText, "Result UI → Result Stats Text");
        Check(bestScoreText, "Result UI → Best Score Text");

        Check(restartButton, "Result UI → Restart Button");
        Check(restartButtonText, "Result UI → Restart Button Text");

        Check(pausePanel, "Pause UI → Pause Panel");

        if (string.IsNullOrEmpty(missing))
            return true;

        Debug.LogError(
            "다음 필수 항목이 연결되지 않았습니다:\n" + missing,
            this
        );

        return false;
    }

    private void Update()
    {
        if (!initialized)
            return;

        Keyboard keyboard = Keyboard.current;

        // 도전과제 창이 열려 있으면 ESC는 창 닫기로 사용한다.
        // 같은 입력으로 일시정지 창까지 열리지 않도록 바로 종료한다.
        if (IsAchievementsOpen)
        {
            if (keyboard != null &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseAchievements();
            }

            return;
        }

        // 기존 일시정지 처리
        if (keyboard != null &&
            keyboard.escapeKey.wasPressedThisFrame &&
            !(hasSession && hasEnded))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        // 허브에서 자유롭게 이동 가능한 상태일 때만 연다.
        if (keyboard != null &&
            keyboard.jKey.wasPressedThisFrame &&
            CanMove)
        {
            OpenAchievements();
            return;
        }

        if (!hasSession || hasEnded || isPaused)
            return;

        // 라운드 클리어 후 자동 진행
        if (autoNextRoundPending)
        {
            if (Time.time >= autoNextRoundTime)
            {
                roundIndex++;
                BeginAttempt();
            }

            return;
        }

        if (!IsReactionTime && !IsTracking && !IsMemory)
        {
            UpdateTimeText();

            if (Time.time >= endTime)
            {
                EndGame(IsTimeAttack);
            }
        }
    }

    // --------------------------------------------------
    // 메모리 모드
    // --------------------------------------------------

    private void InitializeMemory()
    {
        memoryTargets = targetSpawner.SpawnMemoryTargets();

        if (memoryTargets == null || memoryTargets.Length < 2)
        {
            Debug.LogError(
                "메모리 타겟을 생성하지 못했습니다. " +
                "프리팹과 Spawn Points 연결을 확인해 주세요.",
                this
            );

            CancelMode();
            return;
        }

        int sequenceLength =
            Mathf.Max(1, memoryFirstSequenceLength) + roundIndex;

        memorySequence = new int[sequenceLength];

        memoryAnswerDuration =
            Mathf.Max(0.1f, memoryFirstAnswerDuration)
            + roundIndex * Mathf.Max(0f, memoryExtraSecondsPerRound);

        memoryCorrectCount = 0;
        memoryShotCount = 0;
        memoryAnswerElapsed = 0f;
        memoryFailureReason = "";

        // 첫 타겟은 전체 타겟 중에서 선택한다.
        memorySequence[0] = Random.Range(0, memoryTargets.Length);

        for (int i = 1; i < memorySequence.Length; i++)
        {
            // 직전 타겟을 제외하고 선택한다.
            // 다른 타겟을 거친 뒤 같은 타겟이 다시 나올 수 있다.
            int next = Random.Range(0, memoryTargets.Length - 1);

            if (next >= memorySequence[i - 1])
            {
                next++;
            }

            memorySequence[i] = next;
        }

        RestartMemoryPresentation();
    }

    private void RestartMemoryPresentation()
    {
        if (memoryTargets == null || memorySequence == null)
            return;

        foreach (ShootingTarget target in memoryTargets)
        {
            if (target != null)
            {
                target.SetMemoryCue(false);
            }
        }

        memoryShowingIndex = 0;
        memoryState = MemoryState.Preparing;

        memoryPhaseRemaining = Mathf.Max(
            0.1f,
            memoryPrepareDuration
        );

        roundText.text =
            "Memory R" + CurrentRound
            + "   Remember " + MemorySequenceLength;

        timeText.text = "Get Ready";
    }

    private void UpdateMemory()
    {
        if (memoryState == MemoryState.None ||
            memoryState == MemoryState.Complete)
        {
            return;
        }

        if (!MemoryTargetsAvailable())
        {
            FinishMemory(false, "Target unavailable");
            return;
        }

        if (memoryState == MemoryState.Answering)
        {
            memoryAnswerElapsed = Mathf.Clamp(
                Time.time - memoryAnswerStartTime,
                0f,
                memoryAnswerDuration
            );

            UpdateTimeText();

            if (Time.time >= memoryAnswerEndTime)
            {
                FinishMemory(false, "Time expired");
            }

            return;
        }

        memoryPhaseRemaining -= Time.deltaTime;

        if (memoryPhaseRemaining > 0f)
            return;

        switch (memoryState)
        {
            case MemoryState.Preparing:
                ShowMemoryCue();
                break;

            case MemoryState.Showing:
                memoryTargets[
                    memorySequence[memoryShowingIndex]
                ].SetMemoryCue(false);

                memoryState = MemoryState.Gap;

                memoryPhaseRemaining = Mathf.Max(
                    0.1f,
                    memoryGapDuration
                );
                break;

            case MemoryState.Gap:
                memoryShowingIndex++;

                if (memoryShowingIndex < MemorySequenceLength)
                {
                    ShowMemoryCue();
                }
                else
                {
                    BeginMemoryAnswer();
                }
                break;
        }
    }

    private bool MemoryTargetsAvailable()
    {
        if (memoryTargets == null || memoryTargets.Length < 2)
            return false;

        foreach (ShootingTarget target in memoryTargets)
        {
            if (target == null || !target.isActiveAndEnabled)
            {
                return false;
            }
        }

        return true;
    }

    private void ShowMemoryCue()
    {
        memoryState = MemoryState.Showing;

        memoryPhaseRemaining = Mathf.Max(
            0.1f,
            memoryCueDuration
        );

        int targetIndex = memorySequence[memoryShowingIndex];

        memoryTargets[targetIndex].SetMemoryCue(true);

        timeText.text =
            "Watch " + (memoryShowingIndex + 1)
            + " / " + MemorySequenceLength;
    }

    private void BeginMemoryAnswer()
    {
        memoryState = MemoryState.Answering;

        memoryAnswerStartTime = Time.time;
        memoryAnswerEndTime = Time.time + memoryAnswerDuration;
        memoryAnswerStartFrame = Time.frameCount;

        memoryAnswerElapsed = 0f;

        roundText.text =
            "Memory R" + CurrentRound
            + "   Repeat 0 / " + MemorySequenceLength;

        UpdateTimeText();
    }

    // PlayerShooting에서 한 발당 한 번 호출한다.
    // 허공이나 벽을 맞혔다면 null을 전달한다.
    // 반환값은 정답 명중 여부다.
    public bool SubmitMemoryShot(ShootingTarget target)
    {
        if (!IsMemory || !IsPlaying ||
            memoryState != MemoryState.Answering)
        {
            return false;
        }

        if (Time.time >= memoryAnswerEndTime)
        {
            FinishMemory(false, "Time expired");
            return false;
        }

        if (!CanAnswerMemory)
            return false;

        memoryShotCount++;

        memoryAnswerElapsed = Mathf.Clamp(
            Time.time - memoryAnswerStartTime,
            0f,
            memoryAnswerDuration
        );

        // 빈 공간, 벽, 다른 구역의 타겟은 무시한다.
        // 발사 수는 PlayerShooting에서 이미 도전과제에 기록했다.
        if (target == null ||
            !target.isActiveAndEnabled ||
            !target.IsMemoryTarget ||
            targetSpawner == null ||
            target.MemoryOwner != targetSpawner ||
            !targetSpawner.OwnsTarget(target))
        {
            return false;
        }

        int targetIndex = target.MemoryIndex;

        if (targetIndex < 0 ||
            targetIndex >= memoryTargets.Length ||
            memoryTargets[targetIndex] != target)
        {
            return false;
        }

        if (targetIndex != memorySequence[memoryCorrectCount])
        {
            // 실패 처리 전에 순서 오류를 기록한다.
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.RecordMemoryOrderError();
            }

            FinishMemory(false, "Wrong order");
            return false;
        }

        // 마지막 정답도 완료 판정 전에 기록한다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.RecordHit(TargetType.Static);
        }

        target.ShowMemoryCorrect();
        memoryCorrectCount++;

        roundText.text =
            "Memory R" + CurrentRound
            + "   Repeat " + memoryCorrectCount
            + " / " + MemorySequenceLength;

        if (memoryCorrectCount >= MemorySequenceLength)
        {
            FinishMemory(true, "");
        }

        return true;
    }

    private void FinishMemory(bool cleared, string reason)
    {
        if (!hasSession || hasEnded ||
            memoryState == MemoryState.None ||
            memoryState == MemoryState.Complete)
        {
            return;
        }

        if (memoryState == MemoryState.Answering)
        {
            memoryAnswerElapsed = Mathf.Clamp(
                Time.time - memoryAnswerStartTime,
                0f,
                memoryAnswerDuration
            );
        }

        memoryState = MemoryState.Complete;
        memoryFailureReason = reason;

        EndGame(cleared);
    }

    private void ResetMemoryState()
    {
        // 実際のターゲット削除はTargetSpawnerが担当する。
        memoryState = MemoryState.None;
        memoryTargets = null;
        memorySequence = null;

        memoryShowingIndex = 0;
        memoryCorrectCount = 0;
        memoryShotCount = 0;
        memoryAnswerStartFrame = -1;

        memoryPhaseRemaining = 0f;
        memoryAnswerDuration = 0f;
        memoryAnswerStartTime = 0f;
        memoryAnswerEndTime = 0f;
        memoryAnswerElapsed = 0f;

        memoryFailureReason = "";
    }

    // --------------------------------------------------
    // 트래킹 모드
    // --------------------------------------------------

    private void InitializeTracking()
    {
        trackingStartSpeed = Mathf.Max(0.1f, trackingStartSpeed);
        trackingAcceleration = Mathf.Max(0f, trackingAcceleration);
        trackingLoseDuration = Mathf.Max(0.1f, trackingLoseDuration);

        trackingRecoveryDuration = Mathf.Max(
            0.1f,
            trackingRecoveryDuration
        );

        trackingHoldTime = 0f;
        trackingConsecutiveAimTime = 0f;
        trackingElapsedTime = 0f;
        trackingSuccessTime = 0f;
        trackingLostTime = 0f;

        trackingCurrentSpeed = trackingStartSpeed;

        if (trackingCamera == null)
        {
            trackingCamera = Camera.main;
        }

        if (targetSpawner == null ||
            trackingCamera == null ||
            trackingProgressText == null)
        {
            roundText.text = "Tracking";
            timeText.text = "Check Tracking Setup";

            Debug.LogError(
                "GameManager의 Target Spawner, Tracking Camera, " +
                "Tracking Progress Text를 확인해 주세요.",
                this
            );

            return;
        }

        trackingTarget = targetSpawner.SpawnTrackingTarget(
            out trackingMinPosition,
            out trackingMaxPosition
        );

        if (trackingTarget == null)
        {
            roundText.text = "Tracking";
            timeText.text = "Check Tracking Setup";
            return;
        }

        trackingPhase = new Vector2(
            (trackingMaxPosition.x - trackingMinPosition.x) * 0.5f,
            (trackingMaxPosition.y - trackingMinPosition.y) * 0.5f
        );

        float angle = Random.Range(25f, 65f) * Mathf.Deg2Rad;

        trackingDirection = new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        );

        if (Random.value < 0.5f)
            trackingDirection.x *= -1f;

        if (Random.value < 0.5f)
            trackingDirection.y *= -1f;

        trackingProgressText.gameObject.SetActive(true);

        UpdateTrackingProgress(false);
        UpdateTimeText();

        resumeFrame = Time.frameCount;
    }

    private void UpdateTracking()
    {
        if (trackingTarget == null)
            return;

        float delta = Time.deltaTime;

        if (delta <= 0f)
            return;

        float width =
            trackingMaxPosition.x - trackingMinPosition.x;

        float height =
            trackingMaxPosition.y - trackingMinPosition.y;

        float moveDistance =
            trackingCurrentSpeed * delta
            + 0.5f * trackingAcceleration * delta * delta;

        trackingPhase += trackingDirection * moveDistance;

        trackingTarget.position = new Vector3(
            trackingMinPosition.x
                + Mathf.PingPong(trackingPhase.x, width),

            trackingMinPosition.y
                + Mathf.PingPong(trackingPhase.y, height),

            (trackingMinPosition.z + trackingMaxPosition.z) * 0.5f
        );

        Physics.SyncTransforms();

        bool isOnTarget = false;

        Ray ray = trackingCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            100f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            isOnTarget =
                hit.transform == trackingTarget ||
                hit.transform.IsChildOf(trackingTarget);
        }

        trackingElapsedTime += delta;

        trackingCurrentSpeed =
            trackingStartSpeed
            + trackingAcceleration * trackingElapsedTime;

        if (isOnTarget)
        {
            trackingSuccessTime += delta;

            // 도전과제용 시간은 상한 없이 증가한다.
            trackingConsecutiveAimTime += delta;

            // 기존 실패 시간 회복에 사용하는 값
            trackingHoldTime = Mathf.Min(
                trackingRecoveryDuration,
                trackingHoldTime + delta
            );

            if (trackingHoldTime >= trackingRecoveryDuration)
            {
                trackingLostTime = 0f;
            }
        }
        else
        {
            trackingHoldTime = 0f;
            trackingConsecutiveAimTime = 0f;

            trackingLostTime = Mathf.Min(
                trackingLoseDuration,
                trackingLostTime + delta
            );
        }

        // 게임이 종료되는 마지막 프레임의 기록까지 전달한다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.RecordTracking(
                trackingElapsedTime,
                trackingConsecutiveAimTime
            );
        }

        UpdateTrackingProgress(isOnTarget);
        UpdateTimeText();

        if (trackingLostTime >= trackingLoseDuration)
        {
            EndGame(false);
        }
    }

    private void UpdateTrackingProgress(bool isOnTarget)
    {
        roundText.text =
            "Tracking   Speed: "
            + trackingCurrentSpeed.ToString("F1");

        string status;

        if (!isOnTarget)
        {
            status = "FOLLOW TARGET";
        }
        else if (trackingHoldTime < trackingRecoveryDuration)
        {
            status =
                "RECOVER: "
                + trackingHoldTime.ToString("F1")
                + " / "
                + trackingRecoveryDuration.ToString("F1")
                + " s";
        }
        else
        {
            status = "ON TARGET";
        }

        trackingProgressText.text =
            "Survival: "
            + trackingElapsedTime.ToString("F1")
            + " s\n"
            + status;
    }

    // --------------------------------------------------
    // 반응속도 모드 및 공통 사격 허용
    // --------------------------------------------------

    private void BeginReactionWait()
    {
        targetSpawner.ClearReactionTarget();

        reactionState = ReactionState.Waiting;

        reactionNextTime = Time.timeAsDouble + Random.Range(
            minReactionWait,
            maxReactionWait
        );

        roundText.text =
            "Attempt: " + (completedAttempts + 1)
            + " / " + reactionAttempts;

        timeText.text = "Wait...";
    }

    public bool TryBeginShot()
    {
        if (!IsPlaying)
            return false;

        if (IsTracking)
            return false;

        if (IsMemory)
        {
            if (memoryState == MemoryState.Answering &&
                Time.time >= memoryAnswerEndTime)
            {
                FinishMemory(false, "Time expired");
                return false;
            }

            return CanAnswerMemory;
        }

        // 라운드·타임어택은 바로 사격을 허용한다.
        if (!IsReactionTime)
            return true;

        if (reactionState == ReactionState.Waiting)
        {
            earlyClicks++;
            ShowReactionFeedback("Too Early!");
            return false;
        }

        return reactionState == ReactionState.TargetVisible
            && Time.frameCount > reactionSpawnFrame;
    }

    private void ShowReactionFeedback(string message)
    {
        reactionState = ReactionState.Feedback;

        reactionNextTime =
            Time.timeAsDouble + reactionFeedbackDuration;

        timeText.text = message;
    }

    private void OnReactionTargetHit()
    {
        if (!IsPlaying ||
            reactionState != ReactionState.TargetVisible)
        {
            return;
        }

        double elapsed = System.Math.Max(
            0d,
            Time.timeAsDouble - reactionSpawnTime
        );

        totalReactionSeconds += elapsed;

        fastestReactionSeconds = System.Math.Min(
            fastestReactionSeconds,
            elapsed
        );

        // 초를 밀리초로 변환한다.
        // 마지막 측정도 테스트 종료 전에 기록한다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.RecordReaction(
                (float)(elapsed * 1000d)
            );
        }

        completedAttempts++;

        if (completedAttempts >= reactionAttempts)
        {
            reactionState = ReactionState.Complete;
            EndGame(true);
        }
        else
        {
            ShowReactionFeedback(
                (elapsed * 1000d).ToString("F0") + " ms"
            );
        }
    }

    // --------------------------------------------------
    // 시간 표시
    // --------------------------------------------------

    private void UpdateTimeText()
    {
        if (IsMemory)
        {
            if (memoryState == MemoryState.Answering)
            {
                float remaining = Mathf.Max(
                    0f,
                    memoryAnswerEndTime - Time.time
                );

                timeText.text =
                    "Time: " + Mathf.CeilToInt(remaining);
            }

            return;
        }

        if (IsReactionTime)
            return;

        if (IsTracking)
        {
            float remainingGrace = Mathf.Max(
                0f,
                trackingLoseDuration - trackingLostTime
            );

            timeText.text =
                "Grace: " + remainingGrace.ToString("F1");

            return;
        }

        float remainingTime = Mathf.Max(
            0f,
            endTime - Time.time
        );

        timeText.text =
            "Time: " + Mathf.CeilToInt(remainingTime);
    }

    // --------------------------------------------------
    // 종료 및 결과 표시
    // --------------------------------------------------

    private bool TryAdvanceWithoutResult(bool cleared)
    {
        // 실패하면 기존 결과창을 표시한다.
        if (!cleared)
            return false;

        // 라운드와 메모리 모드에만 적용한다.
        if (!IsRoundMode && !IsMemory)
            return false;

        int interval = Mathf.Max(1, resultEveryNRounds);

        // 설정한 라운드 간격에 도달하면 기존 결과창을 표시한다.
        if (CurrentRound % interval == 0)
            return false;

        autoNextRoundPending = true;

        autoNextRoundTime =
            Time.time + Mathf.Max(0f, nextRoundDelay);

        // 클리어한 라운드의 타겟을 정리한다.
        if (targetSpawner != null)
        {
            targetSpawner.StopSpawning();
        }

        resultPanel.SetActive(false);

        roundText.text = IsMemory
            ? "Memory R" + CurrentRound + " CLEAR!"
            : "Round " + CurrentRound + " CLEAR!";

        timeText.text =
            "Next: Round " + (CurrentRound + 1);

        return true;
    }

    private void EndGame(bool cleared)
    {
        if (hasEnded || autoNextRoundPending)
            return;

        // 결과창을 생략하는 라운드도 완료 기록을 남긴다.
        // 트래킹은 생존 종료를 1회 완료로 취급한다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.FinishAttempt(
                cleared || IsTracking
            );
        }

        // 일반 클리어는 결과창 없이 자동 진행한다.
        if (TryAdvanceWithoutResult(cleared))
            return;

        hasEnded = true;
        lastRunCompleted = cleared;

        ApplyCursor();

        roundCleared = IsRoundMode && cleared;

        if (targetSpawner != null)
        {
            targetSpawner.StopSpawning();
        }

        UpdateTimeText();

        if (IsMemory)
        {
            resultTitle.text = cleared
                ? "MEMORY CLEAR"
                : "MEMORY FAILED";

            finalScoreText.text =
                "Round: " + CurrentRound
                + "\nCorrect: " + memoryCorrectCount
                + " / " + MemorySequenceLength;

            resultStatsText.text =
                "Shots: " + memoryShotCount + "\n"
                + "Answer Time: "
                + memoryAnswerElapsed.ToString("F1")
                + " s";

            if (!cleared &&
                !string.IsNullOrEmpty(memoryFailureReason))
            {
                resultStatsText.text +=
                    "\nReason: " + memoryFailureReason;
            }

            timeText.text = cleared ? "Complete!" : "Failed";
        }
        else if (IsTracking)
        {
            resultTitle.text = "TRACKING FAILED";

            finalScoreText.text =
                "Survived: "
                + trackingElapsedTime.ToString("F1")
                + " s";

            float trackingAccuracy = trackingElapsedTime > 0f
                ? trackingSuccessTime / trackingElapsedTime * 100f
                : 0f;

            resultStatsText.text =
                "Tracked: "
                + trackingSuccessTime.ToString("F1") + " s\n"
                + "On Target: "
                + trackingAccuracy.ToString("F1") + "%\n"
                + "Final Speed: "
                + trackingCurrentSpeed.ToString("F1") + "\n"
                + "Lost Target: "
                + trackingLostTime.ToString("F1") + " s";
        }
        else if (IsReactionTime)
        {
            double averageMs = completedAttempts > 0
                ? totalReactionSeconds / completedAttempts * 1000d
                : 0d;

            resultTitle.text = "REACTION TEST COMPLETE";

            finalScoreText.text =
                "Average: " + averageMs.ToString("F0") + " ms";

            resultStatsText.text =
                "Fastest: "
                + (fastestReactionSeconds * 1000d).ToString("F0")
                + " ms\n"
                + "Completed: " + completedAttempts
                + " / " + reactionAttempts + "\n"
                + "Too Early: " + earlyClicks;

            timeText.text = "Complete!";

            if (completedAttempts > 0)
            {
                UpdateReactionBest((float)averageMs);
            }
        }
        else if (IsTimeAttack)
        {
            resultTitle.text = "TIME ATTACK COMPLETE";

            finalScoreText.text =
                "Targets Hit: " + shootingStats.StaticHits;

            resultStatsText.text =
                "Shots: " + shootingStats.TotalShots + "\n"
                + "Accuracy: "
                + shootingStats.Accuracy.ToString("F1") + "%";

            UpdateBestScore();
        }
        else
        {
            if (cleared)
            {
                resultTitle.text = "ROUND CLEAR";
            }
            else if (shootingStats.ForbiddenHits >= maxForbiddenHits)
            {
                resultTitle.text =
                    "ROUND FAILED\nToo Many X Targets!";
            }
            else
            {
                resultTitle.text = "ROUND FAILED";
            }

            finalScoreText.text =
                "Final Score: " + scoreManager.CurrentScore
                + " / " + TargetScore;

            resultStatsText.text =
                "Shots: " + shootingStats.TotalShots + "\n"
                + "Static Hits: " + shootingStats.StaticHits + "\n"
                + "Moving Hits: " + shootingStats.MovingHits + "\n"
                + "Forbidden Hits: " + shootingStats.ForbiddenHits + "\n"
                + "Accuracy: "
                + shootingStats.Accuracy.ToString("F1") + "%";

            UpdateBestScore();
        }

        // 메모리·트래킹은 이번 구현에서 최고 기록을 표시하지 않는다.
        bestScoreText.gameObject.SetActive(
            !IsTracking && !IsMemory
        );

        if (nextRoundButton != null)
            nextRoundButton.SetActive(false);

        restartButton.SetActive(true);

        restartButtonText.text =
            lastRunCompleted ? "Continue" : "Retry";

        resultPanel.SetActive(true);
    }

    // --------------------------------------------------
    // 최고 기록
    // --------------------------------------------------

    private void UpdateReactionBest(float averageMs)
    {
        string key = ReactionBestKey + reactionAttempts;

        float best = PlayerPrefs.GetFloat(
            key,
            float.MaxValue
        );

        bool isNewRecord = averageMs < best;

        if (isNewRecord)
        {
            best = averageMs;

            PlayerPrefs.SetFloat(key, best);
            PlayerPrefs.Save();
        }

        bestScoreText.text =
            "Best Average: " + best.ToString("F0") + " ms";

        if (isNewRecord)
        {
            bestScoreText.text += "  NEW!";
        }
    }

    private void UpdateBestScore()
    {
        string key = IsTimeAttack
            ? TimeAttackBestHitsKey
            : BestScoreKey;

        int current = IsTimeAttack
            ? shootingStats.StaticHits
            : scoreManager.CurrentScore;

        int best = PlayerPrefs.GetInt(key, 0);

        bool isNewRecord = current > best;

        if (isNewRecord)
        {
            best = current;

            PlayerPrefs.SetInt(key, best);
            PlayerPrefs.Save();
        }

        bestScoreText.text =
            (IsTimeAttack ? "Best Hits: " : "Best Score: ")
            + best;

        if (isNewRecord)
        {
            bestScoreText.text += "  NEW!";
        }
    }

    // --------------------------------------------------
    // 일시정지 및 재개
    // --------------------------------------------------

    public void PauseGame()
    {
        if (!initialized ||
            isPaused ||
            IsAchievementsOpen ||
            (hasSession && hasEnded))
        {
            return;
        }

        isPaused = true;

        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (exitModeButton != null)
        {
            exitModeButton.SetActive(hasSession);
        }

        pausePanel.SetActive(true);

        ApplyCursor();
    }

    public void ResumeGame()
    {
        if (!initialized || !isPaused)
            return;

        isPaused = false;
        resumeFrame = Time.frameCount;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        pausePanel.SetActive(false);

        if (hasSession &&
            IsReactionTime &&
            reactionState != ReactionState.None)
        {
            BeginReactionWait();
        }

        if (hasSession && IsMemory &&
            (memoryState == MemoryState.Preparing ||
             memoryState == MemoryState.Showing ||
             memoryState == MemoryState.Gap))
        {
            // 표시 중 일시정지를 했다면 같은 순서를 다시 보여준다.
            RestartMemoryPresentation();
        }

        // 메모리 답 입력 단계에서는 진행 위치와 남은 시간을 유지한다.
        ApplyCursor();
    }

    // --------------------------------------------------
    // 재시작 및 씬 이동
    // --------------------------------------------------

    public void RestartGame()
    {
        ContinueMode();
    }

    public void ReturnToMainMenu()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.EndSession();
        }

        autoNextRoundPending = false;
        autoNextRoundTime = 0f;

        if (targetSpawner != null)
        {
            targetSpawner.StopSpawning();
        }

        ResetMemoryState();

        roundIndex = 0;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("StartScene");
    }

    public void NextRound()
    {
        if ((IsRoundMode && roundCleared) ||
            (IsMemory && lastRunCompleted))
        {
            ContinueMode();
        }
    }

    public static void ResetRounds()
    {
        roundIndex = 0;
    }

    // --------------------------------------------------
    // 일반 라운드의 실패·클리어 판정
    // --------------------------------------------------

    public void CheckRoundClear()
    {
        if (!IsRoundMode || !IsPlaying)
            return;

        if (shootingStats.ForbiddenHits >= maxForbiddenHits)
        {
            EndGame(false);
            return;
        }

        if (scoreManager.CurrentScore >= TargetScore)
        {
            EndGame(true);
        }
    }

    // --------------------------------------------------
    // 구역 입장 및 모드 시작
    // --------------------------------------------------

    public bool TryEnterZone(ModeEntrance entrance)
    {
        if (!CanEnterZone || entrance == null)
            return false;

        if (entrance.Spawner == null ||
            entrance.PlayPoint == null ||
            entrance.ExitPoint == null)
        {
            Debug.LogError(
                "입장 구역의 Spawner / Play Point / Exit Point를 연결해 주세요.",
                entrance
            );

            return false;
        }

        if (!entrance.Spawner.ValidateMode(
            entrance.Mode,
            out string error))
        {
            Debug.LogError(error, entrance.Spawner);
            return false;
        }

        activeEntrance = entrance;
        targetSpawner = entrance.Spawner;

        GameSettings.SelectedMode = entrance.Mode;

        roundIndex = 0;
        hasSession = true;

        ClearEntryPrompt(null);

        // 새로 입장했을 때만 입장 단위 기록을 초기화한다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.BeginSession(
                GameSettings.SelectedMode
            );
        }

        BeginAttempt();

        return hasSession;
    }

    private void BeginAttempt()
    {
        autoNextRoundPending = false;
        autoNextRoundTime = 0f;

        if (activeEntrance == null || targetSpawner == null)
            return;

        if (!targetSpawner.ValidateMode(
            GameSettings.SelectedMode,
            out string error))
        {
            Debug.LogError(error, targetSpawner);
            CancelMode();
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;

        isPaused = false;
        hasEnded = false;
        roundCleared = false;
        lastRunCompleted = false;

        reactionState = ReactionState.None;
        completedAttempts = 0;
        earlyClicks = 0;
        totalReactionSeconds = 0d;
        fastestReactionSeconds = double.PositiveInfinity;

        reactionNextTime = 0d;
        reactionSpawnTime = 0d;
        reactionSpawnFrame = -1;

        trackingTarget = null;

        ResetMemoryState();

        resultPanel.SetActive(false);
        pausePanel.SetActive(false);

        if (nextRoundButton != null)
            nextRoundButton.SetActive(false);

        player.Teleport(activeEntrance.PlayPoint);

        playerShooting.ResetFeedback();

        if (crosshairUI != null)
        {
            crosshairUI.ResetFeedback();
        }

        shootingStats.ResetStats();
        scoreManager.ResetScore();

        SetModeHUD(true);

        resumeFrame = Time.frameCount;

        targetSpawner.BeginSession(
            this,
            scoreManager,
            CurrentRound
        );

        // 현재 라운드 또는 측정 1회의 기록을 시작한다.
        // 입장 이후 누적된 금지 타겟 명중·순서 오류는 유지한다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.BeginAttempt(CurrentRound);
        }

        ApplyCursor();

        if (IsMemory)
        {
            InitializeMemory();
        }
        else if (IsTracking)
        {
            InitializeTracking();
        }
        else if (IsReactionTime)
        {
            BeginReactionWait();
        }
        else
        {
            endTime = Time.time + Mathf.Max(
                0.1f,
                IsTimeAttack ? timeAttackDuration : gameDuration
            );

            roundText.text = IsTimeAttack
                ? "Time Attack"
                : "Round " + CurrentRound + "   Target: " + TargetScore;

            UpdateTimeText();
        }
    }

    public void ContinueMode()
    {
        if (!initialized || !hasSession || !hasEnded)
            return;

        if ((IsRoundMode && roundCleared) ||
            (IsMemory && lastRunCompleted))
        {
            roundIndex++;
        }

        BeginAttempt();
    }

    public void CancelMode()
    {
        if (!initialized || !hasSession)
            return;

        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.EndSession();
        }

        autoNextRoundPending = false;
        autoNextRoundTime = 0f;

        Transform destination =
            activeEntrance != null &&
            activeEntrance.ExitPoint != null
                ? activeEntrance.ExitPoint
                : hubSpawnPoint;

        if (targetSpawner != null)
        {
            targetSpawner.StopSpawning();
        }

        ResetMemoryState();

        hasSession = false;
        hasEnded = false;
        isPaused = false;

        roundCleared = false;
        lastRunCompleted = false;

        reactionState = ReactionState.None;
        trackingTarget = null;

        targetSpawner = null;
        activeEntrance = null;

        roundIndex = 0;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        resultPanel.SetActive(false);
        pausePanel.SetActive(false);

        if (exitModeButton != null)
            exitModeButton.SetActive(false);

        shootingStats.ResetStats();
        scoreManager.ResetScore();

        SetModeHUD(false);

        playerShooting.ResetFeedback();

        if (crosshairUI != null)
        {
            crosshairUI.ResetFeedback();
        }

        ClearEntryPrompt(null);

        player.Teleport(destination);

        resumeFrame = Time.frameCount;

        ApplyCursor();
    }

    // --------------------------------------------------
    // 공통 UI 및 커서
    // --------------------------------------------------

    private void SetModeHUD(bool visible)
    {
        timeText.gameObject.SetActive(visible);
        roundText.gameObject.SetActive(visible);

        trackingProgressText.gameObject.SetActive(
            visible && IsTracking
        );

        scoreManager.SetVisible(
            visible && !IsReactionTime && !IsMemory
        );
    }

    public void ShowEntryPrompt(ModeEntrance owner, string message)
    {
        if (!CanEnterZone || entryText == null)
            return;

        promptOwner = owner;

        entryText.text = message;
        entryText.gameObject.SetActive(true);
    }

    public void ClearEntryPrompt(ModeEntrance owner)
    {
        if (owner != null && owner != promptOwner)
            return;

        promptOwner = null;

        if (entryText != null)
        {
            entryText.gameObject.SetActive(false);
        }
    }

    public void OpenAchievements()
    {
        // 도전과제 창은 허브에서만 열 수 있다.
        if (!initialized ||
            !InputReady ||
            hasSession ||
            isPaused ||
            IsAchievementsOpen)
        {
            return;
        }

        if (achievementsPanel == null)
        {
            Debug.LogError(
                "GameManager의 Achievements Panel을 연결해 주세요.",
                this
            );

            return;
        }

        ClearEntryPrompt(null);

        // 같은 Canvas 안에서 다른 UI보다 앞에 표시한다.
        achievementsPanel.transform.SetAsLastSibling();
        achievementsPanel.SetActive(true);

        ApplyCursor();
    }

    public void CloseAchievements()
    {
        if (!initialized || !IsAchievementsOpen)
            return;

        achievementsPanel.SetActive(false);

        // 닫기 버튼을 누른 프레임의 입력이 게임에 전달되지 않도록 한다.
        resumeFrame = Time.frameCount;

        ApplyCursor();
    }

    private void ApplyCursor()
    {
        bool locked =
            initialized &&
            !isPaused &&
            !IsAchievementsOpen &&
            (!hasSession || !hasEnded);

        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && initialized)
        {
            PauseGame();
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}