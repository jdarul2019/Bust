using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Przypisz w Inspektorze nazwę sceny, w której znajduje się Hub/Kasyno (np. "GameScene" lub "barScene")
    [Tooltip("Dokładna nazwa sceny z właściwą grą do załadowania")]
    public string gameSceneName = "barScene";

    public void StartCampaignMode()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentMode = GameMode.Campaign;
        }
        LoadGame();
    }

    public void StartEndlessMode()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentMode = GameMode.Endless;
        }
        LoadGame();
    }

    private void LoadGame()
    {
        // Załaduj scenę główną
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("QUIT GAME");
        Application.Quit();
    }
}
