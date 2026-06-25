using UnityEngine;
using TMPro;

public class LaptopShop : MonoBehaviour
{
    [Header("Ustawienia Sklepu")]
    public int suitCost = 5000;

    [Header("UI")]
    public TextMeshProUGUI resultText;
    public GameObject shopPanel;

    private void OnEnable()
    {
        if (resultText != null)
            resultText.text = "";
    }

    public void BuySuit()
    {
        if (GameManager.Instance == null || MoneyManager.Instance == null) return;

        if (GameManager.Instance.hasSuit)
        {
            if (resultText != null) resultText.text = "Już posiadasz garnitur!";
            return;
        }

        if (MoneyManager.Instance.SpendMoney(suitCost))
        {
            // Oznaczamy w menedżerze, że mamy garnitur
            GameManager.Instance.hasSuit = true;
            if (resultText != null) resultText.text = "Zakupiono garnitur! Możesz teraz wejść do kasyna.";

            // --- NOWE: Szukamy gracza na scenie i każemy mu założyć garnitur ---
            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player != null)
            {
                player.PutOnSuit();
            }
        }
        else
        {
            if (resultText != null) resultText.text = "Brak środków! (" + suitCost + "$)";
        }
    }

    public void CloseShop()
    {
        PlayerMovement.canMove = true;
        if (shopPanel != null)
            shopPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
