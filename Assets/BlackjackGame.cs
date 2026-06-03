using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CardRankData
{
    public int pointValue;
    public Sprite[] suitSprites;
}

public class CardData
{
    public int value;
    public Sprite sprite;
}

public class BlackjackGame : MonoBehaviour
{
    public enum GameState { Betting, PlayerTurn, DealerTurn }

    [Header("Konfiguracja Kart (Assets)")]
    public Sprite cardBackSprite;
    public CardRankData[] allRanks;
    public GameObject cardPrefab;

    [Header("UI - Obstawianie")]
    public TMP_InputField betInput;
    public TextMeshProUGUI totalBetText;
    public Button[] chipButtons;
    public Button btnClearBet;
    
    [Header("UI - Akcje Gry")]
    public Button btnDeal;
    public Button btnHit;
    public Button btnStand;
    public Button btnDoubleDown;
    public Button btnSplit;

    [Header("UI - Stół (Jeden Widok)")]
    public Transform playerHandContainer; 
    public Transform playerSplitHandContainer; // Kontener dla drugiej ręki
    public Transform dealerHandContainer; 
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI playerSplitScoreText; // Tekst wyniku dla drugiej ręki
    public TextMeshProUGUI dealerScoreText;
    public TextMeshProUGUI resultText;

    [Header("Zarządzanie")]
    public GameObject instructionPanel;
    public int energyCost = 5;

    // --- Stan Gry ---
    private GameState currentState = GameState.Betting;
    private int currentBet = 0;
    private int splitBet = 0;
    
    private List<CardData> deck = new List<CardData>();
    private List<CardData> playerHand = new List<CardData>();
    private List<CardData> playerSplitHand = new List<CardData>();
    private List<CardData> dealerHand = new List<CardData>();
    
    private GameObject hiddenDealerCardVisual;
    private bool hasSplit = false;
    private bool isPlayingSplitHand = false;

    void Start()
    {
        if (instructionPanel) instructionPanel.SetActive(false);
        GenerateAndShuffleDeck();
        
        currentState = GameState.Betting;
        ClearBet();
        resultText.text = "Place your bet...";
        playerScoreText.text = "";
        dealerScoreText.text = "";
        if (playerSplitScoreText) playerSplitScoreText.text = "";
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
            foreach (var spr in rank.suitSprites)
            {
                if (spr != null)
                {
                    deck.Add(new CardData { value = rank.pointValue, sprite = spr });
                }
            }
        }
        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    private CardData DrawCard()
    {
        if (deck.Count < 15)
        {
            GenerateAndShuffleDeck();
        }
        if (deck.Count == 0) return null;
        
        CardData c = deck[0];
        deck.RemoveAt(0);
        return c;
    }

    // ============================================
    // LOGIKA PUNKTÓW
    // ============================================

    private int CalculateScore(List<CardData> hand)
    {
        int score = 0;
        int aces = 0;
        foreach (var card in hand)
        {
            score += card.value;
            if (card.value == 11) aces++;
        }
        
        while (score > 21 && aces > 0)
        {
            score -= 10;
            aces--;
        }
        return score;
    }

    // ============================================
    // OBSTAWIANIE
    // ============================================

    public void AddToBet(int amount)
    {
        if (currentState != GameState.Betting) return;
        currentBet += amount;
        UpdateBetUI();
    }

    public void ClearBet()
    {
        if (currentState != GameState.Betting) return;
        currentBet = 0;
        splitBet = 0;
        hasSplit = false;
        isPlayingSplitHand = false;
        UpdateBetUI();
    }

    private void UpdateBetUI()
    {
        if (betInput) betInput.text = currentBet.ToString();
        
        if (hasSplit)
            if (totalBetText) totalBetText.text = $"Total Bet: ${currentBet + splitBet}";
        else
            if (totalBetText) totalBetText.text = $"Total Bet: ${currentBet}";
            
        if (btnDeal) btnDeal.interactable = (currentBet > 0);
    }

