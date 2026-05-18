using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RouletteGame : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════
    // INSPECTOR
    // ══════════════════════════════════════════════════════════════

    [Header("Result and Bets")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI totalBetText;   // "Total Bet: $60"
    public TMP_InputField  betInput;       // shows the selected chip value

    [Header("Actions")]
    public Button btnSpin;   // placeBet
    public Button btnReset;  // ResetBet

    [Header("Instrukcja")]
    public Button     btnHelp;
    public GameObject instructionPanel;

    [Header("Animacja Koła")]
    [Tooltip("RectTransform obrazka koła ruletki")]
    public RectTransform wheelTransform;
    [Tooltip("Pusty obiekt na środku koła, którego dzieckiem jest kulka")]
    public RectTransform ballPivotTransform;
    public float spinDuration = 3f;

    [Header("Kalibracja Koła (dopasowanie wizualne)")]
    [Tooltip("Jeśli kulka ląduje np. 2 pola obok, zmień ten kąt (jeden numerek to ok. 9.73 stopnia).")]
    public float zeroOffsetAngle = 0f;
    [Tooltip("Czy liczby na obrazku idą zgodnie ze wskazówkami zegara od zera?")]
    public bool numbersAreClockwise = true;

    [Header("Energy Cost")]
    public int energyCost = 5;

    [Header("Ekrany (Fazy Gry)")]
    [Tooltip("Panel grupujący wszystkie przyciski do obstawiania (liczby, żetony itp.)")]
    public GameObject bettingBoardPanel;
    [Tooltip("Panel grupujący koło ruletki, które pojawia się w trakcie losowania")]
    public GameObject rouletteWheelPanel;
    [Tooltip("Przycisk, który pojawia się po wylosowaniu wyniku, aby gracz mógł kliknąć 'Kontynuuj'")]
    public Button btnContinue;

    [Header("Debug (Narzędzie testowe)")]
    [Tooltip("Zaznacz to w trybie Play, aby zablokować ruletkę i testować przesunięcie kątowe na żywo.")]
    public bool testAlignment = false;
    [Range(0, 36)] 
    public int testNumber = 0;

    // ══════════════════════════════════════════════════════════════
    // PRIVATE STATE
    // ══════════════════════════════════════════════════════════════

    private int  selectedChip = 0;
    private bool isSpinning   = false;

    // zoneKey → total bet amount on this zone
    private readonly Dictionary<string, int> bets
        = new Dictionary<string, int>();

    // Registered zones registry (filled by BettingZone.cs on Start)
    private readonly List<BettingZone> registeredZones = new List<BettingZone>();

    // ── Number definitions ───────────────────────────────────────────
    
    // Klasyczna europejska kolejność liczb na kole (Zmieniona na sekwencyjną, ponieważ grafika ma ułożone liczby pokolei!)
    private readonly int[] wheelNumbers = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36 };

    // Red numbers in this specific game board (All odd numbers are red)
    private static readonly HashSet<int> RED = new HashSet<int>
        { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35 };

    // Rows (columns on the board) — "2/1" on the right side
    private static readonly HashSet<int> ROW1 = new HashSet<int>
        { 3,6,9,12,15,18,21,24,27,30,33,36 };  // top row
    private static readonly HashSet<int> ROW2 = new HashSet<int>
        { 2,5,8,11,14,17,20,23,26,29,32,35 };  // middle row
    private static readonly HashSet<int> ROW3 = new HashSet<int>
        { 1,4,7,10,13,16,19,22,25,28,31,34 };  // bottom row

    // ══════════════════════════════════════════════════════════════
    // INIT & DEBUG
    // ══════════════════════════════════════════════════════════════

    void Start()
    {
        if (instructionPanel) instructionPanel.SetActive(false);

        if (betInput != null)
        {
            betInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            betInput.onValueChanged.AddListener(OnBetInputChanged);
            betInput.text = selectedChip.ToString();
        }

        if (btnContinue != null)
        {
            btnContinue.gameObject.SetActive(false);
            btnContinue.onClick.AddListener(OnContinueClicked);
        }

        // Zawsze zaczynamy od fazy obstawiania
        if (bettingBoardPanel) bettingBoardPanel.SetActive(true);
        if (rouletteWheelPanel) rouletteWheelPanel.SetActive(false);

        RefreshUI();
    }

    void Update()
    {
        if (testAlignment && !isSpinning && wheelTransform != null && ballPivotTransform != null)
        {
            // Resetujemy rotację koła do zera, by łatwiej testować
            wheelTransform.rotation = Quaternion.Euler(0, 0, 0);

            // Obliczamy pozycję wybranej liczby testowej
            int resultIndex = System.Array.IndexOf(wheelNumbers, testNumber);
            float sliceAngle = 360f / 37f;
            float numberAngle = resultIndex * sliceAngle;
            if (numbersAreClockwise) numberAngle = -numberAngle;

            // Na żywo nakładamy Zero Offset Angle, by gracz mógł skalibrować suwakiem obrazek
            float exactBallStop = (0f + numberAngle + zeroOffsetAngle) % 360f;
            if (exactBallStop < 0) exactBallStop += 360f;

            ballPivotTransform.rotation = Quaternion.Euler(0, 0, exactBallStop);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // CHIPS — Same pattern as CoinflipGame / SlotsGame
    // Link: 5Button/10Button/20Button/50Button → On Click → AddToBet(amount)
    // ══════════════════════════════════════════════════════════════

    public void AddToBet(int amount)
    {
        if (isSpinning) return;
        selectedChip += amount;
        if (betInput) betInput.text = selectedChip.ToString();
    }

    public void ClearBet()
    {
        if (isSpinning) return;
        selectedChip = 0;
        if (betInput) betInput.text = "0";
    }

    private void OnBetInputChanged(string val)
    {
        if (string.IsNullOrEmpty(val))
        {
            selectedChip = 0;
            return;
        }

        if (int.TryParse(val, out int parsed) && parsed >= 0)
            selectedChip = parsed;
    }

    // ══════════════════════════════════════════════════════════════
    // ZONE REGISTRATION (called by BettingZone on Start)
    // ══════════════════════════════════════════════════════════════

    public void RegisterZone(BettingZone zone)
    {
        registeredZones.Add(zone);
    }

    // ══════════════════════════════════════════════════════════════
    // BETTING
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by BettingZone after clicking a zone on the board.
    /// Adds the selected chip value to this zone.
    /// </summary>
    public void PlaceBet(string zoneKey)
    {
        if (isSpinning || selectedChip <= 0) return;
        if (!bets.ContainsKey(zoneKey)) bets[zoneKey] = 0;
        bets[zoneKey] += selectedChip;
        RefreshUI();
    }

    public void ResetAllBets()
    {
        if (isSpinning) return;
        bets.Clear();
        RefreshUI();
    }

    private void RefreshUI()
    {
        // Total bet
        int total = 0;
        foreach (var b in bets) total += b.Value;

        if (totalBetText) totalBetText.text = $"Total Bet: ${total}";
        if (btnSpin)  btnSpin.interactable  = total > 0 && !isSpinning;
        if (btnReset) btnReset.interactable = total > 0 && !isSpinning;

        // Refresh labels and chip images on the board zones
        foreach (var zone in registeredZones)
        {
            int amt = bets.ContainsKey(zone.zoneKey) ? bets[zone.zoneKey] : 0;
            zone.UpdateUI(amt);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // SPIN
    // ══════════════════════════════════════════════════════════════

    public void Spin()
    {
        int total = 0;
        foreach (var b in bets) total += b.Value;
        if (total <= 0 || isSpinning) return;

        if (!MoneyManager.Instance.SpendMoney(total))
        {
            resultText.text = "Not enough cash!";
            return;
        }
        if (!EnergyManager.Instance.SpendEnergy(energyCost))
        {
            MoneyManager.Instance.AddMoney(total); // refund
            resultText.text = "Not enough energy!";
            return;
        }

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        
        // Zmiana Ekranu: Ukryj matę, Pokaż tylko koło ruletki
        if (bettingBoardPanel) bettingBoardPanel.SetActive(false);
        if (rouletteWheelPanel) rouletteWheelPanel.SetActive(true);
        if (btnContinue) btnContinue.gameObject.SetActive(false);

        RefreshUI();
        resultText.text = "";

        // LOSUJEMY WYNIK OD RAZU, ABY WIEDZIEĆ GDZIE ZATRZYMAĆ KULKĘ
        int result = Random.Range(0, 37); // 0–36

        // Animacja kręcenia kołem i kulką
        if (wheelTransform != null && ballPivotTransform != null)
        {
            float elapsed = 0f;
            
            float startWheelAngle = wheelTransform.eulerAngles.z;
            float startBallAngle  = ballPivotTransform.eulerAngles.z;

            // Kółko kręci się w prawo (ujemne kąty Z w Unity), dajemy 3 obroty + losowy kąt
            float randomStopAngle = Random.Range(0f, 360f);
            float targetWheelAngle = startWheelAngle - (360f * 3f) - randomStopAngle;

            // SZUKAMY DOCELOWEGO MIEJSCA NA KULKĘ
            int resultIndex = System.Array.IndexOf(wheelNumbers, result);
            float sliceAngle = 360f / 37f;
            
            // Kąt względem zera na grafice
            float numberAngle = resultIndex * sliceAngle;
            if (numbersAreClockwise) numberAngle = -numberAngle; // Obrót w lewo to minus
            
            // Dodajemy mały, losowy offset (+/- 3 stopnie), aby kulka zachowywała się naturalnie 
            // i nie trafiała zawsze mechanicznie w sam, perfekcyjny środek przegródki.
            float randomPocketOffset = Random.Range(-3f, 3f);

            // Gdzie ma stanąć kulka? (Kąt koła + pozycja liczby + kalibracja z Inspektora + losowość)
            float exactBallStop = (targetWheelAngle + numberAngle + zeroOffsetAngle + randomPocketOffset) % 360f;
            if (exactBallStop < 0) exactBallStop += 360f;

            // Kulka ma się kręcić w lewo (dodatnie kąty Z), zróbmy ~4 pełne obroty
            float targetBallAngle = exactBallStop;
            while (targetBallAngle < startBallAngle + (360f * 4f)) 
            {
                targetBallAngle += 360f;
            }

            while (elapsed < spinDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / spinDuration;
                
                // Ease Out Cubic - płynne zwalnianie z wzoru (1 - (1-t)^3)
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);

                float currentWheelZ = Mathf.Lerp(startWheelAngle, targetWheelAngle, easeOut);
                float currentBallZ  = Mathf.Lerp(startBallAngle, targetBallAngle, easeOut);

                wheelTransform.rotation = Quaternion.Euler(0, 0, currentWheelZ);
                ballPivotTransform.rotation = Quaternion.Euler(0, 0, currentBallZ);

                yield return null; // czekaj do następnej klatki
            }
            
            // Wyrównanie dla perfekcyjnej precyzji po skończeniu pętli
            wheelTransform.rotation = Quaternion.Euler(0, 0, targetWheelAngle);
            ballPivotTransform.rotation = Quaternion.Euler(0, 0, targetBallAngle);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        string color = result == 0        ? "🟢"
                     : RED.Contains(result) ? "🔴" : "⚫";

        string breakdown;
        int winnings = CalculateWinnings(result, out breakdown);
        if (winnings > 0) MoneyManager.Instance.AddMoney(winnings);

        if (winnings > 0)
        {
            resultText.text = $"<b>Winning Bets:</b>\n{breakdown}\n\n✅ <b>Total Won: ${winnings}</b>!";
        }
        else
        {
            resultText.text = $"❌ Better luck next time...";
        }

        // Jeśli przypisano przycisk Continue to go pokazujemy, w przeciwnym razie resetujemy grę autoamtycznie
        if (btnContinue != null)
        {
            btnContinue.gameObject.SetActive(true);
        }
        else
        {
            bets.Clear();
            isSpinning = false;
            RefreshUI();
        }
    }

    public void OnContinueClicked()
    {
        // Zmiana Ekranu: Wracamy do obstawiania
        if (bettingBoardPanel) bettingBoardPanel.SetActive(true);
        if (rouletteWheelPanel) rouletteWheelPanel.SetActive(false);
        if (btnContinue) btnContinue.gameObject.SetActive(false);

        bets.Clear();
        isSpinning = false;
        resultText.text = "";
        RefreshUI();
    }

    // ══════════════════════════════════════════════════════════════
    // BET PAYOUTS
    // ══════════════════════════════════════════════════════════════

    private int CalculateWinnings(int result, out string breakdown)
    {
        int total = 0;
        List<string> details = new List<string>();
        foreach (var bet in bets)
        {
            if (BetHits(bet.Key, result))
            {
                int wonAmount = bet.Value * GetMultiplier(bet.Key);
                total += wonAmount;
                details.Add($"• {GetZoneFriendlyName(bet.Key)}: <color=green>+${wonAmount}</color>");
            }
        }
        breakdown = string.Join("\n", details);
        return total;
    }

    private string GetZoneFriendlyName(string zone)
    {
        if (zone.StartsWith("num_")) return $"Number {zone.Substring(4)}";
        return zone switch
        {
            "red" => "Red",
            "black" => "Black",
            "even" => "Even",
            "odd" => "Odd",
            "1to18" => "1 to 18",
            "19to36" => "19 to 36",
            "1st12" => "1st 12",
            "2nd12" => "2nd 12",
            "3rd12" => "3rd 12",
            "row1" => "Top Row (2/1)",
            "row2" => "Middle Row (2/1)",
            "row3" => "Bottom Row (2/1)",
            _ => zone
        };
    }

    private bool BetHits(string zone, int result)
    {
        if (zone.StartsWith("num_"))
        {
            if (int.TryParse(zone.Substring(4), out int num))
                return result == num;
            return false;
        }

        return zone switch
        {
            "red"    => result != 0 && RED.Contains(result),
            "black"  => result != 0 && !RED.Contains(result),
            "even"   => result != 0 && result % 2 == 0,
            "odd"    => result % 2 == 1,
            "1to18"  => result >= 1  && result <= 18,
            "19to36" => result >= 19 && result <= 36,
            "1st12"  => result >= 1  && result <= 12,
            "2nd12"  => result >= 13 && result <= 24,
            "3rd12"  => result >= 25 && result <= 36,
            "row1"   => ROW1.Contains(result),
            "row2"   => ROW2.Contains(result),
            "row3"   => ROW3.Contains(result),
            _ => false
        };
    }

    private int GetMultiplier(string zone)
    {
        if (zone.StartsWith("num_")) return 35; // Wypłata za pojedynczy numer to zazwyczaj 35:1 (x35)

        return zone switch
        {
            "1st12" or "2nd12" or "3rd12" or "row1" or "row2" or "row3" => 3,
            _ => 2  // red/black/even/odd/1to18/19to36
        };
    }

    // ══════════════════════════════════════════════════════════════
    // UI HELPERS
    // ══════════════════════════════════════════════════════════════

    public void OpenInstruction()
    {
        if (isSpinning) return;
        if (instructionPanel != null) instructionPanel.SetActive(true);
    }

    public void CloseInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
    }

    public void ExitMinigame()
    {
        if (isSpinning) return;
        
        PlayerMovement.canMove = true;
        ResetAllBets();
        ClearBet();
        if (resultText != null) resultText.text = "";
        CloseInstruction();
        gameObject.SetActive(false);
    }
}
