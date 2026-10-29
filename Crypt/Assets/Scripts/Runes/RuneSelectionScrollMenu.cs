using UnityEngine;

// Owns the rune-selection window and restores gameplay cursor state when it closes.
public class RuneSelectionScrollMenu : MonoBehaviour
{

    [SerializeField] private Transform content;
    
    public void OnEnable()
    {
        
    }

    public void CloseRuneMenu()
    {
        gameObject.SetActive(false);
        PlayerController player = FindAnyObjectByType<PlayerController>();
        player.SetCursorLocked(true);
    }
    
}
