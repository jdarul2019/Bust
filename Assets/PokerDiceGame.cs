using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public enum PokerDiceHandRank
{
    HighDie, Pair, TwoPairs, ThreeOfAKind, SmallStraight, LargeStraight, FullHouse, FourOfAKind, FiveOfAKind
}

public class PokerDiceHandResult : System.IComparable<PokerDiceHandResult>
{
    public PokerDiceHandRank handRank;
    public List<int> kickers;
    public string handName;

    public int CompareTo(PokerDiceHandResult other)
    {
        if (this.handRank != other.handRank)
            return this.handRank.CompareTo(other.handRank);

        for (int i = 0; i < Mathf.Min(this.kickers.Count, other.kickers.Count); i++)
        {
            if (this.kickers[i] != other.kickers[i])
                return this.kickers[i].CompareTo(other.kickers[i]);
        }
        return 0; // Remis
    }
}

public class PokerDiceGame : MonoBehaviour
{
    [Header("Taca rzutna (Wspólny Canvas Area)")]
    public RectTransform throwArea;
    [Tooltip("Minimalny odstęp w pikselach zapobiegający wejściu kości na kość")]
    public float safeDistance = 80f; 

    [Header("Kości Krupiera (Góra)")]
    [Tooltip("Podepnij 5 obrazków Image dla krupiera")]
    public Image[] dealerDice;
    public TextMeshProUGUI dealerScoreText;

    [Header("Kości Gracza (Dół)")]
    [Tooltip("Podepnij 5 obrazków Image dla gracza")]
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

    // Trzymane w pamięci bezpieczne wektory wygenerowane na stół (dla 10 kości)
    private Vector2[] safePositionsCache = new Vector2[10];

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

