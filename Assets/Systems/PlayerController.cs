using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Experiment on whether we can get away with not using a LayerMask for ground checks
    // Not using a layermask is simpler, but there are concers about accuracy and performance
    // worse case we can keep the option for groundcheck without a layer mask for quick playtests
    public bool groundCheckLayered = false;
    
    public enum MovementState
    {
        Idle,
        Walking,
        Sprinting,
        Crouching,
        Jumping, // Or integrate into velocity checks
        Falling
    }

    public MovementState currentMovementState;

    private int playerLayerMask;

    [Header("Enable/Disable Controls & Features")]
    public bool moveEnabled = true;
    public bool lookEnabled = true;

    [SerializeField] private bool jumpEnabled;

    [SerializeField] private bool sprintEnabled;
    [SerializeField] private bool holdToSprint = true;  // true = HOLD to sprint, false = TOGGLE to sprint

    [SerializeField] private bool crouchEnabled;
    [SerializeField] private bool holdToCrouch = true;  // true = HOLD to crouch, false = TOGGLE to crouch

    [Header("Movement Settings")]
    [SerializeField] private float crouchMoveSpeed = 2.0f;
    [SerializeField] private float walkMoveSpeed = 4.0f;
    [SerializeField] private float sprintMoveSpeed = 7.0f;

    [SerializeField] private float speedTransitionDuration = 0.25f; // Time in seconds for speed transitions
    [SerializeField] private float currentMoveSpeed; // Tracks the current interpolated speed

    private bool sprintInput;
    private bool crouchInput;

    private Vector3 velocity; // Velocity of the character controller

    [Header("Look Settings")]
    [SerializeField] public float horizontalLookSensitivity = 30;
    [SerializeField] public float verticalLookSensitivity = 30;

    [SerializeField] public float LoweLookLimit = -60;
    [SerializeField] public float upperLookLimit = 60;

    [SerializeField] public bool invertLookY { get; private set; } = false;

    [SerializeField] private Transform cameraRoot;
    // Allow public access to the cameraRoot transform
    public Transform CameraRoot => cameraRoot;

    private CinemachineCamera cinemachineCamera;
    
   

    private CinemachinePanTilt panTilt;

    [Header("Gravity & Jump Settings")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private float gravity = 30.0f; // Gravity value for the character controller    
    [SerializeField] private float jumpHeight = 2.0f;
    private float jumpCooldown = 0.2f; // Time before allowing another jump    
    private float jumpCooldownTimer = 0f;
    private float groundCheckRadius = 0.1f; // Radius for ground check sphere
    private bool jumpRequested = false;

    [Header("Crouch Settings")]
    private float standingHeight;
    private Vector3 standingCenter;
    private float standingCamY;
    private bool isObstructed = false;

    [SerializeField] private float crouchTransitionDuration = 0.2f; // Time in seconds for crouch/stand transition (approximate completion)
    [SerializeField] private float crouchingHeight = 1.0f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private float crouchingCamY = 0.75f;

    private float targetHeight;
    private Vector3 targetCenter;
    private float targetCamY; // Target Y position for camera root during crouch transition



    // private references
    private CharacterController characterController;
    [SerializeField] private InputManager inputManager;

    // Input variables
    private Vector2 lookInput;
    private Vector2 moveInput;

    private void Awake()
    {
        #region Initialize Component References

        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component not found.");
            return;
        }



        if (cameraRoot == null)
        {
            Debug.LogError("cameraRoot is null, please assign in the inspector.");
            return;
        }

        // Get reference to the CinemachineCamera component
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
            if (cinemachineCamera == null)
            {
                Debug.LogError("CinemachineCamera component not found in children.");
                return;
            }
        }

        // get reference to the cinemachineCamera PanTilt component
        if (panTilt == null)
        {
            panTilt = cinemachineCamera.gameObject.AddComponent<CinemachinePanTilt>();
            if (panTilt == null)
            {
                Debug.LogError("panTilt component not found in cinemachineCamera.");
                return;
            }
        }
        #endregion

        playerLayerMask = ~LayerMask.GetMask("Player");

        #region Initialize default values

        currentMovementState = MovementState.Idle;

        // Initialize crouch variables
        standingHeight = characterController.height;
        standingCenter = characterController.center;
        standingCamY = cameraRoot.localPosition.y;

        targetHeight = standingHeight;
        targetCenter = standingCenter;
        targetCamY = cameraRoot.localPosition.y;



        // Set default state of bools
        crouchInput = false;
        //isSprinting = false;

        #endregion
    }


    private void Update()
    {
        handlePlayerMovement();

    }

    private void LateUpdate()
    {
        HandlePlayerCameraLook();
    }


    // Call this method in the State_GameWalking
    public void handlePlayerMovement()
    {
        //Debug.Log(characterController.velocity.magnitude);

        if (moveEnabled == true)
        {
            DetermineMovementState();

            GroundedCheck();

            // Update jump cooldown timer
            if (jumpCooldownTimer > 0)
            {
                jumpCooldownTimer -= Time.deltaTime;
            }

            HandleCrouchTransition();
            ApplyMovement();
        }
    }

    private void DetermineMovementState()
    {
        // Determine current movement state based on input and conditions

        // check if the player is not on the ground
        if (isGrounded == false)
        {
            // check if they are jumping or falling
            if (velocity.y > 0)
            {
                currentMovementState = MovementState.Jumping;
            }
            else if (velocity.y < 0)
            {
                currentMovementState = MovementState.Falling;
            }
        }

        else if (isGrounded == true)
        {
            // Check if player is crouching OR still transitioning from crouch
            // This includes: active crouch input, currently transitioning, or not at full standing height
            if (crouchInput == true || isObstructed == true)
            {
                currentMovementState = MovementState.Crouching;
            }

            else if (sprintInput == true && currentMovementState != MovementState.Crouching)
            {
                currentMovementState = MovementState.Sprinting;
            }

            else if (moveInput.magnitude >= 0.1f && sprintInput == false && crouchInput == false)
            {
                currentMovementState = MovementState.Walking;
            }

            else if (moveInput.magnitude <= 0.1f && sprintInput == false && crouchInput == false)
            {
                currentMovementState = MovementState.Idle;
            }
        }
    }

    private void ApplyMovement()
    {

        // if movement is not enabled, do nothing and return
        if (!moveEnabled) return;

        // Step 1: Get input direction
        Vector3 moveInputDirection = new Vector3(moveInput.x, 0, moveInput.y);
        Vector3 worldMoveDirection = transform.TransformDirection(moveInputDirection);

        // Step 2: Determine movement speed
        float targetMoveSpeed;

        switch (currentMovementState)
        {
            case MovementState.Crouching:
                {
                    targetMoveSpeed = crouchMoveSpeed;
                    break;
                }

            case MovementState.Sprinting:
                {
                    targetMoveSpeed = sprintMoveSpeed;
                    break;
                }

            default:
                {
                    targetMoveSpeed = walkMoveSpeed;
                    break;
                }
        }

        // Step 3: Smoothly interpolate current speed towards target speed
        float lerpSpeed = 1f - Mathf.Pow(0.01f, Time.deltaTime / speedTransitionDuration);
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetMoveSpeed, lerpSpeed);

        // Step 4: Handle horizontal movement
        Vector3 horizontalMovement = worldMoveDirection * currentMoveSpeed;

        // Step 5: Handle jumping and gravity
        ApplyJumpAndGravity();

        // Step 6: Combine horizontal and vertical movement
        Vector3 movement = horizontalMovement;
        movement.y = velocity.y;

        // Step 7: Apply final movement
        characterController.Move(movement * Time.deltaTime);
    }
 
    public void HandlePlayerCameraLook()
    {
        if (!lookEnabled) return; // Check if look is enabled 

        float lookX = lookInput.x * horizontalLookSensitivity * Time.deltaTime;
        float lookY = lookInput.y * verticalLookSensitivity * Time.deltaTime;

        // Invert vertical look if needed
        if (invertLookY)
        {
            lookY = -lookY;
        }

        // Rotate character on Y-axis (left/right look)
        transform.Rotate(Vector3.up * lookX);

        // Tilt cameraRoot on X-axis (up/down look)
        Vector3 currentAngles = cameraRoot.localEulerAngles;
        float newRotationX = currentAngles.x - lookY;

        // Convert to signed angle for proper clamping
        newRotationX = (newRotationX > 180) ? newRotationX - 360 : newRotationX;
        newRotationX = Mathf.Clamp(newRotationX, LoweLookLimit, upperLookLimit);

        cameraRoot.localEulerAngles = new Vector3(newRotationX, 0, 0);
    }

    private void ApplyJumpAndGravity()
    {
        // Check if jump feature is enabled, otherwise skip this check
        if (jumpEnabled == true)
        {
            // Process jump if...
            //  + Jump Requested (via input)
            //  + Player is currently grounded
            //  + Player is not crouching

            // Also ensures jump cooldown has passed to prevent spamming.

            if (jumpRequested && isGrounded && currentMovementState != MovementState.Crouching)
            {
                // Calculate the initial upward velocity needed to reach the desired jumpHeight.
                velocity.y = Mathf.Sqrt(2f * jumpHeight * gravity);

                // Reset the jump request flag so it only triggers once per button press.
                jumpRequested = false;

                // Start the jump cooldown timer to prevent immediate re-jumping.
                jumpCooldownTimer = jumpCooldown;
            }
        }

        // Apply gravity based on the player's current state (grounded or in air).
        if (isGrounded == true && velocity.y < 0)
        {
            // If grounded and moving downwards (due to accumulated gravity from previous frames),
            // snap velocity to a small negative value. This keeps the character firmly on the ground
            // without allowing gravity to build up indefinitely, preventing "bouncing" or
            // incorrect ground detection issues.

            velocity.y = -1f;

            // If the player was previously jumping or falling and just landed, update their horizontal state.

        }

        else // If not grounded (in the air):
        {
            // Apply standard gravity.
            velocity.y -= gravity * Time.deltaTime;           
        }
    }
    
    private void HandleCrouchTransition()
    {
        bool shouldCrouch = crouchInput == true && currentMovementState != MovementState.Jumping && currentMovementState != MovementState.Falling;

        // if airborne and was crouching, maintain crouch state (prevents standing up from crouch while walking off a ledge)
        bool wasAlreadyCrouching = characterController.height < (standingHeight - 0.05f);

        if (isGrounded == false && wasAlreadyCrouching)
        {
            shouldCrouch = true; // Maintain crouch state if airborne (walking off ledge while crouching)
        }

        if (shouldCrouch)
        {
            targetHeight = crouchingHeight;
            targetCenter = crouchingCenter;
            targetCamY = crouchingCamY;
            isObstructed = false; // No obstruction when intentionally crouching
        }
        else
        {
            float maxAllowedHeight = GetMaxAllowedHeight();

            if (maxAllowedHeight >= standingHeight - 0.05f)
            {
                // No obstruction, allow immediate transition to standing
                targetHeight = standingHeight;
                targetCenter = standingCenter;
                targetCamY = standingCamY;
                isObstructed = false;
            }

            else
            {
                // Obstruction detected, limit height and center
                targetHeight = Mathf.Min(standingHeight, maxAllowedHeight);
                float standRatio = Mathf.Clamp01((targetHeight - crouchingHeight) / (standingHeight - crouchingHeight));
                targetCenter = Vector3.Lerp(crouchingCenter, standingCenter, standRatio);
                targetCamY = Mathf.Lerp(crouchingCamY, standingCamY, standRatio);
                isObstructed = true;
            }
        }

        // Calculate lerp speed based on desired duration
        // This formula ensures the transition approximately reaches 99% of the target in 'crouchTransitionDuration' seconds.
        float lerpSpeed = 1f - Mathf.Pow(0.01f, Time.deltaTime / crouchTransitionDuration);

        // Smoothly transition to targets
        characterController.height = Mathf.Lerp(characterController.height, targetHeight, lerpSpeed);
        characterController.center = Vector3.Lerp(characterController.center, targetCenter, lerpSpeed);

        Vector3 currentCamPos = cameraRoot.localPosition;
        cameraRoot.localPosition = new Vector3(currentCamPos.x, Mathf.Lerp(currentCamPos.y, targetCamY, lerpSpeed), currentCamPos.z);

    }




    #region Helper Methods






    // Ground Check that requires LayerMask "Ground" to be set in the Unity Editor
    private void GroundedCheck()
    {
        bool previouslyGrounded = isGrounded;

        // Always use CharacterController's ground detection when not using layered check
        if (groundCheckLayered == false)
        {
            isGrounded = characterController.isGrounded;
        }
        else
        {
            // Keep your sphere check for layered mode
            isGrounded = Physics.CheckSphere(transform.position, groundCheckRadius, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore);
        }

        // Landing detection
        if (isGrounded == false && previouslyGrounded == true)
        {
            Debug.Log("Player just left ground");
        }

        if (isGrounded == true && previouslyGrounded == false)
        {
            Debug.Log($"Player just landed at Y position: {transform.position.y}");
        }
    }

    private float GetMaxAllowedHeight()
    {
        // Cast a ray upward to find the maximum height we can achieve
        RaycastHit hit;
        float maxCheckDistance = standingHeight + 0.15f;

        //if (showClearanceCheckDebugRay)
        //{
        //    Debug.DrawRay(transform.position, Vector3.up * maxCheckDistance, Color.blue, 0.1f);
        //}

        if (Physics.Raycast(transform.position, Vector3.up, out hit, maxCheckDistance, playerLayerMask))
        {
            // We hit something, so calculate the maximum height we can be
            // Subtract a small buffer to prevent clipping
            float maxHeight = hit.distance - 0.1f;

            // Ensure we don't go below crouching height
            maxHeight = Mathf.Max(maxHeight, crouchingHeight);

            Debug.Log($"Overhead obstruction detected. Max allowed height: {maxHeight:F2}");
            return maxHeight;
        }

        // No obstruction found, can stand at full height
        return standingHeight;
    }

    private void OnDrawGizmos()
    {
        characterController = GetComponent<CharacterController>();

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + new Vector3(0, 0, 0), 0.1f);

        // Draw ground check sphere
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
    }

    #endregion

    #region Input methods




    private void SetMoveInput(Vector2 inputVector)
    {
        moveInput = new Vector2(inputVector.x, inputVector.y);
    }

    private void SetLookInput(Vector2 vector)
    {
        lookInput = new Vector2(vector.x, vector.y);
    }

    private void JumpInput()
    {
        if (jumpEnabled && !crouchInput && isGrounded && jumpCooldownTimer <= 0f)
        {
            jumpRequested = true;

            // Immediately set a small "input buffer" cooldown to prevent spam
            jumpCooldownTimer = 0.1f;
        }
    }

    private void SprintInputStarted()
    {
        // if Sprint is not enabled, do nothing and just return
        if (sprintEnabled == false) return;

        if (holdToSprint == true)
        {
            sprintInput = true;
        }

        // If holdToSprint is false, Sprint will revert to toggle mode
        else if (holdToSprint == false)
        {
            sprintInput = !sprintInput;
        }
    }

    private void SprintInputCanceled()
    {
        // if Sprint is not enabled, do nothing and just return
        if (sprintEnabled == false) return;

        // Only update sprintInput if holdToSprint is enabled, otherwise in toggle mode we'll ignore the input canceled / button release
        if (holdToSprint == true)
        {
            sprintInput = false;
        }        
    }



    private void CrouchInputStarted()
    {
        // if Crouch is not enabled, do nothing and just return;
        if (crouchEnabled == false) return;

        if (holdToCrouch == true)
        {
            crouchInput = true;
        }

        // If holdToCrouch is false, Crouch will revert to toggle mode
        else if (holdToCrouch == false)
        {
            crouchInput = !crouchInput;
        }
    }

    private void CrouchInputCanceled()
    {
            // if Crouch is not enabled, do nothing and just return
            if (crouchEnabled == false) return;

            // Only update crouchInput if holdToCrouch is enabled, otherwise in toggle mode we'll ignore the input canceled / button release
            if (holdToCrouch == true)
            {
                crouchInput = false;
            }

    }



    #endregion

    private void OnEnable()
    {
        inputManager.MoveEvent += SetMoveInput;
        inputManager.LookEvent += SetLookInput;

        inputManager.JumpEvent += JumpInput;

        inputManager.SprintStartedEvent += SprintInputStarted;
        inputManager.SprintCanceledEvent += SprintInputCanceled;

        inputManager.CrouchStartedEvent += CrouchInputStarted;
        inputManager.CrouchCanceledEvent += CrouchInputCanceled;
    }

    private void OnDisable()
    {
        inputManager.MoveEvent -= SetMoveInput;
        inputManager.LookEvent -= SetLookInput;

        inputManager.JumpEvent -= JumpInput;

        inputManager.SprintStartedEvent -= SprintInputStarted;
        inputManager.SprintCanceledEvent -= SprintInputCanceled;

        inputManager.CrouchStartedEvent -= CrouchInputStarted;
        inputManager.CrouchCanceledEvent -= CrouchInputCanceled;
    }

    

}