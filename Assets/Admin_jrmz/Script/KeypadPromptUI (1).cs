using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// KeypadPromptUI — UI ลอยเหนือประตู
///
/// วิธีใช้:
///   1. ติด Script นี้กับ GameObject ของ Keypad/ประตู
///   2. ลาก "Prompt Anchor" (Empty GameObject ที่วางไว้หน้าประตู) มาใส่ช่อง promptAnchor
///      ถ้าไม่ใส่ จะใช้ตำแหน่งของ GameObject นี้แทน + heightAboveKeypad
///   3. ผูก Player1, Player2
/// </summary>
[RequireComponent(typeof(KeypadUIBuilder))]
public class KeypadPromptUI : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Players ─────────────────────────")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("── Prompt Position ─────────────────")]
    [Tooltip("ลาก Empty GameObject ที่วางไว้หน้าประตูมาใส่ตรงนี้\n" +
             "ถ้าว่างจะใช้ตำแหน่ง GameObject นี้ + heightAboveKeypad")]
    public Transform promptAnchor;
    [Tooltip("ความสูงเหนือ GameObject นี้ (ใช้เมื่อไม่ได้ผูก promptAnchor)")]
    public float heightAboveKeypad = 2.2f;

    [Header("── Show Radius ──────────────────────")]
    [Tooltip("รัศมีที่ UI จะแสดงเมื่อผู้เล่นเข้ามาใกล้")]
    public float showRadius = 3f;

    [Header("── Board Rotation ─────────────────")]
    [Tooltip("ปรับ Y เพื่อหมุนป้ายซ้าย/ขวาให้หันถูกทิศ\n0=+Z  90=+X  180=-Z  270=-X")]
    public Vector3 boardRotation = new Vector3(0f, 180f, 0f);

    // ═══════════════════════════════════════════════════
    //  Colors
    // ═══════════════════════════════════════════════════

    static readonly Color ColBg      = new Color(0.06f, 0.06f, 0.09f, 0.96f);
    static readonly Color ColBorder  = new Color(0.85f, 0.55f, 0.08f, 1.00f);
    static readonly Color ColAccent  = new Color(0.94f, 0.62f, 0.15f, 1.00f);
    static readonly Color ColKeyBg   = new Color(0.14f, 0.14f, 0.20f, 1.00f);
    static readonly Color ColKeyBdr  = new Color(0.70f, 0.45f, 0.05f, 1.00f);
    static readonly Color ColTextKey = new Color(1.00f, 0.80f, 0.20f, 1.00f);
    static readonly Color ColTextSub = new Color(0.80f, 0.80f, 0.82f, 1.00f);
    static readonly Color ColOr      = new Color(0.50f, 0.50f, 0.54f, 1.00f);

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    KeypadUIBuilder _builder;
    GameObject      _board;

    // ═══════════════════════════════════════════════════
    //  Awake
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        _builder = GetComponent<KeypadUIBuilder>();
        BuildBoard();
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        if (_board == null) return;

        // ถ้าประตูเปิดแล้ว ซ่อนถาวร
        if (_builder != null && _builder.DoorUnlocked)
        {
            _board.SetActive(false);
            return;
        }

        bool p1Near = player1 != null &&
                      Vector3.Distance(player1.transform.position, transform.position) <= showRadius;
        bool p2Near = player2 != null &&
                      Vector3.Distance(player2.transform.position, transform.position) <= showRadius;

        // ── กด E / Numpad7 เพื่อเปิด Keypad UI ──────────
        if (_builder != null && !_builder.IsKeypadOpen)
        {
            if (p1Near && Input.GetKeyDown(KeyCode.E))
                _builder.Open(player1, isLeftSide: true);

            if (p2Near && Input.GetKeyDown(KeyCode.Keypad7))
                _builder.Open(player2, isLeftSide: false);
        }

        // ── แสดง/ซ่อนป้าย ────────────────────────────────
        bool shouldShow = (p1Near || p2Near) &&
                          (_builder == null || !_builder.IsKeypadOpen);

        _board.SetActive(shouldShow);
        _board.transform.rotation = Quaternion.Euler(boardRotation);
    }

    // ═══════════════════════════════════════════════════
    //  BuildBoard
    //
    //  ขนาด Canvas : 260 px × 68 px (localScale 0.01 → 2.6m × 0.68m)
    //
    //  ┌──────────────────────────────────────────┐
    //  ║ ▌  [ E ]  or  [ Numpad ]                 ║
    //  ║     Press E or Numpad to enter door code  ║
    //  └──────────────────────────────────────────┘
    // ═══════════════════════════════════════════════════

    void BuildBoard()
    {
        // ── หาตำแหน่งวาง Board ──────────────────────
        Vector3 worldPos = promptAnchor != null
            ? promptAnchor.position
            : transform.position + Vector3.up * heightAboveKeypad;

        // ── Root (World-space Canvas) ────────────────
        _board = new GameObject("_PromptBoard");
        _board.transform.position   = worldPos;
        _board.transform.rotation   = Quaternion.identity;
        // localScale 0.01 → 1 px = 0.01 m
        _board.transform.localScale = Vector3.one * 0.01f;

        var canvas        = _board.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // ขนาด canvas ใน px
        const float W = 260f;
        const float H =  68f;

        var rootRT       = _board.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(W, H);

        // ─── ขอบ (Border) ───────────────────────────
        Img(_board.transform, "Border", 0, 0, W, H, ColBorder);

        // ─── พื้นหลังดำ ───────────────────────────────
        const float bpx = 1.5f;
        Img(_board.transform, "BG",
            0, 0,
            W - bpx * 2f,
            H - bpx * 2f,
            ColBg);

        // ─── Accent bar ซ้าย ─────────────────────────
        const float acW = 5.5f;
        const float acH = H - bpx * 2f;
        float acX = -(W / 2f - bpx - acW / 2f);
        Img(_board.transform, "Accent", acX, 0, acW, acH, ColAccent);

        // ─── Content เริ่มหลัง accent ─────────────────
        float cStartX = acX + acW / 2f + 8f;   // 8 px gap หลัง accent

        // ─── Row 1 (y = +14 px): [ E ]  or  [ Numpad ] ──
        const float rowY   = 13f;
        const float badgeH = 22f;

        // Badge [E]
        const float eW = 30f;
        float eX = cStartX + eW / 2f;
        Badge(_board.transform, "E", eX, rowY, eW, badgeH);

        // "or"
        const float orW = 18f;
        float orX = eX + eW / 2f + 4f + orW / 2f;
        TMP(_board.transform, "Or", "or",
            orX, rowY, orW, badgeH, 8.5f, ColOr);

        // Badge [Numpad]
        const float nW = 52f;
        float nX = orX + orW / 2f + 4f + nW / 2f;
        Badge(_board.transform, "Numpad7", nX, rowY, nW, badgeH);

        // ─── Row 2 (y = -12 px): subtext ──────────────
        const float subY = -12f;
        float subW = W - bpx * 2f - acW - 10f;
        float subX = cStartX + subW / 2f;
        TMP(_board.transform, "Sub",
            "Press E or Numpad7 to enter door code",
            subX, subY, subW, 16f, 7f, ColTextSub);

        _board.SetActive(false);
    }

    // ═══════════════════════════════════════════════════
    //  Badge: ขอบ → พื้น → ข้อความ
    //  แต่ละ layer ใช้ siblingIndex ต่างกัน → ไม่ซ้อนทับ
    // ═══════════════════════════════════════════════════

    void Badge(Transform parent, string label,
               float cx, float cy, float w, float h)
    {
        const float bi = 1.2f;
        Img(parent, "BBdr_" + label, cx, cy, w,           h,           ColKeyBdr);
        Img(parent, "BBg_"  + label, cx, cy, w - bi * 2f, h - bi * 2f, ColKeyBg);
        TMP(parent, "BTxt_" + label,
            label,
            cx, cy,
            w - bi * 2f, h,
            h * 0.52f,
            ColTextKey);
    }

    // ═══════════════════════════════════════════════════
    //  Img helper
    // ═══════════════════════════════════════════════════

    void Img(Transform parent, string goName,
             float cx, float cy, float w, float h, Color color)
    {
        var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(cx, cy);
        rt.sizeDelta        = new Vector2(w, h);

        go.GetComponent<Image>().color = color;
    }

    // ═══════════════════════════════════════════════════
    //  TMP helper
    // ═══════════════════════════════════════════════════

    void TMP(Transform parent, string goName, string text,
             float cx, float cy, float w, float h,
             float fontSize, Color color)
    {
        var go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(cx, cy);
        rt.sizeDelta        = new Vector2(w, h);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = fontSize;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.color              = color;
        tmp.fontStyle          = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        tmp.overflowMode       = TextOverflowModes.Overflow;
    }

    // ═══════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════

    /// <summary>เปลี่ยนจุดแสดง UI ระหว่าง Runtime</summary>
    public void SetPromptAnchor(Transform anchor)
    {
        promptAnchor = anchor;
        if (_board != null && anchor != null)
            _board.transform.position = anchor.position;
    }
}
