using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HigherLowerGame : MonoBehaviour
{
    [Header("Karty (cards_0 .. cards_51)")]
    [Tooltip("Wpisz rozmiar 52 i przeciągnij karty w kolejności cards_0..cards_51")]
    public Sprite[] cardSprites;
    public Sprite cardBack;

    [Header("Wyświetlanie Kart")]
    public Image currentCardImage;
    public Image nextCardImage;

    [Header("UI")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI streakText;
    public TextMeshProUGUI cashOutAmountText;

    [Header("System Zakładów")]
    public TMP_InputField betInput;
    // Żetony (5$, 10$, 20$, 50$) podpinasz bezpośrednio w On Click() → AddToBet(wartość)
    // — tak samo jak w CoinflipGame i DiceGame

    [Header("Przyciski")]
    public Button btnDeal;     // Przycisk ROLL / DEAL — odkrywa pierwszą kartę
    public Button btnHigher;
    public Button btnLower;
    public Button btnCashOut;

    [Header("Koszty")]
    public int energyCost = 3;

    [Header("Instruction Panel")]
    public GameObject instructionPanel;

    // ── Talia ──────────────────────────────────────────────────────────────
    private int[] deck = new int[52];
    private int deckIndex = 0;
    private int currentCardIndex = -1;
    private int lastDrawnCard = -1;

    // ── Stan gry ───────────────────────────────────────────────────────────
    private int  currentBet  = 0;
    private int  streakCount = 0;
    private bool isAnimating = false;
    private bool inStreak    = false;
    private bool cardDealt   = false; // czy pierwsza karta juz odkryta

    // ══════════════════════════════════════════════════════════════════════
    // START
    // ══════════════════════════════════════════════════════════════════════
    void Start()
    {
        ShuffleDeck();
        // NIE losujemy karty na starcie — gracz musi kliknąć ROLL

        if (betInput != null)
        {
            betInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            betInput.onValueChanged.AddListener(OnBetChanged);
            betInput.text = "0";
        }

        RefreshUI();
    }

    // ══════════════════════════════════════════════════════════════════════
    // TALIA
    // ══════════════════════════════════════════════════════════════════════
    private void ShuffleDeck()
    {
        for (int i = 0; i < 52; i++) deck[i] = i;
        for (int i = 51; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
        deckIndex = 0;
    }

    private int DrawCard()
    {
        if (deckIndex >= 52)
        {
            ShuffleDeck();
            StartCoroutine(FlashMessage("<color=#AAAAAA>Deck reshuffled!</color>", 1.2f));
        }
        return deck[deckIndex++];
    }

    // ══════════════════════════════════════════════════════════════════════
    // WARTOŚĆ KARTY
    // Kolejność: 2<3<4<5<6<7<8<9<10<J<Q<K<A
    // cards_0 = As (rank 0) → wartość 13 (najwyższa)
    // cards_1 = 2  (rank 1) → wartość 1  (najniższa)
    // cards_2 = 3  (rank 2) → wartość 2
    // ...
    // cards_12= K  (rank 12)→ wartość 12
    // ══════════════════════════════════════════════════════════════════════
    private int GetValue(int cardIndex)
    {
        int rank = cardIndex % 13;
        if (rank == 0) return 13;   // As — najwyższa karta
        return rank;                // 2=1, 3=2 ... K=12
    }

    private string GetCardName(int cardIndex)
    {
        string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        string[] suits = { "♦", "♥", "♠", "♣" };
        return ranks[cardIndex % 13] + suits[cardIndex / 13];
    }

    // ══════════════════════════════════════════════════════════════════════
    // BET
    // ══════════════════════════════════════════════════════════════════════
    public void AddToBet(int amount)
    {
        if (inStreak || isAnimating) return;
        currentBet += amount;
        if (betInput != null) betInput.text = currentBet.ToString();
    }

    public void ClearBet()
    {
        if (inStreak || isAnimating) return;
        currentBet = 0;
        if (betInput != null) betInput.text = "0";
    }

    private void OnBetChanged(string value)
    {
        if (inStreak || isAnimating) return;
        currentBet = int.TryParse(value, out int v) ? v : 0;
    }

    // ══════════════════════════════════════════════════════════════════════
    // DEAL — odkrycie pierwszej karty (podpięte pod przycisk ROLL)
    // ══════════════════════════════════════════════════════════════════════
    public void DealCard()
    {
        if (isAnimating || cardDealt) return;

        if (currentBet <= 0)
        {
            if (resultText != null) resultText.text = "Place your bet first!";
            return;
        }
        if (EnergyManager.Instance != null && !EnergyManager.Instance.SpendEnergy(energyCost))
        {
            if (resultText != null) resultText.text = "Not enough energy!\n<size=50%>(Cost: " + energyCost + " En.)</size>";
            return;
        }
        if (MoneyManager.Instance == null || !MoneyManager.Instance.SpendMoney(currentBet))
        {
            if (EnergyManager.Instance != null) EnergyManager.Instance.AddEnergy(energyCost);
            if (resultText != null) resultText.text = "Not enough money for this bet!";
            return;
        }

        cardDealt = true;
        currentCardIndex = DrawCard();

        if (currentCardImage != null) 
        {
            currentCardImage.gameObject.SetActive(true);
            currentCardImage.sprite = cardSprites[currentCardIndex];
        }
        // Zaczynamy nową grę — ukrywamy obok całkowicie drugą kartę na czas namysłu
        if (nextCardImage != null) nextCardImage.gameObject.SetActive(false);
        if (resultText != null) resultText.text = "Higher or Lower?";

        // Pokaż HIGHER/LOWER, zablokuj DEAL
        if (btnDeal   != null) btnDeal.interactable = false;
        if (btnHigher != null) btnHigher.interactable = true;
        if (btnLower  != null) btnLower.interactable  = true;
        if (betInput  != null) betInput.interactable = false;
    }

    // ══════════════════════════════════════════════════════════════════════
    // ZGADYWANIE
    // ══════════════════════════════════════════════════════════════════════
    public void GuessHigher() => MakeGuess(true);
    public void GuessLower()  => MakeGuess(false);

    private void MakeGuess(bool guessedHigher)
    {
        if (isAnimating || !cardDealt) return;

        // W streak gracz już zapłacił — nie pobieramy ponownie
        SetAllInteractable(false);
        isAnimating = true;
        StartCoroutine(RevealRoutine(guessedHigher));
    }

    private IEnumerator RevealRoutine(bool guessedHigher)
    {
        // Przed rozpoczęciem "tasowania", skopiuj poprzednio wylosowaną kartę
        // na miejsce "aktualnej" karty (teraz ona jest punktem odniesienia)
        if (inStreak)
        {
            currentCardIndex = lastDrawnCard;
            if (currentCardImage != null) currentCardImage.sprite = cardSprites[currentCardIndex];
            // Malutka przerwa żeby gracz zauważył "przesunięcie" karty
            yield return new WaitForSeconds(0.2f);
        }

        // Pokaż rewers podczas "tasowania" nowej karty
        if (nextCardImage != null) 
        {
            nextCardImage.gameObject.SetActive(true);
            nextCardImage.sprite = cardBack;
        }
        if (resultText != null) resultText.text = "Revealing...";
        yield return new WaitForSeconds(0.6f);

        // Dobierz kartę
        lastDrawnCard = DrawCard();
        if (nextCardImage != null) nextCardImage.sprite = cardSprites[lastDrawnCard];
        yield return new WaitForSeconds(0.35f);

        // Porównaj
        int valCurrent = GetValue(currentCardIndex);
        int valNext    = GetValue(lastDrawnCard);

        bool isTie    = valCurrent == valNext;
        bool isHigher = valNext > valCurrent;

        string nextName = GetCardName(lastDrawnCard);

        if (isTie)
        {
            // Remis — zwróć zakład (tylko jeśli to był pierwszy rzut)
            if (!inStreak && MoneyManager.Instance != null)
                MoneyManager.Instance.AddMoney(currentBet);

            if (resultText != null) resultText.text = "<color=white>TIE! (" + nextName + ")</color>\n" +
                (inStreak ? "Streak lost — bet was at stake." : "Bet returned.");

            FinishRound();
        }
        else if (isHigher == guessedHigher)
        {
            // ✅ Wygrana!
            streakCount++;
            inStreak = true;
            int potentialPayout = currentBet * (streakCount + 1);

            string streakFire = BuildFireString(streakCount);
            if (streakText != null) streakText.text = streakFire + " Streak x" + (streakCount + 1) + "!";
            if (resultText != null) resultText.text = "<color=green>Correct! (" + nextName + ")</color>\nKeep going or Cash Out!";
            if (cashOutAmountText != null) cashOutAmountText.text = "CASH OUT\n" + potentialPayout + "$";
            if (btnCashOut != null) btnCashOut.interactable = true;

            // Zwycięska karta ZOSTAJE na miejscu "nextCardImage" żeby gracz mógł na nią popatrzeć.
            // Przeskoczy ona na miejsce "currentCardImage" dopiero jak gracz naciśnie Higher/Lower (kod wyżej)

            isAnimating = false;
            SetButtons(streakActive: true);
        }
        else
        {
            // ❌ Przegrana
            if (resultText != null) resultText.text = "<color=red>Wrong! (" + nextName + ")</color>\nYou lose " + currentBet + "$!";
            FinishRound();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // CASH OUT
    // ══════════════════════════════════════════════════════════════════════
    public void CashOut()
    {
        if (!inStreak || isAnimating) return;

        int payout = currentBet * (streakCount + 1);
        if (MoneyManager.Instance != null) MoneyManager.Instance.AddMoney(payout);

        string streakFire = BuildFireString(streakCount);
        if (resultText != null) resultText.text = "<color=yellow>" + streakFire + " Cashed out! Won " + payout + "$!</color>";

        FinishRound();
    }

    // ══════════════════════════════════════════════════════════════════════
    // RESET RUNDY
    // ══════════════════════════════════════════════════════════════════════
    private void FinishRound()
    {
        streakCount = 0;
        inStreak    = false;
        isAnimating = false;
        cardDealt   = false;

        currentCardIndex = -1;
        lastDrawnCard    = -1;

        // Nie ukrywamy już kart! Zostają widoczne (podsumowanie gry).
        // Prawy rewers wróci dopiero przy kolejnym kliknięciu "ROLL".
        if (streakText       != null) streakText.text         = "";
        if (btnCashOut       != null) btnCashOut.interactable = false;
        if (cashOutAmountText!= null) cashOutAmountText.text  = "CASH OUT";

        if (btnDeal   != null) btnDeal.interactable   = true;
        if (btnHigher != null) btnHigher.interactable = false;
        if (btnLower  != null) btnLower.interactable  = false;
        if (betInput  != null) betInput.interactable = true;

        if (resultText != null) resultText.text = "Place your bet and ROLL!";
    }

    private void RefreshUI()
    {
        // Start: obie karty całkowicie ukryte
        if (currentCardImage != null) currentCardImage.gameObject.SetActive(false);
        if (nextCardImage    != null) nextCardImage.gameObject.SetActive(false);
        if (resultText       != null) resultText.text = "Place your bet and ROLL!";
        if (streakText       != null) streakText.text = "";
        if (btnCashOut       != null) btnCashOut.interactable = false;
        if (cashOutAmountText!= null) cashOutAmountText.text  = "CASH OUT";

        // DEAL aktywny, HIGHER/LOWER zablokowane
        if (btnDeal   != null) btnDeal.interactable   = true;
        if (btnHigher != null) btnHigher.interactable = false;
        if (btnLower  != null) btnLower.interactable  = false;
        if (betInput  != null) betInput.interactable = true;
    }

    // ══════════════════════════════════════════════════════════════════════
    // STEROWANIE PRZYCISKAMI
    // ══════════════════════════════════════════════════════════════════════
    private void SetButtons(bool streakActive)
    {
        bool canBet = !streakActive && !isAnimating;
        if (betInput != null) betInput.interactable = canBet;
        if (btnHigher != null) btnHigher.interactable = !isAnimating;
        if (btnLower  != null) btnLower.interactable  = !isAnimating;
    }

    private void SetAllInteractable(bool value)
    {
        if (btnHigher  != null) btnHigher.interactable  = value;
        if (btnLower   != null) btnLower.interactable   = value;
        if (btnCashOut != null) btnCashOut.interactable = value;
        if (betInput   != null) betInput.interactable   = value;
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPER
    // ══════════════════════════════════════════════════════════════════════
    private string BuildFireString(int count)
    {
        string result = "";
        int fires = Mathf.Min(count, 5);
        for (int i = 0; i < fires; i++) result += "\uD83D\uDD25";
        return result;
    }

    private IEnumerator FlashMessage(string msg, float duration)
    {
        string prev = resultText != null ? resultText.text : "";
        if (resultText != null) resultText.text = msg;
        yield return new WaitForSeconds(duration);
        if (resultText != null) resultText.text = prev;
    }

    // ══════════════════════════════════════════════════════════════════════
    // INSTRUKCJA
    // ══════════════════════════════════════════════════════════════════════
    public void OpenInstruction()
    {
        if (isAnimating) return;
        if (instructionPanel != null) instructionPanel.SetActive(true);
    }

    public void CloseInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    // WYJŚCIE
    // ══════════════════════════════════════════════════════════════════════
    public void ExitMinigame()
    {
        // Blokada wychodzenia podczas trwającej i nierozstrzygniętej gry
        if (isAnimating || inStreak || cardDealt) return;

        PlayerMovement.canMove = true;
        CloseInstruction();

        // Pełny reset w przypadku zamknięcia gry
        ClearBet();
        FinishRound();
        RefreshUI();

        gameObject.SetActive(false);
    }
}