    // ============================================
    // ROZDANIE (DEAL)
    // ============================================

    public void Deal()
    {
        if (currentState != GameState.Betting || currentBet <= 0) return;

        if (!MoneyManager.Instance.SpendMoney(currentBet))
        {
            resultText.text = "Not enough cash!";
            return;
        }
        if (!EnergyManager.Instance.SpendEnergy(energyCost))
        {
            MoneyManager.Instance.AddMoney(currentBet); // Zwrot gotówki
            resultText.text = "Not enough energy!";
            return;
        }

        currentState = GameState.PlayerTurn;
        
        foreach (Transform child in playerHandContainer) Destroy(child.gameObject);
        if (playerSplitHandContainer) foreach (Transform child in playerSplitHandContainer) Destroy(child.gameObject);
        foreach (Transform child in dealerHandContainer) Destroy(child.gameObject);
        
        playerHand.Clear();
        playerSplitHand.Clear();
        dealerHand.Clear();
        hasSplit = false;
        isPlayingSplitHand = false;
        splitBet = 0;
        
        resultText.text = "";
        UpdateUIButtons();

        StartCoroutine(DealInitialCardsCoroutine());
    }

    private IEnumerator DealInitialCardsCoroutine()
    {
        SpawnCardVisual(DrawCard(), playerHandContainer, playerHand, false);
        yield return new WaitForSeconds(0.3f);
        SpawnCardVisual(DrawCard(), dealerHandContainer, dealerHand, false);
        yield return new WaitForSeconds(0.3f);
        SpawnCardVisual(DrawCard(), playerHandContainer, playerHand, false);
        yield return new WaitForSeconds(0.3f);
        
        CardData dealerHidden = DrawCard();
        hiddenDealerCardVisual = SpawnCardVisual(dealerHidden, dealerHandContainer, dealerHand, true);
        
        UpdateScoresUI(true);
        UpdateUIButtons();

        int pScore = CalculateScore(playerHand);
        int dScore = CalculateScore(dealerHand);
        
        if (pScore == 21 || dScore == 21)
        {
            // Jeśli gracz LUB krupier ma naturalnego Blackjacka z dwóch kart, gra kończy się natychmiastowo.
            RevealDealerCard();
            ResolveGame();
        }
    }

    private GameObject SpawnCardVisual(CardData data, Transform parent, List<CardData> handList, bool isHidden)
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
    // SPLIT, HIT, STAND, DOUBLE
    // ============================================

    public void Split()
    {
        if (currentState != GameState.PlayerTurn || playerHand.Count != 2 || hasSplit) return;
        if (playerHand[0].value != playerHand[1].value) return;

        if (!MoneyManager.Instance.SpendMoney(currentBet))
        {
            resultText.text = "Not enough cash to split!";
            return;
        }

        hasSplit = true;
        splitBet = currentBet;
        UpdateBetUI();

        CardData splitCard = playerHand[1];
        playerHand.RemoveAt(1);
        
        Transform cardObjToMove = playerHandContainer.GetChild(1);
        cardObjToMove.SetParent(playerSplitHandContainer, false);
        playerSplitHand.Add(splitCard);

        SpawnCardVisual(DrawCard(), playerHandContainer, playerHand, false);
        SpawnCardVisual(DrawCard(), playerSplitHandContainer, playerSplitHand, false);

        UpdateScoresUI(true);
        UpdateUIButtons();
    }

    public void Hit()
    {
        if (currentState != GameState.PlayerTurn) return;

        List<CardData> activeHand = isPlayingSplitHand ? playerSplitHand : playerHand;
        Transform activeContainer = isPlayingSplitHand ? playerSplitHandContainer : playerHandContainer;

        SpawnCardVisual(DrawCard(), activeContainer, activeHand, false);
        UpdateScoresUI(true);
        UpdateUIButtons();

        if (CalculateScore(activeHand) > 21)
        {
            AdvanceTurn();
        }
    }

    public void Stand()
    {
        if (currentState != GameState.PlayerTurn) return;
        AdvanceTurn();
    }

