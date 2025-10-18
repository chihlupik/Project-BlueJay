using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 1f;
    public float tiltAmount = 5f;
    public float tiltSmoothness = 5f;
    public float zoomFOV = 45f;
    public float zoomSpeed = 10f;
    public Transform playerBody;

    public KeyCode flashlightKey = KeyCode.F;
    private Light flashlight;
    private bool isFlashlightOn = false;

    [Header("Flashlight Sound")]
    public AudioSource flashlightAudioSource;
    public AudioClip flashlightClickSound;
    [Range(0.0f, 1.0f)]
    public float flashlightVolume = 0.8f;
    [Range(0.5f, 2.0f)]
    public float flashlightPitch = 1.0f;

    [Header("View Bobbing")]
    public bool enableViewBobbing = true;
    [Tooltip("Bobbing intensity when walking")]
    public float walkBobbingIntensity = 0.015f;
    [Tooltip("Bobbing intensity when sprinting")]
    public float sprintBobbingIntensity = 0.025f;
    [Tooltip("Bobbing intensity when crouching")]
    public float crouchBobbingIntensity = 0.008f;
    [Tooltip("Bobbing speed when walking")]
    public float walkBobbingSpeed = 3f;
    [Tooltip("Bobbing speed when sprinting")]
    public float sprintBobbingSpeed = 4f;
    [Tooltip("Bobbing speed when crouching")]
    public float crouchBobbingSpeed = 2f;
    [Tooltip("How quickly bobbing transitions between states")]
    public float bobbingTransitionSharpness = 8f;

    [Header("Object Dragging")]
    public bool enableObjectDragging = true;
    [Tooltip("Maximum distance to pick up objects")]
    public float maxDragDistance = 5f;
    [Tooltip("Strength of the drag force")]
    public float dragStrength = 10f;
    [Tooltip("How smoothly the object follows the cursor")]
    public float dragSmoothness = 8f;
    [Tooltip("How smoothly the object rotates with camera")]
    public float rotationSmoothness = 5f;
    [Tooltip("Maximum mass that can be dragged")]
    public float maxDragMass = 50f;
    [Tooltip("Layer mask for draggable objects")]
    public LayerMask draggableLayers = ~0;

    private float xRotation = 0f;
    private float currentTilt = 0f; 
    private float targetTilt = 0f; 
    private Camera playerCamera;
    private float defaultFOV;
    private float targetFOV;
    private bool isZooming = false;

    // Reference to PlayerController for slide tilt and movement state
    private PlayerController playerController;
    private float externalTilt = 0f;

    // View bobbing variables
    private float bobbingTimer = 0f;
    private float currentBobbingIntensity = 0f;
    private float targetBobbingIntensity = 0f;
    private float currentBobbingSpeed = 0f;
    private float targetBobbingSpeed = 0f;
    private Vector3 originalLocalPosition;
    private bool wasGrounded = false;
    private Vector3 currentBobOffset = Vector3.zero;

    // Object dragging variables
    private Rigidbody draggedObject = null;
    private float originalDrag;
    private float originalAngularDrag;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerCamera = GetComponent<Camera>();
        
        if (playerCamera != null)
        {
            defaultFOV = playerCamera.fieldOfView;
            targetFOV = defaultFOV;
        }

        flashlight = GetComponentInChildren<Light>();
        if (flashlight != null)
        {
            flashlight.enabled = isFlashlightOn;
        }

        // Get reference to PlayerController
        playerController = GetComponentInParent<PlayerController>();

        // Auto-find AudioSource if not assigned
        if (flashlightAudioSource == null)
        {
            flashlightAudioSource = GetComponent<AudioSource>();
        }

        // Store original camera position for bobbing
        originalLocalPosition = transform.localPosition;
    }

    void Update()
    {
        HandleFlashlight();
        HandleZoom();
        HandleMouseLook();
        HandleViewBobbing();
        HandleObjectDragging();
    }

    void HandleFlashlight()
    {
        if (flashlight == null) return;

        if (Input.GetKeyDown(flashlightKey))
        {
            isFlashlightOn = !isFlashlightOn;
            flashlight.enabled = isFlashlightOn;
            
            // Play flashlight sound
            PlayFlashlightSound();
        }
    }

    void HandleZoom()
    {
        if (playerCamera == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            isZooming = true;
            targetFOV = zoomFOV;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isZooming = false;
            targetFOV = defaultFOV;
        }

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        if (isZooming)
        {
            mouseX *= 0.7f;
            mouseY *= 0.7f;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Get external tilt from PlayerController (slide tilt)
        if (playerController != null)
        {
            externalTilt = playerController.GetSlideTilt();
        }

        // Combine mouse-based tilt with external slide tilt
        targetTilt = (-mouseX * tiltAmount) + externalTilt;
        
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothness * Time.deltaTime);
        
        transform.localRotation *= Quaternion.Euler(0f, 0f, currentTilt);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
        else
        {
            transform.Rotate(Vector3.up * mouseX);
        }
    }

    void HandleViewBobbing()
    {
        if (!enableViewBobbing || playerController == null)
        {
            // Reset bobbing offset
            currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, bobbingTransitionSharpness * Time.deltaTime);
            ApplyBobbingOffset();
            return;
        }

        // Check if player is grounded and moving
        bool isGrounded = playerController.IsGrounded();
        Vector3 velocity = playerController.GetVelocity();
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        bool isMoving = horizontalSpeed > 0.1f;

        // Reset bobbing when landing to avoid jarring transitions
        if (isGrounded && !wasGrounded)
        {
            bobbingTimer = 0f;
        }

        // Determine target bobbing parameters based on player state
        if (isGrounded && isMoving && !playerController.IsSliding() && !playerController.IsDashing())
        {
            if (playerController.IsCrouching())
            {
                targetBobbingIntensity = crouchBobbingIntensity;
                targetBobbingSpeed = crouchBobbingSpeed;
            }
            else if (playerController.IsSprinting())
            {
                targetBobbingIntensity = sprintBobbingIntensity;
                targetBobbingSpeed = sprintBobbingSpeed;
            }
            else
            {
                targetBobbingIntensity = walkBobbingIntensity;
                targetBobbingSpeed = walkBobbingSpeed;
            }

            // Scale intensity by speed (normalized to player's current max speed)
            float speedMultiplier = Mathf.Clamp01(horizontalSpeed / playerController.GetSprintSpeed());
            targetBobbingIntensity *= speedMultiplier;
            
            // Further reduce intensity for more subtle effect
            targetBobbingIntensity *= 0.7f;
        }
        else
        {
            // No bobbing when in air, sliding, dashing, or not moving
            targetBobbingIntensity = 0f;
            targetBobbingSpeed = 0f;
        }

        // Smoothly transition bobbing parameters
        currentBobbingIntensity = Mathf.Lerp(currentBobbingIntensity, targetBobbingIntensity, bobbingTransitionSharpness * Time.deltaTime);
        currentBobbingSpeed = Mathf.Lerp(currentBobbingSpeed, targetBobbingSpeed, bobbingTransitionSharpness * Time.deltaTime);

        // Calculate bobbing offset
        Vector3 targetBobOffset = Vector3.zero;
        
        if (currentBobbingIntensity > 0.001f && currentBobbingSpeed > 0.1f)
        {
            bobbingTimer += Time.deltaTime * currentBobbingSpeed;
            
            // Use more subtle bobbing with smoother curves
            float verticalBob = Mathf.Sin(bobbingTimer * 2f) * currentBobbingIntensity * 0.5f;
            float horizontalBob = Mathf.Cos(bobbingTimer) * currentBobbingIntensity * 0.3f;
            
            targetBobOffset = new Vector3(horizontalBob, verticalBob, 0f);
        }
        else
        {
            bobbingTimer = 0f;
        }

        // Smoothly transition to target bobbing offset
        currentBobOffset = Vector3.Lerp(currentBobOffset, targetBobOffset, bobbingTransitionSharpness * Time.deltaTime);
        
        // Apply the bobbing offset
        ApplyBobbingOffset();

        wasGrounded = isGrounded;
    }

    void HandleObjectDragging()
    {
        if (!enableObjectDragging || playerCamera == null) return;

        // Left mouse button pressed - try to pick up object
        if (Input.GetMouseButtonDown(0) && draggedObject == null)
        {
            TryPickUpObject();
        }

        // Left mouse button released - drop object
        if (Input.GetMouseButtonUp(0) && draggedObject != null)
        {
            DropObject();
        }

        // Update dragged object position and rotation
        if (draggedObject != null)
        {
            UpdateDraggedObject();
        }
    }

    void TryPickUpObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDragDistance, draggableLayers))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            
            if (rb != null && rb.mass <= maxDragMass)
            {
                // Pick up the object
                draggedObject = rb;
                
                // Store original drag values
                originalDrag = rb.linearDamping;
                originalAngularDrag = rb.angularDamping;
                
                // Increase drag for smoother movement
                rb.linearDamping = 10f;
                rb.angularDamping = 10f;
                
                // Optional: Wake up the rigidbody
                rb.WakeUp();
                
                // Removed console output
            }
        }
    }

    void UpdateDraggedObject()
    {
        if (draggedObject == null) return;

        // Calculate target position in front of camera
        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * maxDragDistance * 0.7f;
        
        // Smoothly move towards target position
        Vector3 currentPosition = draggedObject.position;
        Vector3 targetVelocity = (targetPosition - currentPosition) * dragStrength;
        
        // Apply smooth movement
        draggedObject.linearVelocity = Vector3.Lerp(draggedObject.linearVelocity, targetVelocity, dragSmoothness * Time.deltaTime);
        
        // Rotate object to match camera rotation smoothly
        Quaternion targetRotation = Quaternion.LookRotation(playerCamera.transform.forward);
        draggedObject.rotation = Quaternion.Slerp(draggedObject.rotation, targetRotation, rotationSmoothness * Time.deltaTime);
        
        // Optional: Add slight rotation damping
        draggedObject.angularVelocity = Vector3.Lerp(draggedObject.angularVelocity, Vector3.zero, dragSmoothness * Time.deltaTime);
    }

    void DropObject()
    {
        if (draggedObject != null)
        {
            // Restore original drag values
            draggedObject.linearDamping = originalDrag;
            draggedObject.angularDamping = originalAngularDrag;
            
            // Removed console output
            draggedObject = null;
        }
    }

    void ApplyBobbingOffset()
    {
        // Apply bobbing offset on top of the current camera position (which includes crouch/slide height)
        transform.localPosition = originalLocalPosition + currentBobOffset;
    }

    // Call this method when the player changes stance (crouch/stand/slide)
    public void UpdateOriginalPosition()
    {
        // This should be called by PlayerController when camera height changes
        originalLocalPosition = transform.localPosition;
    }

    void PlayFlashlightSound()
    {
        if (flashlightAudioSource == null || flashlightClickSound == null) return;

        flashlightAudioSource.clip = flashlightClickSound;
        flashlightAudioSource.volume = flashlightVolume;
        flashlightAudioSource.pitch = flashlightPitch;
        flashlightAudioSource.loop = false;
        flashlightAudioSource.Play();
    }

    // Public method to set external tilt (for PlayerController)
    public void SetExternalTilt(float tilt)
    {
        externalTilt = tilt;
    }

    // Public method to enable/disable view bobbing
    public void SetViewBobbing(bool enabled)
    {
        enableViewBobbing = enabled;
    }

    // Public method to enable/disable object dragging
    public void SetObjectDragging(bool enabled)
    {
        enableObjectDragging = enabled;
        
        // If disabling while dragging an object, drop it
        if (!enabled && draggedObject != null)
        {
            DropObject();
        }
    }

    // Public method to check if currently dragging an object
    public bool IsDraggingObject()
    {
        return draggedObject != null;
    }

    // Public method to get the currently dragged object
    public Rigidbody GetDraggedObject()
    {
        return draggedObject;
    }

    void OnDrawGizmos()
    {
        // Visualize drag range in scene view
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Vector3 endPoint = playerCamera.transform.position + playerCamera.transform.forward * maxDragDistance;
            Gizmos.DrawLine(playerCamera.transform.position, endPoint);
            Gizmos.DrawWireSphere(endPoint, 0.1f);
        }
    }
}