using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float speed = 6f;
    public float crouchSpeed = 3f;
    public float jumpSpeed = 8f;
    public float gravity = 20.0f;
    public float speedTransitionSharpness = 10f;
    public float airControlMultiplier = 0.5f;
    public float jumpCooldown = 0.3f;
    public float crouchCooldown = 0.2f;
    public float groundAcceleration = 12f;
    public float groundDeceleration = 16f;
    public float airAcceleration = 8f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 8f;
    
    public float slideSpeed = 15f;
    public float slideDuration = 1f;
    public float slideCooldown = 1.5f;
    public float slideHeight = 0.5f;
    
    [Header("Dive Settings")]
    public float diveSpeed = 18f; // Increased for more distance
    public float diveDuration = 1.2f; // Increased duration
    public float diveCooldown = 1f;
    public float diveHeight = 0.5f;
    public float diveControlMultiplier = 0.05f; // Almost no control for straight trajectory
    public float diveJumpMultiplier = 1.5f;
    public float diveGravityMultiplier = 0.2f; // Greatly reduced gravity during dive
    public bool lockDiveDirection = true; // Lock direction once dive starts
    public float diveForwardBoost = 1.5f; // Extra forward boost on dive
    
    [Header("Double Jump Settings")]
    public bool enableDoubleJump = true;
    public int maxAirJumps = 1;
    public float doubleJumpSpeed = 8f;
    public float doubleJumpCooldown = 0.2f;
    
    [Header("Directional Speed Settings")]
    public float forwardSpeedMultiplier = 1.0f;
    public float backwardSpeedMultiplier = 0.6f;
    public float strafeSpeedMultiplier = 0.8f;
    
    [Header("Slide Decay Settings")]
    public float slideDecayRate = 0.8f;
    public float minSlideSpeed = 3f;
    
    [Header("Dive Decay Settings")]
    public float diveDecayRate = 0.5f; // Slower decay for longer dive
    public float minDiveSpeed = 8f; // Higher min speed
    
    public float standingCameraHeight = 1.6f;
    public float crouchingCameraHeight = 0.8f;
    public float slidingCameraHeight = 0.5f;
    public float divingCameraHeight = 0.3f;
    
    public float slideTiltAngle = 10f;
    public float tiltTransitionSpeed = 5f;
    
    [Header("Movement Control During Actions")]
    public float slideControlMultiplier = 0.4f;
    
    [Header("Footstep Sounds")]
    public AudioSource footstepAudioSource;
    public AudioClip walkFootstepSound;
    public AudioClip slideSound;
    public AudioClip jumpSound;
    public AudioClip doubleJumpSound;
    public AudioClip landingSound;
    public AudioClip diveSound;
    
    [Header("Footstep Timing")]
    public float walkStepInterval = 0.5f;
    public float crouchStepInterval = 0.7f;
    
    [Header("Footstep Audio Settings")]
    public float pitchRandomness = 0.1f;
    public float walkVolume = 0.8f;
    public float crouchVolume = 0.6f;
    public float walkPitch = 1.0f;
    public float crouchPitch = 0.8f;
    
    [Header("Jump Sound Settings")]
    public float jumpVolume = 0.8f;
    public float jumpPitch = 1.0f;
    
    [Header("Double Jump Sound Settings")]
    public float doubleJumpVolume = 0.8f;
    public float doubleJumpPitch = 1.2f;
    
    [Header("Dive Sound Settings")]
    public float diveVolume = 1.0f;
    public float divePitch = 1.2f;
    
    [Header("Landing Sound Settings")]
    public float landingThreshold = 2.0f;
    public float landingBaseVolume = 0.5f;
    public float landingSpeedVolume = 0.5f;
    public float landingPitchMin = 0.9f;
    public float landingPitchMax = 1.1f;
    public float maxLandingVelocity = 10f;
    
    [Header("Slide Sound Settings")]
    public float slideVolume = 1.0f;
    public float slidePitch = 1.0f;
    
    private CharacterController controller;
    private CapsuleCollider capsuleCollider;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 currentVelocity;
    private bool isCrouching = false;
    private bool isSliding = false;
    private bool isDiving = false;
    private bool wantsToCrouch = false;
    private float currentSpeed;
    private float jumpCooldownTimer = 0f;
    private float crouchCooldownTimer = 0f;
    private float standingHeight;
    private float currentHeight;
    private float targetHeight;
    private Vector3 standingCenter;

    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;
    private Vector3 slideDirection;
    private float currentSlideSpeed;
    private bool isSlideFromDive = false;

    private float diveTimer = 0f;
    private float diveCooldownTimer = 0f;
    private Vector3 diveDirection;
    private float currentDiveSpeed;
    private float originalGravity; // Store original gravity for dive

    // Double jump tracking
    private int airJumpsRemaining = 0;
    private bool hasDoubleJumped = false;
    private float doubleJumpCooldownTimer = 0f;

    private CameraController cameraController;
    private float currentSlideTilt = 0f;
    private float targetSlideTilt = 0f;

    private float stepTimer = 0f;
    private bool wasGrounded = false;
    private bool hasLanded = false;

    public Transform cameraTransform;
    public Transform groundCheck;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        cameraController = GetComponentInChildren<CameraController>();
        
        currentSpeed = speed;
        currentVelocity = Vector3.zero;
        originalGravity = gravity;
        
        standingHeight = controller.height;
        standingCenter = controller.center;
        
        currentHeight = standingHeight;
        targetHeight = standingHeight;

        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }
        
        UpdateCameraHeight();
        
        // Initialize air jumps
        airJumpsRemaining = maxAirJumps;
    }

    void Update()
    {
        // Update timers
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }
        
        if (doubleJumpCooldownTimer > 0f)
        {
            doubleJumpCooldownTimer -= Time.deltaTime;
        }
        
        if (crouchCooldownTimer > 0f)
        {
            crouchCooldownTimer -= Time.deltaTime;
        }

        if (slideCooldownTimer > 0f)
        {
            slideCooldownTimer -= Time.deltaTime;
        }

        if (diveCooldownTimer > 0f)
        {
            diveCooldownTimer -= Time.deltaTime;
        }

        HandleCrouchInput();
        HandleSliding();
        HandleDiveInput();
        HandleCrouch();
        
        HandleMovement();
        
        HandleSlideTilt();
        HandleFootsteps();
        
        if (isCrouching && !isSliding && !isDiving && !wantsToCrouch && CanStandUp())
        {
            AutoStandUp();
        }
        
        // Reset air jumps when grounded
        if (controller.isGrounded)
        {
            airJumpsRemaining = maxAirJumps;
            hasDoubleJumped = false;
        }
    }

    void HandleCrouchInput()
    {
        // Crouch on SHIFT key
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && crouchCooldownTimer <= 0f)
        {
            wantsToCrouch = true;
            
            if (controller.isGrounded && IsSlidingAllowed())
            {
                StartSlide();
                crouchCooldownTimer = crouchCooldown;
            }
            else if (!isCrouching)
            {
                isCrouching = true;
                targetHeight = crouchHeight;
                crouchCooldownTimer = crouchCooldown;
            }
        }
        
        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            wantsToCrouch = false;
            
            if (isSliding && !isSlideFromDive)
            {
                EndSlide();
                crouchCooldownTimer = crouchCooldown;
            }
            else if (isCrouching && !isSliding && !isDiving && CanStandUp())
            {
                AutoStandUp();
            }
        }
    }

    void AutoStandUp()
    {
        if (isCrouching && !isSliding && !isDiving && CanStandUp() && crouchCooldownTimer <= 0f)
        {
            isCrouching = false;
            targetHeight = standingHeight;
            crouchCooldownTimer = crouchCooldown;
        }
    }

    void HandleSliding()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            float normalizedTime = 1f - (slideTimer / slideDuration);
            currentSlideSpeed = Mathf.Lerp(slideSpeed, minSlideSpeed, normalizedTime * slideDecayRate);
            
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (input.magnitude > 0.1f)
            {
                Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
                desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
                
                Vector3 steerDirection = Vector3.Lerp(slideDirection, desiredMoveDirection, slideControlMultiplier * Time.deltaTime);
                slideDirection = steerDirection.normalized;
            }
            
            currentVelocity = slideDirection * currentSlideSpeed;
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;

            bool shouldCancelSlide = false;

            if (isSlideFromDive)
            {
                if (wantsToCrouch)
                {
                    shouldCancelSlide = true;
                }
            }
            else
            {
                if (!wantsToCrouch)
                {
                    shouldCancelSlide = true;
                }
            }
            
            if (slideTimer <= 0f || Input.GetButton("Jump"))
            {
                shouldCancelSlide = true;
            }
            else if (CheckWallCollision())
            {
                shouldCancelSlide = true;
            }

            if (shouldCancelSlide && crouchCooldownTimer <= 0f)
            {
                EndSlide();
                crouchCooldownTimer = crouchCooldown;
            }
        }
    }

    void HandleDiveInput()
    {
        // Handle dive - E key press while in air
        if (Input.GetKeyDown(KeyCode.E) && !controller.isGrounded && diveCooldownTimer <= 0f)
        {
            StartDive();
        }
        
        if (isDiving)
        {
            HandleDive();
        }
    }

    void HandleDive()
    {
        diveTimer -= Time.deltaTime;

        // Apply speed decay for dive
        float normalizedTime = 1f - (diveTimer / diveDuration);
        currentDiveSpeed = Mathf.Lerp(diveSpeed, minDiveSpeed, normalizedTime * diveDecayRate);
        
        // Minimal control during dive for straight trajectory
        if (!lockDiveDirection)
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (input.magnitude > 0.1f)
            {
                Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
                desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
                
                // Very minimal steering for straight dive
                Vector3 steerDirection = Vector3.Lerp(diveDirection, desiredMoveDirection, diveControlMultiplier * Time.deltaTime);
                diveDirection = steerDirection.normalized;
            }
        }
        
        currentVelocity = diveDirection * currentDiveSpeed;
        moveDirection.x = currentVelocity.x;
        moveDirection.z = currentVelocity.z;

        // Apply reduced gravity during dive for straighter trajectory
        float diveGravity = gravity * diveGravityMultiplier;
        moveDirection.y -= diveGravity * Time.deltaTime;

        // End dive when timer runs out or we hit the ground
        if (diveTimer <= 0f || controller.isGrounded)
        {
            EndDive();
            
            if (controller.isGrounded && !isSliding && slideCooldownTimer <= 0f)
            {
                StartSlideFromDive();
            }
        }
    }

    bool CheckWallCollision()
    {
        float checkDistance = 0.5f;
        float sphereRadius = 0.3f;
        Vector3 castOrigin = transform.position + Vector3.up * 0.1f;
        
        if (Physics.SphereCast(castOrigin, sphereRadius, slideDirection, out RaycastHit hit, checkDistance))
        {
            if (!hit.collider.isTrigger && Vector3.Angle(hit.normal, Vector3.up) > 60f)
            {
                return true;
            }
        }
        
        return false;
    }

    bool IsSlidingAllowed()
    {
        bool isMovingForward = Input.GetKey(KeyCode.W) || Input.GetAxis("Vertical") > 0.1f;
        
        return controller.isGrounded && 
               slideCooldownTimer <= 0f && 
               crouchCooldownTimer <= 0f &&
               !isSliding &&
               currentVelocity.magnitude > speed * 0.7f &&
               isMovingForward;
    }

    void StartSlide()
    {
        isSliding = true;
        isSlideFromDive = false;
        isCrouching = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;
        
        slideDirection = transform.forward;
        
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        if (horizontalVelocity.magnitude > 0.5f)
        {
            slideDirection = horizontalVelocity.normalized;
        }

        targetHeight = slideHeight;
        currentSlideSpeed = slideSpeed;
        
        targetSlideTilt = -slideTiltAngle;
    }

    void StartSlideFromDive()
    {
        isSliding = true;
        isSlideFromDive = true;
        isCrouching = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;
        
        slideDirection = diveDirection;
        currentSlideSpeed = Mathf.Max(currentDiveSpeed * 0.8f, minSlideSpeed);

        targetHeight = slideHeight;
        targetSlideTilt = -slideTiltAngle;
        
        PlaySlideSound();
    }

    void StartDive()
    {
        isDiving = true;
        isCrouching = true;
        diveTimer = diveDuration;
        diveCooldownTimer = diveCooldown;
        
        // Lock dive direction to forward with optional boost
        diveDirection = transform.forward;
        
        // Apply forward boost if enabled
        if (diveForwardBoost > 1f)
        {
            diveDirection *= diveForwardBoost;
        }
        
        // Use current movement direction if moving and not locking direction
        if (!lockDiveDirection)
        {
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            if (horizontalVelocity.magnitude > 0.5f)
            {
                diveDirection = horizontalVelocity.normalized;
            }
        }

        targetHeight = diveHeight;
        currentDiveSpeed = diveSpeed;
        
        // Apply jump boost if diving from a jump
        if (moveDirection.y > 0)
        {
            moveDirection.y *= diveJumpMultiplier;
        }
        else
        {
            // Give a slight upward boost if diving from neutral/falling
            moveDirection.y = jumpSpeed * 0.5f;
        }
        
        PlayDiveSound();
    }

    void EndSlide()
    {
        isSliding = false;
        isSlideFromDive = false;
        
        targetSlideTilt = 0f;
        
        if (wantsToCrouch)
        {
            targetHeight = crouchHeight;
            isCrouching = true;
        }
        else
        {
            if (CanStandUp())
            {
                AutoStandUp();
            }
            else
            {
                targetHeight = crouchHeight;
                isCrouching = true;
            }
        }
    }

    void EndDive()
    {
        isDiving = false;
        
        if (wantsToCrouch)
        {
            targetHeight = crouchHeight;
            isCrouching = true;
        }
        else
        {
            if (CanStandUp())
            {
                AutoStandUp();
            }
            else
            {
                targetHeight = crouchHeight;
                isCrouching = true;
            }
        }
    }

    void HandleSlideTilt()
    {
        currentSlideTilt = Mathf.Lerp(currentSlideTilt, targetSlideTilt, tiltTransitionSpeed * Time.deltaTime);
        
        if (cameraController != null)
        {
            ApplySlideTiltToCamera();
        }
        else
        {
            ApplySlideTiltDirect();
        }
    }

    void ApplySlideTiltToCamera()
    {
        try
        {
            var slideTiltField = cameraController.GetType().GetField("externalTilt");
            if (slideTiltField != null)
            {
                slideTiltField.SetValue(cameraController, currentSlideTilt);
            }
        }
        catch
        {
            ApplySlideTiltDirect();
        }
    }

    void ApplySlideTiltDirect()
    {
        if (cameraTransform != null)
        {
            Vector3 currentRotation = cameraTransform.localEulerAngles;
            cameraTransform.localEulerAngles = new Vector3(
                currentRotation.x, 
                currentRotation.y, 
                currentSlideTilt
            );
        }
    }

    void HandleCrouch()
    {
        if (isSliding)
        {
            targetHeight = slideHeight;
        }
        else if (isDiving)
        {
            targetHeight = diveHeight;
        }
        else if (isCrouching)
        {
            targetHeight = crouchHeight;
        }
        else
        {
            targetHeight = standingHeight;
        }

        UpdateHeight();
        UpdateCameraHeight();
    }

    void UpdateHeight()
    {
        float previousHeight = currentHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2f, 0);

        if (capsuleCollider != null)
        {
            capsuleCollider.height = currentHeight;
            capsuleCollider.center = new Vector3(0, currentHeight / 2f, 0);
        }

        if (currentHeight < previousHeight)
        {
            float heightDifference = previousHeight - currentHeight;
            transform.position += new Vector3(0, heightDifference / 2f, 0);
        }

        if (groundCheck != null)
        {
            groundCheck.localPosition = new Vector3(0, 0.1f, 0);
        }
    }

    void UpdateCameraHeight()
    {
        if (cameraTransform != null)
        {
            float targetCameraHeight;
            
            if (isSliding)
            {
                targetCameraHeight = slidingCameraHeight;
            }
            else if (isDiving)
            {
                targetCameraHeight = divingCameraHeight;
            }
            else if (isCrouching)
            {
                targetCameraHeight = crouchingCameraHeight;
            }
            else
            {
                targetCameraHeight = standingCameraHeight;
            }
            
            Vector3 currentCameraPos = cameraTransform.localPosition;
            Vector3 targetCameraPos = new Vector3(0, targetCameraHeight, 0);
            cameraTransform.localPosition = Vector3.Lerp(currentCameraPos, targetCameraPos, crouchTransitionSpeed * Time.deltaTime);

            CameraController cameraController = cameraTransform.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.UpdateOriginalPosition();
            }
        }
    }

    bool CanStandUp()
    {
        if (!isCrouching || isSliding || isDiving) return false;
        
        float checkDistance = standingHeight - currentHeight + 0.2f; 
        Vector3 rayStart = transform.position + Vector3.up * (currentHeight + 0.1f);
        
        float checkRadius = capsuleCollider != null ? capsuleCollider.radius * 0.8f : 0.3f;
        
        if (Physics.Raycast(rayStart, Vector3.up, out RaycastHit hit, checkDistance))
        {
            return false;
        }
        
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized
        };
        
        foreach (Vector3 dir in directions)
        {
            Vector3 offsetStart = rayStart + dir * checkRadius;
            if (Physics.Raycast(offsetStart, Vector3.up, out RaycastHit offsetHit, checkDistance))
            {
                return false;
            }
        }
        
        if (capsuleCollider != null)
        {
            Vector3 point1 = transform.position + Vector3.up * capsuleCollider.radius;
            Vector3 point2 = transform.position + Vector3.up * (standingHeight - capsuleCollider.radius);
            float radius = capsuleCollider.radius * 0.9f;
            
            if (Physics.CapsuleCast(point1, point2, radius, Vector3.up, out RaycastHit capsuleHit, checkDistance))
            {
                return false;
            }
        }
        
        return true;
    }

    void HandleMovement()
    {
        if (isSliding || isDiving)
        {
            // For dive, gravity is already handled in HandleDive()
            if (!isDiving)
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }
            controller.Move(moveDirection * Time.deltaTime);
            return;
        }

        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        
        if (input.magnitude > 1f)
        {
            input.Normalize();
        }
        
        Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
        desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
        
        float targetMovementSpeed;
        
        if (isCrouching && controller.isGrounded)
        {
            targetMovementSpeed = crouchSpeed;
        }
        else
        {
            targetMovementSpeed = speed;
        }
        
        targetMovementSpeed = ApplyDirectionalSpeedMultipliers(targetMovementSpeed);
        
        currentSpeed = Mathf.Lerp(currentSpeed, targetMovementSpeed, speedTransitionSharpness * Time.deltaTime);

        if (controller.isGrounded)
        {
            Vector3 targetVelocity = desiredMoveDirection * currentSpeed;
            
            if (input.magnitude > 0.1f)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, groundAcceleration * Time.deltaTime);
            }
            else
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, groundDeceleration * Time.deltaTime);
            }
            
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;

            if (Input.GetButton("Jump") && jumpCooldownTimer <= 0f && !isCrouching)
            {
                PlayJumpSound();
                moveDirection.y = jumpSpeed;
                jumpCooldownTimer = jumpCooldown;
                
                Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                if (horizontalVelocity.magnitude > currentSpeed)
                {
                    horizontalVelocity = horizontalVelocity.normalized * currentSpeed;
                }
                currentVelocity = horizontalVelocity;
                
                airJumpsRemaining = maxAirJumps;
            }
            else
            {
                moveDirection.y = -0.1f;
            }
        }
        else
        {
            if (enableDoubleJump && Input.GetButtonDown("Jump") && airJumpsRemaining > 0 && doubleJumpCooldownTimer <= 0f && !isCrouching)
            {
                PerformDoubleJump();
            }
            
            Vector3 targetAirVelocity = desiredMoveDirection * currentSpeed;
            
            if (input.magnitude > 0.1f)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, targetAirVelocity, airAcceleration * Time.deltaTime);
            }
            else
            {
                currentVelocity = Vector3.Lerp(currentVelocity, new Vector3(currentVelocity.x, 0, currentVelocity.z), airAcceleration * 0.3f * Time.deltaTime);
            }
            
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;
        }

        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
    }

    void PerformDoubleJump()
    {
        moveDirection.y = doubleJumpSpeed;
        
        airJumpsRemaining--;
        hasDoubleJumped = true;
        doubleJumpCooldownTimer = doubleJumpCooldown;
        
        PlayDoubleJumpSound();
    }

    float ApplyDirectionalSpeedMultipliers(float baseSpeed)
    {
        bool isMovingForward = Input.GetKey(KeyCode.W);
        bool isMovingBackward = Input.GetKey(KeyCode.S);
        bool isMovingLeft = Input.GetKey(KeyCode.A);
        bool isMovingRight = Input.GetKey(KeyCode.D);

        if (!isMovingForward && !isMovingBackward && !isMovingLeft && !isMovingRight)
            return baseSpeed;

        float speedMultiplier = 1.0f;

        if (isMovingForward && !isMovingBackward)
        {
            speedMultiplier = forwardSpeedMultiplier;
        }
        else if (isMovingBackward && !isMovingForward)
        {
            speedMultiplier = backwardSpeedMultiplier;
        }
        else if ((isMovingLeft || isMovingRight) && !isMovingForward && !isMovingBackward)
        {
            speedMultiplier = strafeSpeedMultiplier;
        }
        else
        {
            if (isMovingBackward)
            {
                speedMultiplier = backwardSpeedMultiplier;
            }
            else
            {
                speedMultiplier = Mathf.Min(forwardSpeedMultiplier, strafeSpeedMultiplier);
            }
        }

        return baseSpeed * speedMultiplier;
    }

    void HandleFootsteps()
    {
        if (footstepAudioSource == null || walkFootstepSound == null) return;

        bool isGrounded = controller.isGrounded;
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        
        bool hasMovementInput = input.magnitude > 0.1f;
        bool isMoving = currentVelocity.magnitude > 0.1f;
        
        bool shouldPlayFootsteps = isGrounded && hasMovementInput && isMoving && !isSliding && !isDiving;

        if (isGrounded && !wasGrounded)
        {
            float landingVelocity = Mathf.Abs(moveDirection.y);
            
            if (landingVelocity > landingThreshold)
            {
                PlayLandingSound(landingVelocity);
                hasLanded = true;
            }
        }

        if (shouldPlayFootsteps)
        {
            float stepInterval = GetStepInterval();
            
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                PlayFootstepSound();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
            
            if (footstepAudioSource.isPlaying && footstepAudioSource.clip == walkFootstepSound)
            {
                footstepAudioSource.Stop();
            }
        }

        if (isSliding)
        {
            if (!footstepAudioSource.isPlaying || footstepAudioSource.clip != slideSound)
            {
                PlaySlideSound();
            }
        }
        else if (isDiving)
        {
            if (!footstepAudioSource.isPlaying || footstepAudioSource.clip != diveSound)
            {
                PlayDiveSound();
            }
        }
        else if (footstepAudioSource.clip == slideSound && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop();
        }

        wasGrounded = isGrounded;
    }

    float GetStepInterval()
    {
        if (isCrouching)
            return crouchStepInterval;
        else
            return walkStepInterval;
    }

    void PlayFootstepSound()
    {
        if (footstepAudioSource == null || walkFootstepSound == null) return;

        footstepAudioSource.clip = walkFootstepSound;
        
        float basePitch = walkPitch;
        float baseVolume = walkVolume;

        if (isCrouching)
        {
            basePitch = crouchPitch;
            baseVolume = crouchVolume;
        }

        footstepAudioSource.pitch = basePitch + Random.Range(-pitchRandomness, pitchRandomness);
        footstepAudioSource.volume = baseVolume;
        footstepAudioSource.loop = false;

        footstepAudioSource.Play();
    }

    void PlaySlideSound()
    {
        if (footstepAudioSource == null || slideSound == null) return;

        footstepAudioSource.clip = slideSound;
        footstepAudioSource.pitch = slidePitch;
        footstepAudioSource.volume = slideVolume;
        footstepAudioSource.loop = true;
        footstepAudioSource.Play();
    }

    void PlayDiveSound()
    {
        if (footstepAudioSource == null || diveSound == null) return;

        footstepAudioSource.clip = diveSound;
        footstepAudioSource.pitch = divePitch;
        footstepAudioSource.volume = diveVolume;
        footstepAudioSource.loop = false;
        footstepAudioSource.Play();
    }

    void PlayJumpSound()
    {
        if (footstepAudioSource == null || jumpSound == null) return;

        footstepAudioSource.clip = jumpSound;
        footstepAudioSource.pitch = jumpPitch;
        footstepAudioSource.volume = jumpVolume;
        footstepAudioSource.loop = false;
        footstepAudioSource.Play();
    }

    void PlayDoubleJumpSound()
    {
        if (footstepAudioSource == null || doubleJumpSound == null) return;

        footstepAudioSource.clip = doubleJumpSound;
        footstepAudioSource.pitch = doubleJumpPitch;
        footstepAudioSource.volume = doubleJumpVolume;
        footstepAudioSource.loop = false;
        footstepAudioSource.Play();
    }

    void PlayLandingSound(float landingVelocity)
    {
        if (footstepAudioSource == null || landingSound == null) return;

        float landingIntensity = Mathf.Clamp01(landingVelocity / maxLandingVelocity);
        
        footstepAudioSource.clip = landingSound;
        footstepAudioSource.pitch = Random.Range(landingPitchMin, landingPitchMax);
        footstepAudioSource.volume = landingBaseVolume + (landingIntensity * landingSpeedVolume);
        footstepAudioSource.loop = false;
        footstepAudioSource.Play();
    }

    public float GetSlideTilt()
    {
        return currentSlideTilt;
    }

    public bool IsSliding()
    {
        return isSliding;
    }

    public bool IsDiving()
    {
        return isDiving;
    }

    public bool IsGrounded()
    {
        return controller.isGrounded;
    }

    public Vector3 GetVelocity()
    {
        return currentVelocity;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }

    public float GetSpeed()
    {
        return speed;
    }
    
    public int GetRemainingAirJumps()
    {
        return airJumpsRemaining;
    }
    
    public bool CanDoubleJump()
    {
        return enableDoubleJump && airJumpsRemaining > 0 && doubleJumpCooldownTimer <= 0f && !controller.isGrounded && !isCrouching;
    }
}