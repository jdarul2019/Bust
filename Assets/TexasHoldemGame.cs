using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public enum PokerSuit { Spades = 0, Hearts = 1, Clubs = 2, Diamonds = 3 }

[System.Serializable]
public class PokerCardRankData
{
    [Tooltip("Wartość (np. 11 = Jopek, 12 = Dama, 13 = Król, 14 = As)")]
    public int rankValue;
    [Tooltip("4 grafiki w ustalonej kolejności dla każdego koloru")]
    public Sprite[] suitSprites;
}

public class PokerCardData
{
    public int rank;
    public PokerSuit suit;
    public Sprite sprite;
}

public enum HandRank
{
    HighCard, Pair, TwoPair, ThreeOfAKind, Straight, Flush, FullHouse, FourOfAKind, StraightFlush, RoyalFlush
}

public class HandResult : System.IComparable<HandResult>
{
    public HandRank handRank;
    public List<int> kickers; // posortowane od najważniejszej
    public string handName;

    public int CompareTo(HandResult other)
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

public class TexasHoldemGame : MonoBehaviour
{
    public enum GameState { Betting, PlayerDecision, DealerTurn }

    [Header("Konfiguracja Kart (Assets)")]
    public Sprite cardBackSprite;
    public PokerCardRankData[] allRanks;
    public GameObject cardPrefab;

    [Header("UI - Obstawianie")]
    public TMP_InputField betInput;
    public TextMeshProUGUI totalBetText;
    public Button[] chipButtons;
    public Button btnClearBet;

    [Header("UI - Akcje Gry")]
    public Button btnDeal; // Wpłaca Ante i odpala Flop
    public Button btnCall; // Płaci 2x Ante i ogląda resztę
    public Button btnFold; // Poddaje grę

    [Header("UI - Stół (Trzy rzędy)")]
    public Transform dealerHandContainer; 
    public Transform communityCardsContainer; 
    public Transform playerHandContainer; 
    
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI dealerScoreText;
    public TextMeshProUGUI resultText;

    [Header("Zarządzanie")]
    public GameObject instructionPanel;
    public int energyCost = 5;

    // --- Stan Gry ---
    private GameState currentState = GameState.Betting;
    private int anteBet = 0;
    private int callBet = 0;
    
    private List<PokerCardData> deck = new List<PokerCardData>();
    private List<PokerCardData> playerHand = new List<PokerCardData>();
    private List<PokerCardData> dealerHand = new List<PokerCardData>();
    private List<PokerCardData> communityCards = new List<PokerCardData>();
    
    private List<GameObject> hiddenDealerCardVisuals = new List<GameObject>();

    void Start()
    {
        if (instructionPanel) instructionPanel.SetActive(false);
        GenerateAndShuffleDeck();
        
        currentState = GameState.Betting;
        ClearBet();
        resultText.text = "Place your Ante...";
        playerScoreText.text = "";
        dealerScoreText.text = "";
        UpdateUIButtons();
    }

    // ============================================
    // DECK MANAGEMENT
    // ============================================

