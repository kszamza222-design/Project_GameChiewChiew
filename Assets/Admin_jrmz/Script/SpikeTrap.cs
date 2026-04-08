using UnityEngine;

/// <summary>
/// SpikeTrap — กับดักหนามขยับขึ้น-ลงวนซ้ำ
///
/// Setup:
///   1. ติด Script นี้กับ GameObject หนาม (หรือ Parent ของหนาม)
///   2. ปรับค่าใน Inspector ตามต้องการ
///   3. ผูก HealthSystem ถ้าต้องการหักเลือดผู้เล่น
///
/// วิธีทำงาน:
///   หนามจะขยับขึ้น → รอ → ขยับลง → รอ → วนซ้ำ
///   ระหว่างหนามยืดออก (Active) จะหักเลือดผู้เล่นที่สัมผัส
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Movement ────────────────────────")]
    [Tooltip("ความสูงที่หนามยกขึ้น (เมตร)")]
    public float riseHeight    = 1.5f;

    [Tooltip("ความเร็วที่หนามยกขึ้น")]
    public float riseSpeed     = 4f;

    [Tooltip("ความเร็วที่หนามลงไป")]
    public float retractSpeed  = 2f;

    [Tooltip("เวลารอค้างบน (วินาที)")]
    public float stayUpTime    = 1.0f;

    [Tooltip("เวลารอค้างล่าง (วินาที)")]
    public float stayDownTime  = 2.0f;

    [Tooltip("เริ่มต้นในสถานะอะไร")]
    public bool startUp        = false;

    [Header("── Animation Style ─────────────────")]
    [Tooltip("แบบเคลื่อนที่\n" +
             "Linear = สม่ำเสมอ\n" +
             "EaseInOut = นุ่มนวล\n" +
             "Bounce = เด้ง")]
    public MoveStyle moveStyle = MoveStyle.EaseInOut;

    [Header("── Damage ───────────────────────────")]
    [Tooltip("เปิด/ปิด การหักเลือด")]
    public bool dealDamage     = true;

    [Tooltip("เลือดที่หักต่อครั้ง")]
    public int  damageAmount   = 20;

    [Tooltip("ความถี่หักเลือด (วินาที/ครั้ง)")]
    public float damageInterval = 0.5f;

    [Tooltip("ผูก HealthSystem ที่ใช้ในฉาก")]
    public HealthSystem healthSystem;

    [Tooltip("Tag ของ Player1")]
    public string player1Tag   = "Player1";
    [Tooltip("Tag ของ Player2")]
    public string player2Tag   = "Player2";

    [Header("── Visual Feedback ─────────────────")]
    [Tooltip("สีของ Material ตอนหนามยืดออก (ว่าง = ไม่เปลี่ยนสี)")]
    public Color activeColor   = new Color(1f, 0.3f, 0.1f, 1f);
    [Tooltip("สีของ Material ตอนหนามเก็บลง")]
    public Color inactiveColor = new Color(0.35f, 0.45f, 0.6f, 1f);
    [Tooltip("เปิดใช้การเปลี่ยนสี")]
    public bool  useColorChange = true;

    [Header("── Gizmo ───────────────────────────")]
    public bool showGizmo      = true;

    // ═══════════════════════════════════════════════════
    //  Enum
    // ═══════════════════════════════════════════════════

    public enum MoveStyle { Linear, EaseInOut, Bounce }

    enum TrapState { Down, Rising, Up, Retracting }

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    TrapState _state;
    float     _timer;
    float     _moveT;        // 0-1 progress ของการขยับ

    Vector3   _originPos;    // ตำแหน่งเริ่มต้น (ล่าง)
    Vector3   _upPos;        // ตำแหน่งขึ้นสูงสุด

    // Damage cooldown แยกต่อ Player
    float _dmgTimerP1;
    float _dmgTimerP2;

    // Renderer สำหรับเปลี่ยนสี
    Renderer[] _renderers;

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        _originPos = transform.position;
        _upPos     = _originPos + Vector3.up * riseHeight;
        _renderers = GetComponentsInChildren<Renderer>();

        if (startUp)
        {
            _state     = TrapState.Up;
            _timer     = stayUpTime;
            _moveT     = 1f;
            transform.position = _upPos;
        }
        else
        {
            _state = TrapState.Down;
            _timer = stayDownTime;
            _moveT = 0f;
        }

        UpdateColor();
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        UpdateTrap();
        UpdateDamageCooldown();
    }

    // ═══════════════════════════════════════════════════
    //  UpdateTrap — State Machine
    // ═══════════════════════════════════════════════════

    void UpdateTrap()
    {
        switch (_state)
        {
            // ── รอค้างล่าง ──────────────────────
            case TrapState.Down:
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _state = TrapState.Rising;
                    _moveT = 0f;
                }
                break;

            // ── กำลังขึ้น ────────────────────────
            case TrapState.Rising:
                _moveT += riseSpeed * Time.deltaTime / riseHeight;
                _moveT  = Mathf.Clamp01(_moveT);
                transform.position = Vector3.Lerp(_originPos, _upPos, Ease(_moveT));

                if (_moveT >= 1f)
                {
                    _state = TrapState.Up;
                    _timer = stayUpTime;
                    transform.position = _upPos;
                    UpdateColor();
                }
                break;

            // ── รอค้างบน ────────────────────────
            case TrapState.Up:
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _state = TrapState.Retracting;
                    _moveT = 1f;
                }
                break;

            // ── กำลังลง ──────────────────────────
            case TrapState.Retracting:
                _moveT -= retractSpeed * Time.deltaTime / riseHeight;
                _moveT  = Mathf.Clamp01(_moveT);
                transform.position = Vector3.Lerp(_originPos, _upPos, Ease(_moveT));

                if (_moveT <= 0f)
                {
                    _state = TrapState.Down;
                    _timer = stayDownTime;
                    transform.position = _originPos;
                    UpdateColor();
                }
                break;
        }
    }

    // ── Easing ────────────────────────────────────────
    float Ease(float t)
    {
        switch (moveStyle)
        {
            case MoveStyle.EaseInOut:
                return t * t * (3f - 2f * t);

            case MoveStyle.Bounce:
                if (t < 0.5f) return 4f * t * t * t;
                float f = 2f * t - 2f;
                return 0.5f * f * f * f + 1f;

            default: // Linear
                return t;
        }
    }

    // ── ตรวจ Active ───────────────────────────────────
    /// <summary>true = หนามกำลังยืดออกหรือค้างบน (อันตราย)</summary>
    public bool IsActive => _state == TrapState.Rising || _state == TrapState.Up;

    // ═══════════════════════════════════════════════════
    //  Damage
    // ═══════════════════════════════════════════════════

    void UpdateDamageCooldown()
    {
        if (_dmgTimerP1 > 0f) _dmgTimerP1 -= Time.deltaTime;
        if (_dmgTimerP2 > 0f) _dmgTimerP2 -= Time.deltaTime;
    }

    void OnTriggerStay(Collider other)
    {
        if (!dealDamage || !IsActive || healthSystem == null) return;

        if (other.CompareTag(player1Tag) && _dmgTimerP1 <= 0f)
        {
            healthSystem.TakeDamage(true, damageAmount);
            _dmgTimerP1 = damageInterval;
        }
        else if (other.CompareTag(player2Tag) && _dmgTimerP2 <= 0f)
        {
            healthSystem.TakeDamage(false, damageAmount);
            _dmgTimerP2 = damageInterval;
        }
    }

    // ═══════════════════════════════════════════════════
    //  Color
    // ═══════════════════════════════════════════════════

    void UpdateColor()
    {
        if (!useColorChange || _renderers == null) return;
        Color c = IsActive ? activeColor : inactiveColor;
        foreach (var r in _renderers)
        {
            if (r.material != null)
                r.material.color = c;
        }
    }

    // ═══════════════════════════════════════════════════
    //  Gizmo
    // ═══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        Vector3 origin = Application.isPlaying ? _originPos : transform.position;
        Vector3 up     = origin + Vector3.up * riseHeight;

        // เส้นแสดงระยะขึ้น-ลง
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.8f);
        Gizmos.DrawLine(origin, up);
        Gizmos.DrawWireCube(up, Vector3.one * 0.2f);

        // แสดงตำแหน่งปัจจุบัน
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}
