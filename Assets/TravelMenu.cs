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

    [Header("Referencja do DoorTrigger (do zamknięcia menu)")]
    public DoorTrigger doorTrigger;

    // Nazwy scen — muszą zgadzać się z Build Settings!
    private const string SCENE_BAR    = "barScene";
    private const string SCENE_HOME   = "roomScene";
    private const string SCENE_CASINO = "casinoScene";

    void OnEnable()
    {
        // Przy każdym otwarciu panelu odśwież stan przycisków
        SetupButtons();
    }

    private void SetupButtons()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Włącz wszystkie, zablokuj tylko aktualną scenę
        SetButton(btnHome,   currentScene != SCENE_HOME,   "🏠  Home");
        SetButton(btnBar,    currentScene != SCENE_BAR,    "🍺  Bar");
        SetButton(btnCasino, currentScene != SCENE_CASINO, "🎰  Casino");
    }

    private void SetButton(Button btn, bool interactable, string label)
    {
        if (btn == null) return;
        btn.interactable = interactable;

        // Opcjonalne: przyciemnij tekst na zablokowanym przycisku
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.color = interactable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
    }

    // ============================================
    // Metody podpinane pod On Click() przycisków
    // ============================================
    public void GoHome()    => TravelTo(SCENE_HOME);
    public void GoBar()     => TravelTo(SCENE_BAR);
    public void GoCasino()  => TravelTo(SCENE_CASINO);

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