    private void GenerateAndShuffleDeck()
    {
        deck.Clear();
        if (allRanks == null) return;
        
        foreach (var rank in allRanks)
        {
            if (rank.suitSprites == null) continue;
            for (int i = 0; i < rank.suitSprites.Length; i++)
            {
                if (rank.suitSprites[i] != null)
                {
                    deck.Add(new PokerCardData { rank = rank.rankValue, suit = (PokerSuit)i, sprite = rank.suitSprites[i] });
                }
            }
        }
        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            PokerCardData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    private PokerCardData DrawCard()
    {
        if (deck.Count < 10)
        {
            GenerateAndShuffleDeck();
        }
        if (deck.Count == 0) return null;
        
        PokerCardData c = deck[0];
        deck.RemoveAt(0);
        return c;
    }

    // ============================================
    // OBSTAWIANIE
    // ============================================

    public void AddToBet(int amount)
    {
        if (currentState != GameState.Betting) return;
        anteBet += amount;
        UpdateBetUI();
    }

    public void ClearBet()
    {
        if (currentState != GameState.Betting) return;
        anteBet = 0;
        callBet = 0;
        UpdateBetUI();
    }

    private void UpdateBetUI()
    {
        if (betInput) betInput.text = anteBet.ToString();
        
        int total = anteBet + callBet;
        if (totalBetText) totalBetText.text = $"Total Bet: ${total}";
            
        if (btnDeal) btnDeal.interactable = (anteBet > 0);
    }

    // ============================================
    // ROZDANIE (DEAL) - ANTE + FLOP
    // ============================================

    public void Deal()
    {
        if (currentState != GameState.Betting || anteBet <= 0) return;

        if (!MoneyManager.Instance.SpendMoney(anteBet))
        {
            resultText.text = "Not enough cash!";
            return;
        }
        if (!EnergyManager.Instance.SpendEnergy(energyCost))
        {
            MoneyManager.Instance.AddMoney(anteBet); // Zwrot gotówki
            resultText.text = "Not enough energy!";
            return;
        }

        currentState = GameState.PlayerDecision;
        
        ClearTable();
        resultText.text = "";
        UpdateUIButtons();

        StartCoroutine(DealInitialCardsCoroutine());
    }

    private void ClearTable()
    {
        foreach (Transform child in playerHandContainer) Destroy(child.gameObject);
        foreach (Transform child in dealerHandContainer) Destroy(child.gameObject);
        foreach (Transform child in communityCardsContainer) Destroy(child.gameObject);
        
        playerHand.Clear();
        dealerHand.Clear();
        communityCards.Clear();
        hiddenDealerCardVisuals.Clear();
        callBet = 0;
        playerScoreText.text = "";
        dealerScoreText.text = "";
    }

    private IEnumerator DealInitialCardsCoroutine()
    {
        // 2 karty gracza
        SpawnCardVisual(DrawCard(), playerHandContainer, playerHand, false);
        yield return new WaitForSeconds(0.2f);
        SpawnCardVisual(DrawCard(), playerHandContainer, playerHand, false);
        yield return new WaitForSeconds(0.2f);

        // 2 karty krupiera (ukryte)
        hiddenDealerCardVisuals.Add(SpawnCardVisual(DrawCard(), dealerHandContainer, dealerHand, true));
        yield return new WaitForSeconds(0.2f);
        hiddenDealerCardVisuals.Add(SpawnCardVisual(DrawCard(), dealerHandContainer, dealerHand, true));
        yield return new WaitForSeconds(0.2f);

        // Flop (3 karty na środek)
        SpawnCardVisual(DrawCard(), communityCardsContainer, communityCards, false);
        yield return new WaitForSeconds(0.2f);
        SpawnCardVisual(DrawCard(), communityCardsContainer, communityCards, false);
        yield return new WaitForSeconds(0.2f);
        SpawnCardVisual(DrawCard(), communityCardsContainer, communityCards, false);
        
        UpdateScoresUI();
        UpdateUIButtons();
    }

    private GameObject SpawnCardVisual(PokerCardData data, Transform parent, List<PokerCardData> handList, bool isHidden)
    {
        if (data != null) handList.Add(data);
        
        GameObject cardObj = Instantiate(cardPrefab, parent);
        Image img = cardObj.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = isHidden ? cardBackSprite : data.sprite;
        }
        return cardObj;
    }

    // ============================================
    // DECYZJA (FOLD / CALL)
    // ============================================

    public void Fold()
    {
        if (currentState != GameState.PlayerDecision) return;
        
        resultText.text = $"Folded. You lost your ${anteBet} Ante.";
        EndGameAndReset();
    }

    public void Call()
    {
        if (currentState != GameState.PlayerDecision) return;

        int requiredCall = anteBet * 2;
        if (!MoneyManager.Instance.SpendMoney(requiredCall))
        {
            resultText.text = "Not enough cash to Call!";
            return;
        }

        callBet = requiredCall;
        UpdateBetUI();
        
        StartCoroutine(DealerTurnCoroutine());
    }

