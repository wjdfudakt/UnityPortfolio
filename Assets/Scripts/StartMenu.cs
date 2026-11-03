using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료 버튼을 눌렀습니다.");

        Application.Quit();
    }
}