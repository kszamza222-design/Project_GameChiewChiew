using UnityEngine;

/// <summary>
/// CheckpointManager — จัดการ Checkpoint และ Respawn
///
/// P1 และ P2 มี Checkpoint เป็นของตัวเองอิสระ
/// ตกต่ำกว่า fallY (-100) → Respawn กลับ Checkpoint ล่าสุดทันที
///
/// Setup:
///   1. วาง Script นี้บน GameObject ชื่อ "CheckpointManager"
///   2. ผูก player1, player2
///   3. ผูก spawnP1, spawnP2 (Transform จุด Spawn เริ่มต้น)
///   4. ตั้ง fallY = -100 (หรือต่ำกว่าพื้นที่ต่ำสุดของ Map)
///
/// Checkpoint.cs จะเรียก SaveCheckpoint() อัตโนมัติเมื่อผู้เล่นเดินผ่าน
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Players ─────────────────────────")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("── Default Spawn ───────────────────")]
    [Tooltip("จุด Spawn เริ่มต้น P1 (ก่อนเข้า Checkpoint แรก)")]
    public Transform spawnP1;
    [Tooltip("จุด Spawn เริ่มต้น P2 (ก่อนเข้า Checkpoint แรก)")]
    public Transform spawnP2;

    [Header("── Fall Detection ───────────────────")]
    [Tooltip("ตกต่ำกว่าค่านี้ = Respawn อัตโนมัติ\n" +
             "ตั้งให้ต่ำกว่าพื้นที่ต่ำสุดของ Map\n" +
             "ค่าแนะนำ: -100 สำหรับ Map ทั่วไป")]
    public float fallY = -100f;

    [Header("── Respawn Settings ─────────────────")]
    [Tooltip("หน่วงเวลาก่อน Respawn (วินาที)")]
    public float respawnDelay = 1.0f;
    [Tooltip("ระยะเลื่อน Spawn point ออกด้านข้างป้องกันซ้อนกัน")]
    public float spawnOffset  = 0.6f;

    // ═══════════════════════════════════════════════════
    //  Checkpoint State
    // ═══════════════════════════════════════════════════

    Vector3 _checkpointP1;
    Vector3 _checkpointP2;
    bool    _hasCheckpointP1 = false;
    bool    _hasCheckpointP2 = false;

    // ═══════════════════════════════════════════════════
    //  Respawn State
    // ═══════════════════════════════════════════════════

    bool  _respawningP1 = false;
    bool  _respawningP2 = false;
    float _timerP1      = 0f;
    float _timerP2      = 0f;

    // ═══════════════════════════════════════════════════
    //  Awake
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Checkpoint เริ่มต้น = ตำแหน่ง SpawnPoint
        _checkpointP1 = spawnP1 != null ? spawnP1.position : Vector3.zero;
        _checkpointP2 = spawnP2 != null ? spawnP2.position : Vector3.zero;

        Debug.Log($"[CheckpointManager] เริ่มต้น: P1={_checkpointP1}, P2={_checkpointP2}, fallY={fallY}");
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        CheckFall();
        TickTimers();
    }

    // ═══════════════════════════════════════════════════
    //  Fall Detection — ตรวจ Y ต่ำเกินกำหนด
    // ═══════════════════════════════════════════════════

    void CheckFall()
    {
        if (player1 != null && !_respawningP1 &&
            player1.transform.position.y < fallY)
        {
            Debug.Log($"[CheckpointManager] P1 ตกถึง Y={player1.transform.position.y:F1} → Respawn");
            TriggerRespawn(isP1: true);
        }

        if (player2 != null && !_respawningP2 &&
            player2.transform.position.y < fallY)
        {
            Debug.Log($"[CheckpointManager] P2 ตกถึง Y={player2.transform.position.y:F1} → Respawn");
            TriggerRespawn(isP1: false);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Countdown Timers
    // ═══════════════════════════════════════════════════

    void TickTimers()
    {
        if (_respawningP1)
        {
            _timerP1 -= Time.deltaTime;
            if (_timerP1 <= 0f) DoRespawn(isP1: true);
        }
        if (_respawningP2)
        {
            _timerP2 -= Time.deltaTime;
            if (_timerP2 <= 0f) DoRespawn(isP1: false);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// บันทึก Checkpoint ล่าสุด — เรียกจาก Checkpoint.cs
    /// </summary>
    public void SaveCheckpoint(Vector3 position, bool forP1, bool forP2)
    {
        if (forP1)
        {
            _checkpointP1    = position;
            _hasCheckpointP1 = true;
            Debug.Log($"[CheckpointManager] บันทึก Checkpoint P1 ที่ {position}");
        }
        if (forP2)
        {
            _checkpointP2    = position;
            _hasCheckpointP2 = true;
            Debug.Log($"[CheckpointManager] บันทึก Checkpoint P2 ที่ {position}");
        }
    }

    /// <summary>
    /// เรียก Respawn — เรียกได้จากภายนอก เช่น ระบบ HP
    /// </summary>
    public void TriggerRespawn(bool isP1)
    {
        if (isP1 && !_respawningP1)
        {
            _respawningP1 = true;
            _timerP1      = respawnDelay;
            DisablePlayer(player1);
            RespawnEffect.Instance?.Play(isP1: true);
        }
        else if (!isP1 && !_respawningP2)
        {
            _respawningP2 = true;
            _timerP2      = respawnDelay;
            DisablePlayer(player2);
            RespawnEffect.Instance?.Play(isP1: false);
        }
    }

    // ═══════════════════════════════════════════════════
    //  DoRespawn — ย้าย Player กลับ Checkpoint ล่าสุด
    // ═══════════════════════════════════════════════════

    void DoRespawn(bool isP1)
    {
        if (isP1)
        {
            Vector3 pos = _hasCheckpointP1
                ? _checkpointP1
                : (spawnP1 != null ? spawnP1.position : Vector3.zero);

            // เลื่อนออกเล็กน้อยป้องกันซ้อน P2
            pos += Vector3.right * spawnOffset;

            TeleportPlayer(player1, pos);
            EnablePlayer(player1);
            _respawningP1 = false;

            Debug.Log($"[CheckpointManager] P1 Respawn สำเร็จที่ {pos} ✓");
        }
        else
        {
            Vector3 pos = _hasCheckpointP2
                ? _checkpointP2
                : (spawnP2 != null ? spawnP2.position : Vector3.zero);

            pos -= Vector3.right * spawnOffset;

            TeleportPlayer(player2, pos);
            EnablePlayer(player2);
            _respawningP2 = false;

            Debug.Log($"[CheckpointManager] P2 Respawn สำเร็จที่ {pos} ✓");
        }
    }

    // ═══════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════

    void TeleportPlayer(PlayerController pc, Vector3 pos)
    {
        if (pc == null) return;

        // ปิด CharacterController ก่อน Teleport (บังคับ)
        var cc = pc.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        pc.transform.position = pos + Vector3.up * 0.15f;

        if (cc != null) cc.enabled = true;
    }

    void DisablePlayer(PlayerController pc)
    {
        if (pc == null) return;
        var input = pc.GetComponent<PlayerInputHandler>();
        if (input != null) input.enabled = false;
        pc.enabled = false;
    }

    void EnablePlayer(PlayerController pc)
    {
        if (pc == null) return;
        var input = pc.GetComponent<PlayerInputHandler>();
        if (input != null) input.enabled = true;
        pc.enabled = true;
    }

    // ═══════════════════════════════════════════════════
    //  Gizmos — แสดงเส้น fallY และ Checkpoint ล่าสุดใน Scene
    // ═══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // เส้น fallY (สีแดง)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawLine(new Vector3(-200f, fallY, 0f),   new Vector3(200f, fallY, 0f));
        Gizmos.DrawLine(new Vector3(0f,    fallY, -200f), new Vector3(0f,   fallY, 200f));

        // Label fallY
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            new Vector3(0f, fallY + 1f, 0f),
            $"Fall Y = {fallY}",
            new GUIStyle { normal = { textColor = Color.red } });
        #endif

        // Checkpoint P1 (สีน้ำเงิน)
        if (_hasCheckpointP1)
        {
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.9f);
            Gizmos.DrawSphere(_checkpointP1 + Vector3.up * 0.5f, 0.35f);
        }

        // Checkpoint P2 (สีส้ม)
        if (_hasCheckpointP2)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.9f);
            Gizmos.DrawSphere(_checkpointP2 + Vector3.up * 0.5f, 0.35f);
        }

        // Spawn เริ่มต้น
        if (spawnP1 != null)
        {
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.4f);
            Gizmos.DrawWireSphere(spawnP1.position, 0.5f);
        }
        if (spawnP2 != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(spawnP2.position, 0.5f);
        }
    }
}
