using StarterAssets;
using UnityEngine;

/// <summary>
/// Управляет движением дракона-маунта и синхронизирует его с наездником.
/// </summary>
public class DragonMountController : MonoBehaviour
{
    [Header("Seat")]
    [Tooltip("Точка, в которой будет фиксироваться наездник")]
    [SerializeField] private Transform seatPoint;
    [Tooltip("Смещение наездника относительно seatPoint (локальные координаты)")]
    [SerializeField] private Vector3 riderLocalOffset = new Vector3(0f, 0.9f, -0.1f);
    [Tooltip("Место высадки (локально относительно seatPoint)")]
    [SerializeField] private Vector3 dismountLocalOffset = new Vector3(0.5f, 0f, -2f);

    [Header("Flight")]
    [SerializeField] private float forwardSpeed = 18f;
    [SerializeField] private float strafeSpeed = 10f;
    [SerializeField] private float verticalSpeed = 8f;
    [SerializeField] private float rotationSpeed = 4f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float gravityFallback = 0f;
    [SerializeField] private float collisionRadius = 2f;
    [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;

    [Header("Animation")]
    [SerializeField] private Animator dragonAnimator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private float animationBlendSpeed = 6f;

    [Header("Orientation")]
    [SerializeField] private float maxPitchAngle = 25f;
    [SerializeField] private float maxRollAngle = 35f;
    [SerializeField] private float orientationLerpSpeed = 5f;
    [SerializeField] private float cameraRollSensitivity = 0.35f;

    [Header("Camera Vertical Control")]
    [SerializeField] private bool useCameraPitchForVertical = true;
    [SerializeField, Range(0f, 0.9f)] private float cameraPitchDeadZone = 0.1f;
    [SerializeField] private float cameraPitchVerticalStrength = 1f;

    [Header("Camera Alignment")]
    [SerializeField] private bool alignWithCameraForward = true;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool autoFindCamera = true;

    [Header("Input")]
    [SerializeField] private KeyCode ascendKey = KeyCode.Space;
    [SerializeField] private KeyCode descendKey = KeyCode.LeftControl;
    [SerializeField] private bool allowKeyboardFallback = true;

    public bool IsOccupied => currentRider != null;
    public Transform SeatPoint => seatPoint != null ? seatPoint : transform;

    private Transform currentRider;
    private StarterAssetsInputs riderInputs;
    private PlayerMountHandler riderHandler;
    private Vector3 currentVelocity;
    private float animationBlend;
    private Vector3 lastPlanarDirection = Vector3.forward;
    private float currentPitchAngle;
    private float currentRollAngle;
    private Transform riderOriginalParent;
    private Vector3 riderOriginalLocalPosition;
    private Quaternion riderOriginalLocalRotation;
    private bool hasSpeedParameter;
    private int speedParameterHash;

    private void Awake()
    {
        if (dragonAnimator == null)
        {
            dragonAnimator = GetComponentInChildren<Animator>();
        }

        if (dragonAnimator == null)
        {
            dragonAnimator = GetComponentInChildren<Animator>();
        }

        speedParameterHash = !string.IsNullOrEmpty(speedParameter)
            ? Animator.StringToHash(speedParameter)
            : 0;

        ValidateAnimatorParameter();

        if (seatPoint == null)
        {
            seatPoint = transform;
        }

        if (transform.forward.sqrMagnitude > 0.01f)
        {
            lastPlanarDirection = transform.forward.normalized;
        }

        if ((cameraTransform == null || !cameraTransform.gameObject.activeInHierarchy) && autoFindCamera)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }
    }

    private void Update()
    {
        if (!IsOccupied)
        {
            UpdateAnimator(0f);
            ApplyIdleGravity();
            RelaxOrientation();
            return;
        }

        Vector2 moveInput = GetPlanarInput();
        float verticalInput = GetVerticalInput();
        bool boost = GetBoostInput();

        HandleMovement(moveInput, verticalInput, boost);
    }

    public bool TryMount(PlayerMountHandler handler, StarterAssetsInputs inputs)
    {
        if (IsOccupied || handler == null)
            return false;

        riderHandler = handler;
        riderInputs = inputs;

        currentRider = handler != null ? handler.GetMountAttachTransform() : null;
        if (currentRider == null)
        {
            currentRider = handler.transform;
        }

        riderOriginalParent = currentRider.parent;
        riderOriginalLocalPosition = currentRider.localPosition;
        riderOriginalLocalRotation = currentRider.localRotation;

        SnapRiderToSeat();
        return true;
    }

