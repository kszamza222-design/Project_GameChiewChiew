using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Player Identity")]
    public bool isPlayerOne = true;

    [Header("Movement Settings")]
    public float walkSpeed   = 5f;
    public float jumpHeight  = 2f;
    public float gravity     = -20f;
    public float rotateSpeed = 15f;

    [Header("Camera Reference")]
    public Transform cameraTransform;

    private CharacterController _cc;
    private Animator            _anim;
    private float               _verticalVelocity;
    private bool                _isGrounded;
    private bool                _wasGrounded;         // ← เพิ่ม: ป้องกัน jump ค้าง
    private bool                _jumpBuffered;        // ← เพิ่ม: buffer กันกด miss

    private static readonly int HashSpeed   = Animator.StringToHash("speed");
    private static readonly int HashJumping = Animator.StringToHash("isJumping");

    private Keyboard _kb;

    void Awake()
    {
        _cc   = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (_kb == null) _kb = Keyboard.current;
        if (_kb == null) return;

        _wasGrounded = _isGrounded;
        _isGrounded  = _cc.isGrounded;

        // Reset vertical velocity เมื่อแตะพื้น
        if (_isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        // ── Input ──────────────────────────────────────────
        float h = 0f, v = 0f;

        if (isPlayerOne)
        {
            if (_kb.dKey.isPressed) h =  1f;
            if (_kb.aKey.isPressed) h = -1f;
            if (_kb.wKey.isPressed) v =  1f;
            if (_kb.sKey.isPressed) v = -1f;

            // Buffer jump input ไว้ก่อน ตรวจสอบตอน isGrounded
            if (_kb.spaceKey.wasPressedThisFrame) _jumpBuffered = true;
        }
        else
        {
            if (_kb.rightArrowKey.isPressed) h =  1f;
            if (_kb.leftArrowKey.isPressed)  h = -1f;
            if (_kb.upArrowKey.isPressed)    v =  1f;
            if (_kb.downArrowKey.isPressed)  v = -1f;

            // ← แก้: ใช้ rightShiftKey แทน leftShiftKey เพื่อไม่ชนกัน
            // หรือจะใช้ leftShift ก็ได้ แต่ต้อง wasPressedThisFrame
            if (_kb.rightShiftKey.wasPressedThisFrame ||
                _kb.leftShiftKey.wasPressedThisFrame)
                _jumpBuffered = true;
        }

        float speed = new Vector2(h, v).magnitude;

        // ── Movement XZ ────────────────────────────────────
        Vector3 moveXZ = Vector3.zero;
        if (speed > 0.1f && cameraTransform != null)
        {
            Vector3 camF = cameraTransform.forward;
            Vector3 camR = cameraTransform.right;
            camF.y = 0f; camF.Normalize();
            camR.y = 0f; camR.Normalize();

            moveXZ = (camF * v + camR * h).normalized * walkSpeed;

            Quaternion targetRot = Quaternion.LookRotation(moveXZ.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // ── Jump ───────────────────────────────────────────
        if (_jumpBuffered && _isGrounded)
        {
            _verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            _jumpBuffered = false;
        }
        // ล้าง buffer ถ้าค้างนานเกิน (ป้องกันกระโดดเมื่อลงพื้นช้า)
        if (!_isGrounded) _jumpBuffered = false;

        // ── Gravity ────────────────────────────────────────
        _verticalVelocity += gravity * Time.deltaTime;

        // ── Move ───────────────────────────────────────────
        _cc.Move(new Vector3(moveXZ.x, _verticalVelocity, moveXZ.z) * Time.deltaTime);

        // ── Animator ───────────────────────────────────────
        _anim.SetFloat(HashSpeed,   speed,       0.1f, Time.deltaTime);
        _anim.SetBool (HashJumping, !_isGrounded);
    }
}