using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour
{
    [Header("Dymek (tak samo jak w GameStation)")]
    public GameObject dymek;

    [Header("Panel wyboru lokacji")]
    public GameObject travelPanel;

    private bool isPlayerInRange = false;
    private bool menuOpen = false;

    void Start()
    {
        // Ukryj obydwa na starcie — identycznie jak GameStation
        if (dymek != null) dymek.SetActive(false);
        if (travelPanel != null) travelPanel.SetActive(false);
    }

    void Update()
    {
        if (!isPlayerInRange) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!menuOpen)
                OpenMenu();
            else
                CloseMenu();
        }

        if (menuOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseMenu();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        isPlayerInRange = true;
        if (dymek != null) dymek.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        isPlayerInRange = false;
        if (dymek != null) dymek.SetActive(false);
        if (menuOpen) CloseMenu();
    }

    private void OpenMenu()
    {
        menuOpen = true;
        PlayerMovement.canMove = false;

        if (dymek != null) dymek.SetActive(false);
        if (travelPanel != null) travelPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        menuOpen = false;
        PlayerMovement.canMove = true;

        if (travelPanel != null) travelPanel.SetActive(false);

        // Przywróć dymek jeśli gracz nadal stoi przy drzwiach
        if (isPlayerInRange && dymek != null) dymek.SetActive(true);
    }
}
