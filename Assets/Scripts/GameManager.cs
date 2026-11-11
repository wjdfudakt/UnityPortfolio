using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
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

    [Header("Round Settings")]
    [SerializeField] private int firstTargetScore = 100;
    [SerializeField] private int scoreIncreasePerRound = 50;

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

    // 반응속도 모드의 진행 상태
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

    // 트래킹 모드의 진행 정보
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

    public bool IsRoundMode =>
        GameSettings.SelectedMode == GameMode.Round;

    public bool IsTimeAttack =>
        GameSettings.SelectedMode == GameMode.TimeAttack;

    public bool IsReactionTime =>
        GameSettings.SelectedMode == GameMode.ReactionTime;

    public bool IsTracking =>
        GameSettings.SelectedMode == GameMode.Tracking;

    public int CurrentRound => roundIndex + 1;

    private int TargetScore =>
        firstTargetScore + roundIndex * scoreIncreasePerRound;

    public bool IsPlaying
    {
        get
        {
            return !hasEnded
                && !isPaused
                && Time.frameCount > resumeFrame
                && (
                    IsReactionTime ||
                    IsTracking ||
                    Time.time < endTime
                );
        }
    }

    private void Start()
    {
        Debug.Log(
            "Selected Game Mode: " + GameSettings.SelectedMode,
            this
        );

        Time.timeScale = 1f;
        AudioListener.pause = false;

        pausePanel.SetActive(false);
        resultPanel.SetActive(false);

        maxForbiddenHits = Mathf.Max(1, maxForbiddenHits);

        // 트래킹 모드 초기화
        if (IsTracking)
        {
            InitializeTracking();
            return;
        }

        // 반응속도 모드 초기화
        if (IsReactionTime)
        {
            reactionAttempts = Mathf.Max(1, reactionAttempts);

            minReactionWait = Mathf.Max(
                0.1f,
                minReactionWait
            );

            maxReactionWait = Mathf.Max(
                minReactionWait,
                maxReactionWait
            );

            reactionFeedbackDuration = Mathf.Max(
                0.1f,
                reactionFeedbackDuration
            );

            // 씬 진입 프레임의 클릭은 사용하지 않는다.
            resumeFrame = Time.frameCount;

            if (targetSpawner == null ||
                !targetSpawner.IsReactionConfigured)
            {
                roundText.text = "Reaction Test";
                timeText.text = "Check Reaction Setup";

                Debug.LogError(
                    "GameManager의 Target Spawner와 " +
                    "TargetSpawner의 Game Manager, " +
                    "Target Prefabs[0], Reaction Spawn Point를 " +
                    "확인해 주세요.",
                    this
                );

                return;
            }

            BeginReactionWait();
            return;
        }

        // 라운드·타임어택 모드 초기화
        float duration = IsTimeAttack
            ? timeAttackDuration
            : gameDuration;

        endTime = Time.time + duration;

        UpdateTimeText();

        roundText.text = IsTimeAttack
            ? "Time Attack"
            : "Round " + CurrentRound + "   Target: " + TargetScore;
    }

    private void Update()
    {
        if (hasEnded)
            return;

        // 라운드·타임어택의 제한 시간을 확인한다.
        // 트래킹은 UpdateTracking에서 별도로 확인한다.
        if (!IsReactionTime &&
            !IsTracking &&
            !isPaused &&
            Time.time >= endTime)
        {
            EndGame(false);
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        if (!isPaused && !IsReactionTime && !IsTracking)
        {
            UpdateTimeText();
        }
    }

    private void LateUpdate()
    {
        // 트래킹 모드 처리
        if (IsTracking)
        {
            if (IsPlaying)
            {
                UpdateTracking();
            }

            return;
        }

        // 반응속도 모드 처리
        if (!IsReactionTime || !IsPlaying)
            return;

        // 해당 프레임의 사격 입력 처리 후 타겟을 생성한다.
        if (reactionState == ReactionState.Waiting &&
            Time.timeAsDouble >= reactionNextTime)
        {
            bool spawned = targetSpawner.SpawnReactionTarget(
                OnReactionTargetHit
            );

            if (!spawned)
            {
                reactionState = ReactionState.None;
                timeText.text = "Check Reaction Setup";

                Debug.LogError(
                    "반응속도 타겟을 생성하지 못했습니다. " +
                    "TargetSpawner의 연결과 활성 상태를 확인해 주세요.",
                    this
                );

                return;
            }

            reactionSpawnTime = Time.timeAsDouble;
            reactionSpawnFrame = Time.frameCount;

            reactionState = ReactionState.TargetVisible;
            timeText.text = "Hit!";
        }
        else if (reactionState == ReactionState.Feedback &&
                 Time.timeAsDouble >= reactionNextTime)
        {
            BeginReactionWait();
        }
    }

    // --------------------------------------------------
    // 트래킹 모드
    // --------------------------------------------------

    private void InitializeTracking()
    {
        trackingStartSpeed = Mathf.Max(
            0.1f,
            trackingStartSpeed
        );

        trackingAcceleration = Mathf.Max(
            0f,
            trackingAcceleration
        );

        trackingLoseDuration = Mathf.Max(
            0.1f,
            trackingLoseDuration
        );

        trackingRecoveryDuration = Mathf.Max(
            0.1f,
            trackingRecoveryDuration
        );

        trackingHoldTime = 0f;

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

        // 이동 범위 중앙에서 시작한다.
        trackingPhase = new Vector2(
            (trackingMaxPosition.x - trackingMinPosition.x) * 0.5f,
            (trackingMaxPosition.y - trackingMinPosition.y) * 0.5f
        );

        // 처음 이동할 대각선 방향을 무작위로 정한다.
        float angle = Random.Range(25f, 65f) * Mathf.Deg2Rad;

        trackingDirection = new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        );

        if (Random.value < 0.5f)
        {
            trackingDirection.x *= -1f;
        }

        if (Random.value < 0.5f)
        {
            trackingDirection.y *= -1f;
        }

        trackingProgressText.gameObject.SetActive(true);

        UpdateTrackingProgress(false);
        UpdateTimeText();

        // 씬 진입 프레임은 측정하지 않는다.
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

        // 이번 프레임 동안 속도가 증가하는 것을 반영한 이동 거리.
        float moveDistance =
            trackingCurrentSpeed * delta
            + 0.5f * trackingAcceleration * delta * delta;

        trackingPhase += trackingDirection * moveDistance;

        // 이동 범위 끝에 닿으면 해당 축의 방향을 반전한다.
        trackingTarget.position = new Vector3(
            trackingMinPosition.x
                + Mathf.PingPong(trackingPhase.x, width),

            trackingMinPosition.y
                + Mathf.PingPong(trackingPhase.y, height),

            (trackingMinPosition.z + trackingMaxPosition.z) * 0.5f
        );

        // 이동한 Collider 위치를 판정에 반영한다.
        Physics.SyncTransforms();

        bool isOnTarget = false;

        if (Mouse.current != null)
        {
            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            if (trackingCamera.pixelRect.Contains(mousePosition))
            {
                Ray ray = trackingCamera.ScreenPointToRay(
                    mousePosition
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
            }
        }

        trackingElapsedTime += delta;

        // 플레이 시간이 지날수록 속도가 계속 증가한다.
        trackingCurrentSpeed =
            trackingStartSpeed
            + trackingAcceleration * trackingElapsedTime;

        if (isOnTarget)
        {
            // 결과창에 사용할 누적 추적 시간
            trackingSuccessTime += delta;

            // 타겟을 연속으로 따라간 시간
            trackingHoldTime = Mathf.Min(
                trackingRecoveryDuration,
                trackingHoldTime + delta
            );

            // 필요한 유지 시간을 채웠을 때만
            // 실패까지 남은 시간을 완전히 회복한다.
            if (trackingHoldTime >= trackingRecoveryDuration)
            {
                trackingLostTime = 0f;
            }

            // 유지 시간을 채우는 동안에는
            // trackingLostTime을 증가시키거나 초기화하지 않는다.
        }
        else
        {
            // 타겟을 놓치면 연속 유지 시간은 초기화한다.
            trackingHoldTime = 0f;

            // 이전에 사용한 실패 시간을 이어서 누적한다.
            trackingLostTime = Mathf.Min(
                trackingLoseDuration,
                trackingLostTime + delta
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
    // 반응속도 모드
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

        // 트래킹 모드에서는 사격하지 않는다.
        if (IsTracking)
            return false;

        // 라운드·타임어택 모드는 바로 사격을 허용한다.
        if (!IsReactionTime)
            return true;

        if (reactionState == ReactionState.Waiting)
        {
            earlyClicks++;

            ShowReactionFeedback("Too Early!");

            return false;
        }

        // 타겟이 화면에 표시된 다음 프레임부터 사격한다.
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

    private void EndGame(bool cleared)
    {
        if (hasEnded)
            return;

        hasEnded = true;

        roundCleared = IsRoundMode && cleared;

        targetSpawner.StopSpawning();

        UpdateTimeText();

        if (IsTracking)
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
            double averageMs =
                totalReactionSeconds / completedAttempts * 1000d;

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

            UpdateReactionBest((float)averageMs);
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

        // 트래킹 모드는 최고 기록 표시를 사용하지 않는다.
        bestScoreText.gameObject.SetActive(!IsTracking);

        bool usesRounds = IsRoundMode;

        nextRoundButton.SetActive(usesRounds && cleared);
        restartButton.SetActive(!usesRounds || !cleared);

        restartButtonText.text = (IsReactionTime || IsTracking)
            ? "Retry"
            : "Continue";

        resultPanel.SetActive(true);
    }

    // --------------------------------------------------
    // 최고 기록
    // --------------------------------------------------

    private void UpdateReactionBest(float averageMs)
    {
        // 측정 횟수가 다르면 최고 기록도 따로 저장한다.
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
        if (hasEnded || isPaused)
            return;

        if (!IsReactionTime &&
            !IsTracking &&
            Time.time >= endTime)
        {
            return;
        }

        isPaused = true;

        Time.timeScale = 0f;
        AudioListener.pause = true;

        pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (hasEnded || !isPaused)
            return;

        isPaused = false;
        resumeFrame = Time.frameCount;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        pausePanel.SetActive(false);

        // 반응속도는 완료 기록을 유지하고,
        // 진행 중인 시도만 새 무작위 대기로 시작한다.
        if (IsReactionTime &&
            reactionState != ReactionState.None)
        {
            BeginReactionWait();
        }

        // 트래킹은 위치와 누적 시간을 유지한 채 계속 진행한다.
    }

    // --------------------------------------------------
    // 재시작 및 씬 이동
    // --------------------------------------------------

    public void RestartGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // roundIndex를 유지하므로 같은 라운드로 재시작한다.
        SceneManager.LoadScene("GameScene");
    }

    public void ReturnToMainMenu()
    {
        roundIndex = 0;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene("StartScene");
    }

    public void NextRound()
    {
        if (!IsRoundMode || !hasEnded || !roundCleared)
            return;

        roundIndex++;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene("GameScene");
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

        // X 과녁 파괴 횟수가 제한에 도달하면 즉시 실패한다.
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

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}