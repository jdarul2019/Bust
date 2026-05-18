using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Podepnij na każdy przezroczysty Button nad strefą planszy ruletki.
/// Skrypt sam się rejestruje w RouletteGame — nie musisz nic podpinać w On Click().
/// </summary>
[RequireComponent(typeof(Button))]
public class BettingZone : MonoBehaviour
{
    [Tooltip("Klucz strefy — musi zgadzać się z logiką w RouletteGame.cs")]
    public string zoneKey;

    [Header("UI Zakładu")]
    [Tooltip("Obiekt graficzny żetonu (Image), który ma się pojawiać po obstawieniu")]
    public GameObject chipVisual;

    [Tooltip("Tekst pokazujący postawioną kwotę")]
    public TextMeshProUGUI betLabel;

    void Awake()
    {
        // 1. Wymuś klucz z nazwy obiektu. Split(' ')[0] ucina wszystko od pierwszej spacji.
        // Dzięki temu z "num_0 (1)" zrobi się "num_0", a jak zmienisz ręcznie na "num_1", to będzie "num_1".
        // Ignorujemy to co przypadkiem skopiowało się w Inspektorze!
        zoneKey = gameObject.name.Split(' ')[0];

        // 2. Automatyczne szukanie tekstu (szuka w dzieciach nawet jeśli są wyłączone)
        if (betLabel == null)
        {
            betLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // 3. Automatyczne szukanie grafiki żetonu (zakładamy, że żeton to pierwsze dziecko przycisku)
        if (chipVisual == null && transform.childCount > 0)
        {
            chipVisual = transform.GetChild(0).gameObject;
        }
    }

    void Start()
    {
        // Znajdź RouletteGame w rodzicu (Canvas)
        RouletteGame roulette = GetComponentInParent<RouletteGame>();

        if (roulette == null)
        {
            Debug.LogError($"[BettingZone] Nie znaleziono RouletteGame w rodzicu obiektu '{gameObject.name}'!");
            return;
        }

        // Podepnij kliknięcie — automatycznie, bez On Click() w Inspektorze
        GetComponent<Button>().onClick.AddListener(() => roulette.PlaceBet(zoneKey));

        // Zarejestruj całą strefę
        roulette.RegisterZone(this);

        // Wyzeruj UI na starcie
        UpdateUI(0);
    }

    public void UpdateUI(int amount)
    {
        bool hasBet = amount > 0;
        
        // Pokazuj/ukrywaj grafikę żetonu
        if (chipVisual != null) chipVisual.SetActive(hasBet);
        
        // Aktualizuj i pokazuj/ukrywaj sam tekst
        if (betLabel != null)
        {
            betLabel.text = hasBet ? $"${amount}" : "";
            betLabel.gameObject.SetActive(hasBet);
        }
    }
}
