using UnityEngine;

/// <summary>
/// SlidingDoor — ประตูเปิดค้าง ไม่วนกลับ
///
/// Animator Setup ที่ถูกต้อง:
///   States : Idle (Default) → DoorOpen
///   Parameter : Bool "IsOpen"
///   Transition Idle → DoorOpen :
///     Condition    : IsOpen = true
///     Has Exit Time: ✗ (ปิด!)
///     Transition Duration: 0
///   DoorOpen State:
///     Speed        : 1
///     Loop Time    : ✗ (ปิด! — สำคัญมาก ไม่งั้นวนซ้ำ)
///     ไม่มี Transition กลับ Idle
/// </summary>
public class SlidingDoor : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Animator ────────────────────────")]
    [Tooltip("Animator ของประตู (ถ้าว่างจะหาอัตโนมัติ)")]
    public Animator doorAnimator;
    [Tooltip("ชื่อ Bool Parameter ใน Animator")]
    public string   openParameterName = "IsOpen";

    [Header("── เสียง (ไม่บังคับ) ─────────────")]
    public AudioClip correctSound;
    public AudioClip wrongSound;

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    bool        _isOpen = false;
    AudioSource _audio;
    int         _openHash;

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Start()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();

        if (doorAnimator == null)
        {
            Debug.LogError("[SlidingDoor] ไม่พบ Animator!");
            return;
        }

        _openHash = Animator.StringToHash(openParameterName);
        _audio    = GetComponent<AudioSource>();

        // ── ตรวจ Parameter ──
        bool found = false;
        foreach (var param in doorAnimator.parameters)
            if (param.name == openParameterName) { found = true; break; }

        if (!found)
            Debug.LogError($"[SlidingDoor] ไม่พบ Bool Parameter '{openParameterName}' ใน Animator!");
        else
            Debug.Log("[SlidingDoor] Animator พร้อมแล้ว ✓");

        // ── ปิด Loop Time ของ Clip DoorOpen อัตโนมัติ ──
        DisableLoopOnDoorClip();
    }

    // ═══════════════════════════════════════════════════
    //  ปิด Loop ของ Animation Clip อัตโนมัติ
    // ═══════════════════════════════════════════════════

    void DisableLoopOnDoorClip()
    {
        if (doorAnimator == null || doorAnimator.runtimeAnimatorController == null) return;

        foreach (var clip in doorAnimator.runtimeAnimatorController.animationClips)
        {
            // หา Clip ที่ไม่ใช่ Idle (ชื่อมี "Open", "Door" หรืออะไรก็ตามที่ไม่ใช่ Idle)
            if (!clip.name.ToLower().Contains("idle"))
            {
                // AnimationClip.legacy ต้อง false เพื่อใช้ loopTime
                if (clip.isLooping)
                {
                    // แจ้งให้รู้ว่า Clip ยัง Loop อยู่
                    Debug.LogWarning($"[SlidingDoor] Clip '{clip.name}' ยังเปิด Loop Time อยู่!\n" +
                                     "กรุณาปิด Loop Time ใน Animation Clip:\n" +
                                     "Project → ดับเบิลคลิก Clip → Inspector → Loop Time ✗");
                }
                else
                {
                    Debug.Log($"[SlidingDoor] Clip '{clip.name}' Loop Time = Off ✓");
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════

    /// <summary>เปิดประตู — เรียกจาก KeypadUIBuilder เมื่อรหัสถูก</summary>
    public void ForceOpen()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (doorAnimator != null)
            doorAnimator.SetBool(_openHash, true);

        if (_audio != null && correctSound != null)
            _audio.PlayOneShot(correctSound);

        Debug.Log("[SlidingDoor] ประตูเปิดแล้ว! ✓");
    }

    /// <summary>เล่นเสียงผิด — เรียกเมื่อรหัสผิด</summary>
    public void PlayWrong()
    {
        if (_audio != null && wrongSound != null)
            _audio.PlayOneShot(wrongSound);
    }

    // ═══════════════════════════════════════════════════
    //  Gizmo
    // ═══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        Gizmos.color = _isOpen ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
