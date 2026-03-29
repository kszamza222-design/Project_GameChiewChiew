using UnityEngine;

/// <summary>
/// กล้อง Third-Person Smooth Follow สำหรับแต่ละ Player
/// วาง Script นี้บน Camera GameObject
///
/// ─ Features ─
///   • Smooth position lerp
///   • LookAt target ด้วย upward offset
///   • ชนสิ่งกีดขวาง → ดึงกล้องเข้าหาตัวละคร (camera collision)
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("── Target ─────────────────────────")]
    public Transform target;

    [Header("── Offset ──────────────────────────")]
    [Tooltip("ระยะห่างจากตัวละคร")]
    public float distance   = 6f;
    [Tooltip("ความสูงกล้องเหนือตัวละคร")]
    public float height     = 3f;
    [Tooltip("Yaw offset คงที่ (0 = ตามหลังตรง ๆ)")]
    public float yawOffset  = 0f;

    [Header("── Smoothing ────────────────────────")]
    [Tooltip("ความเร็วตามตัวละคร (สูง = เร็ว)")]
    public float positionSmooth = 8f;
    [Tooltip("ความเร็วหมุนกล้อง")]
    public float rotationSmooth = 12f;

    [Header("── Look At ─────────────────────────")]
    [Tooltip("กล้องมองตรงไปที่จุดสูงขึ้นจากตัวละครกี่เมตร")]
    public float lookAtHeightOffset = 1f;

    [Header("── Camera Collision ─────────────────")]
    [Tooltip("เปิด/ปิด ตรวจกล้องชนสิ่งกีดขวาง")]
    public bool cameraCollision = true;
    public LayerMask collisionMask;
    [Tooltip("ระยะกันชนขั้นต่ำ")]
    public float minDistance = 1f;

    // ═══════════════════════════════════════════════════
    //  LateUpdate
    // ═══════════════════════════════════════════════════

    void LateUpdate()
    {
        if (target == null) return;

        // ── คำนวณตำแหน่งที่ต้องการ ──
        float   yaw        = yawOffset;
        Vector3 offset     = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, height, -distance);
        Vector3 desiredPos = target.position + offset;

        // ── Camera Collision ──
        if (cameraCollision)
        {
            Vector3 dir = desiredPos - target.position;
            float   maxDist = dir.magnitude;
            if (Physics.SphereCast(target.position, 0.2f, dir.normalized,
                                   out RaycastHit hit, maxDist, collisionMask))
            {
                float safeDist = Mathf.Max(hit.distance - 0.1f, minDistance);
                desiredPos = target.position + dir.normalized * safeDist;
            }
        }

        // ── Smooth Position ──
        transform.position = Vector3.Lerp(transform.position, desiredPos,
                                          positionSmooth * Time.deltaTime);

        // ── Look At ──
        Vector3 lookTarget = target.position + Vector3.up * lookAtHeightOffset;
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot,
                                               rotationSmooth * Time.deltaTime);
    }

    // ── เปลี่ยน Target จากภายนอก ──────────────────────
    public void SetTarget(Transform t) => target = t;
}
