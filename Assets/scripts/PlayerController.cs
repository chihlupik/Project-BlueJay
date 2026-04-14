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
    public float diveSpeed = 18f;
    public float diveDuration = 2.5f;
    public float diveCooldown = 1f;
    public float diveHeight = 0.5f;
    public float diveControlMultiplier = 0.05f;
    public float diveJumpMultiplier = 1.5f;
    public float diveGravityMultiplier = 0.08f;
    public bool lockDiveDirection = true;
    public float diveForwardBoost = 1.5f;
    public float blockDiveTimer = 0f;
    public float diveCancelJumpMultiplier = 0.5f;
    public bool allowDiveCancelWithJump = true;
    public float minDiveSpeedThreshold = 0.5f;
    public float diveVerticalMomentum = 0f;
    public bool preserveVerticalVelocity = true;
    public bool useSmoothGravityIncrease = true;
    public float diveDecayRate = 0.5f;
    public float minDiveSpeed = 8f;
    
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
    
    [Header("Grappling Hook Settings")]
    public KeyCode grappleKey = KeyCode.E;
    public float grappleRange = 20f;
    public float grappleSpeed = 25f;
    public float grappleCooldown = 1.5f;
    public LayerMask grappleLayerMask = -1;
    public float grappleBoostMultiplier = 2f;
    public float grappleBoostDuration = 1.5f;
    public float grappleBoostControlMultiplier = 0.8f;
    public bool canCancelGrappleBoostWithJump = true;
    
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
    public AudioClip landingSound;
    public AudioClip diveSound;
    public AudioClip grappleSound;
    
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
    
    [Header("Grapple Sound Settings")]
    public float grappleVolume = 1.0f;
    public float grapplePitch = 1.0f;
    
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
    private float originalGravity;
    private float verticalVelocityOnDiveStart;
    private bool isDiveEnding = false;
    private float diveHeightOnStart;
    
    // Счетчики для воздуха
    private int airJumpsRemaining = 0;
    private bool hasDoubleJumped = false;
    private float doubleJumpCooldownTimer = 0f;
    private bool hasUsedDive = false; // Флаг: использовал ли дайв в текущем полете
    
    // Grappling hook variables
    private bool isGrappling = false;
    private float grappleCooldownTimer = 0f;
    private Vector3 grappleTargetPoint;
    private float grappleProgress = 0f;
    private float grappleJumpResetTimer = 0f;
    private bool isGrappleBoosting = false;
    private float grappleBoostTimer = 0f;
    private Vector3 grappleBoostDirection;

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
        
        airJumpsRemaining = maxAirJumps;
        diveHeightOnStart = diveHeight;
        hasUsedDive = false;
    }

    void Update()
    {
        if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;
        if (doubleJumpCooldownTimer > 0f) doubleJumpCooldownTimer -= Time.deltaTime;
        if (blockDiveTimer > 0f) blockDiveTimer -= Time.deltaTime;
        if (crouchCooldownTimer > 0f) crouchCooldownTimer -= Time.deltaTime;
        if (slideCooldownTimer > 0f) slideCooldownTimer -= Time.deltaTime;
        if (diveCooldownTimer > 0f) diveCooldownTimer -= Time.deltaTime;
        if (grappleCooldownTimer > 0f) grappleCooldownTimer -= Time.deltaTime;
        if (grappleJumpResetTimer > 0f) grappleJumpResetTimer -= Time.deltaTime;

        HandleCrouchInput();
        HandleSliding();
        HandleDiveInput();
        HandleGrappleInput();
        HandleCrouch();
        
        HandleMovement();
        
        HandleSlideTilt();
        HandleFootsteps();
        
        if (isCrouching && !isSliding && !isDiving && !wantsToCrouch && CanStandUp())
        {
            AutoStandUp();
        }
        
        // Сброс флага дайва при касании земли
        if (controller.isGrounded)
        {
            airJumpsRemaining = maxAirJumps;
            hasDoubleJumped = false;
            hasUsedDive = false; // Сбрасываем флаг дайва
        }
    }
    
    void HandleGrappleInput()
    {
        if (Input.GetKeyDown(grappleKey) && grappleCooldownTimer <= 0f && !isGrappling && !isDiving && !isSliding && !isGrappleBoosting)
        {
            TryGrapple();
        }
        
        if (isGrappling)
        {
            UpdateGrapple();
        }
        
        if (canCancelGrappleBoostWithJump && isGrappleBoosting && Input.GetButtonDown("Jump"))
        {
            CancelGrappleBoost();
        }
        
        if (isGrappleBoosting)
        {
            UpdateGrappleBoost();
        }
    }
    
    void TryGrapple()
    {
        if (cameraTransform == null) return;
        
        RaycastHit hit;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        
        if (Physics.Raycast(ray, out hit, grappleRange, grappleLayerMask))
        {
            GrapplePoint grapplePoint = hit.collider.GetComponent<GrapplePoint>();
            if (grapplePoint != null && grapplePoint.IsAvailable())
            {
                StartGrapple(hit.point, grapplePoint);
            }
        }
    }
    
    void StartGrapple(Vector3 targetPoint, GrapplePoint grapplePoint)
    {
        isGrappling = true;
        grappleTargetPoint = targetPoint;
        grappleProgress = 0f;
        grappleCooldownTimer = grappleCooldown;
        
        PlayGrappleSound();
    }
    
    void UpdateGrapple()
    {
        Vector3 toTarget = grappleTargetPoint - transform.position;
        float distanceToTarget = toTarget.magnitude;
        
        if (distanceToTarget < 0.5f)
        {
            EndGrapple();
            return;
        }
        
        Vector3 grappleDirection = toTarget.normalized;
        Vector3 newPosition = Vector3.MoveTowards(transform.position, grappleTargetPoint, grappleSpeed * Time.deltaTime);
        
        Vector3 velocity = (newPosition - transform.position) / Time.deltaTime;
        currentVelocity = velocity;
        moveDirection = velocity;
        
        controller.Move(moveDirection * Time.deltaTime);
        grappleProgress += Time.deltaTime;
    }
    
    void EndGrapple()
    {
        isGrappling = false;
        
        isGrappleBoosting = true;
        grappleBoostTimer = grappleBoostDuration;
        
        grappleBoostDirection = currentVelocity.normalized;
        if (grappleBoostDirection.magnitude < 0.1f)
        {
            grappleBoostDirection = transform.forward;
        }
        
        float boostSpeed = speed * grappleBoostMultiplier;
        currentVelocity = grappleBoostDirection * boostSpeed;
        moveDirection.x = currentVelocity.x;
        moveDirection.z = currentVelocity.z;
        
        airJumpsRemaining = maxAirJumps;
        hasDoubleJumped = false;
        hasUsedDive = false; // Сбрасываем флаг дайва
        grappleJumpResetTimer = 0.5f;
    }
    
    void UpdateGrappleBoost()
    {
        if (grappleBoostTimer > 0f)
        {
            grappleBoostTimer -= Time.deltaTime;
            
            float t = grappleBoostTimer / grappleBoostDuration;
            float currentBoostMultiplier = Mathf.Lerp(1f, grappleBoostMultiplier, t);
            float currentBoostSpeed = speed * currentBoostMultiplier;
            
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (input.magnitude > 0.1f)
            {
                Vector3 desiredDirection = new Vector3(input.x, 0, input.y);
                desiredDirection = transform.TransformDirection(desiredDirection);
                
                grappleBoostDirection = Vector3.Lerp(grappleBoostDirection, desiredDirection.normalized, grappleBoostControlMultiplier * Time.deltaTime).normalized;
            }
            
            currentVelocity = grappleBoostDirection * currentBoostSpeed;
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;
            
            moveDirection.y -= gravity * 0.5f * Time.deltaTime;
            
            controller.Move(moveDirection * Time.deltaTime);
        }
        else
        {
            isGrappleBoosting = false;
        }
    }
    
    void CancelGrappleBoost()
    {
        isGrappleBoosting = false;
        moveDirection.y = jumpSpeed;
        PlayJumpSound();
    }
    
    public void GrantJump()
    {
        airJumpsRemaining = maxAirJumps;
        hasDoubleJumped = false;
        doubleJumpCooldownTimer = 0f;
        hasUsedDive = false; // Сбрасываем флаг дайва
        
        if (!controller.isGrounded)
        {
            moveDirection.y = jumpSpeed * 1.2f;
            PlayJumpSound();
        }
    }

    void HandleCrouchInput()
    {
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
                if (wantsToCrouch) shouldCancelSlide = true;
            }
            else
            {
                if (!wantsToCrouch) shouldCancelSlide = true;
            }
            
            if (slideTimer <= 0f || Input.GetButton("Jump")) shouldCancelSlide = true;
            else if (CheckWallCollision()) shouldCancelSlide = true;

            if (shouldCancelSlide && crouchCooldownTimer <= 0f)
            {
                EndSlide();
                crouchCooldownTimer = crouchCooldown;
            }
        }
    }

    void HandleDiveInput()
    {
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        bool isMovingHorizontally = horizontalVelocity.magnitude > minDiveSpeedThreshold;
        
        // Проверка: можно ли сделать дайв (только один раз за полет)
        bool canDive = !controller.isGrounded && 
                       diveCooldownTimer <= 0f && 
                       blockDiveTimer <= 0f && 
                       isMovingHorizontally && 
                       !isGrappling && 
                       !isGrappleBoosting &&
                       !hasUsedDive; // Ключевое условие - дайв не использован в текущем полете
        
        if (Input.GetKeyDown(KeyCode.Q) && canDive)
        {
            StartDive();
        }
        
        if (isDiving && allowDiveCancelWithJump && Input.GetButtonDown("Jump")) 
            CancelDiveWithJump();
        if (isDiving) 
            HandleDive();
    }

    void HandleDive()
    {
        diveTimer -= Time.deltaTime;

        float normalizedTime = 1f - (diveTimer / diveDuration);
        currentDiveSpeed = Mathf.Lerp(diveSpeed, minDiveSpeed, normalizedTime * diveDecayRate);
        
        if (!lockDiveDirection)
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (input.magnitude > 0.1f)
            {
                Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
                desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
                
                Vector3 steerDirection = Vector3.Lerp(diveDirection, desiredMoveDirection, diveControlMultiplier * Time.deltaTime);
                diveDirection = steerDirection.normalized;
            }
        }
        
        currentVelocity = diveDirection * currentDiveSpeed;
        moveDirection.x = currentVelocity.x;
        moveDirection.z = currentVelocity.z;

        if (useSmoothGravityIncrease)
        {
            float currentGravityMultiplier = Mathf.Lerp(diveGravityMultiplier, 1.0f, normalizedTime * normalizedTime);
            float diveGravity = gravity * currentGravityMultiplier;
            moveDirection.y -= diveGravity * Time.deltaTime;
        }
        else
        {
            float diveGravity = gravity * diveGravityMultiplier;
            moveDirection.y -= diveGravity * Time.deltaTime;
        }

        if (moveDirection.y < -8f) moveDirection.y = -8f;

        if (controller.isGrounded)
        {
            EndDive();
            if (!isSliding && slideCooldownTimer <= 0f) StartSlideFromDive();
        }
        else if (diveTimer <= 0f && !isDiveEnding) 
            EndDiveInAir();
    }

    void CancelDiveWithJump()
    {
        if (!isDiving) return;
        
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        isDiving = false;
        isDiveEnding = false;
        moveDirection.y = jumpSpeed * diveCancelJumpMultiplier;
        currentVelocity = horizontalVelocity * 0.7f;
        moveDirection.x = currentVelocity.x;
        moveDirection.z = currentVelocity.z;
        StartCoroutine(SmoothExitDive());
        blockDiveTimer = 0.3f;
        PlayJumpSound();
    }

    void EndDiveInAir()
    {
        if (!isDiving) return;
        isDiveEnding = true;
        isDiving = false;
        StartCoroutine(SmoothExitDive());
    }

    IEnumerator SmoothExitDive()
    {
        float elapsedTime = 0f;
        float transitionDuration = 0.2f;
        float startHeight = currentHeight;
        float endHeight = standingHeight;
        
        if (!CanStandUp() || wantsToCrouch)
        {
            endHeight = crouchHeight;
            isCrouching = true;
        }
        else isCrouching = false;
        
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            float newHeight = Mathf.Lerp(startHeight, endHeight, t);
            controller.height = newHeight;
            controller.center = new Vector3(0, newHeight / 2f, 0);
            if (capsuleCollider != null)
            {
                capsuleCollider.height = newHeight;
                capsuleCollider.center = new Vector3(0, newHeight / 2f, 0);
            }
            yield return null;
        }
        
        targetHeight = endHeight;
        currentHeight = endHeight;
        isDiveEnding = false;
    }

    bool CheckWallCollision()
    {
        float checkDistance = 0.5f;
        float sphereRadius = 0.3f;
        Vector3 castOrigin = transform.position + Vector3.up * 0.1f;
        
        if (Physics.SphereCast(castOrigin, sphereRadius, slideDirection, out RaycastHit hit, checkDistance))
        {
            if (!hit.collider.isTrigger && Vector3.Angle(hit.normal, Vector3.up) > 60f) return true;
        }
        return false;
    }

    bool IsSlidingAllowed()
    {
        bool isMovingForward = Input.GetKey(KeyCode.W) || Input.GetAxis("Vertical") > 0.1f;
        return controller.isGrounded && slideCooldownTimer <= 0f && crouchCooldownTimer <= 0f && !isSliding && currentVelocity.magnitude > speed * 0.7f && isMovingForward;
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
        if (horizontalVelocity.magnitude > 0.5f) slideDirection = horizontalVelocity.normalized;

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
        diveDirection = transform.forward;
        
        // Отмечаем, что дайв использован
        hasUsedDive = true;
        
        if (diveForwardBoost > 1f) diveDirection *= diveForwardBoost;
        
        if (!lockDiveDirection)
        {
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            if (horizontalVelocity.magnitude > 0.5f) diveDirection = horizontalVelocity.normalized;
        }

        targetHeight = diveHeight;
        currentDiveSpeed = diveSpeed;
        verticalVelocityOnDiveStart = moveDirection.y;
        
        if (hasDoubleJumped && doubleJumpCooldownTimer > 0f)
        {
            if (preserveVerticalVelocity) { }
        }
        else
        {
            if (diveVerticalMomentum != 0f) moveDirection.y = diveVerticalMomentum;
            else if (preserveVerticalVelocity)
            {
                if (moveDirection.y > 0) moveDirection.y = Mathf.Min(moveDirection.y, jumpSpeed * 0.4f);
                else if (moveDirection.y < -3f) moveDirection.y = Mathf.Max(moveDirection.y, -2f);
                else moveDirection.y = Mathf.Max(moveDirection.y, 0f);
            }
            else moveDirection.y = 0f;
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
            if (CanStandUp()) AutoStandUp();
            else
            {
                targetHeight = crouchHeight;
                isCrouching = true;
            }
        }
    }

    void EndDive()
    {
        if (!isDiving) return;
        isDiving = false;
        isDiveEnding = false;
        
        if (wantsToCrouch)
        {
            targetHeight = crouchHeight;
            isCrouching = true;
        }
        else
        {
            if (CanStandUp()) AutoStandUp();
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
        
        if (cameraController != null) ApplySlideTiltToCamera();
        else ApplySlideTiltDirect();
    }

    void ApplySlideTiltToCamera()
    {
        try
        {
            var slideTiltField = cameraController.GetType().GetField("externalTilt");
            if (slideTiltField != null) slideTiltField.SetValue(cameraController, currentSlideTilt);
        }
        catch { ApplySlideTiltDirect(); }
    }

    void ApplySlideTiltDirect()
    {
        if (cameraTransform != null)
        {
            Vector3 currentRotation = cameraTransform.localEulerAngles;
            cameraTransform.localEulerAngles = new Vector3(currentRotation.x, currentRotation.y, currentSlideTilt);
        }
    }

    void HandleCrouch()
    {
        if (isSliding) targetHeight = slideHeight;
        else if (isDiving) targetHeight = diveHeight;
        else if (isCrouching) targetHeight = crouchHeight;
        else targetHeight = standingHeight;

        UpdateHeight();
        UpdateCameraHeight();
    }

    void UpdateHeight()
    {
        if (isDiveEnding) return;
            
        float previousHeight = currentHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2f, 0);

        if (capsuleCollider != null)
        {
            capsuleCollider.height = currentHeight;
            capsuleCollider.center = new Vector3(0, currentHeight / 2f, 0);
        }

        if (currentHeight < previousHeight) transform.position += new Vector3(0, (previousHeight - currentHeight) / 2f, 0);
        if (groundCheck != null) groundCheck.localPosition = new Vector3(0, 0.1f, 0);
    }

    void UpdateCameraHeight()
    {
        if (cameraTransform != null)
        {
            float targetCameraHeight;
            if (isSliding) targetCameraHeight = slidingCameraHeight;
            else if (isDiving) targetCameraHeight = divingCameraHeight;
            else if (isCrouching) targetCameraHeight = crouchingCameraHeight;
            else targetCameraHeight = standingCameraHeight;
            
            Vector3 currentCameraPos = cameraTransform.localPosition;
            Vector3 targetCameraPos = new Vector3(0, targetCameraHeight, 0);
            cameraTransform.localPosition = Vector3.Lerp(currentCameraPos, targetCameraPos, crouchTransitionSpeed * Time.deltaTime);

            CameraController cameraController = cameraTransform.GetComponent<CameraController>();
            if (cameraController != null) cameraController.UpdateOriginalPosition();
        }
    }

    bool CanStandUp()
    {
        if (!isCrouching || isSliding || isDiving) return false;
        
        float checkDistance = standingHeight - currentHeight + 0.2f; 
        Vector3 rayStart = transform.position + Vector3.up * (currentHeight + 0.1f);
        float checkRadius = capsuleCollider != null ? capsuleCollider.radius * 0.8f : 0.3f;
        
        if (Physics.Raycast(rayStart, Vector3.up, out RaycastHit hit, checkDistance)) return false;
        
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward, Vector3.back, Vector3.right, Vector3.left,
            (Vector3.forward + Vector3.right).normalized, (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized, (Vector3.back + Vector3.left).normalized
        };
        
        foreach (Vector3 dir in directions)
        {
            Vector3 offsetStart = rayStart + dir * checkRadius;
            if (Physics.Raycast(offsetStart, Vector3.up, out RaycastHit offsetHit, checkDistance)) return false;
        }
        
        if (capsuleCollider != null)
        {
            Vector3 point1 = transform.position + Vector3.up * capsuleCollider.radius;
            Vector3 point2 = transform.position + Vector3.up * (standingHeight - capsuleCollider.radius);
            float radius = capsuleCollider.radius * 0.9f;
            if (Physics.CapsuleCast(point1, point2, radius, Vector3.up, out RaycastHit capsuleHit, checkDistance)) return false;
        }
        
        return true;
    }

    void HandleMovement()
    {
        if (isSliding || isDiving || isGrappling || isGrappleBoosting)
        {
            if (!isDiving && !isGrappling && !isGrappleBoosting) 
                moveDirection.y -= gravity * Time.deltaTime;
            controller.Move(moveDirection * Time.deltaTime);
            return;
        }

        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (input.magnitude > 1f) input.Normalize();
        
        Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
        desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
        
        float targetMovementSpeed = (isCrouching && controller.isGrounded) ? crouchSpeed : speed;
        targetMovementSpeed = ApplyDirectionalSpeedMultipliers(targetMovementSpeed);
        currentSpeed = Mathf.Lerp(currentSpeed, targetMovementSpeed, speedTransitionSharpness * Time.deltaTime);

        if (controller.isGrounded)
        {
            Vector3 targetVelocity = desiredMoveDirection * currentSpeed;
            if (input.magnitude > 0.1f) currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, groundAcceleration * Time.deltaTime);
            else currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, groundDeceleration * Time.deltaTime);
            
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;

            if (Input.GetButton("Jump") && jumpCooldownTimer <= 0f && !isCrouching && !isDiving)
            {
                PlayJumpSound();
                moveDirection.y = jumpSpeed;
                jumpCooldownTimer = jumpCooldown;
                
                Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                if (horizontalVelocity.magnitude > currentSpeed) horizontalVelocity = horizontalVelocity.normalized * currentSpeed;
                currentVelocity = horizontalVelocity;
                airJumpsRemaining = maxAirJumps;
            }
            else moveDirection.y = -0.1f;
        }
        else
        {
            if (enableDoubleJump && Input.GetButtonDown("Jump") && airJumpsRemaining > 0 && doubleJumpCooldownTimer <= 0f && !isCrouching && !isDiving && !isGrappling)
            {
                PerformDoubleJump();
            }
            
            Vector3 targetAirVelocity = desiredMoveDirection * currentSpeed;
            if (input.magnitude > 0.1f) currentVelocity = Vector3.Lerp(currentVelocity, targetAirVelocity, airAcceleration * Time.deltaTime);
            else currentVelocity = Vector3.Lerp(currentVelocity, new Vector3(currentVelocity.x, 0, currentVelocity.z), airAcceleration * 0.3f * Time.deltaTime);
            
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;
        }

        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
    }

    void PerformDoubleJump()
    {
        moveDirection.y = doubleJumpSpeed;
        if (grappleJumpResetTimer > 0f) moveDirection.y *= 1.2f;
        airJumpsRemaining--;
        hasDoubleJumped = true;
        doubleJumpCooldownTimer = doubleJumpCooldown;
        blockDiveTimer = 0.3f;
        PlayJumpSound();
    }

    float ApplyDirectionalSpeedMultipliers(float baseSpeed)
    {
        bool isMovingForward = Input.GetKey(KeyCode.W);
        bool isMovingBackward = Input.GetKey(KeyCode.S);
        bool isMovingLeft = Input.GetKey(KeyCode.A);
        bool isMovingRight = Input.GetKey(KeyCode.D);

        if (!isMovingForward && !isMovingBackward && !isMovingLeft && !isMovingRight) return baseSpeed;

        float speedMultiplier = 1.0f;

        if (isMovingForward && !isMovingBackward) speedMultiplier = forwardSpeedMultiplier;
        else if (isMovingBackward && !isMovingForward) speedMultiplier = backwardSpeedMultiplier;
        else if ((isMovingLeft || isMovingRight) && !isMovingForward && !isMovingBackward) speedMultiplier = strafeSpeedMultiplier;
        else speedMultiplier = isMovingBackward ? backwardSpeedMultiplier : Mathf.Min(forwardSpeedMultiplier, strafeSpeedMultiplier);

        return baseSpeed * speedMultiplier;
    }

    void HandleFootsteps()
    {
        if (footstepAudioSource == null || walkFootstepSound == null) return;

        bool isGrounded = controller.isGrounded;
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        bool hasMovementInput = input.magnitude > 0.1f;
        bool isMoving = currentVelocity.magnitude > 0.1f;
        bool shouldPlayFootsteps = isGrounded && hasMovementInput && isMoving && !isSliding && !isDiving && !isGrappling && !isGrappleBoosting;

        if (isGrounded && !wasGrounded)
        {
            float landingVelocity = Mathf.Abs(moveDirection.y);
            if (landingVelocity > landingThreshold) PlayLandingSound(landingVelocity);
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
        else stepTimer = 0f;

        if (isSliding)
        {
            if (!footstepAudioSource.isPlaying || footstepAudioSource.clip != slideSound) PlaySlideSound();
        }
        else if (isDiving)
        {
            if (!footstepAudioSource.isPlaying || footstepAudioSource.clip != diveSound) PlayDiveSound();
        }
        else if (footstepAudioSource.clip == slideSound && footstepAudioSource.isPlaying) footstepAudioSource.Stop();

        wasGrounded = isGrounded;
    }

    float GetStepInterval() => isCrouching ? crouchStepInterval : walkStepInterval;

    void PlayFootstepSound()
    {
        if (footstepAudioSource == null || walkFootstepSound == null) return;
        footstepAudioSource.clip = walkFootstepSound;
        float basePitch = isCrouching ? crouchPitch : walkPitch;
        float baseVolume = isCrouching ? crouchVolume : walkVolume;
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

    void PlayGrappleSound()
    {
        if (footstepAudioSource == null || grappleSound == null) return;
        footstepAudioSource.PlayOneShot(grappleSound, grappleVolume);
    }

    void OnTriggerEnter(Collider other)
    {
        GrapplePoint grapplePoint = other.GetComponent<GrapplePoint>();
        if (grapplePoint != null && grapplePoint.IsAvailable())
        {
            grapplePoint.OnPlayerTouch(this);
            // Сбрасываем флаг дайва при касании точки
            hasUsedDive = false;
        }
    }

    public float GetSlideTilt() => currentSlideTilt;
    public bool IsSliding() => isSliding;
    public bool IsDiving() => isDiving;
    public bool IsGrappling() => isGrappling;
    public bool IsGrappleBoosting() => isGrappleBoosting;
    public bool IsGrounded() => controller.isGrounded;
    public Vector3 GetVelocity() => currentVelocity;
    public bool IsCrouching() => isCrouching;
    public float GetSpeed() => speed;
    public int GetRemainingAirJumps() => airJumpsRemaining;
    public bool CanDoubleJump() => enableDoubleJump && airJumpsRemaining > 0 && doubleJumpCooldownTimer <= 0f && !controller.isGrounded && !isCrouching;
    public bool CanDive() => !controller.isGrounded && !hasUsedDive && !isDiving && !isGrappling && !isGrappleBoosting;
}