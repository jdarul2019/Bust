using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinflipGame : MonoBehaviour
{
    public enum CoinSide { None, Heads, Tails }
    
    [Header("Elementy UI (Do przypisania w Inspektorze)")]
    public Button btnHeads;     // Przycisk "Orzeł"
    public Button btnTails;     // Przycisk "Reszka"
    public Button btnFlip;      // Przycisk "Rzuć"
    public TextMeshProUGUI resultText; // Tekst wyświetlający wynik/status
    
    [Header("Ruch Monety na Canvasie")]
    public RectTransform dropZone;
    public RectTransform coinTransform;

    [Header("Grafiki Monety (Opcjonalne)")]
    public Image coinImage;     // Komponent Image wyświetlający monetę
    public Sprite spriteHeads;  // Grafika "Orzeł" (np. coin_1)
    public Sprite spriteTails;  // Grafika "Reszka" (np. coin_2)

    [Header("System Zakładów")]
    public TMP_InputField betInput; // Pole tekstowe do ręcznego wpisywania kwoty
    private int currentBet = 0;     // Zmienna w tle trzymająca obecny zakład

    private CoinSide selectedSide = CoinSide.None;
    private bool isFlipping = false;

    // Odpala się przy starcie sceny/aktywacji obiektu
    void Start()
    {
        // Przycisk rzutu na początku jest nieaktywny, póki nie wybierzemy strony monety
        if (btnFlip != null) btnFlip.interactable = false; 
        if (resultText != null) resultText.text = "Wybierz Orła lub Reszkę.";

        // Konfiguracja Input Fielda
        if (betInput != null)
        {
            // Zabezpieczenie przed wpisywaniem liter (wymusza tylko integrery - liczby całkowite)
            betInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            // Dodanie listenera (nasłuchiwania), aby zmienna aktualizowała się, gdy gracz coś wpisze z klawiatury
            betInput.onValueChanged.AddListener(OnBetInputChanged);
            betInput.text = "0"; // Startowa wartość
        }
    }

    // ++ FUNKCJA DO PODPIĘCIA POD ŻETONY (5, 10, 20, 50) ++
    // W Inspektorze z listy podepnij "AddToBet", a w pustym okienku poniżej wpisz wartość danego żetonu
    public void AddToBet(int amount)
    {
        if (isFlipping) return;

        currentBet += amount;
        
        // Zmiana tekstu w InputField autmatycznie wywoła naszą funkcję OnBetInputChanged poniżej
        if (betInput != null)
        {
            betInput.text = currentBet.ToString();
        }
    }

    // ++ FUNKCJA WEWNĘTRZNA (Nasłuchuje klawiatury) ++
    private void OnBetInputChanged(string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            currentBet = 0;
            return;
        }

        if (int.TryParse(newValue, out int parsedValue))
        {
            currentBet = parsedValue;
        }
    }

    // Funkcja do podpięcia pod przycisk Orła w Unity (On Click)
    public void SelectHeads()
    {
        if (isFlipping) return;
        selectedSide = CoinSide.Heads;
        UpdateSelectionUI();
    }

    // Funkcja do podpięcia pod przycisk Reszki w Unity (On Click)
    public void SelectTails()
    {
        if (isFlipping) return;
        selectedSide = CoinSide.Tails;
        UpdateSelectionUI();
    }

    // Aktualizacja tekstów i odblokowanie przycisku "Rzuć"
    private void UpdateSelectionUI()
    {
        if (resultText != null)
        {
            string strSide = (selectedSide == CoinSide.Heads) ? "Orzeł" : "Reszka";
            resultText.text = "Wybrano: " + strSide + ". Naciśnij Rzuć!";
        }
        
        if (btnFlip != null) btnFlip.interactable = true;
    }

    // Funkcja do podpięcia pod główny przycisk Rzuć (On Click)
    public void StartFlip()
    {
        // Zabezpieczenie przed podwójnym rzutem lub brakiem wyboru
        if (isFlipping || selectedSide == CoinSide.None) return;

        // ++ ZABEZPIECZENIE: Sprawdzamy czy zakład jest większy od zera
        if (currentBet <= 0)
        {
            if (resultText != null) resultText.text = "Musisz postawić jakąś kwotę!";
            return; // Przerwij dalsze działanie - gracz nie pociągnie za wajchę
        }

        // ++ ZABEZPIECZENIE: Próba pobrania pieniędzy przez MoneyManager
        // "SpendMoney" zwraca true (jeśli się udało obciążyć konto) lub false (jeśli nie miał środków)
        if (MoneyManager.Instance == null || !MoneyManager.Instance.SpendMoney(currentBet))
        {
            if (resultText != null) resultText.text = "Brak środków na ten zakład!";
            return; // Przerwij - brakuje kasy
        }

        // Skoro wszystko ok, startujemy losowanie i podajemy mu stawkę, o jaką gramy
        StartCoroutine(FlipRoutine(currentBet));
    }

    // Główna logika losowania oparta na czasie (Korutyna)
    private IEnumerator FlipRoutine(int currentGameBet)
    {
        isFlipping = true;
        
        // Blokujemy przyciski na czas losowania
        if (btnHeads != null) btnHeads.interactable = false;
        if (btnTails != null) btnTails.interactable = false;
        if (btnFlip != null) btnFlip.interactable = false;

        if (resultText != null) resultText.text = "Trwa losowanie monety...";

        // Odczekanie 2 sekund - według Twoich instrukcji.
        // Daje to potem miejsce grafikowi na dołączenie ładnej animacji kręcącej się monety!
        yield return new WaitForSeconds(2f);

        if (dropZone != null && coinTransform != null)
        {
            float szerokosc = dropZone.rect.width;
            float wysokosc = dropZone.rect.height;
        
            float losoweX = Random.Range(-szerokosc / 2f, szerokosc / 2f);
            float losoweY = Random.Range(-wysokosc / 2f, wysokosc / 2f);
        
            coinTransform.anchoredPosition = new Vector2(losoweX, losoweY);
        }

        // Losowanko: 0 to Orzeł (Heads), 1 to Reszka (Tails)
        int randomResult = Random.Range(0, 2);
        CoinSide flippedSide = (randomResult == 0) ? CoinSide.Heads : CoinSide.Tails;
        string flippedSideName = (flippedSide == CoinSide.Heads) ? "Orzeł" : "Reszka";

        // Zmiana grafiki (obrazka) monety w zależności od tego, co wypadło
        if (coinImage != null)
        {
            coinImage.sprite = (flippedSide == CoinSide.Heads) ? spriteHeads : spriteTails;
        }

        // Sprawdzanie warunku wygranej i WYPŁATA (x2)
        if (flippedSide == selectedSide)
        {
            int winnings = currentGameBet * 2;
            
            // Wypłacamy nagrodę do portfela
            if (MoneyManager.Instance != null) MoneyManager.Instance.AddMoney(winnings);
            
            if (resultText != null) resultText.text = "Wylosowano: <color=green>" + flippedSideName + "</color>.\nWYGRYWASZ " + winnings + " $!";
        }
        else
        {
            if (resultText != null) resultText.text = "Wylosowano: <color=red>" + flippedSideName + "</color>.\nPRZEGRYWASZ " + currentGameBet + " $...";
        }

        // Odblokowanie przycisków po zakończeniu losowania do kolejnej gry
        isFlipping = false;
        if (btnHeads != null) btnHeads.interactable = true;
        if (btnTails != null) btnTails.interactable = true;
        
        // Resetujemy wybór: gracz znów musi kliknąć orła/reszkę aby móc rzucić
        selectedSide = CoinSide.None;
    }

    // Funkcja do wyjścia z mini-gry
    public void ExitMinigame()
    {
        // Jeśli dodasz przycisk "Wyjdź", blokujemy wyjście w trakcie losowania
        if (isFlipping) return;

        // Resetowanie stanu ekranu przed kolejnym wejściem
        selectedSide = CoinSide.None;
        if (btnFlip != null) btnFlip.interactable = false;
        if (resultText != null) resultText.text = "Wybierz Orła lub Reszkę.";
        
        // Ukrywamy cały Canvas/Panel z grą
        gameObject.SetActive(false);
    }
}
public class CoinMover : MonoBehaviour
{
    public RectTransform strefaZrzutu;
    public RectTransform moneta;

    // Tę funkcję Koder odpali w momencie "rzutu"
    public void WylosujMiejsceLadowania()
    {
        // 1. Sprawdzamy, jak szerokie i wysokie jest Twoje niewidzialne pudełko
        float szerokosc = strefaZrzutu.rect.width;
        float wysokosc = strefaZrzutu.rect.height;

        // 2. Losujemy pozycję X oraz Y. 
        // Skoro (0,0) to idealny środek pudełka, losujemy od minus połowy do plus połowy szerokości/wysokości!
        float losoweX = Random.Range(-szerokosc / 2f, szerokosc / 2f);
        float losoweY = Random.Range(-wysokosc / 2f, wysokosc / 2f);

        // 3. Teleportujemy monetę w wylosowane miejsce
        moneta.anchoredPosition = new Vector2(losoweX, losoweY);
    }
}