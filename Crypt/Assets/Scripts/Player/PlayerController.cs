using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public class PlayerController : MonoBehaviour
{
    private bool canMove;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private Camera cam;
    [SerializeField] private float interactionDistance = 3f;

    [SerializeField] private Transform cameraTarget;

    [SerializeField] private float mouseSensitivity;
    [SerializeField] private float gamePadSensitivity;
    [SerializeField] private float minPitch;
    [SerializeField] private float maxPitch;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float airDrag = 2f;
    [SerializeField] private float dashDrag = 10f;
    [SerializeField] private float dashForce = 10f;

    [SerializeField] private float wallJumpForce;
    [SerializeField] private float wallJumpForwardForce;
    private bool doJump = false;
    private bool hasDoubleJumped = false;
    private bool isGrounded;

    private bool doDash = false;
    private bool dashOnCooldown = false;
    private float dashCooldown = 1.5f;
    [SerializeField]private float dashCooldownMax = 1.5f;
    

    private bool wallJump = false;
    private bool hasWallJumped = false;
    private Vector3 walljumpVelocity;
    private Vector3 dashVelocity;

    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField] private Transform groundCheckOrigin;
    [SerializeField] public GameObject runeSelectionCanvas;

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


    private CharacterController characterController;
    private float pitch;
    private float verticalVelocity;
    private bool moving;


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        SetCursorLocked(true);
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
    }

    private void Update()
    {
        HandleLook();
        HandleJump();
        HandleDash();
        HandleMovement();
        HandleInteractionText();
        HandleInteraction();

        //This will become opening menu rather than just freeing the mouse.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked) SetCursorLocked(false);
            else SetCursorLocked(true);
        }
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
        float speed = sprintAction.action.IsPressed() ? sprintSpeed : moveSpeed;

        //if jumping or wall jumping alter vertical velocity
        if(doJump)
        {
            verticalVelocity = jumpForce;
            doJump = false;
            Debug.Log("applied jump force");
            if(!characterController.isGrounded && !wallJump) hasDoubleJumped = true;
        } 
        if(wallJump)
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
        if(wallJump)
        {
            walljumpVelocity += cam.transform.forward * wallJumpForwardForce;
            wallJump = false;
            //hasWallJumped = true;
        }
        if(characterController.isGrounded) walljumpVelocity = Vector3.Lerp(walljumpVelocity, Vector3.zero, airDrag * 10 * Time.deltaTime);
        else walljumpVelocity = Vector3.Lerp(walljumpVelocity, Vector3.zero, airDrag * Time.deltaTime);

        if(doDash)
        {
            if(input != Vector2.zero) dashVelocity = transform.right * input.x + transform.forward * input.y;
            else dashVelocity = cam.transform.forward;
            dashVelocity = dashVelocity * dashForce;
            doDash = false;
        }

        dashVelocity = Vector3.Lerp(dashVelocity, Vector3.zero, dashDrag * Time.deltaTime);


        Vector3 finalVelocity = velocity + walljumpVelocity + dashVelocity;
        Vector3 horizontalVelocity = velocity;
        horizontalVelocity.y = 0f;
        bool walking = characterController.isGrounded && horizontalVelocity.sqrMagnitude > 0.01f;
        characterController.Move(finalVelocity * Time.deltaTime);

    }

    private void HandleJump()
    {
        if(characterController.isGrounded)
        {
            hasWallJumped = false;
            hasDoubleJumped = false;
        } 
        if(jumpAction.action.WasPressedThisFrame())
        {
            Debug.Log("Player tried to jump");
            if(characterController.isGrounded) doJump = true;
            else if(!characterController.isGrounded && !hasDoubleJumped) doJump = true;
            else if(Physics.CheckSphere(groundCheckOrigin.position, 1, wallMask)) wallJump = true;

        }
    }
    
    private void HandleDash()
    {
        if(dashAction.action.WasPressedThisFrame())
        {
            if(!dashOnCooldown)
            {
                doDash = true;
                dashOnCooldown = true;
            }
        }

        if(dashOnCooldown)
        {
            dashCooldown -= Time.deltaTime;
            if(dashCooldown <= 0)
            {
                dashCooldown = dashCooldownMax;
                dashOnCooldown = false;
            }
        }
    }

    private void HandlePrimaryAttack()
    {

    }
    
    private void HandleSecondaryAttack()
    {

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
            Debug.LogWarning($"{hit.collider.name} has no IInteractable component on it or its parents" );
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

    public void OnDrawGizmos()
    {
        Gizmos.DrawSphere(groundCheckOrigin.position, 1f);
    }
}