        StartCoroutine(PlayDiceRoutine(currentBet));
    }

    private void GenerateSafePositions()
    {
        if (throwArea == null) 
        {
            for (int i = 0; i < 10; i++) safePositionsCache[i] = Vector2.zero;
            return;
        }

        float w = throwArea.rect.width / 2f;
        float h = throwArea.rect.height / 2f;
        float padding = 40f; 

        for (int i = 0; i < 10; i++)
        {
            bool foundSpot = false;
            for (int attempts = 0; attempts < 150; attempts++)
            {
                Vector2 candidate = new Vector2(
                    Random.Range(-w + padding, w - padding),
                    Random.Range(-h + padding, h - padding)
                );

                bool isSafe = true;
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

            if (!foundSpot)
            {
                safePositionsCache[i] = new Vector2(Random.Range(-60f, 60f), Random.Range(-60f, 60f));
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

        foreach (var d in dealerDice) if (d != null) d.gameObject.SetActive(true);

        int[] dValues = new int[5];

        while (elapsed < dealerSpinTime)
        {
            for(int i = 0; i < Mathf.Min(dealerDice.Length, 5); i++)
            {
                if (dealerDice[i] != null) ShuffleDie(dealerDice[i], safePositionsCache[i], out dValues[i]);
            }
            
            yield return new WaitForSeconds(tickSpeed);
            elapsed += tickSpeed;
        }

        for(int i = 0; i < Mathf.Min(dealerDice.Length, 5); i++)
        {
            if (dealerDice[i] != null) SnapDiceStable(dealerDice[i], safePositionsCache[i]);
        }

        List<int> validDealerValues = new List<int>();
        for(int i=0; i<Mathf.Min(dealerDice.Length, 5); i++) validDealerValues.Add(dValues[i]);

        PokerDiceHandResult dealerResult = EvaluateDiceHand(validDealerValues);
        if (dealerScoreText != null) dealerScoreText.text = "Dealer: " + dealerResult.handName;

        // ============================================
        // FAZA 2: RZUT GRACZA
        // ============================================
        if (resultText != null) resultText.text = "Now it's your turn!";
        yield return new WaitForSeconds(0.5f);

        foreach (var d in playerDice) if (d != null) d.gameObject.SetActive(true);

        float playerSpinTime = 1.5f;
        elapsed = 0f;
        int[] pValues = new int[5];

        while (elapsed < playerSpinTime)
        {
            for(int i = 0; i < Mathf.Min(playerDice.Length, 5); i++)
            {
                if (playerDice[i] != null) ShuffleDie(playerDice[i], safePositionsCache[5 + i], out pValues[i]);
            }
            
            yield return new WaitForSeconds(tickSpeed);
            elapsed += tickSpeed;
        }

        for(int i = 0; i < Mathf.Min(playerDice.Length, 5); i++)
        {
            if (playerDice[i] != null) SnapDiceStable(playerDice[i], safePositionsCache[5 + i]);
        }

        List<int> validPlayerValues = new List<int>();
        for(int i=0; i<Mathf.Min(playerDice.Length, 5); i++) validPlayerValues.Add(pValues[i]);

        PokerDiceHandResult playerResult = EvaluateDiceHand(validPlayerValues);
        if (playerScoreText != null) playerScoreText.text = "You: " + playerResult.handName;

        // ============================================
        // FAZA 3: SĄDZIA (Wyniki)
        // ============================================
        EvaluateWinner(dealerResult, playerResult, betAmount);
        FinishRoutine();
    }

    private void ShuffleDie(Image dieImage, Vector2 targetStableBase, out int valueOut)
    {
        dieImage.sprite = GetRandomDice(out valueOut);

        Vector2 randomRumbleOffset = new Vector2(Random.Range(-15f, 15f), Random.Range(-15f, 15f));
        dieImage.rectTransform.anchoredPosition = targetStableBase + randomRumbleOffset;
        dieImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(-180f, 180f));
    }

    private void SnapDiceStable(Image dieImage, Vector2 stableBase)
    {
        if (dieImage == null) return;
        dieImage.rectTransform.anchoredPosition = stableBase;
        dieImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(-40f, 40f));
    }

    // ============================================
    // DICE POKER EVALUATOR
    // ============================================
    private PokerDiceHandResult EvaluateDiceHand(List<int> dice)
    {
        if (dice.Count < 5) return new PokerDiceHandResult { handRank = PokerDiceHandRank.HighDie, handName = "Invalid Hand", kickers = new List<int>() };

        // Sortowanie malejące
        dice.Sort((a, b) => b.CompareTo(a));
        
        // Zliczanie powtórzeń (grupowanie by łatwo znaleźć Karete, Trójke itd)
        var groups = dice.GroupBy(d => d).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        
        PokerDiceHandResult res = new PokerDiceHandResult();
        res.kickers = new List<int>();

        bool isLargeStraight = (dice[0] == 6 && dice[1] == 5 && dice[2] == 4 && dice[3] == 3 && dice[4] == 2);
        bool isSmallStraight = (dice[0] == 5 && dice[1] == 4 && dice[2] == 3 && dice[3] == 2 && dice[4] == 1);

        if (groups[0].Count() == 5)
        {
            res.handRank = PokerDiceHandRank.FiveOfAKind;
            res.handName = "Five of a Kind";
            res.kickers.Add(groups[0].Key);
            return res;
        }
        if (groups[0].Count() == 4)
        {
            res.handRank = PokerDiceHandRank.FourOfAKind;
            res.handName = "Four of a Kind";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            return res;
        }
        if (groups[0].Count() == 3 && groups[1].Count() == 2)
        {
            res.handRank = PokerDiceHandRank.FullHouse;
            res.handName = "Full House";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            return res;
        }
        if (isLargeStraight)
        {
            res.handRank = PokerDiceHandRank.LargeStraight;
            res.handName = "Large Straight";
            res.kickers.AddRange(dice);
            return res;
        }
        if (isSmallStraight)
        {
            res.handRank = PokerDiceHandRank.SmallStraight;
            res.handName = "Small Straight";
            res.kickers.AddRange(dice);
            return res;
        }
        if (groups[0].Count() == 3)
        {
            res.handRank = PokerDiceHandRank.ThreeOfAKind;
            res.handName = "Three of a Kind";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            res.kickers.Add(groups[2].Key);
            return res;
        }
        if (groups[0].Count() == 2 && groups[1].Count() == 2)
        {
            res.handRank = PokerDiceHandRank.TwoPairs;
            res.handName = "Two Pairs";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            res.kickers.Add(groups[2].Key);
            return res;
        }
        if (groups[0].Count() == 2)
        {
            res.handRank = PokerDiceHandRank.Pair;
            res.handName = "Pair";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            res.kickers.Add(groups[2].Key);
            res.kickers.Add(groups[3].Key);
            return res;
        }

        res.handRank = PokerDiceHandRank.HighDie;
        res.handName = "High Die";
        res.kickers.AddRange(dice);
        return res;
    }

    private void EvaluateWinner(PokerDiceHandResult dealerResult, PokerDiceHandResult playerResult, int betAmount)
    {
        int winAmount = 0;
        string msg = "";

        int comparison = playerResult.CompareTo(dealerResult);

        if (comparison > 0)
        {   
            if (playerResult.handRank == PokerDiceHandRank.FiveOfAKind)
            {
                winAmount = betAmount * 10;
                msg = $"<color=yellow>JACKPOT!</color>\nYou win with {playerResult.handName} (x10)!\n<color=green>+${winAmount}</color>";
            }
            else
            {
                winAmount = betAmount * 2;
                msg = $"<color=green>Victory!</color>\nYou win with {playerResult.handName} (x2)!\n<color=green>+${winAmount}</color>";
            }
        }
        else if (comparison == 0)
        {
            winAmount = betAmount; 
            msg = $"<color=white>PUSH!</color>\nTie with {playerResult.handName}.\n<color=yellow>+${winAmount}</color>";
        }
        else
        {
            msg = $"<color=red>DEFEAT</color>\nDealer wins with {dealerResult.handName}!\n<color=red>-${betAmount}</color>";
        }

        if (winAmount > 0 && MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(winAmount);
        }

        if (resultText != null) resultText.text = msg;
    }

    private Sprite GetRandomDice(out int value)
    {
        int index = Random.Range(0, 6);
        value = index + 1;
        if (diceFaces != null && diceFaces.Length > index) return diceFaces[index];
        return null;
    }

    private void FinishRoutine()
    {
        isPlaying = false;
        if (btnRoll != null) btnRoll.interactable = true;
    }

    public void OpenInstruction()
    {
        if (isPlaying) return;
        if (instructionPanel != null) instructionPanel.SetActive(true);
    }

    public void CloseInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
    }

    public void ExitMinigame()
    {
        if (isPlaying) return;
        PlayerMovement.canMove = true;
        ClearBet();
        ResetUI();
        CloseInstruction();
        gameObject.SetActive(false); 
    }
}
