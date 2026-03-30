using UnityEngine;

/// <summary>
/// ติด Script นี้กับ GameObject ที่ต้องการให้ยก/โยนได้
///
/// Requirements :
///   • GameObject ต้องมี Rigidbody
///   • ตั้ง Layer ของ GameObject เป็น "Pickable"
///   • PlayerController → pickableLayer ต้องเลือก Layer "Pickable"
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PickableObject : MonoBehaviour
{
    [Header("── Object Info ─────────────────────")]
    public string displayName = "Object";

    // ═══════════════════════════════════════════════════
    //  Public helpers (PlayerController เรียก)
    // ═══════════════════════════════════════════════════

    /// <summary>เรียกเมื่อถูกยก</summary>
    public void OnPickedUp() { }

    /// <summary>เรียกเมื่อถูกวาง/โยน</summary>
    public void OnDropped()  { }
}
