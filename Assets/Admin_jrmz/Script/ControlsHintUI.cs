using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ControlsHintUI — แสดงปุ่มควบคุม Player แต่ละคน
///
/// ปรับตำแหน่งได้อิสระใน Inspector:
///   panelOffsetP1 / panelOffsetP2    — ตำแหน่ง Panel คำอธิบาย
///   toggleOffsetP1 / toggleOffsetP2  — ตำแหน่งปุ่ม [?]
/// </summary>
public class ControlsHintUI : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Canvas ───────────────────────────")]
    public Canvas targetCanvas;

    [Header("── Toggle Keys ─────────────────────")]
    public KeyCode toggleKeyP1 = KeyCode.Tab;
    public KeyCode toggleKeyP2 = KeyCode.KeypadPlus;

    [Header("── Panel Position (P1 — จอซ้าย) ────")]
    [Tooltip("ตำแหน่ง Panel คำอธิบาย P1\n" +
             "X: - = ซ้าย, + = ขวา  Y: - = ลง, + = ขึ้น\n" +
             "อ้างอิงจาก anchor กลางขวาของครึ่งซ้าย (เส้นกลาง)")]
    public Vector2 panelOffsetP1  = new Vector2(-4f,  30f);

    [Tooltip("ตำแหน่งปุ่ม [?] P1\n" +
             "อ้างอิงจาก anchor มุมขวาล่างของครึ่งซ้าย")]
    public Vector2 toggleOffsetP1 = new Vector2(-8f,  8f);

    [Header("── Panel Position (P2 — จอขวา) ─────")]
    [Tooltip("ตำแหน่ง Panel คำอธิบาย P2\n" +
             "อ้างอิงจาก anchor มุมขวาบนของครึ่งขวา")]
    public Vector2 panelOffsetP2  = new Vector2(-8f,  30f);

    [Tooltip("ตำแหน่งปุ่ม [?] P2\n" +
             "อ้างอิงจาก anchor มุมขวาล่างของครึ่งขวา")]
    public Vector2 toggleOffsetP2 = new Vector2(-8f,  8f);

    // ═══════════════════════════════════════════════════
    //  Colors
    // ═══════════════════════════════════════════════════

    static readonly Color ColPanelBg  = new Color(0.05f, 0.05f, 0.08f, 0.97f);
    static readonly Color ColBorder   = new Color(0.90f, 0.58f, 0.08f, 1.00f);
    static readonly Color ColKeyBg    = new Color(0.16f, 0.16f, 0.24f, 1.00f);
    static readonly Color ColKeyBdr   = new Color(0.48f, 0.48f, 0.58f, 1.00f);
    static readonly Color ColKeyText  = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    static readonly Color ColDescText = new Color(0.88f, 0.88f, 0.90f, 1.00f);
    static readonly Color ColTogBdr   = new Color(0.90f, 0.58f, 0.08f, 1.00f);
    static readonly Color ColTogBg    = new Color(0.08f, 0.08f, 0.12f, 0.97f);
    static readonly Color ColTogTxt   = new Color(0.94f, 0.75f, 0.15f, 1.00f);
    static readonly Color ColP1       = new Color(0.30f, 0.72f, 1.00f, 1.00f);
    static readonly Color ColP2       = new Color(0.28f, 1.00f, 0.55f, 1.00f);

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    GameObject _panelP1, _panelP2;
    bool       _openP1,  _openP2;
    Sprite     _circle;

    // เก็บ RectTransform ของ panel และ toggle เพื่อ update ตำแหน่ง runtime
    RectTransform _panelRtP1,  _panelRtP2;
    RectTransform _toggleRtP1, _toggleRtP2;

    // ═══════════════════════════════════════════════════
    //  Awake
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        _circle = MakeCircle(64);
        if (targetCanvas == null) { Debug.LogError("[ControlsHintUI] ไม่มี Canvas!"); return; }
        Build();
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        if (Input.GetKeyDown(toggleKeyP1)) Toggle(ref _openP1, _panelP1);
        if (Input.GetKeyDown(toggleKeyP2)) Toggle(ref _openP2, _panelP2);

        // อัปเดตตำแหน่งแบบ real-time เมื่อปรับใน Inspector ขณะ Play
        ApplyOffsets();
    }

    void Toggle(ref bool open, GameObject panel)
    {
        open = !open;
        if (panel != null) panel.SetActive(open);
    }

    // ── อัปเดตตำแหน่งตาม Inspector ──────────────────
    void ApplyOffsets()
    {
        if (_panelRtP1  != null) _panelRtP1.anchoredPosition  = panelOffsetP1;
        if (_panelRtP2  != null) _panelRtP2.anchoredPosition  = panelOffsetP2;
        if (_toggleRtP1 != null) _toggleRtP1.anchoredPosition = toggleOffsetP1;
        if (_toggleRtP2 != null) _toggleRtP2.anchoredPosition = toggleOffsetP2;
    }

    // ═══════════════════════════════════════════════════
    //  Build
    // ═══════════════════════════════════════════════════

    void Build()
    {
        var p1Rows = new (string k, string d)[]
        {
            ("W A S D", "Movement"),
            ("SPACE",   "Jump"),
            ("E",       "Action / Pick up"),
            ("Q",       "Throw"),
            ("TAB",     "Controls Menu"),
        };

        var p2Rows = new (string k, string d)[]
        {
            ("4 8 5 6", "Movement"),
            ("NUM 0",   "Jump"),
            ("NUM 7",   "Action / Pick up"),
            ("NUM .",   "Throw"),
            ("NUM +",   "Controls Menu"),
        };

        BuildPanel(isLeft: true,  rows: p1Rows, out _panelP1, out _panelRtP1);
        BuildToggle(isLeft: true,  out _toggleRtP1);

        BuildPanel(isLeft: false, rows: p2Rows, out _panelP2, out _panelRtP2);
        BuildToggle(isLeft: false, out _toggleRtP2);

        // ใส่ค่าเริ่มต้นจาก Inspector
        ApplyOffsets();
    }

    // ═══════════════════════════════════════════════════
    //  BuildPanel
    // ═══════════════════════════════════════════════════

    void BuildPanel(bool isLeft, (string k, string d)[] rows,
                    out GameObject panel, out RectTransform panelRt)
    {
        const float W    = 180f;
        const float rowH = 28f;
        const float hdrH = 36f;
        const float pad  = 8f;
        float H = hdrH + rows.Length * rowH + pad;

        panel = new GameObject("Panel_" + (isLeft ? "P1" : "P2"),
                               typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(targetCanvas.transform, false);

        var rt = panel.GetComponent<RectTransform>();
        panelRt = rt;

        if (isLeft)
        {
            // P1: anchor เส้นกลาง pivot ขวา-ล่าง
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(1.0f, 0.0f);
        }
        else
        {
            // P2: anchor มุมขวาบนของครึ่งขวา pivot ขวา-ล่าง
            rt.anchorMin = new Vector2(1.0f, 0.5f);
            rt.anchorMax = new Vector2(1.0f, 0.5f);
            rt.pivot     = new Vector2(1.0f, 0.0f);
        }
        rt.anchoredPosition = isLeft ? panelOffsetP1 : panelOffsetP2;
        rt.sizeDelta = new Vector2(W, H);

        panel.GetComponent<Image>().color = Color.clear;

        // ── Border ──────────────────────────────────
        var bdrGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        bdrGo.transform.SetParent(panel.transform, false);
        FillRect(bdrGo.GetComponent<RectTransform>());
        bdrGo.GetComponent<Image>().color = ColBorder;

        // ── BG ──────────────────────────────────────
        var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(panel.transform, false);
        var bgrt = bgGo.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = new Vector2(2f, 2f);
        bgrt.offsetMax = new Vector2(-2f, -2f);
        bgGo.GetComponent<Image>().color = ColPanelBg;

        Transform p = panel.transform;

        // ── Header BG ───────────────────────────────
        var hBgGo = new GameObject("HdrBg", typeof(RectTransform), typeof(Image));
        hBgGo.transform.SetParent(p, false);
        var hbrt = hBgGo.GetComponent<RectTransform>();
        hbrt.anchorMin = new Vector2(0f, 1f); hbrt.anchorMax = new Vector2(1f, 1f);
        hbrt.pivot = new Vector2(0.5f, 1f);
        hbrt.anchoredPosition = Vector2.zero;
        hbrt.sizeDelta = new Vector2(0f, hdrH);
        hBgGo.GetComponent<Image>().color = new Color(0.90f, 0.55f, 0.05f, 0.85f);

        // ── Header Text ─────────────────────────────
        string hTxt = isLeft ? "PLAYER  1" : "PLAYER  2";
        var hGo = new GameObject("HdrTxt", typeof(RectTransform));
        hGo.transform.SetParent(p, false);
        var hrt = hGo.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0f, hdrH);
        var htmp = hGo.AddComponent<TextMeshProUGUI>();
        htmp.text = hTxt; htmp.fontSize = 14f;
        htmp.fontStyle = FontStyles.Bold;
        htmp.alignment = TextAlignmentOptions.Center;
        htmp.color = ColKeyText;

        // ── เส้นแบ่ง ────────────────────────────────
        var sepGo = new GameObject("Sep", typeof(RectTransform), typeof(Image));
        sepGo.transform.SetParent(p, false);
        var seprt = sepGo.GetComponent<RectTransform>();
        seprt.anchorMin = new Vector2(0f, 1f); seprt.anchorMax = new Vector2(1f, 1f);
        seprt.pivot = new Vector2(0.5f, 1f);
        seprt.anchoredPosition = new Vector2(0f, -hdrH);
        seprt.sizeDelta = new Vector2(0f, 1.5f);
        sepGo.GetComponent<Image>().color = new Color(0.90f, 0.58f, 0.08f, 0.60f);

        // ── Rows ─────────────────────────────────────
        for (int i = 0; i < rows.Length; i++)
        {
            float yPos = -(hdrH + 2f) - i * rowH - rowH / 2f;
            BuildRow(p, rows[i].k, rows[i].d, W, rowH, yPos);
        }

        panel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════
    //  BuildRow
    // ═══════════════════════════════════════════════════

    void BuildRow(Transform parent, string keyLabel, string desc,
                  float panelW, float rowH, float yPos)
    {
        const float keyW  = 66f;
        const float pad   =  6f;
        float descW = panelW - keyW - pad * 3f;

        // ── Key Badge ───────────────────────────────
        var keyGo = new GameObject("Key_" + keyLabel, typeof(RectTransform), typeof(Image));
        keyGo.transform.SetParent(parent, false);
        var krt = keyGo.GetComponent<RectTransform>();
        krt.anchorMin = krt.anchorMax = new Vector2(0f, 1f);
        krt.pivot = new Vector2(0f, 0.5f);
        krt.anchoredPosition = new Vector2(pad, yPos);
        krt.sizeDelta = new Vector2(keyW, rowH - 4f);

        // border
        var kbdrGo = new GameObject("KBdr", typeof(RectTransform), typeof(Image));
        kbdrGo.transform.SetParent(keyGo.transform, false);
        FillRect(kbdrGo.GetComponent<RectTransform>());
        kbdrGo.GetComponent<Image>().color = ColKeyBdr;

        // bg
        var kbgGo = new GameObject("KBg", typeof(RectTransform), typeof(Image));
        kbgGo.transform.SetParent(keyGo.transform, false);
        var kbgrt = kbgGo.GetComponent<RectTransform>();
        kbgrt.anchorMin = Vector2.zero; kbgrt.anchorMax = Vector2.one;
        kbgrt.offsetMin = new Vector2(1.5f, 1.5f);
        kbgrt.offsetMax = new Vector2(-1.5f, -1.5f);
        kbgGo.GetComponent<Image>().color = ColKeyBg;

        // text
        var ktGo = new GameObject("KTxt", typeof(RectTransform));
        ktGo.transform.SetParent(keyGo.transform, false);
        FillRect(ktGo.GetComponent<RectTransform>());
        var ktmp = ktGo.AddComponent<TextMeshProUGUI>();
        ktmp.text = keyLabel; ktmp.fontSize = 9f;
        ktmp.fontStyle = FontStyles.Bold;
        ktmp.alignment = TextAlignmentOptions.Center;
        ktmp.color = ColKeyText;
        ktmp.enableWordWrapping = false;

        // ── Description ─────────────────────────────
        var dGo = new GameObject("Desc_" + desc, typeof(RectTransform));
        dGo.transform.SetParent(parent, false);
        var drt = dGo.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0f, 1f);
        drt.pivot = new Vector2(0f, 0.5f);
        drt.anchoredPosition = new Vector2(keyW + pad * 2f, yPos);
        drt.sizeDelta = new Vector2(descW, rowH);
        var dtmp = dGo.AddComponent<TextMeshProUGUI>();
        dtmp.text = desc; dtmp.fontSize = 10.5f;
        dtmp.alignment = TextAlignmentOptions.Left;
        dtmp.color = ColDescText;
        dtmp.enableWordWrapping = false;
    }

    // ═══════════════════════════════════════════════════
    //  BuildToggle — ปุ่ม [?]
    // ═══════════════════════════════════════════════════

    void BuildToggle(bool isLeft, out RectTransform toggleRt)
    {
        const float S = 36f; // ขนาดปุ่ม

        var go = new GameObject("Toggle_" + (isLeft ? "P1" : "P2"),
                                typeof(RectTransform), typeof(Image));
        go.transform.SetParent(targetCanvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        toggleRt = rt;
        rt.sizeDelta = new Vector2(S, S);

        if (isLeft)
        {
            // P1: anchor มุมขวาล่างของครึ่งซ้าย pivot ขวา-ล่าง
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(1f,   0f);
            rt.anchoredPosition = toggleOffsetP1;
        }
        else
        {
            // P2: anchor มุมขวาล่างของครึ่งขวา pivot ขวา-ล่าง
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(1f, 0f);
            rt.anchoredPosition = toggleOffsetP2;
        }

        // ── วงกลมพื้นหลัง ───────────────────────────
        var img = go.GetComponent<Image>();
        img.sprite = _circle;
        img.color  = ColTogBdr;
        img.type   = Image.Type.Simple;

        // ── วงกลม inner ─────────────────────────────
        var innerGo = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(go.transform, false);
        var irt = innerGo.GetComponent<RectTransform>();
        irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot     = new Vector2(0.5f, 0.5f);
        irt.anchoredPosition = Vector2.zero;
        irt.sizeDelta = new Vector2(S - 3f, S - 3f);
        var innerImg = innerGo.GetComponent<Image>();
        innerImg.sprite = _circle;
        innerImg.color  = ColTogBg;

        // ── ข้อความ "?" ─────────────────────────────
        var txtGo = new GameObject("Txt", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot     = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(S, S);
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();

        string label = isLeft
            ? $"?\n<size=7>TAB</size>"
            : $"?\n<size=7>NUM+</size>";

        tmp.text      = label;
        tmp.fontSize  = 14f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = ColTogTxt;
        tmp.enableWordWrapping = false;

        // ── Button ───────────────────────────────────
        var btn = go.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = Color.white;
        bc.highlightedColor = new Color(1f, 0.85f, 0.4f);
        bc.pressedColor     = new Color(0.7f, 0.5f, 0.1f);
        btn.colors = bc;

        bool captureLeft = isLeft;
        btn.onClick.AddListener(() =>
        {
            if (captureLeft) Toggle(ref _openP1, _panelP1);
            else             Toggle(ref _openP2, _panelP2);
        });
    }

    // ═══════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════

    void FillRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    Sprite MakeCircle(int res)
    {
        var tex    = new Texture2D(res, res, TextureFormat.RGBA32, false);
        var pixels = new Color32[res * res];
        float cx = res / 2f, cy = res / 2f, r = res / 2f;
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            byte  a    = dist <= r ? (byte)255 : (byte)0;
            pixels[y * res + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res),
                             new Vector2(0.5f, 0.5f), res);
    }
}
