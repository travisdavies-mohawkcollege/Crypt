using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI interactText;

    [Header("Inventory")]
    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private PlayerInventoryUI inventoryUI;
    [SerializeField] private InputActionReference inventoryAction;
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject settingsMenu;

    [Header("Rune Selection")]
    [SerializeField] private GameObject runeSelectionCanvas;
    [SerializeField] private Transform runeSelectionContent;

    private PlayerController player;
    private bool isPaused;
    private InventoryComponent inventoryComponent;

    public GameObject RuneSelectionCanvas => runeSelectionCanvas;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        inventoryComponent = GetComponent<InventoryComponent>();
    }

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void OnEnable()
    {
        inventoryAction.action.Enable();
        pauseAction.action.Enable();
    }

    private void OnDisable()
    {
        inventoryAction.action.Disable();
        pauseAction.action.Disable();
    }

    private void Update()
    {
        HandleInventory();
        HandlePause();
    }

    private void HandleInventory()
    {
        if (!inventoryAction.action.WasPressedThisFrame()) return;

        bool opening = !inventoryCanvas.activeSelf;

        inventoryCanvas.SetActive(opening);
        SetCursorLocked(!opening);

        if (!opening) return;

        if (inventoryComponent == null || !inventoryComponent.HasInventory)
        {
            Debug.LogError("Player does not have a bound inventory.");

            inventoryCanvas.SetActive(false);
            SetCursorLocked(true);
            return;
        }

        inventoryUI.BindInventory(inventoryComponent.Inventory);
    }

    private void HandlePause()
    {
        if(!pauseAction.action.WasCompletedThisFrame()) return;
        TogglePause();
    }

    public void OpenOtherInventory(
        InventoryComponent otherInventoryComponent)
    {
        SetCursorLocked(false);
        inventoryCanvas.SetActive(true);

        inventoryUI.BindInventory(
            otherInventoryComponent.Inventory,
            inventoryComponent.Inventory);
    }

    public void OpenRuneSelection(
        Pedestal pedestal,
        int equippedRuneID)
    {
        runeSelectionCanvas.SetActive(true);

        RuneLibrary runeLibrary =
            FindAnyObjectByType<RuneLibrary>();

        runeLibrary.IntializeRuneSelection(
            runeSelectionContent,
            equippedRuneID,
            pedestal);
    }

    public void SetInteractionText(string text)
    {
        interactText.text = text;
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
        player.SetGameplayEnabled(locked);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        pauseMenuCanvas.SetActive(isPaused);
        if(pauseMenuCanvas.activeSelf) OpenPauseMenu();
        Time.timeScale = isPaused ? 0f : 1f;
        SetCursorLocked(!isPaused);
    }

    public void Unpause()
    {
        isPaused = false;
        pauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f;
        SetCursorLocked(true);
    }
    public void OpenPauseMenu()
    {
        settingsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void OpenSettings()
    {
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}