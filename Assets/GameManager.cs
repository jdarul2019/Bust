using UnityEngine;

public enum GameMode
{
    Campaign,
    Endless
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("The selected game mode. Set by Main Menu.")]
    public GameMode currentMode = GameMode.Campaign;

    [Tooltip("Amount needed to pay off the mafia at Day 7 (Campaign only)")]
    public int debtAmount = 10000;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TriggerGameOver(string reasonText)
    {
        Debug.Log("GAME OVER: " + reasonText);
        // We will call the UI from DayManager or a separate UI manager to show the End Screen
        if (DayManager.Instance != null)
        {
            DayManager.Instance.ShowEndGameScreen(false, reasonText);
        }
    }

    public void TriggerVictory(string reasonText)
    {
        Debug.Log("VICTORY: " + reasonText);
        if (DayManager.Instance != null)
        {
            DayManager.Instance.ShowEndGameScreen(true, reasonText);
        }
    }
}
