using UnityEngine;

/// <summary>
/// CameraFollow — Soft Yaw Follow
/// กล้องตามหลังตัวละครแบบนุ่มนวล
/// เมื่อตัวละครเดิน/วิ่งไปทิศใหม่ กล้องจะค่อยๆ แพนตาม
/// ไม่หมุนทันทีแบบกล้องล็อค แต่ให้ความรู้สึก Third-Person แบบสมจริง
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Target ─────────────────────────")]
    public Transform target;

    [Header("── Distance & Height ──────────────")]
    [Tooltip("ระยะห่างจากตัวละคร")]
    public float distance        = 6f;
    [Tooltip("ความสูงกล้องเหนือตัวละคร")]
    public float height          = 3f;

    [Header("── Soft Yaw Follow ─────────────────")]
    [Tooltip("ความเร็วที่กล้องแพนตามตัวละคร (ต่ำ = ช้า นุ่มนวล)")]
    public float yawFollowSpeed  = 3f;
    [Tooltip("องศาที่ตัวละครหมุนออกจากกล้องก่อนกล้องจะเริ่มแพนตาม\n0 = ตามทันที, 30 = รอให้หมุน 30 องศาก่อน")]
    [Range(0f, 60f)]
    public float yawDeadzone     = 20f;
    [Tooltip("ตัวละครต้องเคลื่อนที่ถึงความเร็วนี้ถึงจะให้กล้องตาม (m/s)")]
    public float minSpeedToFollow = 0.5f;

    [Header("── Smoothing ────────────────────────")]
    [Tooltip("ความเร็วตามตำแหน่ง")]
    public float positionSmooth  = 8f;
    [Tooltip("ความเร็วหมุน Look At")]
    public float rotationSmooth  = 12f;

    [Header("── Look At ─────────────────────────")]
    [Tooltip("กล้องมองสูงจากตัวละครกี่เมตร")]
    public float lookAtHeightOffset = 1.2f;

    [Header("── Camera Collision ─────────────────")]
    public bool      cameraCollision = true;
    public LayerMask collisionMask;
    public float     minDistance     = 1f;

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    float   _currentYaw;      // มุม Yaw ปัจจุบันของกล้อง
    Vector3 _lastTargetPos;   // ตำแหน่งล่าสุดของ Target

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Start()
    {
        if (target == null) return;

        // กล้องเริ่มต้นอยู่หลังตัวละคร
        _currentYaw    = target.eulerAngles.y;
        _lastTargetPos = target.position;
    }

    // ═══════════════════════════════════════════════════
    //  LateUpdate
    // ═══════════════════════════════════════════════════

    void LateUpdate()
    {
        if (target == null) return;

        UpdateYaw();
        ApplyCamera();

        _lastTargetPos = target.position;
    }

    // ═══════════════════════════════════════════════════
    //  UpdateYaw — แพนกล้องตามทิศตัวละคร
    // ═══════════════════════════════════════════════════

    void UpdateYaw()
    {
        // คำนวณความเร็วตัวละครจาก position delta
        float speed = Vector3.Distance(target.position, _lastTargetPos) / Time.deltaTime;

        // ถ้าตัวละครหยุดหรือเดินช้ามาก → กล้องไม่แพน
        if (speed < minSpeedToFollow) return;

        // มุม Yaw ของตัวละคร
        float targetYaw = target.eulerAngles.y;

        // หา delta มุมระหว่างกล้องกับตัวละคร
        float delta = Mathf.DeltaAngle(_currentYaw, targetYaw);

        // ถ้าอยู่ใน deadzone ยังไม่แพน
        if (Mathf.Abs(delta) <= yawDeadzone) return;

        // แพนแค่ส่วนที่เกิน deadzone
        float followDelta = delta - Mathf.Sign(delta) * yawDeadzone;

        _currentYaw = Mathf.LerpAngle(
            _currentYaw,
            _currentYaw + followDelta,
            yawFollowSpeed * Time.deltaTime
        );
    }

    // ═══════════════════════════════════════════════════
    //  ApplyCamera — คำนวณและเคลื่อน Camera
    // ═══════════════════════════════════════════════════

    void ApplyCamera()
    {
        // คำนวณตำแหน่งกล้องจาก Yaw ปัจจุบัน
        Vector3 offset = Quaternion.Euler(0f, _currentYaw, 0f)
                       * new Vector3(0f, height, -distance);
        Vector3 desiredPos = target.position + offset;

        // ── Camera Collision ──
        if (cameraCollision)
        {
            Vector3 dir     = desiredPos - target.position;
            float   maxDist = dir.magnitude;

            if (Physics.SphereCast(target.position, 0.2f, dir.normalized,
                                   out RaycastHit hit, maxDist, collisionMask))
            {
                float safeDist = Mathf.Max(hit.distance - 0.1f, minDistance);
                desiredPos = target.position + dir.normalized * safeDist;
            }
        }

        // ── Smooth Position ──
        transform.position = Vector3.Lerp(
            transform.position, desiredPos, positionSmooth * Time.deltaTime);

        // ── Look At ──
        Vector3    lookTarget  = target.position + Vector3.up * lookAtHeightOffset;
        Quaternion desiredRot  = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot, rotationSmooth * Time.deltaTime);
    }

    // ── เปลี่ยน Target จากภายนอก ──────────────────────
    public void SetTarget(Transform t)
    {
        target = t;
        if (t != null)
        {
            _currentYaw    = t.eulerAngles.y;
            _lastTargetPos = t.position;
        }
    }
}
