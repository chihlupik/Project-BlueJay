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
    
    [Header("View Bobbing")]
    public bool enableViewBobbing = true;
    public float walkBobbingIntensity = 0.015f;
    public float crouchBobbingIntensity = 0.008f;
    public float walkBobbingSpeed = 3f;
    public float crouchBobbingSpeed = 2f;
    public float bobbingTransitionSharpness = 8f;

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
        originalLocalPosition = transform.localPosition;
    }

    void Update()
    {
        HandleZoom();
        HandleMovementFOV();
        HandleMouseLook();
        HandleViewBobbing();
        
        ApplyFOV();
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

        bool isMovingForward = Input.GetKey(KeyCode.W);
        bool isCrouching = playerController != null && playerController.IsCrouching();

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

    void ApplyBobbingOffset()
    {
        transform.localPosition = originalLocalPosition + currentBobOffset;
    }

    public void UpdateOriginalPosition()
    {
        originalLocalPosition = transform.localPosition;
    }

    public void SetExternalTilt(float tilt)
    {
        externalTilt = tilt;
    }

    public void SetViewBobbing(bool enabled)
    {
        enableViewBobbing = enabled;
    }
}