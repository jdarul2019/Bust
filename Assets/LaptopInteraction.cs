using UnityEngine;
using TMPro;

public class LaptopInteraction : MonoBehaviour
{
    [Header("UI Element (Dymki)")]
    [Tooltip("Dymek 'Press E to use laptop'")]
    public GameObject laptopBubble;

    [Header("Shop UI")]
    public GameObject shopPanel;

    private bool isPlayerInRange = false;

    private void Start()
    {
        if (laptopBubble != null) laptopBubble.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && PlayerMovement.canMove)
        {
            if (shopPanel != null)
            {
                if (laptopBubble != null) laptopBubble.SetActive(false);
                isPlayerInRange = false;
                
                PlayerMovement.canMove = false;
                shopPanel.SetActive(true);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (laptopBubble != null) laptopBubble.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (laptopBubble != null) laptopBubble.SetActive(false);
        }
    }
}
