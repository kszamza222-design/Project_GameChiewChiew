using UnityEngine;

/// <summary>
/// ควบคุมการเคลื่อนที่  กระโดด  ยกของ  ลากของ  โยนของ
/// ต้องการ Component บน GameObject เดียวกัน :
///   • CharacterController
///   • PlayerInputHandler
///   • Animator  (optional — ถ้ามี จะส่ง parameter ให้อัตโนมัติ)
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Movement ──────────────────────")]
    [Tooltip("ความเร็วเดินปกติ (m/s)")]
    public float moveSpeed     = 5f;
    [Tooltip("ความเร็วหมุนตัวละคร (องศา/วินาที)")]
    public float rotationSpeed = 600f;

    [Header("── Jump & Gravity ─────────────────")]
    [Tooltip("ความสูงในการกระโดด (เมตร)")]
    public float jumpHeight    = 1.2f;
    [Tooltip("แรงโน้มถ่วง (ค่าลบ)")]
    public float gravity       = -15f;
    public LayerMask groundLayer;

    [Header("── Carry ──────────────────────────")]
    public Transform holdPoint;
    public float pickupRadius  = 2.2f;
    public LayerMask pickableLayer;

    [Header("── Throw ───────────────────────────")]
    public float throwForce    = 9f;
    [Range(0f, 1f)]
    public float throwUpRatio  = 0.25f;

    [Header("── Drag ────────────────────────────")]
    public float dragForce     = 500f;
    public float dragDamper    = 50f;

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    CharacterController _cc;
    PlayerInputHandler  _input;
    Animator            _anim;
    Camera              _cam;

    Vector3 _moveVelocity;
    bool    _isGrounded;
    float   _jumpCooldown;

    Rigidbody         _heldRb;
    Collider          _myCollider;
    ConfigurableJoint _dragJoint;
    Rigidbody         _draggedRb;

    static readonly int HashSpeed       = Animator.StringToHash("Speed");
    static readonly int HashMotionSpeed = Animator.StringToHash("MotionSpeed");
    static readonly int HashGrounded    = Animator.StringToHash("Grounded");
    static readonly int HashJump        = Animator.StringToHash("Jump");
    static readonly int HashFreeFall    = Animator.StringToHash("FreeFall");

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        _cc         = GetComponent<CharacterController>();
        _input      = GetComponent<PlayerInputHandler>();
        _anim       = GetComponentInChildren<Animator>();
        _myCollider = GetComponent<Collider>();
    }

    public void AssignCamera(Camera cam) => _cam = cam;

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        GroundCheck();
        HandleJump();
        Move();
        HandleInteract();
        UpdateCarriedObject();
        AnimatorSync();
    }

    // ═══════════════════════════════════════════════════
    //  GroundCheck
    // ═══════════════════════════════════════════════════

    void GroundCheck()
    {
        _jumpCooldown -= Time.deltaTime;

        if (_jumpCooldown > 0f)
        {
            _isGrounded = false;
            return;
        }

        _isGrounded = _cc.isGrounded;

        if (_isGrounded && _moveVelocity.y < 0f)
            _moveVelocity.y = -2f;
    }

    // ═══════════════════════════════════════════════════
    //  HandleJump
    // ═══════════════════════════════════════════════════

    void HandleJump()
    {
        if (_input.JumpPressed && _isGrounded)
        {
            _moveVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _jumpCooldown   = 0.2f;
            _anim?.ResetTrigger(HashJump);
            _anim?.SetTrigger(HashJump);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Move — cc.Move() ครั้งเดียวต่อ Frame
    // ═══════════════════════════════════════════════════

    void Move()
    {
        // Gravity
        _moveVelocity.y += gravity * Time.deltaTime;

        // Horizontal
        Vector3 horizontal = Vector3.zero;

        if (_cam != null && _input.MoveInput.sqrMagnitude > 0.01f)
        {
            Vector3 camF = _cam.transform.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = _cam.transform.right;   camR.y = 0f; camR.Normalize();

            Vector3 dir = (camF * _input.MoveInput.y + camR * _input.MoveInput.x).normalized;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            float spd = _heldRb != null ? moveSpeed * 0.7f : moveSpeed;
            horizontal = dir * spd;
        }

        // Move ครั้งเดียว — รวม horizontal + vertical
        Vector3 motion = new Vector3(horizontal.x, _moveVelocity.y, horizontal.z);
        _cc.Move(motion * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════
    //  Interact
    // ═══════════════════════════════════════════════════

    void HandleInteract()
    {
        if (_input.ThrowPressed && _heldRb != null) { ThrowObject(); return; }

        if (_input.InteractPressed)
        {
            if (_heldRb != null) DropObject();
            else                 TryPickup();
        }
    }

    void TryPickup()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, pickableLayer);
        if (hits.Length == 0) return;

        float closest = float.MaxValue;
        Rigidbody bestRb = null;
        foreach (var col in hits)
        {
            if (!col.TryGetComponent(out Rigidbody rb)) continue;
            float d = Vector3.Distance(transform.position, col.transform.position);
            if (d < closest) { closest = d; bestRb = rb; }
        }
        if (bestRb == null) return;

        _heldRb               = bestRb;
        _heldRb.useGravity    = false;
        _heldRb.isKinematic   = true;
        _heldRb.interpolation = RigidbodyInterpolation.Interpolate;
        ToggleHeldCollision(true);
        _heldRb.GetComponent<PickableObject>()?.OnPickedUp();
    }

    void DropObject()
    {
        if (_heldRb == null) return;
        _heldRb.GetComponent<PickableObject>()?.OnDropped();
        ReleaseHeld();
    }

    void ThrowObject()
    {
        if (_heldRb == null) return;
        var rb = _heldRb;
        _heldRb.GetComponent<PickableObject>()?.OnDropped();
        ReleaseHeld();
        Vector3 dir = transform.forward + Vector3.up * throwUpRatio;
        rb.AddForce(dir.normalized * throwForce, ForceMode.Impulse);
    }

    void ReleaseHeld()
    {
        ToggleHeldCollision(false);
        _heldRb.isKinematic = false;
        _heldRb.useGravity  = true;
        _heldRb = null;
    }

    void ToggleHeldCollision(bool ignore)
    {
        if (_heldRb == null || _myCollider == null) return;
        foreach (var col in _heldRb.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(_myCollider, col, ignore);
    }

    // ═══════════════════════════════════════════════════
    //  Carry
    // ═══════════════════════════════════════════════════

    void UpdateCarriedObject()
    {
        if (_heldRb == null || holdPoint == null) return;
        _heldRb.MovePosition(holdPoint.position);
        _heldRb.MoveRotation(holdPoint.rotation);
    }

    // ═══════════════════════════════════════════════════
    //  Animator sync
    // ═══════════════════════════════════════════════════

    void AnimatorSync()
    {
        if (_anim == null) return;

        float spd = _input.MoveInput.magnitude
                  * (_heldRb != null ? moveSpeed * 0.7f : moveSpeed);

        _anim.SetFloat(HashSpeed,       spd, 0.1f, Time.deltaTime);
        _anim.SetFloat(HashMotionSpeed, 1f);
        _anim.SetBool(HashGrounded,     _isGrounded);
        _anim.SetBool(HashFreeFall,     !_isGrounded && _moveVelocity.y < -1f);

        // ล้าง Jump Trigger ทันทีที่ติดพื้น ป้องกัน Trigger ค้างวนลูป
        if (_isGrounded)
            _anim.ResetTrigger(HashJump);
    }

    // ═══════════════════════════════════════════════════
    //  Gizmos
    // ═══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        if (holdPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(holdPoint.position, 0.08f);
        }
    }
}
