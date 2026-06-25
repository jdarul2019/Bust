using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    // Zmienne UI usunięte z poziomu managera, obiekty lokalne sceny będą zlecane do Managera przez Łóżko

    [Header("Transition Settings")]
    public float showScreenDuration = 3f;

    private int currentDay = 1;
    private int moneyAtStartOfDay = 0;
    private bool isTransitioning = false;
    
    [Header("End Game UI")]
    public GameObject finalResultPanel;
    public TextMeshProUGUI finalResultTitle;
    public TextMeshProUGUI finalResultDesc;

    private void Awake()
    {
        // Wzorzec Singleton do zarządzania nockami
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

    private IEnumerator Start()
    {
        // Czekamy ułamek klatki, żeby MoneyManager zdążył załadować budżet początkowy gracza
        yield return new WaitForEndOfFrame();
        
        if (MoneyManager.Instance != null)
        {
            moneyAtStartOfDay = MoneyManager.Instance.GetBalance();
        }
    }

    public void FinishDay(GameObject panel, TextMeshProUGUI txtDay, TextMeshProUGUI txtProfit)
    {
        if (isTransitioning) return;
        
        // Zabezpieczenie braku energii i braku pieniędzy - omdlenie
        // Jeśli kończymy dzień, a gracz nie ma pieniędzy (a w Story nie jest to wyłapane od razu)
        if (MoneyManager.Instance != null && MoneyManager.Instance.GetBalance() <= 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver("You went bankrupt! No money left to play.");
                return;
            }
        }

        StartCoroutine(EndDayRoutine(panel, txtDay, txtProfit));
    }

    private IEnumerator EndDayRoutine(GameObject endOfDayPanel, TextMeshProUGUI dayText, TextMeshProUGUI profitText)
    {
        isTransitioning = true;
        PlayerMovement.canMove = false; // Blokuje ruch gracza (pauza animacji chodzenia postaci obok domku)

        // Wyliczenia zysków 
        int currentMoney = 0;
        if (MoneyManager.Instance != null) currentMoney = MoneyManager.Instance.GetBalance();
        
        int profit = currentMoney - moneyAtStartOfDay;

        // Ustawianie tekstów na UI - Numer Dnia
        if (dayText != null)
        {
            dayText.text = "Day " + currentDay + " Finished";
        }

        // Ustawianie utargu - kolorowanie zielono/czerwono
        if (profitText != null)
        {
            if (profit > 0)
            {
                profitText.text = "Profit: <color=green>+$" + profit + "</color>";
            }
            else if (profit < 0)
            {
                profitText.text = "Profit: <color=red>-$" + Mathf.Abs(profit) + "</color>";
            }
            else
            {
                profitText.text = "Profit: <color=white>$0</color>"; // Remis - wyszliśmy na zero!
            }
        }

        // Pokaż UI ekranu podsumowania i wycisz ekran
        if (endOfDayPanel != null) endOfDayPanel.SetActive(true);

        // Miejsce do rozbudowy w przyszłości: można podpiąć Animator i Fade-To-Black tuła.

        // Odczekaj podany czas wyświetlając ekran (aby gracz mógł się mu rzetelnie przyjrzeć uff)
        yield return new WaitForSeconds(showScreenDuration);

        // --- SPRAWDZENIE KRYTYCZNE DŁUGU (Tylko w trybie Campaign) ---
        if (GameManager.Instance != null && GameManager.Instance.currentMode == GameMode.Campaign)
        {
            if (currentDay == 7)
            {
                if (endOfDayPanel != null) endOfDayPanel.SetActive(false); // Chowamy dzienny
                
                int requiredDebt = GameManager.Instance.debtAmount;
                if (currentMoney >= requiredDebt)
                {
                    // Odciągamy dług z konta (opcjonalnie dla fabuły)
                    MoneyManager.Instance.SpendMoney(requiredDebt);
                    GameManager.Instance.TriggerVictory($"You successfully paid the ${requiredDebt} debt! The mafia is off your back.");
                }
                else
                {
                    GameManager.Instance.TriggerGameOver($"You failed to collect ${requiredDebt} by Day 7. The mafia has found you...");
                }
                yield break; // Zakończ procedurę, jesteśmy na ekranie końcowym
            }
        }

        // Śpimy: Resetowanie energii po nocy do totalnego punktu pod korek
        if (EnergyManager.Instance != null)
        {
            int maxEnergy = EnergyManager.Instance.GetMaxEnergy();
            EnergyManager.Instance.AddEnergy(maxEnergy); // Skrypt z energii samoistnie pilnuje obcięcia przy szczytowych wartościach
        }

        // Śpimy: Resetowanie poziomu alkoholu do zera
        if (AlcoholManager.Instance != null)
        {
            AlcoholManager.Instance.ResetAlcohol();
        }

        // --- Zaczyna się nowy ranek ---
        currentDay++;
        moneyAtStartOfDay = currentMoney; // Zapamiętujemy nowy punkt startowy by poprawnie wyliczyć procent profitu ZA JUTRO!
        
        // Sprzątamy UI
        if (endOfDayPanel != null) endOfDayPanel.SetActive(false);
        PlayerMovement.canMove = true; // Gracz może już chodzić rano i wychodzić do roboty
        isTransitioning = false;
    }

    // Metoda wywoływana przez GameManager
    public void ShowEndGameScreen(bool isVictory, string description)
    {
        Debug.Log("ShowEndGameScreen wywołane! isVictory: " + isVictory);
        
        if (finalResultPanel != null)
        {
            Debug.Log("finalResultPanel NIE jest null. Próbuję go aktywować...");
            finalResultPanel.SetActive(true);
            Debug.Log("finalResultPanel.activeSelf to teraz: " + finalResultPanel.activeSelf + ", a w hierarchii (activeInHierarchy): " + finalResultPanel.activeInHierarchy);
            
            if (finalResultTitle != null)
            {
                finalResultTitle.text = isVictory ? "<color=green>YOU SURVIVED!</color>" : "<color=red>BUSTED!</color>";
            }
            else
            {
                Debug.LogWarning("finalResultTitle jest null, więc tytuł się nie zaktualizuje.");
            }
            
            if (finalResultDesc != null)
            {
                finalResultDesc.text = description;
            }
            else
            {
                Debug.LogWarning("finalResultDesc jest null.");
            }
        }
        else
        {
            Debug.LogError("Brak podpiętego Final Result Panel w DayManagerze!");
        }
    }

    // Pozwala na zaktualizowanie referencji do UI po załadowaniu nowej sceny
    public void RegisterEndGameUI(GameObject panel, TextMeshProUGUI title, TextMeshProUGUI desc)
    {
        finalResultPanel = panel;
        finalResultTitle = title;
        finalResultDesc = desc;
    }

    public void ResetDays()
    {
        currentDay = 1;
        if (MoneyManager.Instance != null)
        {
            moneyAtStartOfDay = MoneyManager.Instance.GetBalance();
        }
        else
        {
            moneyAtStartOfDay = 0;
        }
    }

    public void ReturnToMainMenu()
    {
        // Opcjonalnie: zresetuj stan gry przed wyjściem
        Time.timeScale = 1f; 
    
        // Zakładając, że Twoja scena z menu nazywa się "MainMenu"
        // Sprawdź w File -> Build Settings, czy nazwa jest identyczna!
        SceneManager.LoadScene("MainMenu");
    }
}
