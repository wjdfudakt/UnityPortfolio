using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float timeAttackDuration = 60f;

    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TargetSpawner targetSpawner;
    [SerializeField] private ScoreManager scoreManager;

    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private ShootingStats shootingStats;
    [SerializeField] private TMP_Text resultStatsText;
    [SerializeField] private TMP_Text bestScoreText;

    [SerializeField] private GameObject pausePanel;

    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text resultTitle;

    [SerializeField] private GameObject nextRoundButton;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private TMP_Text restartButtonText;

    [SerializeField] private int firstTargetScore = 100;
    [SerializeField] private int scoreIncreasePerRound = 50;

    [Header("Reaction Test")]
    [SerializeField, Min(1)]
    private int reactionAttempts = 10;

    [SerializeField, Min(0.1f)]
    private float minReactionWait = 1f;

    [SerializeField, Min(0.1f)]
    private float maxReactionWait = 3f;

    [SerializeField, Min(0.1f)]
    private float reactionFeedbackDuration = 0.8f;

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

    private const string BestScoreKey =
        "ShootingRange_BestScore";

    private const string TimeAttackBestHitsKey =
        "ShootingRange_TimeAttack_BestHits";

    private const string ReactionBestKey =
        "ShootingRange_Reaction_BestAverageMs_";

    private bool isPaused;
    private int resumeFrame = -1;

    private float endTime;
    private bool hasEnded;

    private static int roundIndex;
    private bool roundCleared;

    public bool IsRoundMode =>
        GameSettings.SelectedMode == GameMode.Round;

    public bool IsTimeAttack =>
        GameSettings.SelectedMode == GameMode.TimeAttack;

    public bool IsReactionTime =>
        GameSettings.SelectedMode == GameMode.ReactionTime;

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
                && (IsReactionTime || Time.time < endTime);
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

        if (!IsReactionTime &&
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

        if (!isPaused && !IsReactionTime)
        {
            UpdateTimeText();
        }
    }

    private void LateUpdate()
    {
        if (!IsReactionTime || !IsPlaying)
            return;

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

    private void UpdateTimeText()
    {
        if (IsReactionTime)
            return;

        float remainingTime = Mathf.Max(
            0f,
            endTime - Time.time
        );

        timeText.text =
            "Time: " + Mathf.CeilToInt(remainingTime);
    }

    private void EndGame(bool cleared)
    {
        if (hasEnded)
            return;

        hasEnded = true;
        roundCleared = IsRoundMode && cleared;

        targetSpawner.StopSpawning();

        UpdateTimeText();

        if (IsReactionTime)
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
            resultTitle.text = cleared
                ? "ROUND CLEAR"
                : "ROUND FAILED";

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

        bestScoreText.gameObject.SetActive(true);

        nextRoundButton.SetActive(IsRoundMode && cleared);
        restartButton.SetActive(!IsRoundMode || !cleared);

        restartButtonText.text = IsReactionTime
            ? "Retry"
            : "Continue";

        resultPanel.SetActive(true);
    }

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

    public void PauseGame()
    {
        if (hasEnded || isPaused)
            return;

        if (!IsReactionTime && Time.time >= endTime)
            return;

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

        if (IsReactionTime &&
            reactionState != ReactionState.None)
        {
            BeginReactionWait();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene("GameScene");
    }

    public void ReturnToMainMenu()
    {
        roundIndex = 0;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene("StartScene");
    }

    public void CheckRoundClear()
    {
        if (!IsRoundMode || !IsPlaying)
            return;

        if (scoreManager.CurrentScore >= TargetScore)
        {
            EndGame(true);
        }
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

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}