    private IEnumerator DealerTurnCoroutine()
    {
        currentState = GameState.DealerTurn;
        UpdateUIButtons();

        // Turn (4 karta)
        SpawnCardVisual(DrawCard(), communityCardsContainer, communityCards, false);
        yield return new WaitForSeconds(0.5f);
        UpdateScoresUI();

        // River (5 karta)
        SpawnCardVisual(DrawCard(), communityCardsContainer, communityCards, false);
        yield return new WaitForSeconds(0.5f);
        UpdateScoresUI();

        // Odkrycie kart krupiera
        for (int i = 0; i < hiddenDealerCardVisuals.Count; i++)
        {
            Image img = hiddenDealerCardVisuals[i].GetComponent<Image>();
            if (img != null && i < dealerHand.Count)
            {
                img.sprite = dealerHand[i].sprite;
            }
        }
        yield return new WaitForSeconds(0.5f);

        ResolveGame();
    }

    // ============================================
    // ROZSTRZYGNIĘCIE
    // ============================================

    private void ResolveGame()
    {
        HandResult playerResult = GetBestHand(playerHand, communityCards);
        HandResult dealerResult = GetBestHand(dealerHand, communityCards);

        playerScoreText.text = $"You: {playerResult.handName}";
        dealerScoreText.text = $"Dealer: {dealerResult.handName}";

        int comparison = playerResult.CompareTo(dealerResult);
        int totalWagered = anteBet + callBet;

        if (comparison > 0)
        {
            // Player wins! (Z reguły 1:1 na Ante i Call)
            int winnings = totalWagered * 2;
            MoneyManager.Instance.AddMoney(winnings);
            resultText.text = $"You win with {playerResult.handName}!\n<color=green>+${winnings}</color>";
        }
        else if (comparison < 0)
        {
            // Dealer wins
            resultText.text = $"Dealer wins with {dealerResult.handName}!\n<color=red>-${totalWagered}</color>";
        }
        else
        {
            // Tie (Push)
            MoneyManager.Instance.AddMoney(totalWagered);
            resultText.text = $"Push! Tie with {playerResult.handName}.\n<color=yellow>+${totalWagered}</color>";
        }

        EndGameAndReset();
    }

    private void EndGameAndReset()
    {
        currentState = GameState.Betting;
        ClearBet();
        UpdateUIButtons();
    }

    // ============================================
    // UI HELPERS
    // ============================================

    private void UpdateScoresUI()
    {
        if (communityCards.Count > 0)
        {
            HandResult currentBest = GetBestHand(playerHand, communityCards);
            if (playerScoreText) playerScoreText.text = $"You: {currentBest.handName} (Drawing)";
        }
    }

    private void UpdateUIButtons()
    {
        bool isBetting = (currentState == GameState.Betting);
        bool isDecision = (currentState == GameState.PlayerDecision);

        if (btnDeal) btnDeal.gameObject.SetActive(isBetting);
        if (btnClearBet) btnClearBet.interactable = isBetting;
        
        if (btnCall) btnCall.gameObject.SetActive(isDecision);
        if (btnFold) btnFold.gameObject.SetActive(isDecision);
    }

    // ============================================
    // POKER HAND EVALUATOR
    // ============================================

    public HandResult GetBestHand(List<PokerCardData> holeCards, List<PokerCardData> tableCards)
    {
        List<PokerCardData> availableCards = new List<PokerCardData>();
        availableCards.AddRange(holeCards);
        availableCards.AddRange(tableCards);

        // Generowanie wszystkich 5-kartowych kombinacji z 7 dostępnych kart
        List<List<PokerCardData>> combinations = GetCombinations(availableCards, 5);
        
        HandResult bestResult = null;

        foreach (var combo in combinations)
        {
            HandResult res = Evaluate5CardHand(combo);
            if (bestResult == null || res.CompareTo(bestResult) > 0)
            {
                bestResult = res;
            }
        }

        return bestResult;
    }

