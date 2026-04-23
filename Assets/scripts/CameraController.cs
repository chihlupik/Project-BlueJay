using UnityEngine;
using UnityEngine.UI;

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

    [Header("Crosshair")]
    public Image crosshairImage;
    public float grappleRange = 20f;
    public LayerMask grappleLayerMask = -1;
    public Color defaultCrosshairColor = Color.white;
    public Color grappleCrosshairColor = Color.green;
    public float defaultCrosshairSize = 1f;
    public float maxGrappleCrosshairSize = 1.5f; // Maximum size when very close
    public float minGrappleCrosshairSize = 1.05f; // Minimum size at max range

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

    // Crosshair variables
    private bool isAimingAtGrapple = false;
    private float currentDistanceToGrapple = 0f;

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

        // Setup crosshair if not assigned
        if (crosshairImage == null)
        {
            TryFindCrosshair();
        }

        // Set default crosshair state
        if (crosshairImage != null)
        {
            crosshairImage.color = defaultCrosshairColor;
            crosshairImage.rectTransform.localScale = Vector3.one * defaultCrosshairSize;
        }
    }

    void TryFindCrosshair()
    {
        // Try to find crosshair in the scene
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            crosshairImage = canvas.GetComponentInChildren<Image>();
            if (crosshairImage != null && crosshairImage.name.ToLower().Contains("crosshair"))
            {
                return;
            }
        }

        // If still not found, create a simple crosshair
        CreateDefaultCrosshair();
    }

    void CreateDefaultCrosshair()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CrosshairCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(canvas.transform, false);
        
        crosshairImage = crosshairObj.AddComponent<Image>();
        
        // Create a simple crosshair texture if none exists
        Texture2D texture = new Texture2D(32, 32);
        Color32[] colors = new Color32[32 * 32];
        
        for (int i = 0; i < colors.Length; i++)
        {
            int x = i % 32;
            int y = i / 32;
            
            if (x >= 14 && x <= 17 && (y <= 5 || y >= 26)) // Vertical line
                colors[i] = Color.white;
            else if (y >= 14 && y <= 17 && (x <= 5 || x >= 26)) // Horizontal line
                colors[i] = Color.white;
            else
                colors[i] = Color.clear;
        }
        
        texture.SetPixels32(colors);
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        
        crosshairImage.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        crosshairImage.rectTransform.sizeDelta = new Vector2(32, 32);
        
        // Center the crosshair
        RectTransform rect = crosshairImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        HandleZoom();
        HandleMovementFOV();
        HandleMouseLook();
        HandleViewBobbing();
        HandleCrosshair();
        
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

    void HandleCrosshair()
    {
        if (crosshairImage == null) return;

        // Check if aiming at grapple point and get distance
        GrappleHitInfo grappleInfo = GetGrapplePointInfo();
        isAimingAtGrapple = grappleInfo.hitValid;

        if (isAimingAtGrapple)
        {
            // Calculate crosshair size based on distance
            // Closer = bigger, farther = smaller
            float t = 1f - (grappleInfo.distance / grappleRange);
            t = Mathf.Clamp01(t);
            
            float targetSize = Mathf.Lerp(minGrappleCrosshairSize, maxGrappleCrosshairSize, t);
            crosshairImage.rectTransform.localScale = Vector3.one * targetSize;
            crosshairImage.color = grappleCrosshairColor;
        }
        else
        {
            // Reset crosshair
            crosshairImage.rectTransform.localScale = Vector3.one * defaultCrosshairSize;
            crosshairImage.color = defaultCrosshairColor;
        }
    }

    GrappleHitInfo GetGrapplePointInfo()
    {
        if (playerCamera == null) return new GrappleHitInfo();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grappleRange, grappleLayerMask))
        {
            GrapplePoint grapplePoint = hit.collider.GetComponent<GrapplePoint>();
            if (grapplePoint != null && grapplePoint.IsAvailable())
            {
                return new GrappleHitInfo
                {
                    hitValid = true,
                    distance = hit.distance,
                    point = grapplePoint
                };
            }
        }

        return new GrappleHitInfo();
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

    public bool IsAimingAtGrapple()
    {
        return isAimingAtGrapple;
    }

    // Helper struct to return grapple info
    private struct GrappleHitInfo
    {
        public bool hitValid;
        public float distance;
        public GrapplePoint point;
    }
}