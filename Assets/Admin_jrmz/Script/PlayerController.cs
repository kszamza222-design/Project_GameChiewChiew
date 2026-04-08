using UnityEngine;

/// <summary>
/// PlayerController — Human Fall Flat style
/// ใช้ Animator Bool "IsCarrying" แทน Layer Weight
/// รองรับการยกของจากมุมใดก็ได้ (preserveGrabOffset)
///
/// Grab Offset System:
///   เมื่อหยิบวัตถุ จะรักษา Offset ไว้ก่อน แล้วค่อยๆ Lerp เข้า HoldPoint
///   ทำให้ยกจากมุมซ้าย/ขวา/หน้า/หลัง ได้อิสระ
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Movement (HFF Style) ──────────")]
    public float moveSpeed     = 5f;
    public float acceleration  = 50f;
    public float deceleration  = 30f;
    public float rotationSpeed = 600f;

    [Header("── Jump & Gravity ─────────────────")]
    public float jumpHeight    = 1.2f;
    public float gravity       = -15f;
    public LayerMask groundLayer;

    [Header("── Carry ──────────────────────────")]
    [Tooltip("ลาก HoldPoint_P1 หรือ HoldPoint_P2 มาใส่")]
    public Transform holdPoint;
    [Tooltip("รัศมีตรวจหยิบของรอบตัวละคร")]
    public float pickupRadius  = 2.2f;
    public LayerMask pickableLayer;
    [Tooltip("ตัวคูณความเร็วขณะถือของ")]
    [Range(0.1f, 1f)]
    public float carrySpeedMultiplier = 0.6f;

    [Header("── Grab Offset ─────────────────────")]
    [Tooltip("เปิด = รักษา Offset จากจุดที่หยิบ ยกจากมุมไหนก็ได้\nปิด = Snap วัตถุเข้า HoldPoint ทันที")]
    public bool  preserveGrabOffset = true;

    [Tooltip("ความเร็วที่ Offset ค่อยๆ Lerp เข้า HoldPoint\nต่ำ = ลอยอยู่นาน, สูง = เข้าเร็ว (แนะนำ 4-8)")]
    [Range(0.5f, 20f)]
    public float grabLerpSpeed = 5f;

    [Tooltip("ยกวัตถุขึ้นจากพื้นเมื่อหยิบ (Y offset ขั้นต่ำ)")]
    public float liftHeight = 0.3f;

    [Tooltip("ระยะ Offset สูงสุดที่ยอมให้ห่างจาก HoldPoint\n0 = ไม่จำกัด")]
    public float maxHoldDistance = 2f;

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

    Vector3 _horizontalVel;
    float   _verticalVel;
    bool    _isGrounded;
    float   _jumpCooldown;

    Rigidbody _heldRb;
    Collider  _myCollider;

    // ── Grab Offset State ──
    Vector3    _grabOffset;      // Offset ระหว่างวัตถุกับ HoldPoint ณ เวลาหยิบ
    Quaternion _grabWorldRot;    // Rotation ของวัตถุ ณ เวลาหยิบ
    float      _grabLerpT;       // 0 = อยู่ที่จุดหยิบ, 1 = อยู่ที่ HoldPoint

    // ── Animator hash ──
    static readonly int HashSpeed       = Animator.StringToHash("Speed");
    static readonly int HashMotionSpeed = Animator.StringToHash("MotionSpeed");
    static readonly int HashGrounded    = Animator.StringToHash("Grounded");
    static readonly int HashJump        = Animator.StringToHash("Jump");
    static readonly int HashFreeFall    = Animator.StringToHash("FreeFall");
    static readonly int HashIsCarrying  = Animator.StringToHash("IsCarrying");

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

    void Start()
    {
        if (holdPoint == null)
            Debug.LogWarning("[PlayerController] Hold Point ยังไม่ได้ผูก!");

        if (_anim != null)
        {
            bool found = false;
            foreach (var p in _anim.parameters)
                if (p.name == "IsCarrying") { found = true; break; }
            if (!found)
                Debug.LogWarning("[PlayerController] ไม่พบ Parameter 'IsCarrying' ใน Animator");
            else
                Debug.Log("[PlayerController] พบ Parameter 'IsCarrying' ✓");
        }
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
        HandleCarry();
        UpdateCarriedObject();
        AnimatorSync();
    }

    void FixedUpdate()
    {
        if (_heldRb == null || holdPoint == null) return;
        _heldRb.MovePosition(CalcCarryPos());
        _heldRb.MoveRotation(
            Quaternion.Slerp(_grabWorldRot, holdPoint.rotation, _grabLerpT));
    }

    // ═══════════════════════════════════════════════════
    //  GroundCheck
    // ═══════════════════════════════════════════════════

    void GroundCheck()
    {
        _jumpCooldown -= Time.deltaTime;
        if (_jumpCooldown > 0f) { _isGrounded = false; return; }
        _isGrounded = _cc.isGrounded;
        if (_isGrounded && _verticalVel < 0f) _verticalVel = -2f;
    }

    // ═══════════════════════════════════════════════════
    //  Jump
    // ═══════════════════════════════════════════════════

    void HandleJump()
    {
        if (_input.JumpPressed && _isGrounded)
        {
            _verticalVel  = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _jumpCooldown = 0.2f;
            _anim?.ResetTrigger(HashJump);
            _anim?.SetTrigger(HashJump);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Move
    // ═══════════════════════════════════════════════════

    void Move()
    {
        _verticalVel += gravity * Time.deltaTime;

        Vector3 targetVel = Vector3.zero;

        if (_cam != null && _input.MoveInput.sqrMagnitude > 0.01f)
        {
            Vector3 camF = _cam.transform.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = _cam.transform.right;   camR.y = 0f; camR.Normalize();
            Vector3 dir  = (camF * _input.MoveInput.y + camR * _input.MoveInput.x).normalized;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);

            float spd = _heldRb != null ? moveSpeed * carrySpeedMultiplier : moveSpeed;
            targetVel = dir * spd;
        }

        float rate = targetVel.sqrMagnitude > 0.01f ? acceleration : deceleration;
        _horizontalVel = Vector3.MoveTowards(_horizontalVel, targetVel, rate * Time.deltaTime);
        _cc.Move(new Vector3(_horizontalVel.x, _verticalVel, _horizontalVel.z) * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════
    //  HandleCarry
    // ═══════════════════════════════════════════════════

    void HandleCarry()
    {
        if (_input.ThrowPressed && _heldRb != null) { ThrowObject(); return; }
        if (_input.CarryPressed  && _heldRb == null) TryPickup();
        if (_input.CarryReleased && _heldRb != null) DropObject();
    }

    // ────────────────────────────────────────────────────
    //  TryPickup — บันทึก Offset จากจุดที่หยิบจริงๆ
    // ────────────────────────────────────────────────────

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

        if (preserveGrabOffset && holdPoint != null)
        {
            // ─ บันทึกตำแหน่ง/หมุน ณ เวลาหยิบ ─
            _grabWorldRot = _heldRb.rotation;

            // Offset = ตำแหน่งวัตถุ - ตำแหน่ง HoldPoint
            _grabOffset = _heldRb.position - holdPoint.position;

            // ยกขึ้นเล็กน้อยถ้าวัตถุอยู่ต่ำกว่า liftHeight
            if (_grabOffset.y < liftHeight)
                _grabOffset.y = liftHeight;

            // จำกัด maxHoldDistance ตั้งแต่แรก
            if (maxHoldDistance > 0f && _grabOffset.magnitude > maxHoldDistance)
                _grabOffset = _grabOffset.normalized * maxHoldDistance;

            _grabLerpT = 0f;  // เริ่มที่ offset เต็ม → ค่อยๆ เข้า HoldPoint
        }
        else if (holdPoint != null)
        {
            // Snap ทันที (preserveGrabOffset = false)
            _heldRb.position = holdPoint.position;
            _heldRb.rotation = holdPoint.rotation;
            _grabWorldRot    = holdPoint.rotation;
            _grabOffset      = Vector3.zero;
            _grabLerpT       = 1f;
        }

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
        rb.AddForce((transform.forward + Vector3.up * throwUpRatio).normalized
                    * throwForce, ForceMode.Impulse);
    }

    void ReleaseHeld()
    {
        ToggleHeldCollision(false);
        _heldRb.isKinematic = false;
        _heldRb.useGravity  = true;
        _heldRb             = null;
        _grabOffset         = Vector3.zero;
        _grabLerpT          = 0f;
    }

    void ToggleHeldCollision(bool ignore)
    {
        if (_heldRb == null || _myCollider == null) return;
        foreach (var col in _heldRb.GetComponentsInChildren<Collider>())
            Physics.IgnoreCollision(_myCollider, col, ignore);
    }

    // ═══════════════════════════════════════════════════
    //  UpdateCarriedObject
    //  Lerp Offset จากจุดหยิบ → HoldPoint อย่างนุ่มนวล
    // ═══════════════════════════════════════════════════

    void UpdateCarriedObject()
    {
        if (_heldRb == null || holdPoint == null) return;

        if (preserveGrabOffset)
        {
            // เพิ่ม t ทีละน้อย
            _grabLerpT = Mathf.MoveTowards(_grabLerpT, 1f,
                                            grabLerpSpeed * Time.deltaTime);

            _heldRb.transform.position = CalcCarryPos();
            _heldRb.transform.rotation = Quaternion.Slerp(
                _grabWorldRot, holdPoint.rotation, _grabLerpT);
        }
        else
        {
            _heldRb.transform.position = holdPoint.position;
            _heldRb.transform.rotation = holdPoint.rotation;
        }
    }

    // คำนวณตำแหน่งเป้าหมาย
    Vector3 CalcCarryPos()
    {
        if (!preserveGrabOffset || holdPoint == null)
            return holdPoint != null ? holdPoint.position : _heldRb.position;

        // Lerp offset จาก grabOffset → zero
        Vector3 offset = Vector3.Lerp(_grabOffset, Vector3.zero, _grabLerpT);

        // จำกัดระยะสูงสุด
        if (maxHoldDistance > 0f && offset.magnitude > maxHoldDistance)
            offset = offset.normalized * maxHoldDistance;

        return holdPoint.position + offset;
    }

    // ═══════════════════════════════════════════════════
    //  AnimatorSync
    // ═══════════════════════════════════════════════════

    void AnimatorSync()
    {
        if (_anim == null) return;

        _anim.SetFloat(HashSpeed,       _horizontalVel.magnitude, 0.15f, Time.deltaTime);
        _anim.SetFloat(HashMotionSpeed, 1f);
        _anim.SetBool(HashGrounded,     _isGrounded);
        _anim.SetBool(HashFreeFall,     !_isGrounded && _verticalVel < -1f);
        _anim.SetBool(HashIsCarrying,   _heldRb != null);

        if (_isGrounded) _anim.ResetTrigger(HashJump);
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
            Gizmos.DrawSphere(holdPoint.position, 0.12f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(holdPoint.position,
                            holdPoint.position + holdPoint.forward * 0.4f);

            if (maxHoldDistance > 0f)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
                Gizmos.DrawWireSphere(holdPoint.position, maxHoldDistance);
            }
        }
    }
}
