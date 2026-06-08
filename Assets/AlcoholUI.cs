using UnityEngine;
using TMPro;

public class AlcoholUI : MonoBehaviour
{
    [Header("Odnośniki Interfejsu")]
    [Tooltip("Tekst pokazujący ilość wypitych drinków i obecny modyfikator szczęścia")]
    public TextMeshProUGUI alcoholText;

    private void Start()
    {
        if (AlcoholManager.Instance != null)
        {
            // Zasubskrybowanie do Eventu z AlcoholManager
            AlcoholManager.Instance.OnAlcoholLevelChanged += UpdateDisplay;
            
            // Wymuszenie odświeżenia tekstu na starcie
            UpdateDisplay(AlcoholManager.Instance.GetAlcoholLevel());
        }
    }

    private void OnDestroy()
    {
        // Odpięcie eventu po usunięciu komponentu
        if (AlcoholManager.Instance != null)
        {
            AlcoholManager.Instance.OnAlcoholLevelChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(int currentAlcoholLevel)
    {
        if (alcoholText != null)
        {
            // Pobranie matematycznego modyfikatora
            float luckModifier = AlcoholManager.Instance.GetLuckModifier() * 100f; 
            
            // Formatowanie tekstu (żeby dodać + dla dodatnich wartości)
            string luckString = luckModifier >= 0 ? "+" + luckModifier.ToString("0") : luckModifier.ToString("0");

            // Ustawienie tekstu wyświetlającego się dla gracza
            alcoholText.text = $"Drinki: {currentAlcoholLevel} | Szczęście: {luckString}%";
        }
    }
}
