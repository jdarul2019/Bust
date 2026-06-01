using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotsGame : MonoBehaviour
{
    [Header("Bębny Maszyny (Images)")]
    public Image slot1;
    public Image slot2;
    public Image slot3;

    [Header("Baza Ikon (Sprites)")]
    [Tooltip("Wpisz tu rozmiar '8' i przeciągnij 8 wyciętych ikon z Assets")]
    public Sprite[] slotSymbols;

    [Header("System Zakładów")]
    public TMP_InputField betInput;
    public Button btnSpin;
    public TextMeshProUGUI resultText;

    [Header("Koszty Gry")]
    public int energyCost = 5;

    [Header("Zaawansowana Logika (Opcjonalnie)")]
    public TextMeshProUGUI jackpotText;
    public TextMeshProUGUI freeSpinsText;
    public TextMeshProUGUI multiplierText;

    [Header("Instruction Panel")]
    public GameObject instructionPanel;

    // Słownik przechowujący pule Jackpot dla każdej sceny z osobna (Rozwiązuje konflikt Kasyno/Bar)
    private static System.Collections.Generic.Dictionary<string, int> sceneJackpots = new System.Collections.Generic.Dictionary<string, int>();

    private int progressiveJackpot
    {
        get
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!sceneJackpots.ContainsKey(sceneName)) sceneJackpots[sceneName] = 0;
            return sceneJackpots[sceneName];
        }
        set
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            sceneJackpots[sceneName] = value;
        }
    }
    
    // Statusy maszyny
    private int currentBet = 0;
    private bool isSpinning = false;
    private int freeSpinsLeft = 0;
    private int freeSpinBetAmount = 0; // Przechowuje zakład, z którym weszliśmy do darmowych rund
    private int spinMultiplier = 1; // Aktualny siłowy mnożnik zakładu i energii

    // Zmienione wagi by delikatnie "oszukiwać" maszynę faworyzując na plus gracza
    // 0: Diament (5%) | 1: Siódemka (10%) | 2: Wiśnia (15%) | 3: Koniczyna WILD (12%)
    // 4: Pomarańcza(10%) | 5: Winogrono(15%) | 6: Podkowa(15%) | 7: Moneta SCATTER (18%)
    private int[] symbolWeights = { 5, 10, 15, 12, 10, 15, 15, 18 };

    void Start()
    {
        if (btnSpin != null) btnSpin.interactable = true;
        
        if (betInput != null)
        {
            betInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            betInput.onValueChanged.AddListener(OnBetInputChanged);
            betInput.text = "0";
        }

        UpdateJackpotUI();
        UpdateFreeSpinsUI();
        UpdateMultiplierUI();
    }

    // ============================================
    // MNOŻNIKI ZAKŁADÓW (x1, x2, x3)
    // ============================================
    public void SetMultiplierX1() { if (isSpinning || freeSpinsLeft > 0) return; spinMultiplier = 1; UpdateMultiplierUI(); }
    public void SetMultiplierX2() { if (isSpinning || freeSpinsLeft > 0) return; spinMultiplier = 2; UpdateMultiplierUI(); }
    public void SetMultiplierX3() { if (isSpinning || freeSpinsLeft > 0) return; spinMultiplier = 3; UpdateMultiplierUI(); }

    private void UpdateMultiplierUI()
    {
        if (multiplierText != null) multiplierText.text = "SPIN MUX:\n<color=white>x" + spinMultiplier + "</color>";
    }

    public void AddToBet(int amount)
    {
        if (isSpinning || freeSpinsLeft > 0) return; // Blokada mian podczas Free Spinów!
        currentBet += amount;
        if (betInput != null) betInput.text = currentBet.ToString();
    }

    public void ClearBet()
    {
        if (isSpinning || freeSpinsLeft > 0) return;
        currentBet = 0;
        if (betInput != null) betInput.text = "0";
    }

    private void OnBetInputChanged(string newValue)
    {
        if (freeSpinsLeft > 0) return;
        if (string.IsNullOrEmpty(newValue)) { currentBet = 0; return; }
        if (int.TryParse(newValue, out int parsedValue)) { currentBet = parsedValue; }
    }

    public void StartSpin()
    {
        if (isSpinning) return;

        if (slotSymbols == null || slotSymbols.Length == 0)
        {
            if (resultText != null) resultText.text = "Error: No icons in machine!";
            return;
        }

        // Oblczenia po uderzeniu pełnym Mnożnikiem 
        int totalEnergyCost = energyCost * spinMultiplier;
        int totalBetCost = currentBet * spinMultiplier;

        // TURA DARMOWA: Nikt nie płaci gotówką ani energią
        if (freeSpinsLeft > 0)
        {
            freeSpinsLeft--;
            UpdateFreeSpinsUI();
            StartCoroutine(SpinRoutine(freeSpinBetAmount));
            return;
        }

        // TURA NORMALNA:
        if (currentBet <= 0)
        {
            if (resultText != null) resultText.text = "Place a bet to spin!";
            return;
        }

        if (EnergyManager.Instance != null && !EnergyManager.Instance.SpendEnergy(totalEnergyCost))
        {
            if (resultText != null) resultText.text = "Not enough energy! \n<size=50%>(Cost: " + totalEnergyCost + " En.)</size>";
            return;
        }

        if (MoneyManager.Instance == null || !MoneyManager.Instance.SpendMoney(totalBetCost))
        {
            if (EnergyManager.Instance != null) EnergyManager.Instance.AddEnergy(totalEnergyCost);
            if (resultText != null) resultText.text = "Not enough money for this bet!";
            return;
        }

        StartCoroutine(SpinRoutine(totalBetCost));
    }

    private IEnumerator SpinRoutine(int betAmount)
    {
        isSpinning = true;
        if (btnSpin != null) btnSpin.interactable = false;
        if (resultText != null) resultText.text = "Spinning...";

        float spinDuration = 2.0f;
        float elapsedTime = 0f;
        float spinSpeed = 0.05f;

        int finalSymbol1 = 0;
        int finalSymbol2 = 0;
        int finalSymbol3 = 0;

        while (elapsedTime < spinDuration)
        {
            if (elapsedTime < spinDuration * 0.5f)
            {
                if(slot1 != null) slot1.sprite = GetRandomSymbol(out finalSymbol1);
                if(slot2 != null) slot2.sprite = GetRandomSymbol(out finalSymbol2);
                if(slot3 != null) slot3.sprite = GetRandomSymbol(out finalSymbol3);
            }
            else if (elapsedTime < spinDuration * 0.75f)
            {
                if(slot2 != null) slot2.sprite = GetRandomSymbol(out finalSymbol2);
                if(slot3 != null) slot3.sprite = GetRandomSymbol(out finalSymbol3);
            }
            else
            {
                if(slot3 != null) slot3.sprite = GetRandomSymbol(out finalSymbol3);
            }

            yield return new WaitForSeconds(spinSpeed);
            elapsedTime += spinSpeed;
        }

        EvaluateGrid(finalSymbol1, finalSymbol2, finalSymbol3, betAmount);

        isSpinning = false;
        if (btnSpin != null) btnSpin.interactable = true;
    }

    // Zaktualizowany silnik losujący korzystający z dociążeń w tabeli WAG
    private Sprite GetRandomSymbol(out int index)
    {
        int totalWeight = 0;
        for (int i = 0; i < symbolWeights.Length; i++) totalWeight += symbolWeights[i];
        
        int randomVal = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        for (int i = 0; i < symbolWeights.Length; i++)
        {
            currentWeight += symbolWeights[i];
            if (randomVal < currentWeight)
            {
                index = i;
                if (i < slotSymbols.Length) return slotSymbols[i];
                break;
            }
        } // Mechanizm obronny - jeśli matematyka wypadnie z szyn, ustatkuje logikę na bezpiecznej domyślnej Wiśni!
        index = 2; return slotSymbols.Length > 2 ? slotSymbols[2] : null;
    }

    // Nowa zaawansowana Ewaluacja ze SCATTERAMI i WILDAMI
    private void EvaluateGrid(int s1, int s2, int s3, int betAmount)
    {
        int winAmount = 0;
        string msg = "";

        // 1. ROZPATRYWANIE SCATTERÓW (Moneta = 7)
        int coinCount = 0;
        if (s1 == 7) coinCount++;
        if (s2 == 7) coinCount++;
        if (s3 == 7) coinCount++;

        if (coinCount >= 2)
        {
            int addedSpins = (coinCount == 3) ? 10 : 3;
            // Zapamiętanie stawki zakładu dla obrotów Free Spinowych, żebyśmy go nie stracili
            if (freeSpinsLeft == 0) freeSpinBetAmount = betAmount; 

            freeSpinsLeft += addedSpins;
            UpdateFreeSpinsUI();
            msg += "<color=#00FFFF>SCATTER! +" + addedSpins + " Free Spins!</color>\n";
        }

        // 2. SZACUNEK Z UKŁADÓW + WILD 
        int w = 3; // Wild (Koniczyna = 3)
        int matchCount = 1;
        int winningSymbol = -1;

        // Metoda pomagająca ignorować wilda jako symbol i parująca z czymś innym
        bool Match(int a, int b) { return (a == b) || (a == w) || (b == w); }
        int ResolveWild(int a, int b) {
            if (a == w && b != w) return b;
            if (b == w && a != w) return a;
            return a;
        }

        // --- Czy Mamy Układ z 3 Oczek?
        bool is3Match = false;
        if (Match(s1, s2) && Match(s2, s3) && Match(s1, s3))
        {
            int resolved = -1;
            if (s1 != w) resolved = s1;
            else if (s2 != w) resolved = s2;
            else if (s3 != w) resolved = s3;
            else resolved = w; // Wylosował ukłąd 3 samych Koniczyn!

            is3Match = true;
            // Upewniamy się, że nie jest to mylne fałszywe potrójne dopasowanie np (1, Wild, 2)
            if (s1 != w && s1 != resolved) is3Match = false;
            if (s2 != w && s2 != resolved) is3Match = false;
            if (s3 != w && s3 != resolved) is3Match = false;

            if (is3Match)
            {
                matchCount = 3;
                winningSymbol = resolved;
            }
        }

        // --- Jeśli nie, to szukamy najsilniejszej Pary (2 oczka)
        if (!is3Match)
        {
            if (Match(s1, s2)) { matchCount = 2; winningSymbol = ResolveWild(s1, s2); }
            if (Match(s2, s3)) 
            {
                int cand = ResolveWild(s2, s3);
                if (matchCount < 2 || GetMultiplier(cand, 2) > GetMultiplier(winningSymbol, 2))
                { matchCount = 2; winningSymbol = cand; }
            }
            if (Match(s1, s3))
            {
                int cand = ResolveWild(s1, s3);
                if (matchCount < 2 || GetMultiplier(cand, 2) > GetMultiplier(winningSymbol, 2))
                { matchCount = 2; winningSymbol = cand; }
            }
        }

        // 3. OBLICZANIE KASY I AKTUALIZACJA JACKPOTA
        if (matchCount > 1)
        {
            float multiplier = GetMultiplier(winningSymbol, matchCount);

            if (multiplier > 0f)
            {
                winAmount = Mathf.CeilToInt(betAmount * multiplier);

                // Warunek wyzwolenia Progresywnego Jackpota (3 Diamenty)
                if (matchCount == 3 && winningSymbol == 0)
                {
                    msg += "<color=yellow>DIAMOND JACKPOT! x" + multiplier.ToString("0.##") + "</color>\nYou win " + winAmount + "$";
                    if (progressiveJackpot > 0)
                    {
                        msg += " + POOL: " + progressiveJackpot + "$!";
                        winAmount += progressiveJackpot;
                        progressiveJackpot = 0; // Opróżnienie kasy maszyny!
                        UpdateJackpotUI();
                    }
                    else { msg += "!"; }
                }
                else
                {
                    if (matchCount == 3) msg += "<color=yellow>MEGA HIT! x" + multiplier.ToString("0.##") + "</color>\nYou win " + winAmount + "$!";
                    else msg += "<color=green>Winner! x" + multiplier.ToString("0.##") + "</color>\nYou win " + winAmount + "$!";
                }
            }
            else
            {
                // Trafił niepłatną parę - dorzuca się do Jackpota puli miasta!
                msg += "<color=red>No payout for this pair...</color>\nYou lose " + betAmount + "$";
                AddToJackpot(betAmount);
            }
        }
        else
        {
            // Traci hajs, dorzuca haracz do puli miasta
            msg += "<color=red>Miss...</color>\nYou lose " + betAmount + "$";
            AddToJackpot(betAmount);
        }

        // Zlecamy Przelew
        if (winAmount > 0 && MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(winAmount);
        }

        if (resultText != null) resultText.text = msg;
    }

    private void AddToJackpot(int lostBet)
    {
        // Pula pochłania równe 50% potknięcia gracza
        int taxForCity = Mathf.CeilToInt(lostBet * 0.5f);
        progressiveJackpot += taxForCity;
        UpdateJackpotUI();
    }

    private void UpdateJackpotUI()
    {
        if (jackpotText != null) jackpotText.text = "JACKPOT POOL:\n<color=yellow>" + progressiveJackpot + " $</color>";
    }

    private void UpdateFreeSpinsUI()
    {
        if (freeSpinsText != null) freeSpinsText.text = "FREE SPINS:\n<color=#00FFFF>" + freeSpinsLeft + "</color>";
        
        // Wyłączenie możliwości psucia dotykania inputów i zerowania w trakcie Darmowych Run
        if (betInput != null) betInput.interactable = (freeSpinsLeft <= 0);
    }

    // Klasyfikator Mnożników (Matematyka bazy)
    private float GetMultiplier(int symbolIndex, int matchCount)
    {
        if (matchCount == 3)
        {
            switch (symbolIndex)
            {
                case 0: return 100f; // Diament
                case 1: return 50f;  // Siódemka
                case 6: return 20f;  // Podkowa
                case 7: return 10f;  // Moneta
                case 3: return 25f;  // 3x WILD (Koniczyna) -> własny bonus! Złoty środek
                case 5: return 2f;   // Winogrono
                case 4: return 1.5f; // Pomarańcza
                case 2: return 1f;   // Wiśnia
            }
        }
        else if (matchCount == 2)
        {
            switch (symbolIndex)
            {
                case 0: return 10f;  // Diament
                case 1: return 5f;   // Siódemka
                case 6: return 2f;   // Podkowa
                case 7: return 1f;   // Moneta
                case 2: return 0.5f; // Wiśnia
                default: return 0f;  // Reszta płaci 0
            }
        }
        return 0f;
    }

    // ============================================
    // INSTRUKCJA GRY (?)
    // ============================================
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
        if (freeSpinsLeft <= 0)
        {
            ClearBet(); 
            spinMultiplier = 1;
            UpdateMultiplierUI();
        }
        if (resultText != null) resultText.text = "Place a bet...";
        CloseInstruction();
        gameObject.SetActive(false); 
    }
}