    public void DoubleDown()
    {
        if (currentState != GameState.PlayerTurn) return;
        
        List<CardData> activeHand = isPlayingSplitHand ? playerSplitHand : playerHand;
        Transform activeContainer = isPlayingSplitHand ? playerSplitHandContainer : playerHandContainer;
        
        if (activeHand.Count != 2) return;

        int betToDouble = isPlayingSplitHand ? splitBet : currentBet;

        if (!MoneyManager.Instance.SpendMoney(betToDouble))
        {
            resultText.text = "Not enough cash to double down!";
            return;
        }

        if (isPlayingSplitHand) splitBet *= 2;
        else currentBet *= 2;
        UpdateBetUI();

        SpawnCardVisual(DrawCard(), activeContainer, activeHand, false);
        UpdateScoresUI(true);
        
        AdvanceTurn();
    }

    private void AdvanceTurn()
    {
        if (hasSplit && !isPlayingSplitHand)
        {
            isPlayingSplitHand = true;
            UpdateScoresUI(true);
            UpdateUIButtons();
        }
        else
        {
            bool hand1Bust = CalculateScore(playerHand) > 21;
            bool hand2Bust = hasSplit ? (CalculateScore(playerSplitHand) > 21) : true;
            
            if (hand1Bust && hand2Bust) 
            {
                RevealDealerCard();
                ResolveGame();
            }
            else
            {
                StartCoroutine(DealerTurnCoroutine());
            }
        }
    }

    private void RevealDealerCard()
    {
        if (hiddenDealerCardVisual != null)
        {
            Image img = hiddenDealerCardVisual.GetComponent<Image>();
            if (img != null && dealerHand.Count >= 2)
            {
                img.sprite = dealerHand[1].sprite;
            }
        }
    }

    private IEnumerator DealerTurnCoroutine()
    {
        currentState = GameState.DealerTurn;
        UpdateUIButtons();

        RevealDealerCard();
        UpdateScoresUI(false);
        yield return new WaitForSeconds(1f);

        while (CalculateScore(dealerHand) < 17)
        {
            SpawnCardVisual(DrawCard(), dealerHandContainer, dealerHand, false);
            UpdateScoresUI(false);
            yield return new WaitForSeconds(1f);
        }

        ResolveGame();
    }

    // ============================================
    // ROZSTRZYGNIĘCIE
    // ============================================

    private void ResolveGame()
    {
        int dScore = CalculateScore(dealerHand);
        string finalMessage = "";

        if (hasSplit)
        {
            int h1Win = ResolveHand(playerHand, currentBet, dScore, false);
            int h2Win = ResolveHand(playerSplitHand, splitBet, dScore, false);
            
            finalMessage = $"Hand 1: {GetResultMessage(h1Win, currentBet)}\nHand 2: {GetResultMessage(h2Win, splitBet)}";
        }
        else
        {
            int win = ResolveHand(playerHand, currentBet, dScore, true);
            finalMessage = GetResultMessage(win, currentBet);
        }
        
        currentState = GameState.Betting;
        UpdateScoresUI(false); 
        resultText.text = finalMessage;
        
        ClearBet();
        UpdateUIButtons();
    }

    // Zwraca wypłatę (0 = loss, bet = tie, >bet = win)
    private int ResolveHand(List<CardData> hand, int betAmount, int dScore, bool allowNaturalBlackjack)
    {
        int pScore = CalculateScore(hand);

        if (pScore > 21)
        {
            return 0; // Bust, loss
        }
        else if (dScore > 21)
        {
            if (allowNaturalBlackjack && pScore == 21 && hand.Count == 2)
                return Mathf.FloorToInt(betAmount * 2.5f);
            return betAmount * 2;
        }
        else if (pScore > dScore)
        {
            if (allowNaturalBlackjack && pScore == 21 && hand.Count == 2)
                return Mathf.FloorToInt(betAmount * 2.5f);
            return betAmount * 2;
        }
        else if (pScore < dScore)
        {
            return 0;
        }
        else
        {
            return betAmount; // Push
        }
    }

