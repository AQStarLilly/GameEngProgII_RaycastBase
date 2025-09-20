using System;
using UnityEngine;
using UnityEngine.InputSystem;

// The InputManager handles all player input and distributes it via events.
// It follows the Singleton pattern to ensure only one instance exists at all times.
public class InputManager : MonoBehaviour, Inputs.IPlayerActions
{
   

    // Reference to the generated Input System class
    private Inputs inputs;

    void Awake()
    {
        // Initialize the Input System
        try
        {
            inputs = new Inputs();
            inputs.Player.SetCallbacks(this); // Set the callbacks for the Player action map
            inputs.Player.Enable(); // Enables the "Player" action map
        }
        catch (Exception exception)
        {
            Debug.LogError("Error initializing InputManager: " + exception.Message);
        }
    }

    #region Input Events
    // Events triggered when player inputs are detected
    public event Action<Vector2> MoveEvent;
    public event Action<Vector2> LookEvent; 

    public event Action SprintStartedEvent;
    public event Action SprintCanceledEvent;

    public event Action CrouchStartedEvent;
    public event Action CrouchCanceledEvent;

    public event Action PauseInputEvent;
    public event Action JumpEvent;
    public event Action InteractEvent;

    public event Action FireEvent;
    #endregion

    #region Input Callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) JumpEvent?.Invoke();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started) SprintStartedEvent?.Invoke();
        if (context.canceled) SprintCanceledEvent?.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started) CrouchStartedEvent?.Invoke();
        if (context.canceled) CrouchCanceledEvent?.Invoke();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started) InteractEvent?.Invoke();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.started) PauseInputEvent?.Invoke();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if(context.started) FireEvent?.Invoke();
    }
    #endregion

    void OnEnable()
    {
        if (inputs != null)
        {
            inputs.Player.Enable();
        }
    }

    void OnDisable()
    {
        if (inputs != null)
        {
            inputs.Player.Disable();
        }
    }


   
}