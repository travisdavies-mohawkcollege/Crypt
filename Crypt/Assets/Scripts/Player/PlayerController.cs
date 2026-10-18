using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;



public class PlayerController : MonoBehaviour, IDamageable
{
    private float health = 100;
    private float iFrameMax = 0.25f;
    private float iFrameTimer = 0.25f;
    private bool canTakeDamage = true;
    private bool canMove = true;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private Camera cam;
    [SerializeField] private float interactionDistance = 3f;

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Animator handAnimator;

    [SerializeField] private float mouseSensitivity;
    [SerializeField] private float gamePadSensitivity;
    [SerializeField] private float minPitch;
    [SerializeField] private float maxPitch;

    //Movement
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float airDrag = 2f;
    [SerializeField] private float dashDrag = 10f;
    [SerializeField] private float dashForce = 10f;

    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallJumpForwardForce;
    [SerializeField] private PlayerCombatStats playerCombatStats;
    private bool doJump = false;
    private bool hasDoubleJumped = false;
    private bool isGrounded;
    private bool doDash = false;
    private bool dashOnCooldown = false;
    private float dashCooldown = 1.5f;
    [SerializeField] private float dashCooldownMax = 1.5f;
    private bool wallJump = false;
    //private bool hasWallJumped = false;
    private Vector3 walljumpVelocity;
    private Vector3 dashVelocity;

    private enum AttackInputPhase
    {
        Pressed,
        Held,
        Released
    }


