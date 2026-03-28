using UnityEngine;

/// <summary>
/// ติด Script นี้กับ GameObject ที่ต้องการให้ยก/ลาก/โยนได้
///
/// Requirements :
///   • GameObject ต้องมี Rigidbody
///   • ตั้ง Layer ของ GameObject เป็น "Pickable"  (สร้างใน Tags & Layers)
///   • PlayerController → pickableLayer ต้องเลือก Layer "Pickable"
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PickableObject : MonoBehaviour
{
    [Header("── Object Info ─────────────────────")]
    public string displayName = "Object";

    [Header("── Highlight ────────────────────────")]
    [Tooltip("เปิด Effect เรืองแสงเมื่อ Player อยู่ใกล้")]
    public bool enableHighlight = true;
    [Tooltip("รัศมีแสดง Highlight")]
    public float highlightRadius = 2.8f;
    [Tooltip("ความสว่าง Emission")]
    [Range(0f, 2f)]
    public float emissionIntensity = 0.5f;
    public Color highlightColor = new Color(1f, 0.92f, 0.3f);

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    Rigidbody  _rb;
    Renderer[] _renderers;
    bool       _highlighted;

    static readonly int PropEmission = Shader.PropertyToID("_EmissionColor");

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        _rb        = GetComponent<Rigidbody>();
        _renderers = GetComponentsInChildren<Renderer>();

        // เปิด Emission keyword บน Material instance
        foreach (var r in _renderers)
            r.material.EnableKeyword("_EMISSION");
    }

    // ═══════════════════════════════════════════════════
    //  Update — ตรวจ Player ใกล้
    // ═══════════════════════════════════════════════════

    void Update()
    {
        if (!enableHighlight) return;

        bool near = false;
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (Vector3.Distance(transform.position, p.transform.position) <= highlightRadius)
            { near = true; break; }
        }

        if (near != _highlighted)
        {
            _highlighted = near;
            ApplyHighlight(near);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Highlight
    // ═══════════════════════════════════════════════════

    void ApplyHighlight(bool on)
    {
        Color emit = on ? highlightColor * emissionIntensity : Color.black;
        foreach (var r in _renderers)
            r.material.SetColor(PropEmission, emit);
    }

    // ═══════════════════════════════════════════════════
    //  Public helpers (PlayerController เรียก)
    // ═══════════════════════════════════════════════════

    /// <summary>เรียกเมื่อถูกยก — บันทึก state ถ้าต้องการ</summary>
    public void OnPickedUp()   { /* ขยายได้ */ }

    /// <summary>เรียกเมื่อถูกวาง</summary>
    public void OnDropped()    { /* ขยายได้ */ }

    // ═══════════════════════════════════════════════════
    //  Gizmo
    // ═══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, highlightRadius);
    }
}
