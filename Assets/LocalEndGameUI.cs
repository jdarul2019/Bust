using UnityEngine;
using TMPro;

public class LocalEndGameUI : MonoBehaviour
{
    [Header("End Game UI Elements")]
    public GameObject finalResultPanel;
    public TextMeshProUGUI finalResultTitle;
    public TextMeshProUGUI finalResultDesc;

    private void Start()
    {
        // Wyłączamy panel na starcie dla pewności
        if (finalResultPanel != null)
        {
            finalResultPanel.SetActive(false);
        }

        // Rejestrujemy lokalne referencje w globalnym Singletonie
        if (DayManager.Instance != null)
        {
            DayManager.Instance.RegisterEndGameUI(finalResultPanel, finalResultTitle, finalResultDesc);
        }
        else
        {
            Debug.LogWarning("LocalEndGameUI: Nie znaleziono instancji DayManager. Czy jesteś w scenie bez GameManagera?");
        }
    }
}
