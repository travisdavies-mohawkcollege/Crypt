using UnityEngine;

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
