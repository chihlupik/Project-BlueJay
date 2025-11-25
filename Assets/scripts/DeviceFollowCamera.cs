using UnityEngine;

public class DeviceFollowCamera : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform playerCamera;
    
    [Header("Default State Settings")]
    public Vector3 defaultOrbitOffset = Vector3.zero; // Position relative to camera
    public Vector3 defaultRotationOffset = Vector3.zero;
    public float defaultPositionSmoothness = 5f;
    public float defaultRotationSmoothness = 5f;
    
    [Header("Right Mouse Button State Settings")]
    public Vector3 rightMouseOrbitOffset = Vector3.zero;
    public Vector3 rightMouseRotationOffset = Vector3.zero;
    public float rightMousePositionSmoothness = 5f;
    public float rightMouseRotationSmoothness = 5f;
    
    [Header("Follow Options")]
    public bool followPosition = true;
    public bool followRotation = true;
    public bool orbitAroundCamera = true; // New: Device orbits around camera instead of moving with it
    
    [Header("Input Settings")]
    public KeyCode rightMouseKey = KeyCode.Mouse1;
    public bool rightMouseToggle = false;
    
    // Current state
    private bool isRightMouseState = false;
    private Vector3 currentOrbitOffset;
    private Vector3 currentRotationOffset;
    private float currentPositionSmoothness;
    private float currentRotationSmoothness;
    
    // Orbit system
    private Vector3 currentOrbitPosition;
    
    private void Start()
    {
        // If player camera is not assigned, try to find it automatically
        if (playerCamera == null)
        {
            // Look for the main camera
            playerCamera = Camera.main?.transform;
            
            // If no main camera found, try to find any camera in the scene
            if (playerCamera == null)
            {
                Camera cam = FindObjectOfType<Camera>();
                if (cam != null)
                {
                    playerCamera = cam.transform;
                }
            }
            
            if (playerCamera != null)
            {
                Debug.Log("DeviceFollowCamera: Found camera automatically - " + playerCamera.name);
            }
            else
            {
                Debug.LogWarning("DeviceFollowCamera: No camera found! Please assign a camera transform.");
            }
        }
        
        // Set initial state to default
        SetToDefaultState();
        
        // Initialize orbit position
        currentOrbitPosition = CalculateOrbitPosition();
    }
    
    private void Update()
    {
        if (playerCamera == null)
            return;
            
        HandleInput();
        FollowCamera();
    }
    
    private void HandleInput()
    {
        if (rightMouseToggle)
        {
            // Toggle mode - switch state on click
            if (Input.GetKeyDown(rightMouseKey))
            {
                isRightMouseState = !isRightMouseState;
                UpdateCurrentState();
            }
        }
        else
        {
            // Hold mode - switch state while holding
            bool rightMousePressed = Input.GetKey(rightMouseKey);
            
            if (rightMousePressed != isRightMouseState)
            {
                isRightMouseState = rightMousePressed;
                UpdateCurrentState();
            }
        }
    }
    
    private void UpdateCurrentState()
    {
        if (isRightMouseState)
        {
            // Use right mouse button parameters
            currentOrbitOffset = rightMouseOrbitOffset;
            currentRotationOffset = rightMouseRotationOffset;
            currentPositionSmoothness = rightMousePositionSmoothness;
            currentRotationSmoothness = rightMouseRotationSmoothness;
        }
        else
        {
            // Use default parameters
            currentOrbitOffset = defaultOrbitOffset;
            currentRotationOffset = defaultRotationOffset;
            currentPositionSmoothness = defaultPositionSmoothness;
            currentRotationSmoothness = defaultRotationSmoothness;
        }
    }
    
    private void FollowCamera()
    {
        if (followPosition)
        {
            Vector3 targetPosition;
            
            if (orbitAroundCamera)
            {
                // Calculate orbit position around camera
                targetPosition = CalculateOrbitPosition();
                
                // Smoothly interpolate to target orbit position
                currentOrbitPosition = Vector3.Lerp(currentOrbitPosition, targetPosition, currentPositionSmoothness * Time.deltaTime);
                transform.position = currentOrbitPosition;
            }
            else
            {
                // Old behavior: follow camera directly
                targetPosition = playerCamera.position + currentOrbitOffset;
                transform.position = Vector3.Lerp(transform.position, targetPosition, currentPositionSmoothness * Time.deltaTime);
            }
        }
        
        if (followRotation)
        {
            Quaternion targetRotation;
            
            if (orbitAroundCamera)
            {
                // Make device face the camera while maintaining rotation offset
                Vector3 directionToCamera = playerCamera.position - transform.position;
                if (directionToCamera != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
                    targetRotation = lookRotation * Quaternion.Euler(currentRotationOffset);
                }
                else
                {
                    targetRotation = playerCamera.rotation * Quaternion.Euler(currentRotationOffset);
                }
            }
            else
            {
                // Old behavior: match camera rotation
                targetRotation = playerCamera.rotation * Quaternion.Euler(currentRotationOffset);
            }
            
            // Smoothly interpolate to target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotationSmoothness * Time.deltaTime);
        }
    }
    
    private Vector3 CalculateOrbitPosition()
    {
        // Calculate position in orbit around camera based on camera's rotation
        return playerCamera.position + 
               playerCamera.right * currentOrbitOffset.x +
               playerCamera.up * currentOrbitOffset.y +
               playerCamera.forward * currentOrbitOffset.z;
    }
    
    // Method to instantly snap to camera orbit position/rotation
    public void SnapToCamera()
    {
        if (playerCamera == null)
            return;
            
        if (followPosition)
        {
            if (orbitAroundCamera)
            {
                currentOrbitPosition = CalculateOrbitPosition();
                transform.position = currentOrbitPosition;
            }
            else
            {
                transform.position = playerCamera.position + currentOrbitOffset;
            }
        }
        
        if (followRotation)
        {
            Quaternion targetRotation;
            
            if (orbitAroundCamera)
            {
                Vector3 directionToCamera = playerCamera.position - transform.position;
                if (directionToCamera != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
                    targetRotation = lookRotation * Quaternion.Euler(currentRotationOffset);
                }
                else
                {
                    targetRotation = playerCamera.rotation * Quaternion.Euler(currentRotationOffset);
                }
            }
            else
            {
                targetRotation = playerCamera.rotation * Quaternion.Euler(currentRotationOffset);
            }
            
            transform.rotation = targetRotation;
        }
    }
    
    // Method to set to default state
    public void SetToDefaultState()
    {
        isRightMouseState = false;
        UpdateCurrentState();
    }
    
    // Method to set to right mouse state
    public void SetToRightMouseState()
    {
        isRightMouseState = true;
        UpdateCurrentState();
    }
    
    // Method to toggle between states
    public void ToggleState()
    {
        isRightMouseState = !isRightMouseState;
        UpdateCurrentState();
    }
    
    // Method to update the target camera at runtime
    public void SetTargetCamera(Transform newCamera)
    {
        playerCamera = newCamera;
    }
    
    // Method to update default offsets at runtime
    public void SetDefaultOffsets(Vector3 newOrbitOffset, Vector3 newRotationOffset)
    {
        defaultOrbitOffset = newOrbitOffset;
        defaultRotationOffset = newRotationOffset;
        if (!isRightMouseState) UpdateCurrentState();
    }
    
    // Method to update right mouse offsets at runtime
    public void SetRightMouseOffsets(Vector3 newOrbitOffset, Vector3 newRotationOffset)
    {
        rightMouseOrbitOffset = newOrbitOffset;
        rightMouseRotationOffset = newRotationOffset;
        if (isRightMouseState) UpdateCurrentState();
    }
    
    // Method to update default smoothness at runtime
    public void SetDefaultSmoothness(float positionSmoothness, float rotationSmoothness)
    {
        defaultPositionSmoothness = positionSmoothness;
        defaultRotationSmoothness = rotationSmoothness;
        if (!isRightMouseState) UpdateCurrentState();
    }
    
    // Method to update right mouse smoothness at runtime
    public void SetRightMouseSmoothness(float positionSmoothness, float rotationSmoothness)
    {
        rightMousePositionSmoothness = positionSmoothness;
        rightMouseRotationSmoothness = rotationSmoothness;
        if (isRightMouseState) UpdateCurrentState();
    }
    
    // Method to toggle orbit mode
    public void SetOrbitMode(bool enableOrbit)
    {
        orbitAroundCamera = enableOrbit;
        if (enableOrbit)
        {
            currentOrbitPosition = CalculateOrbitPosition();
        }
    }
    
    // Getters for current settings
    public Transform GetTargetCamera() => playerCamera;
    public Vector3 GetCurrentOrbitOffset() => currentOrbitOffset;
    public Vector3 GetCurrentRotationOffset() => currentRotationOffset;
    public float GetCurrentPositionSmoothness() => currentPositionSmoothness;
    public float GetCurrentRotationSmoothness() => currentRotationSmoothness;
    public bool IsInRightMouseState() => isRightMouseState;
    public bool IsInOrbitMode() => orbitAroundCamera;
    
    // Getters for preset values
    public Vector3 GetDefaultOrbitOffset() => defaultOrbitOffset;
    public Vector3 GetDefaultRotationOffset() => defaultRotationOffset;
    public Vector3 GetRightMouseOrbitOffset() => rightMouseOrbitOffset;
    public Vector3 GetRightMouseRotationOffset() => rightMouseRotationOffset;
}