    private string GetResultMessage(int winnings, int originalBet)
    {
        if (winnings > originalBet)
        {
            MoneyManager.Instance.AddMoney(winnings);
            return $"Win! <color=green>+${winnings}</color>";
        }
        else if (winnings == originalBet)
        {
            MoneyManager.Instance.AddMoney(winnings); // Return bet
            return $"Push <color=yellow>+${winnings}</color>";
        }
        else
        {
            return $"Loss <color=red>-${originalBet}</color>";
        }
    }

    // ============================================
    // UI HELPERS
    // ============================================

    private void UpdateScoresUI(bool hideDealerScore)
    {
        int pScore = CalculateScore(playerHand);
        string colorTag1 = (!hasSplit || (hasSplit && !isPlayingSplitHand)) ? "<color=#FFFF00>" : "<color=#555555>";
        if (playerScoreText) playerScoreText.text = $"{colorTag1}You: {pScore}</color>";

        if (hasSplit && playerSplitScoreText)
        {
            int pSplitScore = CalculateScore(playerSplitHand);
            string colorTag2 = (hasSplit && isPlayingSplitHand) ? "<color=#FFFF00>" : "<color=#555555>";
            playerSplitScoreText.text = $"{colorTag2}Split: {pSplitScore}</color>";
        }
        else if (playerSplitScoreText)
        {
            playerSplitScoreText.text = "";
        }

        if (dealerScoreText)
        {
            if (hideDealerScore && dealerHand.Count > 0)
                dealerScoreText.text = $"Dealer: {dealerHand[0].value} + ?";
            else
                dealerScoreText.text = $"Dealer: {CalculateScore(dealerHand)}";
        }

        // Dodatkowe wyraźne wizualne podświetlenie aktywnych kart
        UpdateHandVisuals();
    }

    private void UpdateHandVisuals()
    {
        if (!hasSplit)
        {
            SetContainerColor(playerHandContainer, Color.white);
            if (playerSplitHandContainer) SetContainerColor(playerSplitHandContainer, Color.white);
            return;
        }

        // Przyciemnianie nieaktywnej puli kart do szarego koloru
        if (isPlayingSplitHand)
        {
            SetContainerColor(playerHandContainer, new Color(0.4f, 0.4f, 0.4f, 1f));
            if (playerSplitHandContainer) SetContainerColor(playerSplitHandContainer, Color.white);
        }
        else
        {
            SetContainerColor(playerHandContainer, Color.white);
            if (playerSplitHandContainer) SetContainerColor(playerSplitHandContainer, new Color(0.4f, 0.4f, 0.4f, 1f));
        }
    }

    private void SetContainerColor(Transform container, Color color)
    {
        foreach (Transform child in container)
        {
            Image img = child.GetComponent<Image>();
            if (img != null) img.color = color;
        }
    }

    private void UpdateUIButtons()
    {
        bool isBetting = (currentState == GameState.Betting);
        bool isPlaying = (currentState == GameState.PlayerTurn);

        if (btnDeal) btnDeal.gameObject.SetActive(isBetting);
        if (btnClearBet) btnClearBet.interactable = isBetting;
        
        List<CardData> activeHand = isPlayingSplitHand ? playerSplitHand : playerHand;
        
        if (btnHit) btnHit.gameObject.SetActive(isPlaying);
        if (btnStand) btnStand.gameObject.SetActive(isPlaying);
        if (btnDoubleDown) btnDoubleDown.gameObject.SetActive(isPlaying && activeHand.Count == 2);
        
        if (btnSplit) btnSplit.gameObject.SetActive(
            isPlaying && !hasSplit && playerHand.Count == 2 && playerHand[0].value == playerHand[1].value
        );
    }

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
        if (currentState == GameState.PlayerTurn || currentState == GameState.DealerTurn) return;
        PlayerMovement.canMove = true;
        CloseInstruction();
        gameObject.SetActive(false);
    }
}
