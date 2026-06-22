using System;
using UnityEngine;

public class EnergyManager : MonoBehaviour
{
    // Główny zarządca energii w grze
    public static EnergyManager Instance { get; private set; }

    // Różnica w logice - tym razem powiadamiamy eventem też zmienną `Max`
    // Akceptuje parametry (obecna_energia, maksymalna_energia)
    public event Action<int, int> OnEnergyChanged;

    [Header("Właściwości Energii")]
    [SerializeField] private int maxEnergy = 200;
    
    private int currentEnergy;

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

    private void Start()
    {
        // Pasek startuje zawsze pełny
        currentEnergy = maxEnergy; 
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    // Pozwala na wyleczenie / zakup energy drinków w grze w przyszłości
    public void AddEnergy(int amount)
    {
        if (amount < 0) return;

        currentEnergy += amount;
        
        // Zabezpieczenie przed przepełnieniem
        if (currentEnergy > maxEnergy) 
        {
            currentEnergy = maxEnergy;
        }

        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    // Wydawanie energii. Zwraca true jeśli pobranie się udało.
    public bool SpendEnergy(int amount)
    {
        if (amount < 0 || currentEnergy < amount)
        {
            return false;
        }

        currentEnergy -= amount;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        return true;
    }

    public int GetEnergy()
    {
        return currentEnergy;
    }

    public int GetMaxEnergy()
    {
        return maxEnergy;
    }

    public void ResetEnergy()
    {
        currentEnergy = maxEnergy;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
}