    private HandResult Evaluate5CardHand(List<PokerCardData> hand)
    {
        // Sortuj malejąco po randze
        hand.Sort((a, b) => b.rank.CompareTo(a.rank));

        bool isFlush = hand.All(c => c.suit == hand[0].suit);
        
        bool isStraight = true;
        for (int i = 0; i < 4; i++)
        {
            if (hand[i].rank - 1 != hand[i + 1].rank)
            {
                isStraight = false;
                break;
            }
        }

        // Specjalny przypadek strita: As do 5 (As, 5, 4, 3, 2)
        bool isLowStraight = false;
        if (!isStraight && hand[0].rank == 14 && hand[1].rank == 5 && hand[2].rank == 4 && hand[3].rank == 3 && hand[4].rank == 2)
        {
            isStraight = true;
            isLowStraight = true;
        }

        // Zliczanie częstotliwości
        var groups = hand.GroupBy(c => c.rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        
        HandResult res = new HandResult();
        res.kickers = new List<int>();

        if (isStraight && isFlush)
        {
            res.handRank = hand[0].rank == 14 && !isLowStraight ? HandRank.RoyalFlush : HandRank.StraightFlush;
            res.handName = res.handRank == HandRank.RoyalFlush ? "Royal Flush" : "Straight Flush";
            res.kickers.Add(isLowStraight ? 5 : hand[0].rank);
            return res;
        }

        if (groups[0].Count() == 4)
        {
            res.handRank = HandRank.FourOfAKind;
            res.handName = "Four of a Kind";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            return res;
        }

        if (groups[0].Count() == 3 && groups[1].Count() == 2)
        {
            res.handRank = HandRank.FullHouse;
            res.handName = "Full House";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            return res;
        }

        if (isFlush)
        {
            res.handRank = HandRank.Flush;
            res.handName = "Flush";
            res.kickers.AddRange(hand.Select(c => c.rank));
            return res;
        }

        if (isStraight)
        {
            res.handRank = HandRank.Straight;
            res.handName = "Straight";
            res.kickers.Add(isLowStraight ? 5 : hand[0].rank);
            return res;
        }

        if (groups[0].Count() == 3)
        {
            res.handRank = HandRank.ThreeOfAKind;
            res.handName = "Three of a Kind";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            res.kickers.Add(groups[2].Key);
            return res;
        }

        if (groups[0].Count() == 2 && groups[1].Count() == 2)
        {
            res.handRank = HandRank.TwoPair;
            res.handName = "Two Pairs";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            res.kickers.Add(groups[2].Key);
            return res;
        }

        if (groups[0].Count() == 2)
        {
            res.handRank = HandRank.Pair;
            res.handName = "Pair";
            res.kickers.Add(groups[0].Key);
            res.kickers.Add(groups[1].Key);
            res.kickers.Add(groups[2].Key);
            res.kickers.Add(groups[3].Key);
            return res;
        }

        res.handRank = HandRank.HighCard;
        res.handName = "High Card";
        res.kickers.AddRange(hand.Select(c => c.rank));
        return res;
    }

    // Generator Kombinacji bez powtórzeń (C(n,k))
    private List<List<PokerCardData>> GetCombinations(List<PokerCardData> list, int k)
    {
        List<List<PokerCardData>> result = new List<List<PokerCardData>>();
        if (k == 0)
        {
            result.Add(new List<PokerCardData>());
            return result;
        }
        if (list.Count == 0) return result;

        PokerCardData head = list[0];
        List<PokerCardData> tail = new List<PokerCardData>(list.GetRange(1, list.Count - 1));

        List<List<PokerCardData>> withHead = GetCombinations(tail, k - 1);
        foreach (var combo in withHead)
        {
            combo.Insert(0, head);
            result.Add(combo);
        }

        List<List<PokerCardData>> withoutHead = GetCombinations(tail, k);
        result.AddRange(withoutHead);

        return result;
    }

    // ============================================
    // MENU ACTIONS
    // ============================================

    public void OpenInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(true);
    }

    public void CloseInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
    }

    public void ExitMinigame()
    {
        if (currentState == GameState.PlayerDecision || currentState == GameState.DealerTurn) return;
        PlayerMovement.canMove = true;
        CloseInstruction();
        gameObject.SetActive(false);
    }
}
