using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameManager gameManager;
    private int score;
    public int CurrentScore => score;

    public void ResetScore()
    {
        score = 0;
        UpdateScoreText();
    }

    public void SetVisible(bool visible)
    {
        if (scoreText != null) scoreText.gameObject.SetActive(visible);
    }

    public void AddScore(int amount)
    {
        if (gameManager == null || !gameManager.IsPlaying
            || gameManager.IsReactionTime || gameManager.IsTracking) return;
        score = Mathf.Max(0, score + amount);
        UpdateScoreText();
        gameManager.CheckRoundClear();
    }

    private void UpdateScoreText()
    {
        if (scoreText == null || gameManager == null
            || gameManager.IsReactionTime || gameManager.IsTracking) return;
        scoreText.text = (gameManager.IsTimeAttack ? "Targets Hit: " : "Score: ") + score;
    }
}
