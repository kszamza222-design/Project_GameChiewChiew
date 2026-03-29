using UnityEngine;

/// <summary>
/// PlayerController — Human Fall Flat style
/// ใช้ Animator Bool "IsCarrying" แทน Layer Weight
/// สร้าง Transition ใน Base Layer: Idle Walk Run Blend ↔ Carry_Blend
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
    [Tooltip("ตัวคูณความเร็วขณะถือของ (0.5 = ช้าลงครึ่งนึง)")]
    [Range(0.1f, 1f)]
    public float carrySpeedMultiplier = 0.5f;

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

    Rigidbody         _heldRb;
    Collider          _myCollider;
    ConfigurableJoint _dragJoint;
    Rigidbody         _draggedRb;

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
            Debug.LogWarning($"[{name}] Hold Point ยังไม่ได้ผูก!");

        if (_anim != null)
        {
            bool found = false;
            foreach (var p in _anim.parameters)
                if (p.name == "IsCarrying") { found = true; break; }

            if (!found)
                Debug.LogError($"[{name}] ไม่พบ Parameter 'IsCarrying' ใน Animator!");
            else
                Debug.Log($"[{name}] พบ Parameter 'IsCarrying' ✓");
        }
        else
        {
            Debug.LogError($"[{name}] ไม่พบ Animator Component!");
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
        if (_heldRb != null && holdPoint != null)
        {
            _heldRb.MovePosition(holdPoint.position);
            _heldRb.MoveRotation(holdPoint.rotation);
        }
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

            Vector3 dir = (camF * _input.MoveInput.y + camR * _input.MoveInput.x).normalized;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            // ── ถือของ = ช้าลงตาม carrySpeedMultiplier ──
            float spd = _heldRb != null ? moveSpeed * carrySpeedMultiplier : moveSpeed;
            targetVel = dir * spd;
        }

        float rate = targetVel.sqrMagnitude > 0.01f ? acceleration : deceleration;
        _horizontalVel = Vector3.MoveTowards(_horizontalVel, targetVel, rate * Time.deltaTime);

        Vector3 motion = new Vector3(_horizontalVel.x, _verticalVel, _horizontalVel.z);
        _cc.Move(motion * Time.deltaTime);
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

    void TryPickup()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, pickableLayer);
        if (hits.Length == 0)
        {
            Debug.Log($"[{name}] TryPickup: ไม่พบ Object ใน radius {pickupRadius}");
            return;
        }

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

        if (holdPoint != null)
        {
            _heldRb.position = holdPoint.position;
            _heldRb.rotation = holdPoint.rotation;
        }

        ToggleHeldCollision(true);
        _heldRb.GetComponent<PickableObject>()?.OnPickedUp();
        Debug.Log($"[{name}] หยิบ {bestRb.name} ✓");
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
    //  UpdateCarriedObject
    // ═══════════════════════════════════════════════════

    void UpdateCarriedObject()
    {
        if (_heldRb == null || holdPoint == null) return;
        _heldRb.transform.position = holdPoint.position;
        _heldRb.transform.rotation = holdPoint.rotation;
    }

    // ═══════════════════════════════════════════════════
    //  AnimatorSync
    // ═══════════════════════════════════════════════════

    void AnimatorSync()
    {
        if (_anim == null) return;

        bool isCarrying = _heldRb != null;
        float spd       = _horizontalVel.magnitude;

        _anim.SetFloat(HashSpeed,       spd, 0.15f, Time.deltaTime);
        _anim.SetFloat(HashMotionSpeed, 1f);
        _anim.SetBool(HashGrounded,     _isGrounded);
        _anim.SetBool(HashFreeFall,     !_isGrounded && _verticalVel < -1f);
        _anim.SetBool(HashIsCarrying,   isCarrying);

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
            Gizmos.DrawSphere(holdPoint.position, 0.12f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(holdPoint.position,
                            holdPoint.position + holdPoint.forward * 0.4f);
        }
    }
}
