using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TravelMenu : MonoBehaviour
{
    [Header("Przyciski Lokacji")]
    public Button btnHome;
    public Button btnBar;
    public Button btnCasino;
    public Button btnMainMenu;

    [Header("Referencja do DoorTrigger (do zamknięcia menu)")]
    public DoorTrigger doorTrigger;

    // Nazwy scen — muszą zgadzać się z Build Settings!
    private const string SCENE_BAR    = "barScene";
    private const string SCENE_HOME   = "roomScene";
    private const string SCENE_CASINO = "casinoScene";
    private const string SCENE_MAINMENU = "MainMenu";

    void OnEnable()
    {
        // Przy każdym otwarciu panelu odśwież stan przycisków
        SetupButtons();
    }

    private void SetupButtons()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        bool canGoHome = currentScene != SCENE_HOME;
        bool canGoBar = currentScene != SCENE_BAR;
        bool canGoCasino = currentScene != SCENE_CASINO;

        if (GameManager.Instance != null && GameManager.Instance.currentMode == GameMode.Campaign && !GameManager.Instance.hasSuit)
        {
            canGoCasino = false;
        }

        // Włącz wszystkie, zablokuj tylko aktualną scenę
        SetButton(btnHome,   canGoHome,   "Home");
        SetButton(btnBar,    canGoBar,    "Bar");
        
        string casinoLabel = (GameManager.Instance != null && GameManager.Instance.currentMode == GameMode.Campaign && !GameManager.Instance.hasSuit) 
                             ? "Casino (You need a suit)" 
                             : "Casino";
        SetButton(btnCasino, canGoCasino, casinoLabel);
        SetButton(btnMainMenu, currentScene != SCENE_MAINMENU, "Main Menu");
    }

    private void SetButton(Button btn, bool interactable, string label)
    {
        if (btn == null) return;
        btn.interactable = interactable;

        // Opcjonalne: przyciemnij tekst na zablokowanym przycisku
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = interactable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            tmp.text = label;
        }
    }

    // ============================================
    // Metody podpinane pod On Click() przycisków
    // ============================================
    public void GoHome()    => TravelTo(SCENE_HOME);
    public void GoBar()     => TravelTo(SCENE_BAR);
    public void GoCasino()  => TravelTo(SCENE_CASINO);
    public void GoMainMenu()=> TravelTo(SCENE_MAINMENU);

    private void TravelTo(string sceneName)
    {
        PlayerMovement.canMove = true; // Przywróć ruch przed zmianą sceny
        SceneManager.LoadScene(sceneName);
    }

    // Przycisk "X" / Zamknij
    public void CloseMenu()
    {
        // Zawsze przywróć ruch
        PlayerMovement.canMove = true;

        // Jeśli nie podpięto ręcznie — znajdź automatycznie w scenie
        if (doorTrigger == null)
            doorTrigger = FindObjectOfType<DoorTrigger>();

        if (doorTrigger != null)
            doorTrigger.CloseMenu(); // DoorTrigger zadba o dymek i stan menuOpen
        else
            gameObject.SetActive(false); // ostateczny fallback
    }
}
