using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameManager gameManager;

    private int score = 0;

    public int CurrentScore
    {
        get
        {
            return score;
        }
    }

    private void Start()
    {
        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        score += amount;
        score = Mathf.Max(0, score);

        UpdateScoreText();

        gameManager.CheckRoundClear();
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }
}