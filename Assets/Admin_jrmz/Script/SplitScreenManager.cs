using UnityEngine;

/// <summary>
/// จัดการ Split-Screen :
///   • ตั้ง Viewport ซ้าย / ขวา ให้ Camera แต่ละตัว
///   • วาดเส้นแบ่งกลางจอ
///   • ส่ง Camera reference ให้ PlayerController
///
/// วิธีใช้ : วาง Script นี้บน GameObject เปล่าชื่อ "SplitScreenManager"
/// </summary>
public class SplitScreenManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Cameras ──────────────────────────")]
    [Tooltip("Camera ฝั่งซ้าย (Player 1)")]
    public Camera cameraP1;
    [Tooltip("Camera ฝั่งขวา (Player 2)")]
    public Camera cameraP2;

    [Header("── Players ─────────────────────────")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("── Divider Line ────────────────────")]
    [Tooltip("ความกว้างเส้น (สัดส่วนหน้าจอ 0‒0.05)")]
    [Range(0.001f, 0.05f)]
    public float dividerWidthRatio = 0.003f;

    [Tooltip("สีเส้นแบ่ง")]
    public Color dividerColor = Color.white;

    [Tooltip("วาดเส้นแบ่งหรือไม่")]
    public bool showDivider = true;

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    Texture2D _divTex;
    GUIStyle  _divStyle;

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        ApplyViewports();
        LinkCameras();
        BuildDividerStyle();
    }

    // ── Viewport : ซ้าย 50% | ขวา 50% ─────────────────
    void ApplyViewports()
    {
        if (cameraP1 != null) cameraP1.rect = new Rect(0.0f, 0f, 0.5f, 1f);
        if (cameraP2 != null) cameraP2.rect = new Rect(0.5f, 0f, 0.5f, 1f);
    }

    // ── ส่ง Camera ให้ PlayerController ─────────────────
    void LinkCameras()
    {
        if (player1 != null && cameraP1 != null) player1.AssignCamera(cameraP1);
        if (player2 != null && cameraP2 != null) player2.AssignCamera(cameraP2);
    }

    // ── สร้าง Texture สำหรับเส้น ─────────────────────
    void BuildDividerStyle()
    {
        _divTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _divTex.SetPixel(0, 0, dividerColor);
        _divTex.Apply();

        _divStyle = new GUIStyle();
        _divStyle.normal.background = _divTex;
    }

    // ═══════════════════════════════════════════════════
    //  OnGUI — วาดเส้นกลางจอ
    // ═══════════════════════════════════════════════════

    void OnGUI()
    {
        if (!showDivider || _divStyle == null) return;

        float pxW = Screen.width * dividerWidthRatio;
        float x   = Screen.width * 0.5f - pxW * 0.5f;

        GUI.Box(new Rect(x, 0f, pxW, Screen.height), GUIContent.none, _divStyle);
    }

    // ── รีเฟรชสีถ้าเปลี่ยนใน Inspector (Play mode) ────
    void OnValidate()
    {
        if (_divTex != null)
        {
            _divTex.SetPixel(0, 0, dividerColor);
            _divTex.Apply();
        }
    }
}
