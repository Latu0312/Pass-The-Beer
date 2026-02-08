using UnityEngine;

public class CanvasSwitcher : MonoBehaviour
{
    public GameObject mainMenuCanvas;
    public GameObject gameplayCanvas;

    public void GoToGameplay()
    {
        Time.timeScale = 1f;
        mainMenuCanvas.SetActive(false);
        gameplayCanvas.SetActive(true);
        GameManager.Instance.StartGame();
    }

    public void GoToMenu()
    {
        Time.timeScale = 0f;
        gameplayCanvas.SetActive(false);
        mainMenuCanvas.SetActive(true);
        GameManager.Instance.BackToMenu();
    }
}
