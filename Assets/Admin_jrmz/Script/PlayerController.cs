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
    [Tooltip("ความสูงในการกระโดด (เมตร) — แนะนำ 1.2")]
    public float jumpHeight    = 1.2f;
    [Tooltip("แรงโน้มถ่วง (ค่าลบ) — แนะนำ -15")]
    public float gravity       = -15f;
    [Tooltip("ระยะตรวจพื้น (เล็กน้อย เช่น 0.05)")]
    public float groundCheckOffset = 0.05f;
    public LayerMask groundLayer;

    [Header("── Carry / Drag ──────────────────")]
    [Tooltip("Transform ลูกของตัวละคร — จุดที่ถือของ (หน้าอก/มือ)")]
    public Transform holdPoint;
    [Tooltip("รัศมีตรวจ pickup รอบตัวละคร")]
    public float pickupRadius  = 2.2f;
    [Tooltip("Layer ที่ยกได้")]
    public LayerMask pickableLayer;

    [Header("── Throw ────────────────────────")]
    [Tooltip("แรงโยน (Impulse)")]
    public float throwForce    = 9f;
    [Tooltip("สัดส่วน upward ของแรงโยน")]
    [Range(0f, 1f)]
    public float throwUpRatio  = 0.25f;

    [Header("── Drag (ลากของบนพื้น) ──────────")]
    [Tooltip("แรงดึง Joint สำหรับลาก")]
    public float dragForce     = 500f;
    public float dragDamper    = 50f;

    // ═══════════════════════════════════════════════════
    //  Private state
    // ═══════════════════════════════════════════════════

    CharacterController _cc;
    PlayerInputHandler  _input;
    Animator            _anim;
    Camera              _cam;

    Vector3  _velocity;
    bool     _isGrounded;
    float    _jumpCooldown = 0f;

    // Carry
    Rigidbody        _heldRb;
    Collider         _myCollider;

    // Drag
    ConfigurableJoint _dragJoint;
    Rigidbody         _draggedRb;

    // ── Animator hash (ชื่อตรงกับ Starter Assets Animator Controller) ──
    static readonly int HashSpeed       = Animator.StringToHash("Speed");
    static readonly int HashMotionSpeed = Animator.StringToHash("MotionSpeed");
    static readonly int HashGrounded    = Animator.StringToHash("Grounded");
    static readonly int HashJump        = Animator.StringToHash("Jump");
    static readonly int HashFreeFall    = Animator.StringToHash("FreeFall");
    static readonly int HashCarry       = Animator.StringToHash("IsCarrying");

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

    /// <summary>เรียกจาก SplitScreenManager เพื่อผูก Camera ที่ถูกต้อง</summary>
    public void AssignCamera(Camera cam) => _cam = cam;

    // ═══════════════════════════════════════════════════
    //  Update loop
    // ═══════════════════════════════════════════════════

    void Update()
    {
        GroundCheck();
        Move();
        HandleJump();
        ApplyGravity();
        HandleInteract();
        UpdateCarriedObject();
        AnimatorSync();
    }

    // ═══════════════════════════════════════════════════
    //  Ground — คำนวณจากจุดล่างสุดของ CharacterController จริงๆ
    //
    //  CharacterController.center คือ offset จาก transform.position
    //  จุดล่างสุดของ capsule = position + center - down*(height/2 - radius)
    //  แล้ว CheckSphere รัศมีเล็กๆ ตรงนั้น
    // ═══════════════════════════════════════════════════

    void GroundCheck()
    {
        _jumpCooldown -= Time.deltaTime;

        // ยังอยู่ใน cooldown → ถือว่าลอยอยู่
        if (_jumpCooldown > 0f)
        {
            _isGrounded = false;
            return;
        }

        // จุดล่างสุดของ Capsule (ขอบล่างของ Sphere ล่าง)
        Vector3 bottom = transform.position
                       + _cc.center
                       + Vector3.down * (_cc.height * 0.5f - _cc.radius);

        _isGrounded = Physics.CheckSphere(
            bottom,
            _cc.radius + 0.05f,   // บวกนิดนึงให้ตรวจเผื่อ
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
    }

    // ═══════════════════════════════════════════════════
    //  Movement
    // ═══════════════════════════════════════════════════

    void Move()
    {
        if (_cam == null || _input.MoveInput.sqrMagnitude < 0.01f) return;

        // แปลงทิศตาม Camera
        Vector3 camF = _cam.transform.forward; camF.y = 0f; camF.Normalize();
        Vector3 camR = _cam.transform.right;   camR.y = 0f; camR.Normalize();

        Vector3 dir = (camF * _input.MoveInput.y + camR * _input.MoveInput.x).normalized;

        // หมุนตัวละครหาทิศเดิน
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                                                       rotationSpeed * Time.deltaTime);

        float speed = _heldRb != null ? moveSpeed * 0.7f : moveSpeed;
        _cc.Move(dir * speed * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════
    //  Jump & Gravity
    // ═══════════════════════════════════════════════════

    void HandleJump()
    {
        if (_input.JumpPressed && _isGrounded)
        {
            _velocity.y   = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _jumpCooldown = 0.2f;
            _anim?.ResetTrigger(HashJump);
            _anim?.SetTrigger(HashJump);
        }
    }

    void ApplyGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════
    //  Interact (ยก / วาง / โยน)
    // ═══════════════════════════════════════════════════

    void HandleInteract()
    {
        if (_input.ThrowPressed && _heldRb != null)
        {
            ThrowObject();
            return;
        }

        if (_input.InteractPressed)
        {
            if (_heldRb != null)
                DropObject();
            else
                TryPickup();
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

        _heldRb = bestRb;
        _heldRb.useGravity    = false;
        _heldRb.isKinematic   = true;
        _heldRb.interpolation = RigidbodyInterpolation.Interpolate;

        ToggleHeldCollision(ignore: true);
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

        Vector3 throwDir = transform.forward + Vector3.up * throwUpRatio;
        rb.AddForce(throwDir.normalized * throwForce, ForceMode.Impulse);
    }

    void ReleaseHeld()
    {
        ToggleHeldCollision(ignore: false);
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

        float targetSpeed = _input.MoveInput.magnitude
                          * (_heldRb != null ? moveSpeed * 0.7f : moveSpeed);

        _anim.SetFloat(HashSpeed,       targetSpeed, 0.1f, Time.deltaTime);
        _anim.SetFloat(HashMotionSpeed, 1f);
        _anim.SetBool(HashGrounded,     _isGrounded);
        _anim.SetBool(HashFreeFall,     !_isGrounded && _velocity.y < -1f);
        _anim.SetBool(HashCarry,        _heldRb != null);
    }

    // ═══════════════════════════════════════════════════
    //  Gizmos
    // ═══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // Pickup radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // HoldPoint
        if (holdPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(holdPoint.position, 0.08f);
        }

        // GroundCheck sphere — แสดงตำแหน่งจริงที่ตรวจพื้น
        if (_cc != null)
        {
            Vector3 bottom = transform.position
                           + _cc.center
                           + Vector3.down * (_cc.height * 0.5f - _cc.radius);

            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(bottom, _cc.radius + 0.05f);
        }
    }
}
