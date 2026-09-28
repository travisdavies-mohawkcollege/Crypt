using UnityEngine;

public class RuneSelectionScrollMenu : MonoBehaviour
{

    [SerializeField] private Transform content;
    
    public void OnEnable()
    {
        RuneLibrary library = FindAnyObjectByType<RuneLibrary>();
        library.IntializeRuneSelection(content);
    }

    public void CloseRuneMenu()
    {
        gameObject.SetActive(false);
        PlayerController player = FindAnyObjectByType<PlayerController>();
        player.SetCursorLocked(true);
    }
    
}
