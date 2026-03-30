using UnityEngine;

/// <summary>
/// SlidingDoor — รับคำสั่งจาก KeypadUIBuilder
///
/// วิธีใช้:
///   1. วาง Script นี้บน anim_door GameObject
///   2. ผูก Door Animator
///   3. ตั้ง Open Parameter Name ให้ตรงกับ Bool ใน Animator ("IsOpen")
///   4. ไม่ต้องผูก Keypad — KeypadUIBuilder จะหาเองอัตโนมัติ
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
        // หา Animator อัตโนมัติถ้าไม่ได้ผูก
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();

        if (doorAnimator == null)
            Debug.LogError("[SlidingDoor] ไม่พบ Animator!");

        _openHash = Animator.StringToHash(openParameterName);
        _audio    = GetComponent<AudioSource>();

        // ตรวจ Parameter ใน Animator
        if (doorAnimator != null)
        {
            bool found = false;
            foreach (var param in doorAnimator.parameters)
                if (param.name == openParameterName) { found = true; break; }

            if (!found)
                Debug.LogError($"[SlidingDoor] ไม่พบ Bool Parameter '{openParameterName}' ใน Animator!");
            else
                Debug.Log("[SlidingDoor] Animator พร้อมแล้ว ✓");
        }
    }

    // ═══════════════════════════════════════════════════
    //  Public API — เรียกจาก KeypadUIBuilder
    // ═══════════════════════════════════════════════════

    /// <summary>เปิดประตู (เรียกเมื่อรหัสถูก)</summary>
    public void ForceOpen()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (doorAnimator != null)
            doorAnimator.SetBool(_openHash, true);

        if (_audio != null && correctSound != null)
            _audio.PlayOneShot(correctSound);

        Debug.Log("[SlidingDoor] ประตูเปิดแล้ว!");
    }

    /// <summary>เล่นเสียงผิด (เรียกเมื่อรหัสผิด)</summary>
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
