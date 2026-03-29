// PlayerInputHandler.cs
// วางบน GameObject เดียวกับ PlayerRagdoll และ ArmGrabSystem
//
// Setup:
//   1. ติดตั้ง Input System package (Package Manager)
//   2. สร้าง Input Action Asset ชื่อ "PlayerControls"
//   3. สร้าง Action Map ชื่อ "Gameplay" มี Actions:
//      - Move      (Value, Vector2)
//      - Jump      (Button)
//      - GrabLeft  (Button)
//      - GrabRight (Button)
//   4. ผูก Binding:
//      Player 1 — WASD + Space + Q + E
//      Player 2 — Arrows + RShift + / + .
//   5. ใน Inspector เลือก playerIndex (0 = P1, 1 = P2)

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerRagdoll))]
[RequireComponent(typeof(ArmGrabSystem))]
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Player Index")]
    [Tooltip("0 = Player1 (WASD), 1 = Player2 (Arrow Keys)")]
    public int playerIndex = 0;

    // References
    private PlayerRagdoll ragdoll;
    private ArmGrabSystem grabSystem;
    private PlayerInput playerInput;

    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction grabLeftAction;
    private InputAction grabRightAction;

    void Awake()
    {
        ragdoll     = GetComponent<PlayerRagdoll>();
        grabSystem  = GetComponent<ArmGrabSystem>();
        playerInput = GetComponent<PlayerInput>();

        // หา Action จาก Action Asset ที่ผูกไว้
        var gameplay = playerInput.actions.FindActionMap("Gameplay", true);
        moveAction      = gameplay.FindAction("Move",       true);
        jumpAction      = gameplay.FindAction("Jump",       true);
        grabLeftAction  = gameplay.FindAction("GrabLeft",   true);
        grabRightAction = gameplay.FindAction("GrabRight",  true);
    }

    void OnEnable()
    {
        // ผูก Event แบบ Callback (ไม่ต้อง Poll ใน Update ทุก Frame)
        jumpAction.performed      += OnJump;
        grabLeftAction.performed  += OnGrabLeft;
        grabRightAction.performed += OnGrabRight;

        moveAction.Enable();
        jumpAction.Enable();
        grabLeftAction.Enable();
        grabRightAction.Enable();
    }

    void OnDisable()
    {
        jumpAction.performed      -= OnJump;
        grabLeftAction.performed  -= OnGrabLeft;
        grabRightAction.performed -= OnGrabRight;

        moveAction.Disable();
        jumpAction.Disable();
        grabLeftAction.Disable();
        grabRightAction.Disable();
    }

    // ==================== Update ====================

    void Update()
    {
        // Move เป็น Value Action — อ่านทุก frame
        Vector2 move = moveAction.ReadValue<Vector2>();
        ragdoll.SetMoveInput(move);
    }

    // ==================== Callbacks ====================

    void OnJump(InputAction.CallbackContext ctx)
    {
        ragdoll.RequestJump();
    }

    void OnGrabLeft(InputAction.CallbackContext ctx)
    {
        grabSystem.TryGrabLeft();
    }

    void OnGrabRight(InputAction.CallbackContext ctx)
    {
        grabSystem.TryGrabRight();
    }
}
