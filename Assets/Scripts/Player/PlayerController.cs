using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController)), RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float rotationSpeed = 10f;
    public bool cameraRelative = false;

    [Header("Animation")]
    public float animDamp = 0.1f;      // damping for SetFloat smoothing
    public float moveDeadzone = 0.04f; // LS deadzone
    public float lookDeadzone = 0.04f; // RS deadzone

    [Header("Mouse Aim")]
    public bool useMouseAim = true;              // enable/disable mouse-driven facing
    public LayerMask aimLayerMask = ~0;          // what the aim ray can hit (e.g., Ground)
    public float rayMaxDistance = 200f;          // how far the aim ray goes
    public float fallbackPlaneY = 0f;            // if no collider hit, intersect a Y=constant plane

    private CharacterController controller;
    private PlayerInput input;
    private InputAction moveAction;
    private InputAction lookAction;              // still used for right-stick / mouse-delta if desired
    private Transform cam;
    private Animator anim;

    private HealthBarUI healthBarUI;

    public int playerNumber { get; private set; } = 0;

    private float lockedY;

    public AudioSource walkingSound;

    // Control scheme gate: true only when current active scheme is Keyboard+Mouse
    private bool isKeyboardMouse;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();
        moveAction = input.actions["Move"];
        lookAction = input.actions["Look"];
        anim = GetComponentInChildren<Animator>();
        if (Camera.main) cam = Camera.main.transform;
    }

    void Start()
    {
        // Store the starting height as the fixed height
        lockedY = transform.position.y;
    }

    public void SetPlayerNumber(int number)
    {
        playerNumber = number;
        Debug.Log($"{name} assigned player number {playerNumber}");

        // Ensure HealthBarUI knows about the player number
        if (healthBarUI == null)
        {
            healthBarUI = GetComponentInChildren<HealthBarUI>();
        }

        if (healthBarUI != null)
        {
            healthBarUI.SetPlayerNumber(number);
        }
        else
        {
            Debug.LogWarning($"{name}: Cannot forward player number to HealthBarUI because it's missing.");
        }
    }

    void OnEnable()
    {
        // Subscribe to control scheme changes and do an initial evaluation
        UpdateAimMode();
        input.onControlsChanged += OnControlsChanged;
    }

    void OnDisable()
    {
        if (input != null)
            input.onControlsChanged -= OnControlsChanged;
    }

    private void OnControlsChanged(PlayerInput pi) => UpdateAimMode();

    private void UpdateAimMode()
    {
        // Prefer control scheme name (e.g., "Keyboard&Mouse", "Gamepad")
        var scheme = input.currentControlScheme ?? string.Empty;

        // Consider any scheme with "keyboard" or "mouse" in its name as KB&M
        isKeyboardMouse =
            scheme.IndexOf("keyboard", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            scheme.IndexOf("mouse", System.StringComparison.OrdinalIgnoreCase) >= 0;

        // Fallback: inspect attached devices (robust if scheme names differ)
        if (!isKeyboardMouse)
        {
            var devices = input.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i] is Keyboard || devices[i] is Mouse)
                {
                    isKeyboardMouse = true;
                    break;
                }
            }
        }
        // Debug.Log($"[PlayerController] Scheme='{scheme}'  isKeyboardMouse={isKeyboardMouse}");
    }

    void Update()
    {
        Vector2 move2D = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        Vector2 look2D = lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        Vector3 moveDir = StickToWorld(move2D);
        Vector3 lookDir = StickToWorld(look2D);

        bool isMoving = moveDir.sqrMagnitude > moveDeadzone * moveDeadzone;

        // ---- MOVE ----
        if (isMoving)
        {
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
            
            if (!walkingSound.isPlaying)
            {
                walkingSound.Play();
            }
        }
        else
        {
            if (walkingSound.isPlaying)
            {
                walkingSound.Stop();
            }
        }
        // ---- ROTATE (Mouse > RS > Move) ----
        Vector3 faceDir = Vector3.zero;

        // Allow mouse aim only when the active scheme is Keyboard&Mouse
        bool allowMouseAim = useMouseAim && isKeyboardMouse;

        // 1) Mouse cursor facing (highest priority when allowed)
        if (allowMouseAim && TryGetMouseAimDirection(out var mouseDir))
        {
            faceDir = mouseDir;
        }
        // 2) Right stick / Look vector
        else if (lookDir.sqrMagnitude > lookDeadzone * lookDeadzone)
        {
            faceDir = lookDir;
        }
        // 3) Move direction
        else if (moveDir.sqrMagnitude > moveDeadzone * moveDeadzone)
        {
            faceDir = moveDir;
        }

        if (faceDir != Vector3.zero)
        {
            Quaternion target = Quaternion.LookRotation(faceDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        // ---- ANIMATOR ----
        UpdateAnimatorValues(moveDir, faceDir);
    }

    void LateUpdate()
    {
        // Lock the player's height so they can never rise or fall
        Vector3 pos = transform.position;
        pos.y = lockedY;
        transform.position = pos;
    }

    private void UpdateAnimatorValues(Vector3 moveWorld, Vector3 faceWorld)
    {
        bool hasMove = moveWorld.sqrMagnitude > moveDeadzone * moveDeadzone;

        // Speed from left stick magnitude (0..1)
        float speed = Mathf.Clamp01((moveAction?.ReadValue<Vector2>() ?? Vector2.zero).magnitude);

        // Movement direction relative to CURRENT facing (local space)
        Vector3 localMove = hasMove ? transform.InverseTransformDirection(moveWorld.normalized) : Vector3.zero;

        anim.SetFloat("Speed", speed, animDamp, Time.deltaTime);
        anim.SetFloat("MoveX", localMove.x, animDamp, Time.deltaTime);
        anim.SetFloat("MoveY", localMove.z, animDamp, Time.deltaTime);
    }

    private Vector3 StickToWorld(Vector2 stick)
    {
        if (stick.sqrMagnitude < 0.0001f) return Vector3.zero;

        if (!cameraRelative || cam == null)
            return new Vector3(stick.x, 0f, stick.y).normalized;

        // Camera-relative XZ
        Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = cam.right; right.y = 0f; right.Normalize();
        return (fwd * stick.y + right * stick.x).normalized;
    }

    // --- Mouse-to-world aim helper ---
    private bool TryGetMouseAimDirection(out Vector3 dir)
    {
        dir = Vector3.zero;
        if (cam == null) return false;

        Vector2 screenPos = Vector2.zero;
        if (Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();
        else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            screenPos = Touchscreen.current.touches[0].position.ReadValue();
        else
            return false;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // Prefer hitting real colliders (e.g., ground/navmesh)
        if (Physics.Raycast(ray, out var hit, rayMaxDistance, aimLayerMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 v = hit.point - transform.position;
            v.y = 0f;
            if (v.sqrMagnitude > 0.0001f) { dir = v.normalized; return true; }
        }
        else
        {
            // Fallback: intersect an infinite horizontal plane at Y = fallbackPlaneY
            Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackPlaneY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter);
                Vector3 v = point - transform.position;
                v.y = 0f;
                if (v.sqrMagnitude > 0.0001f) { dir = v.normalized; return true; }
            }
        }

        return false;
    }
}

// Small helper so SetBool checks don�t throw if param doesn�t exist
public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator self, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in self.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }
}
