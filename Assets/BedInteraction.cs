using TMPro;
using UnityEngine;

public class BedInteraction : MonoBehaviour
{
    [Header("UI Element (Dymki)")]
    [Tooltip("Dymek 'Press E to finish day'")]
    public GameObject bedBubble;

    [Header("End Of Day UI (Tu wciągnij teksty i panel!)")]
    public GameObject endOfDayPanel;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI profitText;

    private bool isPlayerInRange = false;

    private void Start()
    {
        if (bedBubble != null) bedBubble.SetActive(false); // Domyślnie na starcie ukryte
        if (endOfDayPanel != null) endOfDayPanel.SetActive(false); // Panel też domyślnie ukrywamy
    }

    private void Update()
    {
        // Pytamy czy gracz w zasięgu łóżka wcisnął klawisz 'E'
        // Dodatkowo PlayerMovement.canMove zabezpiecza aby nie dało się odpalić nocy podczas już trwającej minigry.
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && PlayerMovement.canMove)
        {
            if (DayManager.Instance != null)
            {
                // Schowaj dymek żeby się nie naświetlał podczas czarnego ekranu snu
                if (bedBubble != null) bedBubble.SetActive(false);
                isPlayerInRange = false; // Przerywa możliwość zaspamowania e-ciskiem i wymusza ponowne wejście

                // Odpala Menedżer Dni z przekazaniem mu interfejsu (żeby działał niezależnie od scen!)
                DayManager.Instance.FinishDay(endOfDayPanel, dayText, profitText);
            }
            else
            {
                Debug.LogWarning("BedInteraction: Brak instancji DayManager w pokoju! Podepnij skrypt i panel UI!");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (bedBubble != null) bedBubble.SetActive(true); // Pokaż dymek uderzenia [E]
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (bedBubble != null) bedBubble.SetActive(false); // Schowaj dymek po odejściu
        }
    }
}
