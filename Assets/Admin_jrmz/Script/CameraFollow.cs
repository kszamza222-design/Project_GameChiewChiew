using UnityEngine;

/// <summary>
/// CameraFollow — Soft Yaw Follow (ปรับให้ไม่มึนหัว)
///
/// สิ่งที่แก้จากเดิม:
///   • distance    6  → 9   (ถอยไกลขึ้น เห็นพื้นที่กว้างขึ้น)
///   • height      3  → 5   (สูงขึ้น มองลงมา ไม่ติดสิ่งกีดขวาง)
///   • yawFollowSpeed  3 → 1.5  (หมุนช้าลงครึ่งนึง)
///   • yawDeadzone    20 → 40   (รอให้ตัวละครหมุนมากขึ้นก่อนกล้องจะตาม)
///   • minSpeedToFollow 0.5 → 1.0 (กล้องไม่แพนเมื่อเดินช้า)
///   • positionSmooth  8 → 6   (กล้องตามช้าลงเล็กน้อย นุ่มขึ้น)
///   • lookAtHeightOffset 1.2 → 1.4 (มองสูงขึ้นเล็กน้อย)
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("── Target ─────────────────────────")]
    public Transform target;

    [Header("── Distance & Height ──────────────")]
    [Tooltip("ระยะห่างจากตัวละคร (เพิ่มจาก 6 → 9)")]
    public float distance        = 9f;

    [Tooltip("ความสูงกล้องเหนือตัวละคร (เพิ่มจาก 3 → 5)")]
    public float height          = 5f;

    [Header("── Soft Yaw Follow ─────────────────")]
    [Tooltip("ความเร็วที่กล้องแพนตามตัวละคร\nลดจาก 3 → 1.5 (หมุนช้าลงมาก ไม่มึน)")]
    public float yawFollowSpeed  = 1.5f;

    [Tooltip("องศาที่ตัวละครหมุนออกจากกล้องก่อนกล้องจะเริ่มแพนตาม\nเพิ่มจาก 20 → 40 (รอนานขึ้น กล้องนิ่งขึ้น)")]
    [Range(0f, 60f)]
    public float yawDeadzone     = 40f;

    [Tooltip("ตัวละครต้องเดินถึงความเร็วนี้ถึงจะให้กล้องตาม\nเพิ่มจาก 0.5 → 1.0 (ไม่แพนขณะเดินช้า)")]
    public float minSpeedToFollow = 1.0f;

    [Header("── Smoothing ────────────────────────")]
    [Tooltip("ความเร็วตามตำแหน่ง (ลดจาก 8 → 6 นุ่มขึ้น)")]
    public float positionSmooth  = 6f;

    [Tooltip("ความเร็วหมุน Look At")]
    public float rotationSmooth  = 10f;

    [Header("── Look At ─────────────────────────")]
    [Tooltip("กล้องมองสูงจากตัวละครกี่เมตร (เพิ่มจาก 1.2 → 1.4)")]
    public float lookAtHeightOffset = 1.4f;

    [Header("── Camera Collision ─────────────────")]
    public bool      cameraCollision = true;
    public LayerMask collisionMask;
    public float     minDistance     = 2f;

    // ─────────────────────────────────────────────────
    float   _currentYaw;
    Vector3 _lastTargetPos;

    void Start()
    {
        if (target == null) return;
        _currentYaw    = target.eulerAngles.y;
        _lastTargetPos = target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;
        UpdateYaw();
        ApplyCamera();
        _lastTargetPos = target.position;
    }

    void UpdateYaw()
    {
        float speed = Vector3.Distance(target.position, _lastTargetPos) / Time.deltaTime;
        if (speed < minSpeedToFollow) return;

        float targetYaw = target.eulerAngles.y;
        float delta     = Mathf.DeltaAngle(_currentYaw, targetYaw);
        if (Mathf.Abs(delta) <= yawDeadzone) return;

        float followDelta = delta - Mathf.Sign(delta) * yawDeadzone;
        _currentYaw = Mathf.LerpAngle(
            _currentYaw,
            _currentYaw + followDelta,
            yawFollowSpeed * Time.deltaTime
        );
    }

    void ApplyCamera()
    {
        Vector3 offset     = Quaternion.Euler(0f, _currentYaw, 0f)
                           * new Vector3(0f, height, -distance);
        Vector3 desiredPos = target.position + offset;

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

        transform.position = Vector3.Lerp(
            transform.position, desiredPos, positionSmooth * Time.deltaTime);

        Vector3    lookTarget = target.position + Vector3.up * lookAtHeightOffset;
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot, rotationSmooth * Time.deltaTime);
    }

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
