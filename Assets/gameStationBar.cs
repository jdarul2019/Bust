using UnityEngine;

public class GameStation : MonoBehaviour
{
    public string stationName = "Dices"; 
    public GameObject dymek; 
    
    private bool isPlayerInRange = false;

    // DODAJEMY TĘ FUNKCJĘ: Odpala się raz, na samym starcie gry
    void Start()
    {
        if (dymek != null) 
        {
            dymek.SetActive(false); // Wymuszamy ukrycie dymka!
        }
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("BOOM! Odpalasz grę: " + stationName);
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
        }
    }
}