using UnityEngine;
using TMPro;

public class BarShop : MonoBehaviour
{
    [Header("Ustawienia Sklepu")]
    public int drinkCost = 20;
    public int energyAmount = 30;
    
    public int alcoholCost = 50;
    public int alcoholAmount = 1;

    [Header("UI & Interakcja")]
    public GameObject dymek; // Dymek pokazujący przycisk interakcji np. 'E'
    public GameObject shopUI; // Okno sklepu z przyciskami
    public TextMeshProUGUI resultText; // Komunikaty w oknie sklepu

    private bool isPlayerInRange = false;

    void Start()
    {
        if (dymek != null) dymek.SetActive(false);
        if (shopUI != null) shopUI.SetActive(false);
        if (resultText != null) resultText.text = "";
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            OpenShop();
        }
    }

    public void OpenShop()
    {
        if (shopUI != null)
        {
            shopUI.SetActive(true);
            PlayerMovement.canMove = false; // Zatrzymaj gracza
            if (resultText != null) resultText.text = ""; // Wyczyść komunikaty
        }
        else
        {
            Debug.LogWarning("Shop UI is not assigned!");
        }
    }

    public void CloseShop()
    {
        if (shopUI != null)
        {
            shopUI.SetActive(false);
            PlayerMovement.canMove = true; // Pozwól graczowi znowu się poruszać
        }
    }

    // Ta funkcja będzie podpięta pod przycisk "Kup" w UI
    public void BuyEnergyDrink()
    {
        if (MoneyManager.Instance == null || EnergyManager.Instance == null)
        {
            Debug.LogWarning("Missing MoneyManager or EnergyManager in the scene!");
            return;
        }

        if (EnergyManager.Instance.GetEnergy() >= EnergyManager.Instance.GetMaxEnergy())
        {
            if (resultText != null) resultText.text = "You already have max energy!";
            return;
        }

        if (MoneyManager.Instance.SpendMoney(drinkCost))
        {
            EnergyManager.Instance.AddEnergy(energyAmount);
            if (resultText != null) resultText.text = "Energy drink purchased! +" + energyAmount + " Energy";
        }
        else
        {
            if (resultText != null) resultText.text = "Not enough money! (" + drinkCost + "$)";
        }
    }

    // Ta funkcja będzie podpięta pod przycisk "Kup Alkohol" w UI
    public void BuyAlcoholDrink()
    {
        if (MoneyManager.Instance == null || AlcoholManager.Instance == null)
        {
            Debug.LogWarning("Missing MoneyManager or AlcoholManager in the scene!");
            return;
        }

        if (MoneyManager.Instance.SpendMoney(alcoholCost))
        {
            AlcoholManager.Instance.AddAlcohol(alcoholAmount);
            if (resultText != null) resultText.text = "Alcohol drink purchased! You feel a bit braver...";
        }
        else
        {
            if (resultText != null) resultText.text = "Not enough money! (" + alcoholCost + "$)";
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (dymek != null) dymek.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (dymek != null) dymek.SetActive(false);
            
            // Opcjonalnie zamykaj sklep gdy gracz wyjdzie z obszaru
            CloseShop(); 
        }
    }
}
