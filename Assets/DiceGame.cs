using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceGame : MonoBehaviour
{
    [Header("Taca rzutna (Wspólny Canvas Area)")]
    public RectTransform throwArea;
    [Tooltip("Minimalny odstęp w pikselach zapobiegający wejściu kości na kość")]
    public float safeDistance = 120f;

    [Header("Kości Krupiera (Góra)")]
    public Image[] dealerDice;
    public TextMeshProUGUI dealerScoreText;

    [Header("Kości Gracza (Dół)")]
    public Image[] playerDice;
    public TextMeshProUGUI playerScoreText;

    [Header("Baza Ikon (Sześć ścianek od 1 do 6)")]
    [Tooltip("Wklej tutaj 6 plików graficznych. Indeks 0 = 1 oczko. Indeks 5 = 6 oczek.")]
    public Sprite[] diceFaces;

    [Header("System Zakładów")]
    public TMP_InputField betInput;
    public Button btnRoll;
    public TextMeshProUGUI resultText;

    [Header("Koszty Gry")]
    public int energyCost = 5;

    [Header("Instruction Panel")]
    public GameObject instructionPanel;

    // Maszyny stanów
    private int currentBet = 0;
    private bool isPlaying = false;

    // Trzymane w pamięci bezpieczne wektory wygenerowane na stół
    private Vector2[] safePositionsCache = new Vector2[6];

    void Start()
    {
        if (btnRoll != null) btnRoll.interactable = true;
        
        if (betInput != null)
        {
            betInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            betInput.onValueChanged.AddListener(OnBetInputChanged);
            betInput.text = "0";
        }

        ResetUI();
    }

    public void AddToBet(int amount)
    {
        if (isPlaying) return;
        currentBet += amount;
        if (betInput != null) betInput.text = currentBet.ToString();
    }

    public void ClearBet()
    {
        if (isPlaying) return;
        currentBet = 0;
        if (betInput != null) betInput.text = "0";
    }

    private void OnBetInputChanged(string newValue)
    {
        if (isPlaying) return;
        if (string.IsNullOrEmpty(newValue)) { currentBet = 0; return; }
        if (int.TryParse(newValue, out int parsedValue)) { currentBet = parsedValue; }
    }

    private void ResetUI()
    {
        if (dealerScoreText != null) dealerScoreText.text = "Dealer: ?";
        if (playerScoreText != null) playerScoreText.text = "You: ?";
        if (resultText != null) resultText.text = "Roll against me...";
        
        // Czysty stół na wejściu, zanim kości zostaną wyrzucone
        HideAllDice();
    }

    private void HideAllDice()
    {
        foreach (var d in dealerDice) if (d != null) d.gameObject.SetActive(false);
        foreach (var d in playerDice) if (d != null) d.gameObject.SetActive(false);
    }

    public void StartRoll()
    {
        if (isPlaying) return;

        if (diceFaces == null || diceFaces.Length < 6)
        {
            if (resultText != null) resultText.text = "Error: Missing dice faces (min 6)!";
            return;
        }

        if (currentBet <= 0)
        {
            if (resultText != null) resultText.text = "Place your bet first!";
            return;
        }

        if (EnergyManager.Instance != null && !EnergyManager.Instance.SpendEnergy(energyCost))
        {
            if (resultText != null) resultText.text = "Not enough energy! \n<size=50%>(Cost: " + energyCost + " En.)</size>";
            return;
        }

        if (MoneyManager.Instance == null || !MoneyManager.Instance.SpendMoney(currentBet))
        {
            if (EnergyManager.Instance != null) EnergyManager.Instance.AddEnergy(energyCost);
            if (resultText != null) resultText.text = "Not enough money!";
            return;
        }

        // Przygotowanie logiki 
        StartCoroutine(PlayDiceRoutine(currentBet));
    }

    private void GenerateSafePositions()
    {
        if (throwArea == null) 
        {
            // Fallback na wypadek gdybyś nie uzupełnił w Inspektorze!
            for (int i = 0; i < 6; i++) safePositionsCache[i] = Vector2.zero;
            return;
        }

        float w = throwArea.rect.width / 2f;
        float h = throwArea.rect.height / 2f;
        float padding = 40f; // Margines błędu od krawędzi niewidzialnego panelu 

        for (int i = 0; i < 6; i++)
        {
            bool foundSpot = false;
            // Próbuj do 100 razy poszukać igłowego trafienia bezpiecznej pochyłości
            for (int attempts = 0; attempts < 100; attempts++)
            {
                Vector2 candidate = new Vector2(
                    Random.Range(-w + padding, w - padding),
                    Random.Range(-h + padding, h - padding)
                );

                bool isSafe = true;
                // Skontaktuj te propozycję ze WSZYSTKIMI już wcześniej zarejestrowanymi koścmi (z tych 6 leżących na wierzchu)
                for (int j = 0; j < i; j++)
                {
                    if (Vector2.Distance(candidate, safePositionsCache[j]) < safeDistance)
                    {
                        isSafe = false;
                        break;
                    }
                }

                if (isSafe)
                {
                    safePositionsCache[i] = candidate;
                    foundSpot = true;
                    break;
                }
            }

            // Ostateczność, gdybyś rzeźbił pole w edytorze zbyt małe i zapchał fizykiem stół do zera
            if (!foundSpot)
            {
                safePositionsCache[i] = new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f));
            }
        }
    }

    private IEnumerator PlayDiceRoutine(int betAmount)
    {
        isPlaying = true;
        if (btnRoll != null) btnRoll.interactable = false;
        
        HideAllDice();
        GenerateSafePositions();

        if (resultText != null) resultText.text = "Dealer throwing...";

        // ============================================
        // FAZA 1: RZUT KRUPIERA
        // ============================================
        float dealerSpinTime = 1.0f;
        float elapsed = 0f;
        float tickSpeed = 0.05f;

        // Pojawienie się kostek wroga na blacie
        foreach (var d in dealerDice) if (d != null) d.gameObject.SetActive(true);

        int d1 = 1, d2 = 1, d3 = 1;

        while (elapsed < dealerSpinTime)
        {
            if (dealerDice.Length > 0 && dealerDice[0] != null) ShuffleDie(dealerDice[0], safePositionsCache[0], out d1, false);
            if (dealerDice.Length > 1 && dealerDice[1] != null) ShuffleDie(dealerDice[1], safePositionsCache[1], out d2, false);
            if (dealerDice.Length > 2 && dealerDice[2] != null) ShuffleDie(dealerDice[2], safePositionsCache[2], out d3, false);
            
            yield return new WaitForSeconds(tickSpeed);
            elapsed += tickSpeed;
        }

        // Wbicie Kostek na Sztywno w Blat (Koniec rzutu dla tej puli)
        SnapDiceStable(dealerDice[0], safePositionsCache[0]);
        SnapDiceStable(dealerDice[1], safePositionsCache[1]);
        SnapDiceStable(dealerDice[2], safePositionsCache[2]);

        int dealerSum = d1 + d2 + d3;
        if (dealerScoreText != null) dealerScoreText.text = "Dealer: " + dealerSum;

        if (d1 == 6 && d2 == 6 && d3 == 6)
        {
            if (resultText != null) resultText.text = "<color=red>DEALER ROLLS [6-6-6]!\nDevastating Blow!</color>\nYou lose " + betAmount + "$!";
            FinishRoutine();
            yield break;
        }

        // ============================================
        // FAZA 2: RZUT GRACZA
        // ============================================
        if (resultText != null) resultText.text = "Now it's your turn!";
        yield return new WaitForSeconds(0.5f);

        // Pojawienie się Twoich kości
        foreach (var d in playerDice) if (d != null) d.gameObject.SetActive(true);

        float playerSpinTime = 1.5f;
        elapsed = 0f;
        int p1 = 1, p2 = 1, p3 = 1;

        while (elapsed < playerSpinTime)
        {
            if (playerDice.Length > 0 && playerDice[0] != null) ShuffleDie(playerDice[0], safePositionsCache[3], out p1, true);
            if (playerDice.Length > 1 && playerDice[1] != null) ShuffleDie(playerDice[1], safePositionsCache[4], out p2, true);
            if (playerDice.Length > 2 && playerDice[2] != null) ShuffleDie(playerDice[2], safePositionsCache[5], out p3, true);
            
            yield return new WaitForSeconds(tickSpeed);
            elapsed += tickSpeed;
        }

        SnapDiceStable(playerDice[0], safePositionsCache[3]);
        SnapDiceStable(playerDice[1], safePositionsCache[4]);
        SnapDiceStable(playerDice[2], safePositionsCache[5]);

        int playerSum = p1 + p2 + p3;
        if (playerScoreText != null) playerScoreText.text = "You: " + playerSum;

        // ============================================
        // FAZA 3: SĄDZIA (Wyniki)
        // ============================================
        EvaluateWinner(dealerSum, playerSum, p1, p2, p3, betAmount);
        FinishRoutine();
    }

    // Mikser odpowiedzialny za wizualny "TUMBLE" kości w fazie lotu - symuluje mocne uderzanie w pole
    private void ShuffleDie(Image dieImage, Vector2 targetStableBase, out int valueOut, bool isPlayerRoll)
    {
        dieImage.sprite = GetRandomDice(out valueOut, isPlayerRoll);

        // Symulacja podskakiwania i trzęsienia się kości przed zatrzymaniem
        Vector2 randomRumbleOffset = new Vector2(Random.Range(-15f, 15f), Random.Range(-15f, 15f));
        dieImage.rectTransform.anchoredPosition = targetStableBase + randomRumbleOffset;

        // Skrajnie zwariowane zawirowania kątowe podczas lotu
        dieImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(-180f, 180f));
    }

    // Usadza Kość Sztywno na stole opadając w gładki naturalny Kąt
    private void SnapDiceStable(Image dieImage, Vector2 stableBase)
    {
        if (dieImage == null) return;
        dieImage.rectTransform.anchoredPosition = stableBase;
        
        // Finalny kąt np od -40 do +40, udając realistyczne naturalne oparcie o podłoże!
        dieImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(-40f, 40f));
    }

    private void EvaluateWinner(int dealerScore, int playerScore, int p1, int p2, int p3, int betAmount)
    {
        int winAmount = 0;
        string msg = "";

        bool isTriple = (p1 == p2 && p2 == p3);

        if (playerScore > dealerScore)
        {   
            if (isTriple)
            {
                winAmount = betAmount * 10;
                msg = "<color=yellow>DARK HORSE!\nYou win with Triple (x10)!</color>\nYou win " + winAmount + "$";
            }
            else
            {
                winAmount = betAmount * 2;
                msg = "<color=green>Victory (x2)!</color>\nYou win " + winAmount + "$";
            }
        }
        else if (playerScore == dealerScore)
        {
            winAmount = betAmount; 
            msg = "<color=white>TIE!</color>\nDealer returns " + winAmount + "$";
        }
        else
        {
            msg = "<color=red>Lose</color>\nYou lose " + betAmount + "$";
        }

        if (winAmount > 0 && MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(winAmount);
        }

        if (resultText != null) resultText.text = msg;
    }

    private Sprite GetRandomDice(out int value, bool isPlayerRoll)
    {
        int index = Random.Range(0, 6);

        // Wpływ szczęścia (Alkohol) - działa tylko na rzuty gracza
        if (isPlayerRoll && AlcoholManager.Instance != null)
        {
            float luck = AlcoholManager.Instance.GetLuckModifier();
            if (luck > 0f)
            {
                // Dodatnie szczęście - szansa na przerzucenie bardzo słabego rzutu
                if (index < 2 && Random.value < luck * 2.5f) // luck 0.2 to 50% szans
                {
                    index = Random.Range(2, 6); // Zmiana na (3-6)
                }
            }
            else if (luck < 0f)
            {
                // Ujemne szczęście - szansa na popsucie dobrego rzutu
                float badLuckChance = Mathf.Abs(luck);
                if (index >= 3 && Random.value < badLuckChance) // bad luck 0.8 to 80% szans
                {
                    index = Random.Range(0, 3); // Zmiana na (1-3)
                }
            }
        }

        value = index + 1;
        if (diceFaces != null && diceFaces.Length > index) return diceFaces[index];
        return null;
    }

    private void FinishRoutine()
    {
        isPlaying = false;
        if (btnRoll != null) btnRoll.interactable = true;
    }

    // ============================================
    // INSTRUKCJA GRY (?)
    // ============================================
    public void OpenInstruction()
    {
        if (isPlaying) return; // Nie otwieraj podczas rzutu!
        if (instructionPanel != null) instructionPanel.SetActive(true);
    }

    public void CloseInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
    }

    // Bezpieczny przycisk powrotny pod Exit na Canvasie
    public void ExitMinigame()
    {
        if (isPlaying) return;
        
        PlayerMovement.canMove = true;
        ClearBet();
        ResetUI();
        CloseInstruction(); // Zabezpieczenie zamknięcia instrukcji przy wyjsciu
        gameObject.SetActive(false); 
    }
}
