using UnityEngine;
using UnityEngine.UI; // Wymagane dla operowania na paskach graficznych ucinanych ułamkami
using TMPro;

public class EnergyUI : MonoBehaviour
{
    [Header("Obiekty Graficzne Paska")]
    [Tooltip("Obiekt Twojego paska energii! Musi mieć zaznaczoną opcję 'Image Type: Filled' w Unity!")]
    public Image energyFillBar;

    [Tooltip("Opcjonalny obiekt tekstowy dla energii, np. pokazujący: 180 / 200")]
    public TextMeshProUGUI energyText;

    private void Start()
    {
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.OnEnergyChanged += UpdateDisplay;
            
            // Wymuszenie fizycznego zapełnienia paska na pulpicie przy włączeniu gry
            UpdateDisplay(EnergyManager.Instance.GetEnergy(), EnergyManager.Instance.GetMaxEnergy());
        }
    }

    private void OnDestroy()
    {
        // Zwolnienie pamięci po usunięciu komponentu
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.OnEnergyChanged -= UpdateDisplay;
        }
    }

    // Automatycznie odbiera wynik z Managera
    private void UpdateDisplay(int currentEnergy, int maxEnergy)
    {
        if (energyFillBar != null)
        {
            // Typ Filled wymaga ułamka matematycznego (od 0.00 do 1.00 - dla 100%)
            // Rzutujemy (float), aby system poprawnie obliczył przecinek np. 150/200 = 0.75f
            energyFillBar.fillAmount = (float)currentEnergy / (float)maxEnergy;
        }

        if (energyText != null)
        {
            energyText.text = currentEnergy + " / " + maxEnergy;
        }
    }
}