    //References
    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField] private Transform groundCheckOrigin;
    [SerializeField] public GameObject runeSelectionCanvas;
    [SerializeField] private Transform runeSelectionContent;
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private PlayerInventoryUI inventoryUI;
    [SerializeField] private ItemCatalog itemCatalog;
    private InventoryComponent inventoryComponent;

    [SerializeField] private LayerMask wallMask;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference crouchAction;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference primaryAttackAction;
    [SerializeField] private InputActionReference secondaryAttackAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference inventoryAction;
    private InputAction equipPrimaryAction;
    private InputAction equipSecondaryAction;
    private InputAction unequipAction;

    [Header("Particle System Attacks")]
    [SerializeField] private ParticleSystem flamethrower;
    [SerializeField] private GameObject flameSnap;

    [Header("Passive Element Effects")]
    [SerializeField] private ParticleSystem waterPassive;
    [SerializeField] private ParticleSystem firePassive;
    [SerializeField] private ParticleSystem icePassive;
    [SerializeField] private ParticleSystem stonePassive;
    [SerializeField] private ParticleSystem naturePassive;
    [SerializeField] private ParticleSystem airPassive;
    [SerializeField] private ParticleSystem lightningPassive;
    [SerializeField] private ParticleSystem arcanePassive;


    private CharacterController characterController;
    private float pitch;
    private float verticalVelocity;
    public bool moving;
    public bool sprinting;
    private EquipmentSlot activeWeaponSlot = EquipmentSlot.MainHand;
    public EquipmentSlot ActiveWeaponSlot => activeWeaponSlot;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inventoryComponent = GetComponent<InventoryComponent>();
        equipPrimaryAction = primaryAttackAction.action.actionMap.FindAction("EquipPrimary");
        equipSecondaryAction = primaryAttackAction.action.actionMap.FindAction("EquipSecondary");
        unequipAction = primaryAttackAction.action.actionMap.FindAction("Unequip");
    }

    private void Start()
    {
        SetCursorLocked(true);
        SubscribeToEquipmentChanges();
        UpdatePassiveWeaponEffect();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        sprintAction.action.Enable();
        crouchAction.action.Enable();
        interactAction.action.Enable();
        primaryAttackAction.action.Enable();
        secondaryAttackAction.action.Enable();
        jumpAction.action.Enable();
        dashAction.action.Enable();
        inventoryAction.action.Enable();
        equipPrimaryAction?.Enable();
        equipSecondaryAction?.Enable();
        unequipAction?.Enable();
        SubscribeToEquipmentChanges();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        sprintAction.action.Disable();
        crouchAction.action.Disable();
        interactAction.action.Disable();
        primaryAttackAction.action.Disable();
        secondaryAttackAction.action.Disable();
        jumpAction.action.Disable();
        dashAction.action.Disable();
        inventoryAction.action.Disable();
        equipPrimaryAction?.Disable();
        equipSecondaryAction?.Disable();
        unequipAction?.Disable();
        if (PlayerManager.Instance != null) PlayerManager.Instance.PlayerEquipment.Changed -= UpdatePassiveWeaponEffect;
        StopAllPassiveElementEffects();
    }

    private void SubscribeToEquipmentChanges()
    {
        if (PlayerManager.Instance == null) return;
        PlayerManager.Instance.PlayerEquipment.Changed -= UpdatePassiveWeaponEffect;
        PlayerManager.Instance.PlayerEquipment.Changed += UpdatePassiveWeaponEffect;
    }

    private void Update()
    {
        HandleInventory();
        HandleEquippedWeapon();

        if (!canTakeDamage)
        {
            iFrameTimer -= Time.deltaTime;

            if (iFrameTimer <= 0)
            {
                canTakeDamage = true;
                iFrameTimer = iFrameMax;
            }
        }

        if (!canMove) return;

        HandleLook();
        HandleJump();
        HandleDash();
        HandleMovement();
        HandleInteractionText();
        HandleInteraction();
        HandlePrimaryAttack();
        HandleSecondaryAttack();
    }

    private void HandleInventory()
    {
        if (!inventoryAction.action.WasPerformedThisFrame()) return;

        bool opening = !inventoryCanvas.activeSelf;
        inventoryCanvas.SetActive(opening);
        SetCursorLocked(!opening);

        if (opening)
        {
            if (inventoryComponent == null || !inventoryComponent.HasInventory)
            {
                Debug.LogError("Player does not have a bound inventory.");
                inventoryCanvas.SetActive(false);
                SetCursorLocked(true);
                return;
            }

            inventoryUI.BindInventory(inventoryComponent.Inventory);
        }
    }

    public void OpenOtherInventory(InventoryComponent chestInventoryComponent)
    {
        SetCursorLocked(false);
        inventoryCanvas.SetActive(true);

        inventoryUI.BindInventory(chestInventoryComponent.Inventory, this.inventoryComponent.Inventory);
    }

    private void HandleInteractionText()
    {
        Ray finder = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(finder.origin, finder.direction, Color.yellow, 1f);

        if (Physics.Raycast(finder, out RaycastHit hit, interactionDistance))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null) interactText.text = interactable.InteractText;
            else interactText.text = "";
        }
        else interactText.text = "";
    }

    private void HandleInteraction()
    {
        if (interactAction.action.WasPressedThisFrame())
        {
            Debug.Log("Interact pressed");
            TryInteract();
        }
    }

    private void HandleMovement()
    {
        if (!canMove) return;
        //Read movement input
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        //Chart movement onto a vector 3
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        //clamp the vector 3
        move = Vector3.ClampMagnitude(move, 1f);
        //is sprint pressed?
        sprinting = sprintAction.action.IsPressed();
        float baseSpeed = sprinting ? sprintSpeed : moveSpeed;
        float speed = playerCombatStats != null ? playerCombatStats.GetMoveSpeed(baseSpeed) : baseSpeed;

        //if jumping or wall jumping alter vertical velocity
        if (doJump)
        {
            verticalVelocity = jumpForce;
            doJump = false;
            Debug.Log("applied jump force");
            if (!characterController.isGrounded && !wallJump) hasDoubleJumped = true;
        }
        if (wallJump)
        {
            verticalVelocity = jumpForce;
            hasDoubleJumped = false;
        }
        //if grounded change our vertical velocity otherwise apply gravity
        else if (characterController.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;

        //Calculate our velocity
        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;

        //if we are walljumping add a boost of speed
        if (wallJump)
        {
            walljumpVelocity += cam.transform.forward * wallJumpForwardForce;
            wallJump = false;
            //hasWallJumped = true;
        }
        if (characterController.isGrounded) walljumpVelocity = Vector3.Lerp(walljumpVelocity, Vector3.zero, airDrag * 10 * Time.deltaTime);
        else walljumpVelocity = Vector3.Lerp(walljumpVelocity, Vector3.zero, airDrag * Time.deltaTime);

        if (doDash)
        {
            if (input != Vector2.zero) dashVelocity = transform.right * input.x + transform.forward * input.y;
            else dashVelocity = cam.transform.forward;
            dashVelocity = dashVelocity * dashForce;
            doDash = false;
        }

        dashVelocity = Vector3.Lerp(dashVelocity, Vector3.zero, dashDrag * Time.deltaTime);


        Vector3 finalVelocity = velocity + walljumpVelocity + dashVelocity;
        Vector3 horizontalVelocity = velocity;
        horizontalVelocity.y = 0f;
        moving = characterController.isGrounded && horizontalVelocity.sqrMagnitude > 0.01f;
        characterController.Move(finalVelocity * Time.deltaTime);

    }

    private void HandleJump()
    {
        if (characterController.isGrounded)
        {
            hasDoubleJumped = false;
        }
        if (jumpAction.action.WasPressedThisFrame())
        {
            Debug.Log("Player tried to jump");
            if (characterController.isGrounded) doJump = true;
            else if (!characterController.isGrounded && !hasDoubleJumped) doJump = true;
            else if (Physics.CheckSphere(groundCheckOrigin.position, 1, wallMask)) wallJump = true;

        }
    }

    private void HandleDash()
    {
        if (dashAction.action.WasPressedThisFrame())
        {
            if (!dashOnCooldown)
            {
                doDash = true;
                dashOnCooldown = true;
            }
        }

        if (dashOnCooldown)
        {
            dashCooldown -= Time.deltaTime;
            if (dashCooldown <= 0)
            {
                dashCooldown = dashCooldownMax;
                dashOnCooldown = false;
            }
        }
    }

    private void HandlePrimaryAttack()
    {
        AttackInputPhase phase;

        if (primaryAttackAction.action.WasReleasedThisFrame()) phase = AttackInputPhase.Released;
        else if (primaryAttackAction.action.WasPressedThisFrame()) phase = AttackInputPhase.Pressed;
        else if (primaryAttackAction.action.IsPressed()) phase = AttackInputPhase.Held;
        else return;

        switch (activeWeaponSlot)
        {
            case EquipmentSlot.MainHand:
                HandleMainHandPrimaryAction(phase);
                return;

            case EquipmentSlot.OffHand:
                HandleOffHandPrimaryAction(phase);
                return;

            default:
                activeWeaponSlot = EquipmentSlot.MainHand;
                HandleMainHandPrimaryAction(phase);
                return;
        }
    }

    private void HandleSecondaryAttack()
    {
        AttackInputPhase phase;

        if (secondaryAttackAction.action.WasReleasedThisFrame()) phase = AttackInputPhase.Released;
        else if (secondaryAttackAction.action.WasPressedThisFrame()) phase = AttackInputPhase.Pressed;
        else if (secondaryAttackAction.action.IsPressed()) phase = AttackInputPhase.Held;
        else return;

        switch (activeWeaponSlot)
        {
            case EquipmentSlot.MainHand:
                HandleMainHandSecondaryAction(phase);
                return;

            case EquipmentSlot.OffHand:
                HandleOffHandSecondaryAction(phase);
                return;

            default:
                activeWeaponSlot = EquipmentSlot.MainHand;
                HandleMainHandSecondaryAction(phase);
                return;
        }
    }

    private void HandleEquippedWeapon()
    {
        if (equipPrimaryAction != null && equipPrimaryAction.WasPressedThisFrame())
        {
            activeWeaponSlot = EquipmentSlot.MainHand;
            UpdatePassiveWeaponEffect();
            return;
        }

        if (equipSecondaryAction != null && equipSecondaryAction.WasPressedThisFrame())
        {
            activeWeaponSlot = EquipmentSlot.OffHand;
            UpdatePassiveWeaponEffect();
            return;
        }

        if (unequipAction != null && unequipAction.WasPressedThisFrame())
        {
            activeWeaponSlot = EquipmentSlot.None;
            UpdatePassiveWeaponEffect();
        }
    }

    private void UpdatePassiveWeaponEffect()
    {
        EElements equippedElement = EElements.None;

        if (PlayerManager.Instance != null && PlayerManager.Instance.PlayerEquipment.TryGetEquippedItem(activeWeaponSlot, out EquippedItem equippedItem) && TryGetWeaponDefinition(equippedItem, out ItemDefinition weaponDefinition)) equippedElement = weaponDefinition.ItemElement;

        StopAllPassiveElementEffects();

        switch (equippedElement)
        {
            case EElements.None:
                return;

            case EElements.Water:
                PlayPassiveElementEffect(waterPassive);
                return;

            case EElements.Fire:
                PlayPassiveElementEffect(firePassive);
                return;

            case EElements.Ice:
                PlayPassiveElementEffect(icePassive);
                return;

            case EElements.Stone:
                PlayPassiveElementEffect(stonePassive);
                return;

            case EElements.Nature:
                PlayPassiveElementEffect(naturePassive);
                return;

            case EElements.Air:
                PlayPassiveElementEffect(airPassive);
                return;

            case EElements.Lightning:
                PlayPassiveElementEffect(lightningPassive);
                return;

            case EElements.Arcane:
                PlayPassiveElementEffect(arcanePassive);
                return;
        }
    }

    private void PlayPassiveElementEffect(ParticleSystem passiveEffect)
    {
        if (passiveEffect == null) return;
        ParticleSystem.EmissionModule emission = passiveEffect.emission;
        emission.enabled = true;
        if (!passiveEffect.isPlaying) passiveEffect.Play(true);
    }

    private void StopAllPassiveElementEffects()
    {
        StopPassiveElementEffect(waterPassive);
        StopPassiveElementEffect(firePassive);
        StopPassiveElementEffect(icePassive);
        StopPassiveElementEffect(stonePassive);
        StopPassiveElementEffect(naturePassive);
        StopPassiveElementEffect(airPassive);
        StopPassiveElementEffect(lightningPassive);
        StopPassiveElementEffect(arcanePassive);
    }

    private void StopPassiveElementEffect(ParticleSystem passiveEffect)
    {
        if (passiveEffect == null) return;
        ParticleSystem.EmissionModule emission = passiveEffect.emission;
        emission.enabled = false;
    }

    private void HandleMainHandPrimaryAction(AttackInputPhase phase)
    {
        if (PlayerManager.Instance == null) return;

        ItemDefinition mainHandWeapon = null;

        switch (PlayerManager.Instance.PlayerEquipment.TryGetEquippedItem(EquipmentSlot.MainHand, out EquippedItem equippedItem))
        {
            case false:
                //No weapon is equipped in the main hand.
                return;

            case true:
                if (!TryGetWeaponDefinition(equippedItem, out mainHandWeapon)) return;
                break;
        }

        switch (mainHandWeapon.ItemElement)
        {
            case EElements.None:
                //Main hand primary action without an element.
                break;

            case EElements.Water:
                //Main hand primary water attack.
                break;

            case EElements.Fire:
                if (phase == AttackInputPhase.Pressed || phase == AttackInputPhase.Held)
                {
                    handAnimator.SetBool("Shooting", true);
                    if (!flamethrower.isPlaying) flamethrower.Play();
                }

                if (phase == AttackInputPhase.Released)
                {
                    handAnimator.SetBool("Shooting", false);
                    if (flamethrower.isPlaying) flamethrower.Stop();
                }
                break;

            case EElements.Ice:
                //Main hand primary ice attack.
                break;

            case EElements.Stone:
                //Main hand primary stone attack.
                break;

            case EElements.Nature:
                //Main hand primary nature attack.
                break;

            case EElements.Air:
                //Main hand primary air attack.
                break;

            case EElements.Lightning:
                //Main hand primary lightning attack.
                break;

            case EElements.Arcane:
                //Main hand primary arcane attack.
                break;
        }
    }

    private void HandleOffHandPrimaryAction(AttackInputPhase phase)
    {
        if (PlayerManager.Instance == null) return;

        ItemDefinition offHandWeapon = null;

        switch (PlayerManager.Instance.PlayerEquipment.TryGetEquippedItem(EquipmentSlot.OffHand, out EquippedItem equippedItem))
        {
            case false:
                //No weapon is equipped in the off hand.
                return;

            case true:
                if (!TryGetWeaponDefinition(equippedItem, out offHandWeapon)) return;
                break;
        }

        switch (offHandWeapon.ItemElement)
        {
            case EElements.None:
                //Off hand primary action without an element.
                break;

            case EElements.Water:
                //Off hand primary water attack.
                break;
            case EElements.Fire:
                //Off hand primary fire attack.
                if (phase == AttackInputPhase.Pressed || phase == AttackInputPhase.Held)
                {
                    handAnimator.SetBool("Shooting", true);
                    if (!flamethrower.isPlaying) flamethrower.Play();
                }

                if (phase == AttackInputPhase.Released)
                {
                    handAnimator.SetBool("Shooting", false);
                    if (flamethrower.isPlaying) flamethrower.Stop();
                }
                break;

            case EElements.Ice:
                //Off hand primary ice attack.
                break;

            case EElements.Stone:
                //Off hand primary stone attack.
                break;

            case EElements.Nature:
                //Off hand primary nature attack.
                break;

            case EElements.Air:
                //Off hand primary air attack.
                break;

            case EElements.Lightning:
                //Off hand primary lightning attack.
                break;

            case EElements.Arcane:
                //Off hand primary arcane attack.
                break;
        }
    }

    private void HandleMainHandSecondaryAction(AttackInputPhase phase)
    {
        if (PlayerManager.Instance == null) return;

        ItemDefinition mainHandWeapon = null;

        switch (PlayerManager.Instance.PlayerEquipment.TryGetEquippedItem(EquipmentSlot.MainHand, out EquippedItem equippedItem))
        {
            case false:
                //No weapon is equipped in the main hand.
                return;

            case true:
                if (!TryGetWeaponDefinition(equippedItem, out mainHandWeapon)) return;
                break;
        }

        switch (mainHandWeapon.ItemElement)
        {
            case EElements.None:
                //Main hand secondary action without an element.
                break;

            case EElements.Water:
                //Main hand secondary water attack.
                break;

            case EElements.Fire:
                if (phase != AttackInputPhase.Pressed) break;

                handAnimator.SetTrigger("Point");

                Ray attackRay = new Ray(cam.transform.position, cam.transform.forward);
                int triggerMask = ~LayerMask.GetMask("Trigger");
                if (Physics.Raycast(attackRay, out RaycastHit hit, 50f, triggerMask))
                {
                    GameObject impact = Instantiate(flameSnap, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, 2f);
                }
                break;

            case EElements.Ice:
                //Main hand secondary ice attack.
                break;

            case EElements.Stone:
                //Main hand secondary stone attack.
                break;

            case EElements.Nature:
                //Main hand secondary nature attack.
                break;

            case EElements.Air:
                //Main hand secondary air attack.
                break;

            case EElements.Lightning:
                //Main hand secondary lightning attack.
                break;

            case EElements.Arcane:
                //Main hand secondary arcane attack.
                break;
        }
    }

    private void HandleOffHandSecondaryAction(AttackInputPhase phase)
    {
        if (PlayerManager.Instance == null) return;

        ItemDefinition offHandWeapon = null;

        switch (PlayerManager.Instance.PlayerEquipment.TryGetEquippedItem(EquipmentSlot.OffHand, out EquippedItem equippedItem))
        {
            case false:
                //No weapon is equipped in the off hand.
                return;

            case true:
                if (!TryGetWeaponDefinition(equippedItem, out offHandWeapon)) return;
                break;
        }

        switch (offHandWeapon.ItemElement)
        {
            case EElements.None:
                //Off hand secondary action without an element.
                break;

            case EElements.Water:
                //Off hand secondary water attack.
                break;

            case EElements.Fire:
                if (phase != AttackInputPhase.Pressed) break;

                handAnimator.SetTrigger("Point");

                Ray attackRay = new Ray(cam.transform.position, cam.transform.forward);
                int triggerMask = ~LayerMask.GetMask("Trigger");

                if (Physics.Raycast(attackRay, out RaycastHit hit, 50f, triggerMask))
                {
                    GameObject impact = Instantiate(flameSnap, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, 2f);
                }
                break;

            case EElements.Ice:
                //Off hand secondary ice attack.
                break;

            case EElements.Stone:
                //Off hand secondary stone attack.
                break;

            case EElements.Nature:
                //Off hand secondary nature attack.
                break;

            case EElements.Air:
                //Off hand secondary air attack.
                break;

            case EElements.Lightning:
                //Off hand secondary lightning attack.
                break;

            case EElements.Arcane:
                //Off hand secondary arcane attack.
                break;
        }
    }

    private bool TryGetWeaponDefinition(EquippedItem equippedItem, out ItemDefinition definition)
    {
        definition = null;

        if (PlayerManager.Instance == null || itemCatalog == null) return false;
        if (equippedItem == null || string.IsNullOrWhiteSpace(equippedItem.instanceID)) return false;

        if (!PlayerManager.Instance.PlayerInventory.TryFindByInstanceID(equippedItem.instanceID, out InventoryEntry inventoryEntry))
        {
            Debug.LogWarning($"Equipped item '{equippedItem.instanceID}' was not found in the player inventory.");
            return false;
        }

        if (!itemCatalog.TryGetItemDefinition(inventoryEntry.item.definitionID, out definition))
        {
            Debug.LogWarning($"Could not find item definition '{inventoryEntry.item.definitionID}'.");
            return false;
        }

        if (definition.ItemType == ItemType.Weapon) return true;

        Debug.LogWarning($"{definition.ItemName} is equipped in {equippedItem.slot}, but it is not a weapon.");
        definition = null;
        return false;
    }

    private void HandleLook()
    {
        if (!canMove) return;
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        if (lookAction.action.activeControl?.device is Mouse) lookInput *= mouseSensitivity;
        else lookInput *= gamePadSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * lookInput.x);
        pitch -= lookInput.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void TryInteract()
    {
        Debug.Log("Interact input received");

        if (!canMove)
        {
            Debug.LogWarning("Interaction blocked because canMove is false");
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red, 2f);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Debug.Log("Raycast hit nothing");
            return;
        }

        Debug.Log($"Raycast hit: {hit.collider.name}");

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            Debug.LogWarning($"{hit.collider.name} has no IInteractable component on it or its parents");
            return;
        }

        Debug.Log($"Interacting with: {hit.collider.name}");
        interactable.Interact(this);
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        canMove = locked;
    }

    public void TakeDamage(float damage)
    {
        playerCombatStats.TakeDamage(damage);
    }

    public void ActivateRunePanel(Pedestal pedastal, int equippedRuneID)
    {
        runeSelectionCanvas.SetActive(true);
        RuneLibrary runeLibrary = FindAnyObjectByType<RuneLibrary>();
        runeLibrary.IntializeRuneSelection(runeSelectionContent, equippedRuneID, pedastal);
    }


    public void OnDrawGizmos()
    {
        Gizmos.DrawSphere(groundCheckOrigin.position, 1f);
    }
}
