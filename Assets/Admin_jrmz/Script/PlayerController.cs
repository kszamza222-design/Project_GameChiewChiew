using UnityEngine;
using UnityEngine.InputSystem;  // ← เปลี่ยน namespace

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

    private static readonly int HashSpeed   = Animator.StringToHash("speed");
    private static readonly int HashJumping = Animator.StringToHash("isJumping");

    // ── Keyboard reference (New Input System)
    private Keyboard _kb;

    void Awake()
    {
        _cc   = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();
        _kb   = Keyboard.current;
    }

    void Update()
    {
        if (_kb == null) _kb = Keyboard.current;
        if (_kb == null) return; // ยังไม่มี keyboard

        _isGrounded = _cc.isGrounded;

        if (_isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;

        // ── Input ──────────────────────────────────────────
        float h = 0f, v = 0f;

        if (isPlayerOne)
        {
            if (_kb.dKey.isPressed) h =  1f;
            if (_kb.aKey.isPressed) h = -1f;
            if (_kb.wKey.isPressed) v =  1f;
            if (_kb.sKey.isPressed) v = -1f;
        }
        else
        {
            if (_kb.rightArrowKey.isPressed) h =  1f;
            if (_kb.leftArrowKey.isPressed)  h = -1f;
            if (_kb.upArrowKey.isPressed)    v =  1f;
            if (_kb.downArrowKey.isPressed)  v = -1f;
        }

        float speed = new Vector2(h, v).magnitude;

        // ── Movement XZ ────────────────────────────────────
        Vector3 moveXZ = Vector3.zero;
        if (speed > 0.1f)
        {
            Vector3 camF = cameraTransform != null
                           ? cameraTransform.forward : Vector3.forward;
            Vector3 camR = cameraTransform != null
                           ? cameraTransform.right   : Vector3.right;
            camF.y = 0f; camF.Normalize();
            camR.y = 0f; camR.Normalize();

            moveXZ = (camF * v + camR * h).normalized * walkSpeed;

            Quaternion targetRot = Quaternion.LookRotation(moveXZ.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // ── Jump ───────────────────────────────────────────
        bool jumpPressed = isPlayerOne
            ? _kb.spaceKey.wasPressedThisFrame
            : _kb.leftShiftKey.wasPressedThisFrame;

        if (jumpPressed && _isGrounded)
            _verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);

        // ── Move (รวมทุกแกนครั้งเดียว) ─────────────────────
        _cc.Move(new Vector3(moveXZ.x, _verticalVelocity, moveXZ.z) * Time.deltaTime);

        // ── Animator ───────────────────────────────────────
        _anim.SetFloat(HashSpeed,   speed,       0.1f, Time.deltaTime);
        _anim.SetBool (HashJumping, !_isGrounded);
    }
}