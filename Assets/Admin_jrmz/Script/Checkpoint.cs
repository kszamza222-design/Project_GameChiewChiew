using UnityEngine;

/// <summary>
/// Checkpoint — บันทึกจุด Respawn เมื่อ Player เดินผ่าน
/// ไม่มี Notification UI — เปลี่ยนสีแค่นั้น
///
/// วิธีใช้:
///   1. สร้าง GameObject (เช่น Cylinder) ชื่อ "Checkpoint_1"
///   2. Add Component → Checkpoint
///   3. Add Component → SphereCollider → Is Trigger = true, Radius = 2
///   4. ผูก player1, player2
///   5. ตั้ง checkpointID ให้ไม่ซ้ำกัน (0, 1, 2, 3...)
///   6. ลาก Renderer ของ Object ใส่ช่อง checkpointRenderer
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("── ID ──────────────────────────────")]
    [Tooltip("ต้องไม่ซ้ำกันแต่ละ Checkpoint (0, 1, 2, 3...)")]
    public int checkpointID = 0;

    [Header("── Players ─────────────────────────")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("── Visual ───────────────────────────")]
    [Tooltip("ลาก Renderer ของ Checkpoint Object มาใส่ — จะเปลี่ยนสีเมื่อผ่านแล้ว")]
    public Renderer checkpointRenderer;
    public Color inactiveColor = new Color(0.6f, 0.6f, 0.6f);
    public Color p1Color       = new Color(0.3f, 0.6f, 1.0f);
    public Color p2Color       = new Color(1.0f, 0.5f, 0.1f);
    public Color bothColor     = new Color(0.2f, 1.0f, 0.4f);

    // ── State ────────────────────────────────────
    bool _activatedByP1 = false;
    bool _activatedByP2 = false;

    void Awake()
    {
        if (checkpointRenderer != null)
            checkpointRenderer.material.color = inactiveColor;
    }

    // ═══════════════════════════════════════════════════
    //  Trigger Detection
    // ═══════════════════════════════════════════════════

    void OnTriggerEnter(Collider other)
    {
        bool isP1 = IsPlayer(other, player1);
        bool isP2 = IsPlayer(other, player2);

        if (!isP1 && !isP2) return;

        if (isP1) Activate(ref _activatedByP1, forP1: true);
        if (isP2) Activate(ref _activatedByP2, forP1: false);

        UpdateColor();
    }

    bool IsPlayer(Collider col, PlayerController pc)
    {
        if (pc == null) return false;
        return col.gameObject == pc.gameObject
            || col.transform.IsChildOf(pc.transform);
    }

    void Activate(ref bool activated, bool forP1)
    {
        // บันทึก Checkpoint ทุกครั้ง (อัปเดตจุดล่าสุดเสมอ)
        CheckpointManager.Instance?.SaveCheckpoint(
            transform.position, forP1, !forP1);

        if (!activated)
        {
            activated = true;
            Debug.Log($"[CP {checkpointID}] {(forP1 ? "P1" : "P2")} Checkpoint บันทึกแล้ว ✓");
        }
    }

    void UpdateColor()
    {
        if (checkpointRenderer == null) return;
        if      (_activatedByP1 && _activatedByP2) checkpointRenderer.material.color = bothColor;
        else if (_activatedByP1)                   checkpointRenderer.material.color = p1Color;
        else if (_activatedByP2)                   checkpointRenderer.material.color = p2Color;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}
