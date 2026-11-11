using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public void StartGame()
    {
        GameSettings.SelectedMode = GameMode.Round;

        GameManager.ResetRounds();

        SceneManager.LoadScene("GameScene");
    }

    public void StartTimeAttack()
    {
        GameSettings.SelectedMode = GameMode.TimeAttack;

        SceneManager.LoadScene("GameScene");
    }

    public void StartReactionTime()
    {
        GameSettings.SelectedMode = GameMode.ReactionTime;

        SceneManager.LoadScene("GameScene");
    }

    public void StartTracking()
    {
        GameSettings.SelectedMode = GameMode.Tracking;

        GameManager.ResetRounds();

        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료 버튼을 눌렀습니다.");

        Application.Quit();
    }
}