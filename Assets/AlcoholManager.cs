using System;
using UnityEngine;

public class AlcoholManager : MonoBehaviour
{
    public static AlcoholManager Instance { get; private set; }

    public event Action<int> OnAlcoholLevelChanged;

    [Header("Właściwości Alkoholu")]
    private int currentAlcoholLevel = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddAlcohol(int amount = 1)
    {
        if (amount < 0) return;

        currentAlcoholLevel += amount;
        OnAlcoholLevelChanged?.Invoke(currentAlcoholLevel);
    }

    public void ResetAlcohol()
    {
        currentAlcoholLevel = 0;
        OnAlcoholLevelChanged?.Invoke(currentAlcoholLevel);
    }

    public int GetAlcoholLevel()
    {
        return currentAlcoholLevel;
    }

    /// <summary>
    /// Zwraca matematyczny modyfikator szczęścia.
    /// Wzór: y = -0.05 * (x - 2)^2 + 0.2
    /// 0 drinków: 0.0 (brak zmian)
    /// 2 drinki: +0.2 (+20% szans)
    /// Powyżej 4 drinków: Ujemne szczęście.
    /// Zabezpieczone limitami -0.8 (aby gracz miał chociaż jakieś ułamki szans w najgorszym stanie).
    /// </summary>
    public float GetLuckModifier()
    {
        float modifier = -0.05f * Mathf.Pow(currentAlcoholLevel - 2, 2) + 0.2f;
        
        // Zabezpieczenie przed totalnie zerową lub ujemną szansą (-100%)
        // Ograniczamy debuff do maksymalnie -0.80 (-80% szans)
        if (modifier < -0.8f)
        {
            modifier = -0.8f;
        }

        return modifier;
    }
}
