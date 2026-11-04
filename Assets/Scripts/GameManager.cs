using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    [SerializeField] private float gameDuration = 60f;
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

    private bool isPaused = false;
    private int resumeFrame = -1;

    private const string BestScoreKey = "ShootingRange_BestScore"; 

    private float endTime;
    private bool hasEnded = false;

    private static int roundIndex = 0;

    private readonly int[] targetScores = { 100, 150, 200 };

    private bool roundCleared = false;

    public int CurrentRound
    {
        get
        {
            return roundIndex + 1;
        }
    }

    public bool IsPlaying
    {
        get
        {
            return !hasEnded
                && !isPaused
                && Time.time < endTime
                && Time.frameCount > resumeFrame;
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        pausePanel.SetActive(false);
        resultPanel.SetActive(false);

        endTime = Time.time + gameDuration;

        UpdateTimeText();

        roundText.text =
        "Round " + (roundIndex + 1) +
        " / " + targetScores.Length +
        "   Target: " + TargetScore;
    }

    private void Update()
    {
        if (hasEnded)
        {
            return;
        }

        if (!isPaused && Time.time >= endTime)
        {
            UpdateTimeText();
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

        if (isPaused)
        {
            return;
        }

        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        float remainingTime = Mathf.Max(0f, endTime - Time.time);

        timeText.text = "Time: " + Mathf.CeilToInt(remainingTime);
    }

    private void EndGame(bool cleared)
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        roundCleared = cleared;

        targetSpawner.StopSpawning();

        UpdateTimeText();

        if (cleared)
        {
            if (IsLastRound)
            {
                resultTitle.text = "ALL ROUNDS CLEAR";
            }
            else
            {
                resultTitle.text = "ROUND CLEAR";
            }
        }
        else
        {
            resultTitle.text = "ROUND FAILED";
        }

        finalScoreText.text =
            "Final Score: " + scoreManager.CurrentScore +
            " / " + TargetScore;

        resultStatsText.text =
            "Shots: " + shootingStats.TotalShots + "\n" +
            "Static Hits: " + shootingStats.StaticHits + "\n" +
            "Moving Hits: " + shootingStats.MovingHits + "\n" +
            "Forbidden Hits: " + shootingStats.ForbiddenHits + "\n" +
            "Accuracy: " + shootingStats.Accuracy.ToString("F1") + "%";

        nextRoundButton.SetActive(cleared && !IsLastRound);

        restartButton.SetActive(!cleared || IsLastRound);

        if (cleared && IsLastRound)
        {
            restartButtonText.text = "Play Again";
        }
        else
        {
            restartButtonText.text = "Restart";
        }

        UpdateBestScore();

        resultPanel.SetActive(true);
    }

    public void RestartGame()
    {
        if (roundCleared && IsLastRound)
        {
            roundIndex = 0;
        }

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

    private void UpdateBestScore()
    {
        int bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

        int currentScore = scoreManager.CurrentScore;

        bool isNewRecord = currentScore > bestScore;

        if (isNewRecord)
        {
            bestScore = currentScore;

            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        bestScoreText.text = "Best Score: " + bestScore;

        if (isNewRecord)
        {
            bestScoreText.text += "  NEW!";
        }
    }

    public void PauseGame()
    {
        if (hasEnded || isPaused || Time.time >= endTime)
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
        {
            return;
        }

        isPaused = false;

        resumeFrame = Time.frameCount;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        pausePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private int TargetScore
    {
        get
        {
            return targetScores[roundIndex];
        }
    }

    private bool IsLastRound
    {
        get
        {
            return roundIndex == targetScores.Length - 1;
        }
    }

    public void CheckRoundClear()
    {
        if (!IsPlaying)
        {
            return;
        }

        if (scoreManager.CurrentScore >= TargetScore)
        {
            EndGame(true);
        }
    }

    public void NextRound()
    {
        if (!hasEnded || !roundCleared || IsLastRound)
        {
            return;
        }

        roundIndex++;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene("GameScene");
    }

    public static void ResetRounds()
    {
        roundIndex = 0;
    }
}