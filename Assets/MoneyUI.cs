using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    [Header("Odnośniki Interfejsu")]
    [Tooltip("Obiekt tekstowy TextMeshPro na ekranie, który trzyma wynik (np. 1500)")]
    public TextMeshProUGUI moneyText;

    private void Start()
    {
        // Bezpieczne zabezpieczenie 
        if (MoneyManager.Instance != null)
        {
            // Zasubskrybowanie - "Dopnij moją funkcję UpdateDisplay() do Eventu Twojego portfela w MoneyManager"
            // Zawsze używamy tu +=
            MoneyManager.Instance.OnMoneyChanged += UpdateDisplay;
            
            // Konieczny wymóg pobrania i odświeżenia jednorazowego przy samym włączeniu gry/odpaleniu UI
            UpdateDisplay(MoneyManager.Instance.GetBalance());
        }
    }

    private void OnDestroy()
    {
        // Bezpieczne odpinanie się z Eventu na końcu niszczenia skryptu
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= UpdateDisplay;
        }
    }

    // Funkcja z automatu jest wołana KAŻDORAZOWO przez ten MoneyManager wyżej.
    private void UpdateDisplay(int newBalance)
    {
        if (moneyText != null)
        {
            // Aktualizowanie wartości w Canvasie. 
            moneyText.text = newBalance.ToString(); 

            // Przykładowo, jeśli byś chciał w tym miejscu też np. "100 $" zostawiasz ten kod poniżej:
            // moneyText.text = newBalance.ToString() + " $";
        }
    }
}
