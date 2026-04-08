using UnityEngine;

/// <summary>
/// PressurePlate — ปุ่มกด (Pressure Plate)
///
/// ระบบ 2 ปุ่ม 1 ประตู:
///   • Player นอกเหยียบปุ่มนอก → ประตูเปิด → Player ในเข้ามา
///   • Player ในเหยียบปุ่มใน   → ประตูเปิด → Player นอกเข้ามา
///   • ใครเหยียบก็ได้ ประตูเปิดทันที
///   • ปล่อยทั้งคู่ → ประตูปิด
///
/// Setup:
///   1. ติด Script นี้กับปุ่มทั้งสองอัน (แยก GameObject)
///   2. ผูก player1, player2 ทั้งคู่
///   3. ผูก doorObject เป็น GameObject ประตูเดียวกัน
///   4. ผูก otherPlate = PressurePlate ของปุ่มอีกอัน
///   5. *** อย่าลืมผูก otherPlate ทั้งสองฝั่ง ***
///      Plate A → otherPlate = Plate B
///      Plate B → otherPlate = Plate A
/// </summary>
public class PressurePlate : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Players ─────────────────────────")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("── Door ─────────────────────────────")]
    [Tooltip("GameObject ของประตูที่จะเปิด/ปิด")]
    public GameObject doorObject;

    [Header("── Other Plate ──────────────────────")]
    [Tooltip("ลาก PressurePlate อีกอันมาใส่ตรงนี้\n" +
             "ต้องผูกทั้งสองฝั่ง (A→B และ B→A)")]
    public PressurePlate otherPlate;

    [Header("── Settings ────────────────────────")]
    [Tooltip("ระยะที่ปุ่มเลื่อนลงเมื่อถูกกด (เมตร)")]
    public float pressDistance = 0.2f;
    [Tooltip("ระยะที่ประตูเลื่อนขึ้นเมื่อเปิด (เมตร)")]
    public float doorDistance  = 3f;
    [Tooltip("ความเร็วในการเลื่อน")]
    public float moveSpeed     = 2f;
    [Tooltip("รัศมีตรวจจับผู้เล่น")]
    public float triggerRadius = 1.2f;

    // ═══════════════════════════════════════════════════
    //  Public State
    // ═══════════════════════════════════════════════════

    /// <summary>ปุ่มนี้ถูกเหยียบอยู่ไหม — ปุ่มอีกอันอ่านได้</summary>
    public bool IsPressed { get; private set; } = false;

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    Vector3 _btnClosedPos;
    Vector3 _btnPressedPos;
    Vector3 _doorClosedPos;
    Vector3 _doorOpenPos;

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Start()
    {
        // บันทึกตำแหน่งเริ่มต้น
        _btnClosedPos  = transform.position;
        _btnPressedPos = transform.position + Vector3.down * pressDistance;

        if (doorObject != null)
        {
            _doorClosedPos = doorObject.transform.position;
            _doorOpenPos   = doorObject.transform.position + Vector3.up * doorDistance;
        }
        else
        {
            Debug.LogWarning($"[PressurePlate] '{name}' ยังไม่ได้ผูก Door Object!");
        }
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        // ── ตรวจว่าปุ่มนี้ถูกเหยียบ ────────────────────
        IsPressed = IsPlayerOn(player1) || IsPlayerOn(player2);

        // ── ขยับปุ่มลง/ขึ้น ────────────────────────────
        MoveButton();

        // ── ตัดสินใจเปิด/ปิดประตู ───────────────────────
        // ประตูเปิดถ้า "ปุ่มนี้" หรือ "ปุ่มอีกอัน" ถูกเหยียบ
        bool otherPressed = otherPlate != null && otherPlate.IsPressed;
        bool shouldOpen   = IsPressed || otherPressed;

        // ── ขยับประตู ────────────────────────────────────
        // ** แต่ละปุ่มควบคุมประตูเองได้เลย ไม่ต้องแข่ง ID **
        MoveDoor(shouldOpen);
    }

    // ═══════════════════════════════════════════════════
    //  ตรวจ Player อยู่บนปุ่มไหม
    // ═══════════════════════════════════════════════════

    bool IsPlayerOn(PlayerController player)
    {
        if (player == null) return false;

        // วัดระยะเฉพาะแกน X,Z (ไม่สนความสูง)
        Vector3 plateXZ  = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 playerXZ = new Vector3(player.transform.position.x, 0f,
                                        player.transform.position.z);
        return Vector3.Distance(plateXZ, playerXZ) <= triggerRadius;
    }

    // ═══════════════════════════════════════════════════
    //  ขยับปุ่ม
    // ═══════════════════════════════════════════════════

    void MoveButton()
    {
        Vector3 target = IsPressed ? _btnPressedPos : _btnClosedPos;
        transform.position = Vector3.MoveTowards(
            transform.position, target, moveSpeed * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════
    //  ขยับประตู
    //
    //  แก้จากเดิม: ลบการเช็ค GetInstanceID() ออก
    //  ทั้งสองปุ่มขยับประตูได้เองโดยตรง
    //  เพราะทั้งคู่ส่ง target ตำแหน่งเดียวกัน → MoveTowards ไม่ชน
    // ═══════════════════════════════════════════════════

    void MoveDoor(bool open)
    {
        if (doorObject == null) return;

        Vector3 target = open ? _doorOpenPos : _doorClosedPos;
        doorObject.transform.position = Vector3.MoveTowards(
            doorObject.transform.position, target,
            moveSpeed * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════
    //  Gizmos
    // ═══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // รัศมีปุ่ม
        Gizmos.color = IsPressed ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // แสดงตำแหน่งประตูตอนเปิด
        if (doorObject != null)
        {
            Vector3 openPos = doorObject.transform.position + Vector3.up * doorDistance;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(openPos, doorObject.transform.localScale * 0.9f);
            Gizmos.DrawLine(doorObject.transform.position, openPos);
        }

        // เส้นเชื่อมไปปุ่มอีกอัน
        if (otherPlate != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawLine(transform.position, otherPlate.transform.position);
        }
    }
}
