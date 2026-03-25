using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    // Wzorzec Singleton - pozwala innym skryptom łatwo dobierać się do MoneyManagera bez zmiennych w Inspektorze
    public static MoneyManager Instance { get; private set; }

    // System Zdarzeń (Events). Informuje kod, gdy ilość gotówki ulegnie zmianie
    // (Przekazuje nową wartość konta jako `int` do wszystkich podpiętych słuchaczy, a konkretnie do skryptu UI)
    public event Action<int> OnMoneyChanged;

    [Header("Startowe Dane")]
    [Tooltip("Ile gotówki posiada gracz przy pierwszym włączeniu gry?")]
    [SerializeField] private int startingMoney = 0;
    
    // Główna zmienna prywatna trzymająca środki. Dostęp mna zewnątrz tylko przez metody (Get/Add/Spend)
    private int currentMoney;

    private void Awake()
    {
        // Przypisanie jedynej, głównej instancji tej klasy
        if (Instance == null)
        {
            Instance = this;
            // Opcjonalnie jeśli chcesz aby gotówka nie resetywała się co włączenie innej sceny w Unity odkomentuj:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Usuwamy powielone skrypty (gdyby przez pomyłkę dodało się go znowu na scenę)
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Przypisujemy kasę z okienka "startingMoney"
        currentMoney = startingMoney;
        
        // Zgłaszamy event, żeby ewentualny MoneyUI odświeżył sobie stan liczbowy
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // Funkcja dodawania pieniędzy
    public void AddMoney(int amount)
    {
        if (amount < 0) return; // Zabezpiecznie przez błędem

        currentMoney += amount;
        
        // Wysyłamy komunikat o zmianie! (UI zaktualizuje tekst)
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // Funkcja pobierania pieniędzy (Zwraca informację true/false, czy gracz był wypłacalny)
    public bool SpendMoney(int amount)
    {
        // Sprawdzanie czy podana kwota jest ujemna i czy w ogóle stać gracza na zapłatę
        if (amount < 0 || currentMoney < amount)
        {
            return false;
        }

        currentMoney -= amount;

        // Bierzemy pieniądze z puli i informujemy resztę.
        OnMoneyChanged?.Invoke(currentMoney);
        return true;
    }

    // Dodatkowa metoda pomocnicza (Getter)
    public int GetBalance()
    {
        return currentMoney;
    }
}
