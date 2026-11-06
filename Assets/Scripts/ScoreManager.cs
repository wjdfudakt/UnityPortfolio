using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameManager gameManager;

    private int score;

    public int CurrentScore => score;

    private void Start()
    {
        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        if (gameManager != null && gameManager.IsReactionTime)
            return;

        score += amount;
        score = Mathf.Max(0, score);

        UpdateScoreText();

        if (gameManager != null)
        {
            gameManager.CheckRoundClear();
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText == null)
            return;

        bool isReaction =
            gameManager != null && gameManager.IsReactionTime;

        scoreText.gameObject.SetActive(!isReaction);

        if (isReaction)
            return;

        if (gameManager != null && gameManager.IsTimeAttack)
        {
            scoreText.text = "Targets Hit: " + score;
        }
        else
        {
            scoreText.text = "Score: " + score;
        }
    }
}