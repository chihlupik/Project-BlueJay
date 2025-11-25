using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 1f;
    public float tiltAmount = 5f;
    public float tiltSmoothness = 5f;
    public float zoomFOV = 45f;
    public float zoomSpeed = 10f;
    public Transform playerBody;

    [Header("Movement FOV Settings")]
    public bool enableMovementFOV = true;
    public float movementFOV = 65f;
    public float fovChangeSpeed = 8f;
    
    [Header("Device Settings")]
    public KeyCode toggleDeviceKey = KeyCode.Q;
    public GameObject deviceObject; // Assign your device in the inspector
    private bool isDeviceActive = false;

    [Header("Device Sound")]
    public AudioSource deviceAudioSource;
    public AudioClip activateSound;
    public AudioClip deactivateSound;
    public float deviceVolume = 0.8f;
    public float devicePitch = 1.0f;

    [Header("View Bobbing")]
    public bool enableViewBobbing = true;
    public float walkBobbingIntensity = 0.015f;
    public float crouchBobbingIntensity = 0.008f;
    public float walkBobbingSpeed = 3f;
    public float crouchBobbingSpeed = 2f;
    public float bobbingTransitionSharpness = 8f;

    [Header("Object Dragging")]
    public bool enableObjectDragging = true;
    public float maxDragDistance = 5f;
    public float dragStrength = 10f;
    public float dragSmoothness = 8f;
    public float rotationSmoothness = 5f;
    public float maxDragMass = 50f;
    public LayerMask draggableLayers = ~0;

    [Header("Door Interaction")]
    public float doorInteractionDistance = 3f;
    public KeyCode interactKey = KeyCode.E;

    private float xRotation = 0f;
    private float currentTilt = 0f; 
    private float targetTilt = 0f; 
    private Camera playerCamera;
    private float defaultFOV;
    private float targetFOV;
    private bool isZooming = false;

    private PlayerController playerController;
    private float externalTilt = 0f;

    private float bobbingTimer = 0f;
    private float currentBobbingIntensity = 0f;
    private float targetBobbingIntensity = 0f;
    private float currentBobbingSpeed = 0f;
    private float targetBobbingSpeed = 0f;
    private Vector3 originalLocalPosition;
    private bool wasGrounded = false;
    private Vector3 currentBobOffset = Vector3.zero;

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

        playerController = GetComponentInParent<PlayerController>();

        if (deviceAudioSource == null)
        {
            deviceAudioSource = GetComponent<AudioSource>();
        }

        originalLocalPosition = transform.localPosition;

        // Initialize device state
        if (deviceObject != null)
        {
            deviceObject.SetActive(false);
            isDeviceActive = false;
        }
    }

    void Update()
    {
        HandleDeviceToggle();
        HandleZoom();
        HandleMovementFOV();
        HandleMouseLook();
        HandleViewBobbing();
        HandleObjectDragging();
        HandleDoorInteraction();
        
        ApplyFOV();
    }

    void HandleDeviceToggle()
    {
        if (Input.GetKeyDown(toggleDeviceKey))
        {
            if (deviceObject == null)
            {
                Debug.LogWarning("No device object assigned to CameraController!");
                return;
            }

            if (isDeviceActive)
            {
                DeactivateDevice();
            }
            else
            {
                ActivateDevice();
            }
        }
    }

    void ActivateDevice()
    {
        if (isDeviceActive || deviceObject == null) return;

        // Simply activate the device - it will handle its own orbiting behavior
        deviceObject.SetActive(true);
        isDeviceActive = true;

        PlayDeviceSound(activateSound);
    }

    void DeactivateDevice()
    {
        if (!isDeviceActive || deviceObject == null) return;

        // Simply deactivate the device
        deviceObject.SetActive(false);
        isDeviceActive = false;

        PlayDeviceSound(deactivateSound);
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
    }

    void HandleMovementFOV()
    {
        if (playerCamera == null || !enableMovementFOV || isZooming) return;

        // Check if moving forward (W key pressed)
        bool isMovingForward = Input.GetKey(KeyCode.W);
        
        // Check if crouching
        bool isCrouching = playerController != null && playerController.IsCrouching();

        // Apply movement FOV only when moving forward AND not crouching AND grounded
        if (playerController != null && playerController.IsGrounded() && isMovingForward && !isCrouching)
        {
            targetFOV = movementFOV;
        }
        else
        {
            targetFOV = defaultFOV;
        }
    }

    void ApplyFOV()
    {
        if (playerCamera == null) return;

        float currentFOV = playerCamera.fieldOfView;
        float interpolationSpeed = isZooming ? zoomSpeed : fovChangeSpeed;

        playerCamera.fieldOfView = Mathf.Lerp(currentFOV, targetFOV, interpolationSpeed * Time.deltaTime);
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

        if (playerController != null)
        {
            externalTilt = playerController.GetSlideTilt();
        }

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
            currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, bobbingTransitionSharpness * Time.deltaTime);
            ApplyBobbingOffset();
            return;
        }

        bool isGrounded = playerController.IsGrounded();
        Vector3 velocity = playerController.GetVelocity();
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        bool isMoving = horizontalSpeed > 0.1f;

        if (isGrounded && !wasGrounded)
        {
            bobbingTimer = 0f;
        }

        if (isGrounded && isMoving && !playerController.IsSliding())
        {
            if (playerController.IsCrouching())
            {
                targetBobbingIntensity = crouchBobbingIntensity;
                targetBobbingSpeed = crouchBobbingSpeed;
            }
            else
            {
                targetBobbingIntensity = walkBobbingIntensity;
                targetBobbingSpeed = walkBobbingSpeed;
            }

            float speedMultiplier = Mathf.Clamp01(horizontalSpeed / playerController.GetSpeed());
            targetBobbingIntensity *= speedMultiplier;
            targetBobbingIntensity *= 0.7f;
        }
        else
        {
            targetBobbingIntensity = 0f;
            targetBobbingSpeed = 0f;
        }

        currentBobbingIntensity = Mathf.Lerp(currentBobbingIntensity, targetBobbingIntensity, bobbingTransitionSharpness * Time.deltaTime);
        currentBobbingSpeed = Mathf.Lerp(currentBobbingSpeed, targetBobbingSpeed, bobbingTransitionSharpness * Time.deltaTime);

        Vector3 targetBobOffset = Vector3.zero;
        
        if (currentBobbingIntensity > 0.001f && currentBobbingSpeed > 0.1f)
        {
            bobbingTimer += Time.deltaTime * currentBobbingSpeed;
            
            float verticalBob = Mathf.Sin(bobbingTimer * 2f) * currentBobbingIntensity * 0.5f;
            float horizontalBob = Mathf.Cos(bobbingTimer) * currentBobbingIntensity * 0.3f;
            
            targetBobOffset = new Vector3(horizontalBob, verticalBob, 0f);
        }
        else
        {
            bobbingTimer = 0f;
        }

        currentBobOffset = Vector3.Lerp(currentBobOffset, targetBobOffset, bobbingTransitionSharpness * Time.deltaTime);
        
        ApplyBobbingOffset();

        wasGrounded = isGrounded;
    }

    void HandleObjectDragging()
    {
        if (!enableObjectDragging || playerCamera == null) return;

        if (Input.GetMouseButtonDown(0) && draggedObject == null)
        {
            TryPickUpObject();
        }

        if (Input.GetMouseButtonUp(0) && draggedObject != null)
        {
            DropObject();
        }

        if (draggedObject != null)
        {
            UpdateDraggedObject();
        }
    }

    void HandleDoorInteraction()
    {
        if (Input.GetKeyDown(interactKey))
        {
            TryInteractWithDoor();
        }
    }

    void TryInteractWithDoor()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, doorInteractionDistance))
        {
            Door door = hit.collider.GetComponentInParent<Door>();
            if (door != null && !door.IsLocked())
            {
                door.ToggleDoor();
            }
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
                draggedObject = rb;
                
                originalDrag = rb.linearDamping;
                originalAngularDrag = rb.angularDamping;
                
                rb.linearDamping = 10f;
                rb.angularDamping = 10f;
                
                rb.WakeUp();
            }
        }
    }

    void UpdateDraggedObject()
    {
        if (draggedObject == null) return;

        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * maxDragDistance * 0.7f;
        
        Vector3 currentPosition = draggedObject.position;
        Vector3 targetVelocity = (targetPosition - currentPosition) * dragStrength;
        
        draggedObject.linearVelocity = Vector3.Lerp(draggedObject.linearVelocity, targetVelocity, dragSmoothness * Time.deltaTime);
        
        Quaternion targetRotation = Quaternion.LookRotation(playerCamera.transform.forward);
        draggedObject.rotation = Quaternion.Slerp(draggedObject.rotation, targetRotation, rotationSmoothness * Time.deltaTime);
        
        draggedObject.angularVelocity = Vector3.Lerp(draggedObject.angularVelocity, Vector3.zero, dragSmoothness * Time.deltaTime);
    }

    void DropObject()
    {
        if (draggedObject != null)
        {
            draggedObject.linearDamping = originalDrag;
            draggedObject.angularDamping = originalAngularDrag;
            draggedObject = null;
        }
    }

    void ApplyBobbingOffset()
    {
        transform.localPosition = originalLocalPosition + currentBobOffset;
    }

    public void UpdateOriginalPosition()
    {
        originalLocalPosition = transform.localPosition;
    }

    void PlayDeviceSound(AudioClip clip)
    {
        if (deviceAudioSource == null || clip == null) return;

        deviceAudioSource.clip = clip;
        deviceAudioSource.volume = deviceVolume;
        deviceAudioSource.pitch = devicePitch;
        deviceAudioSource.loop = false;
        deviceAudioSource.Play();
    }

    public void SetExternalTilt(float tilt)
    {
        externalTilt = tilt;
    }

    public void SetViewBobbing(bool enabled)
    {
        enableViewBobbing = enabled;
    }

    public void SetObjectDragging(bool enabled)
    {
        enableObjectDragging = enabled;
        
        if (!enabled && draggedObject != null)
        {
            DropObject();
        }
    }

    public bool IsDraggingObject()
    {
        return draggedObject != null;
    }

    public bool IsDeviceActive()
    {
        return isDeviceActive;
    }

    public GameObject GetCurrentDevice()
    {
        return deviceObject;
    }

    public Rigidbody GetDraggedObject()
    {
        return draggedObject;
    }

    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Vector3 endPoint = playerCamera.transform.position + playerCamera.transform.forward * maxDragDistance;
            Gizmos.DrawLine(playerCamera.transform.position, endPoint);
            Gizmos.DrawWireSphere(endPoint, 0.1f);
            
            Gizmos.color = Color.green;
            Vector3 doorEndPoint = playerCamera.transform.position + playerCamera.transform.forward * doorInteractionDistance;
            Gizmos.DrawLine(playerCamera.transform.position, doorEndPoint);
            Gizmos.DrawWireSphere(doorEndPoint, 0.08f);
        }
    }
}