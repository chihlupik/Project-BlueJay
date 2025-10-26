using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float speed = 6f;
    public float sprintSpeed = 10f;
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
    
    [Header("Slide Decay Settings")]
    public float slideDecayRate = 0.8f;
    public float minSlideSpeed = 3f;
    
    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.5f;
    public AnimationCurve dashSpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    [Header("Crouch Dash Settings")]
    public bool enableCrouchDash = true;
    public float crouchDashSpeed = 25f;
    public bool autoSlideOnLanding = true;
    public float crouchDashAirControl = 0.3f;
    
    public float standingCameraHeight = 1.6f;
    public float crouchingCameraHeight = 0.8f;
    public float slidingCameraHeight = 0.5f;
    
    public float slideTiltAngle = 10f;
    public float tiltTransitionSpeed = 5f;
    
    [Header("Footstep Sounds")]
    public AudioSource footstepAudioSource;
    public AudioClip walkFootstepSound;
    public AudioClip slideSound;
    public AudioClip jumpSound;
    public AudioClip landingSound;
    public AudioClip dashSound;
    
    [Header("Footstep Timing")]
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;
    public float crouchStepInterval = 0.7f;
    
    [Header("Footstep Audio Settings")]
    public float pitchRandomness = 0.1f;
    public float walkVolume = 0.8f;
    public float runVolume = 1.0f;
    public float crouchVolume = 0.6f;
    public float walkPitch = 1.0f;
    public float runPitch = 1.3f;
    public float crouchPitch = 0.8f;
    
    [Header("Jump Sound Settings")]
    public float jumpVolume = 0.8f;
    public float jumpPitch = 1.0f;
    
    [Header("Dash Sound Settings")]
    public float dashVolume = 0.8f;
    public float dashPitch = 1.2f;
    
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
    private bool isSprinting = false;
    private bool isCrouching = false;
    private bool isSliding = false;
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

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;

    private bool isCrouchDashing = false;
    private bool shouldAutoSlide = false;
    private Coroutine crouchDashCoroutine;

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
        
        standingHeight = controller.height;
        standingCenter = controller.center;
        
        currentHeight = standingHeight;
        targetHeight = standingHeight;

        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }
        
        UpdateCameraHeight();
    }

    void Update()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
        
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }
        
        if (crouchCooldownTimer > 0f)
        {
            crouchCooldownTimer -= Time.deltaTime;
        }

        HandleDashInput();
        HandleDash();
        HandleCrouchDash();
        HandleCrouchInput();
        HandleSliding();
        HandleCrouch();
        
        if (isDashing)
        {
        }
        else if (isCrouchDashing)
        {
            HandleCrouchDashMovement();
        }
        else
        {
            HandleMovement();
        }
        
        HandleSlideTilt();
        HandleFootsteps();
        
        if (isCrouching && !isSliding && !wantsToCrouch && CanStandUp())
        {
            AutoStandUp();
        }
    }

    void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.Q) && dashCooldownTimer <= 0f && !isDashing && !isSliding && !isCrouchDashing)
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            bool isMovingForward = input.y > 0.1f;

            if (enableCrouchDash && isSprinting && isMovingForward && !controller.isGrounded)
            {
                StartCrouchDash();
            }
            else
            {
                StartDash();
            }
        }
    }

    void HandleDash()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            float normalizedTime = 1f - (dashTimer / dashDuration);
            float speedMultiplier = dashSpeedCurve.Evaluate(normalizedTime);
            float currentDashSpeed = dashSpeed * speedMultiplier;
            
            currentVelocity = dashDirection * currentDashSpeed;
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;

            moveDirection.y -= gravity * Time.deltaTime;
            
            controller.Move(moveDirection * Time.deltaTime);

            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        
        if (input.magnitude > 0.1f)
        {
            dashDirection = new Vector3(input.x, 0, input.y).normalized;
            dashDirection = transform.TransformDirection(dashDirection);
        }
        else
        {
            dashDirection = transform.forward;
        }
        
        PlayDashSound();
    }

    void EndDash()
    {
        isDashing = false;
        currentVelocity = dashDirection * currentSpeed;
        StopDashSound();
    }

    void HandleCrouchDash()
    {
        if (isCrouchDashing && controller.isGrounded && shouldAutoSlide)
        {
            if (IsSlidingAllowed())
            {
                StartSlide();
                shouldAutoSlide = false;
            }
        }
    }

    void HandleCrouchDashMovement()
    {
        if (!controller.isGrounded)
        {
            moveDirection.y -= gravity * 0.5f * Time.deltaTime;
        }
        
        if (!controller.isGrounded)
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (input.magnitude > 0.1f)
            {
                Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
                desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
                
                Vector3 airControl = desiredMoveDirection * (airAcceleration * crouchDashAirControl * Time.deltaTime);
                currentVelocity += airControl;
                
                float horizontalSpeed = new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude;
                if (horizontalSpeed > crouchDashSpeed)
                {
                    Vector3 horizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z).normalized * crouchDashSpeed;
                    currentVelocity = new Vector3(horizontalVel.x, currentVelocity.y, horizontalVel.z);
                }
                
                moveDirection.x = currentVelocity.x;
                moveDirection.z = currentVelocity.z;
            }
        }
        
        controller.Move(moveDirection * Time.deltaTime);
    }

    void StartCrouchDash()
    {
        if (crouchDashCoroutine != null)
            StopCoroutine(crouchDashCoroutine);
            
        crouchDashCoroutine = StartCoroutine(PerformCrouchDash());
    }

    IEnumerator PerformCrouchDash()
    {
        isCrouchDashing = true;
        dashCooldownTimer = dashCooldown;
        
        isCrouching = true;
        targetHeight = crouchHeight;
        UpdateHeight();
        
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (input.magnitude > 0.1f && input.y > 0.1f)
        {
            dashDirection = new Vector3(input.x, 0, input.y).normalized;
            dashDirection = transform.TransformDirection(dashDirection);
        }
        else
        {
            dashDirection = transform.forward;
        }
        
        currentVelocity = dashDirection * crouchDashSpeed;
        moveDirection.x = currentVelocity.x;
        moveDirection.z = currentVelocity.z;
        
        float crouchDashTimer = dashDuration;
        
        while (crouchDashTimer > 0f && isCrouchDashing)
        {
            crouchDashTimer -= Time.deltaTime;
            
            float normalizedTime = 1f - (crouchDashTimer / dashDuration);
            float speedMultiplier = dashSpeedCurve.Evaluate(normalizedTime);
            float currentCrouchDashSpeed = crouchDashSpeed * speedMultiplier;
            
            Vector3 newVelocity = dashDirection * currentCrouchDashSpeed;
            currentVelocity = new Vector3(newVelocity.x, currentVelocity.y, newVelocity.z);
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;
            
            yield return null;
        }
        
        shouldAutoSlide = autoSlideOnLanding;
        
        while (!controller.isGrounded && isCrouchDashing)
        {
            yield return null;
        }
        
        isCrouchDashing = false;
    }

    public void CancelCrouchDash()
    {
        if (isCrouchDashing && crouchDashCoroutine != null)
        {
            StopCoroutine(crouchDashCoroutine);
            isCrouchDashing = false;
            shouldAutoSlide = false;
        }
    }

    void HandleCrouchInput()
    {
        if ((Input.GetKeyDown(KeyCode.LeftControl)) && crouchCooldownTimer <= 0f)
        {
            wantsToCrouch = true;
            
            if (isSprinting && IsSlidingAllowed())
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
        
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            wantsToCrouch = false;
            
            if (isCrouching && !isSliding && CanStandUp())
            {
                AutoStandUp();
            }
        }
    }

    void AutoStandUp()
    {
        if (isCrouching && !isSliding && CanStandUp() && crouchCooldownTimer <= 0f)
        {
            isCrouching = false;
            targetHeight = standingHeight;
            crouchCooldownTimer = crouchCooldown;
        }
    }

    void HandleSliding()
    {
        if (slideCooldownTimer > 0f)
        {
            slideCooldownTimer -= Time.deltaTime;
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            float normalizedTime = 1f - (slideTimer / slideDuration);
            float speedMultiplier = Mathf.Lerp(1f, 0f, normalizedTime * slideDecayRate);
            currentSlideSpeed = Mathf.Lerp(slideSpeed, minSlideSpeed, normalizedTime * slideDecayRate);
            
            currentVelocity = slideDirection * currentSlideSpeed;
            moveDirection.x = currentVelocity.x;
            moveDirection.z = currentVelocity.z;

            if ((slideTimer <= 0f || Input.GetButton("Jump")) && crouchCooldownTimer <= 0f)
            {
                EndSlide();
                crouchCooldownTimer = crouchCooldown;
            }
            else
            {
                Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                if (input.magnitude > 0.1f)
                {
                    Vector3 desiredMoveDirection = new Vector3(input.x, 0, input.y);
                    desiredMoveDirection = transform.TransformDirection(desiredMoveDirection);
                    
                    Vector3 steerDirection = Vector3.Lerp(slideDirection, desiredMoveDirection, 0.3f * Time.deltaTime);
                    slideDirection = steerDirection.normalized;
                }
            }
        }
    }

    bool IsSlidingAllowed()
    {
        return controller.isGrounded && 
               slideCooldownTimer <= 0f && 
               crouchCooldownTimer <= 0f &&
               !isSliding &&
               currentVelocity.magnitude > sprintSpeed * 0.7f;
    }

    void StartSlide()
    {
        isSliding = true;
        isCrouching = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;
        
        slideDirection = new Vector3(currentVelocity.x, 0, currentVelocity.z).normalized;
        if (slideDirection.magnitude < 0.1f)
        {
            slideDirection = transform.forward;
        }

        targetHeight = slideHeight;
        currentSlideSpeed = slideSpeed;
        
        targetSlideTilt = -slideTiltAngle;
    }

    void EndSlide()
    {
        isSliding = false;
        
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
        if (!isCrouching || isSliding) return false;
        
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
        if (isSliding)
        {
            moveDirection.y -= gravity * Time.deltaTime;
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
        
        isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && 
                     !isCrouching && 
                     !isSliding && 
                     !isDashing &&
                     !isCrouchDashing &&
                     input.magnitude > 0.1f;
        
        float targetMovementSpeed;
        if (isCrouching)
        {
            targetMovementSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            targetMovementSpeed = sprintSpeed;
        }
        else
        {
            targetMovementSpeed = speed;
        }
        
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

            if (Input.GetButton("Jump") && jumpCooldownTimer <= 0f && !isCrouching && !isDashing && !isCrouchDashing)
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
            }
            else
            {
                moveDirection.y = -0.1f;
            }
        }
        else
        {
            Vector3 targetAirVelocity = desiredMoveDirection * currentSpeed;
            
            if (input.magnitude > 0.1f)
            {
                float effectiveAirAcceleration = isSprinting ? airAcceleration * 1.5f : airAcceleration;
                currentVelocity = Vector3.Lerp(currentVelocity, targetAirVelocity, effectiveAirAcceleration * Time.deltaTime);
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

    void HandleFootsteps()
    {
        if (footstepAudioSource == null || walkFootstepSound == null) return;

        bool isGrounded = controller.isGrounded;
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        
        bool hasMovementInput = input.magnitude > 0.1f;
        bool isMoving = currentVelocity.magnitude > 0.1f;
        
        bool shouldPlayFootsteps = isGrounded && hasMovementInput && isMoving && !isSliding && !isDashing && !isCrouchDashing;

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
        else if (isSprinting)
            return runStepInterval;
        else
            return walkStepInterval;
    }

    void PlayFootstepSound()
    {
        if (footstepAudioSource == null || walkFootstepSound == null) return;

        footstepAudioSource.clip = walkFootstepSound;
        
        float basePitch = walkPitch;
        float baseVolume = walkVolume;

        if (isSprinting)
        {
            basePitch = runPitch;
            baseVolume = runVolume;
        }
        else if (isCrouching)
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

    void PlayJumpSound()
    {
        if (footstepAudioSource == null || jumpSound == null) return;

        footstepAudioSource.clip = jumpSound;
        footstepAudioSource.pitch = jumpPitch;
        footstepAudioSource.volume = jumpVolume;
        footstepAudioSource.loop = false;
        footstepAudioSource.Play();
    }

    void PlayDashSound()
    {
        if (footstepAudioSource == null || dashSound == null) return;

        footstepAudioSource.clip = dashSound;
        footstepAudioSource.pitch = dashPitch;
        footstepAudioSource.volume = dashVolume;
        footstepAudioSource.loop = false;
        footstepAudioSource.Play();
    }

    void StopDashSound()
    {
        if (footstepAudioSource == null) return;
        
        if (footstepAudioSource.isPlaying && footstepAudioSource.clip == dashSound)
        {
            footstepAudioSource.Stop();
        }
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

    public bool IsDashing()
    {
        return isDashing;
    }

    public bool IsCrouchDashing()
    {
        return isCrouchDashing;
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

    public bool IsSprinting()
    {
        return isSprinting;
    }

    public float GetSprintSpeed()
    {
        return sprintSpeed;
    }

    public float GetDashCooldownProgress()
    {
        return Mathf.Clamp01(1f - (dashCooldownTimer / dashCooldown));
    }
}