    public void ForceDismount(PlayerMountHandler handler)
    {
        if (handler == null || handler != riderHandler)
            return;

        if (currentRider != null)
        {
            currentRider.SetParent(riderOriginalParent, false);
            currentRider.localPosition = riderOriginalLocalPosition;
            currentRider.localRotation = riderOriginalLocalRotation;
        }

        riderHandler = null;
        riderInputs = null;
        currentRider = null;
        currentVelocity = Vector3.zero;
        riderOriginalParent = null;
    }

    public Vector3 GetSafeDismountPosition()
    {
        Transform seat = SeatPoint;
        Vector3 worldOffset = seat.TransformVector(dismountLocalOffset);
        return seat.position + worldOffset + Vector3.up * 0.5f;
    }

    private void SnapRiderToSeat()
    {
        if (currentRider == null)
            return;

        Transform seat = SeatPoint;
        currentRider.SetParent(seat, false);
        currentRider.localPosition = riderLocalOffset;
        currentRider.localRotation = Quaternion.identity;
    }

    private Vector2 GetPlanarInput()
    {
        Vector2 input = Vector2.zero;

        if (riderInputs != null)
        {
            input += riderInputs.move;
        }

        if (allowKeyboardFallback)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            input += new Vector2(horizontal, vertical);
        }

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        return input;
    }

    private float GetVerticalInput()
    {
        float value = 0f;

        if (riderInputs != null && riderInputs.jump)
            value += 1f;

        bool ascendHeld = false;
        if (allowKeyboardFallback && Input.GetKey(ascendKey))
        {
            ascendHeld = true;
        }
        else if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsFlightAscendHeld())
        {
            ascendHeld = true;
        }

        if (ascendHeld)
            value += 1f;

        bool descendHeld = false;
        if (allowKeyboardFallback && Input.GetKey(descendKey))
        {
            descendHeld = true;
        }
        else if (MobileInputManager.Instance != null && MobileInputManager.Instance.IsFlightDescendHeld())
        {
            descendHeld = true;
        }

        if (descendHeld)
            value -= 1f;

        if (alignWithCameraForward && useCameraPitchForVertical)
        {
            Vector3 camForward = GetCameraForward();
            if (camForward != Vector3.zero)
            {
                float pitchFactor = Mathf.Clamp(camForward.y, -1f, 1f);
                float absPitch = Mathf.Abs(pitchFactor);
                float blend = Mathf.InverseLerp(cameraPitchDeadZone, 1f, absPitch);
                float signed = Mathf.Sign(pitchFactor) * blend;
                value += signed * cameraPitchVerticalStrength;
            }
        }

        return Mathf.Clamp(value, -1f, 1f);
    }

    private bool GetBoostInput()
    {
        bool boost = false;

        if (riderInputs != null)
            boost |= riderInputs.sprint;

        if (allowKeyboardFallback)
        {
            boost |= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        return boost;
    }

    private void HandleMovement(Vector2 planarInput, float verticalInput, bool boost)
    {
        float currentForwardSpeed = boost ? forwardSpeed * 1.4f : forwardSpeed;
        float currentStrafeSpeed = boost ? strafeSpeed * 1.3f : strafeSpeed;

        Vector3 referenceForward = GetReferenceForward();
        Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward).normalized;

        Vector3 desiredVelocity =
            referenceForward * planarInput.y * currentForwardSpeed +
            referenceRight * planarInput.x * currentStrafeSpeed +
            Vector3.up * verticalInput * verticalSpeed;

        currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, acceleration * Time.deltaTime);
        MoveWithCollision(currentVelocity * Time.deltaTime);

        Vector3 planarVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        UpdateOrientation(planarInput, planarVelocity);

        UpdateAnimator(currentVelocity.magnitude / currentForwardSpeed);
    }

    private void UpdateAnimator(float normalizedSpeed)
    {
        if (dragonAnimator == null || !hasSpeedParameter)
            return;

        animationBlend = Mathf.Lerp(animationBlend, normalizedSpeed, animationBlendSpeed * Time.deltaTime);
        dragonAnimator.SetFloat(speedParameterHash, animationBlend);
    }

    private void ApplyIdleGravity()
    {
        if (gravityFallback <= 0f)
            return;

        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, pos.y - gravityFallback * Time.deltaTime, 0.5f);
        transform.position = pos;
    }

    private void UpdateOrientation(Vector2 planarInput, Vector3 planarVelocity)
    {
        if (planarVelocity.sqrMagnitude > 0.01f)
        {
            lastPlanarDirection = planarVelocity.normalized;
        }

        if (lastPlanarDirection.sqrMagnitude < 0.01f)
        {
            lastPlanarDirection = transform.forward.sqrMagnitude > 0.01f ? transform.forward.normalized : Vector3.forward;
        }

        Quaternion yawRotation = Quaternion.LookRotation(lastPlanarDirection, Vector3.up);

        float verticalFactor = Mathf.Clamp(currentVelocity.y / Mathf.Max(0.01f, verticalSpeed), -1f, 1f);
        float targetPitch = verticalFactor * maxPitchAngle;
        currentPitchAngle = Mathf.Lerp(currentPitchAngle, targetPitch, orientationLerpSpeed * Time.deltaTime);

        float targetRoll = Mathf.Clamp(planarInput.x, -1f, 1f) * maxRollAngle;
        targetRoll += GetCameraRollOffset();
        targetRoll = Mathf.Clamp(targetRoll, -maxRollAngle, maxRollAngle);
        currentRollAngle = Mathf.Lerp(currentRollAngle, targetRoll, orientationLerpSpeed * Time.deltaTime);

        Quaternion pitchRotation = Quaternion.AngleAxis(currentPitchAngle, yawRotation * Vector3.right);
        Quaternion rollRotation = Quaternion.AngleAxis(-currentRollAngle, yawRotation * Vector3.forward);
        Quaternion targetRotation = rollRotation * pitchRotation * yawRotation;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void RelaxOrientation()
    {
        currentPitchAngle = Mathf.Lerp(currentPitchAngle, 0f, orientationLerpSpeed * Time.deltaTime);
        currentRollAngle = Mathf.Lerp(currentRollAngle, 0f, orientationLerpSpeed * Time.deltaTime);
    }

    private float GetCameraRollOffset()
    {
        if (!alignWithCameraForward || cameraRollSensitivity <= 0f)
            return 0f;

        Vector3 cameraForward = GetCameraFlatForward();
        if (cameraForward.sqrMagnitude < 0.01f || lastPlanarDirection.sqrMagnitude < 0.01f)
            return 0f;

        float yawDelta = Vector3.SignedAngle(lastPlanarDirection, cameraForward, Vector3.up);
        float normalized = Mathf.Clamp(yawDelta * cameraRollSensitivity / maxRollAngle, -1f, 1f);
        return normalized * maxRollAngle;
    }

    private void MoveWithCollision(Vector3 desiredDisplacement)
    {
        Vector3 start = transform.position;

        if (desiredDisplacement.sqrMagnitude < 0.0001f)
            return;

        float distance = desiredDisplacement.magnitude;
        Vector3 direction = desiredDisplacement / distance;

        if (collisionRadius > 0f &&
            Physics.SphereCast(start, collisionRadius, direction, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float allowedDistance = Mathf.Max(0f, hit.distance - 0.05f);
            transform.position = start + direction * allowedDistance;
        }
        else
        {
            transform.position = start + desiredDisplacement;
        }
    }

    private Vector3 GetReferenceForward()
    {
        Vector3 forward = transform.forward;

        if (!alignWithCameraForward)
            return forward.normalized;

        Vector3 cameraForward = GetCameraFlatForward();
        if (cameraForward.sqrMagnitude > 0.01f)
        {
            forward = cameraForward;
        }

        return forward.normalized;
    }

    private Vector3 GetCameraFlatForward()
    {
        Transform cam = GetCameraTransform();
        if (cam == null)
            return Vector3.zero;

        Vector3 camForward = cam.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude > 0.01f)
        {
            camForward.Normalize();
        }

        return camForward;
    }

    private Vector3 GetCameraForward()
    {
        Transform cam = GetCameraTransform();
        if (cam == null)
            return Vector3.zero;

        Vector3 forward = cam.forward;
        if (forward.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return forward.normalized;
    }

    private Transform GetCameraTransform()
    {
        Transform cam = cameraTransform;

        if ((cam == null || !cam.gameObject.activeInHierarchy) && autoFindCamera)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cam = mainCamera.transform;
                cameraTransform = cam;
            }
        }

        return cam;
    }

    private void ValidateAnimatorParameter()
    {
        if (dragonAnimator == null || string.IsNullOrEmpty(speedParameter))
        {
            hasSpeedParameter = false;
            return;
        }

        hasSpeedParameter = AnimatorHasParameter(dragonAnimator, speedParameter, AnimatorControllerParameterType.Float);
        if (!hasSpeedParameter)
        {
            Debug.LogWarning($"[DragonMountController] Animator '{dragonAnimator.name}' не содержит параметр '{speedParameter}'. Скоростной бленд отключён.");
        }
    }

    private bool AnimatorHasParameter(Animator animatorComponent, string paramName, AnimatorControllerParameterType type)
    {
        foreach (var parameter in animatorComponent.parameters)
        {
            if (parameter.type == type && parameter.name == paramName)
            {
                return true;
            }
        }

        return false;
    }
}
