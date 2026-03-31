using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// KeypadPromptUI — ป้าย 3D เหนือประตู Keypad (พร้อมวงกลม progress ฝั่งขวา)
/// ส่ง ref ของ RingFill ให้ KeypadUIBuilder เพื่อ animate
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
    public Transform promptAnchor;
    public float     heightAboveKeypad = 2.2f;

    [Header("── Show Radius ──────────────────────")]
    public float showRadius = 3f;

    [Header("── Board Rotation ──────────────────")]
    public Vector3 boardRotation = new Vector3(0f, 180f, 0f);

    // ═══════════════════════════════════════════════════
    //  Colors
    // ═══════════════════════════════════════════════════

    static readonly Color ColBg       = new Color(0.06f, 0.06f, 0.09f, 0.96f);
    static readonly Color ColBorder   = new Color(0.85f, 0.55f, 0.08f, 1.00f);
    static readonly Color ColAccent   = new Color(0.94f, 0.62f, 0.15f, 1.00f);
    static readonly Color ColKeyBg    = new Color(0.14f, 0.14f, 0.20f, 1.00f);
    static readonly Color ColKeyBdr   = new Color(0.70f, 0.45f, 0.05f, 1.00f);
    static readonly Color ColTextKey  = new Color(1.00f, 0.80f, 0.20f, 1.00f);
    static readonly Color ColTextSub  = new Color(0.80f, 0.80f, 0.82f, 1.00f);
    static readonly Color ColOr       = new Color(0.50f, 0.50f, 0.54f, 1.00f);
    static readonly Color ColRingBg   = new Color(0.12f, 0.12f, 0.18f, 1.00f);
    static readonly Color ColRingFill = new Color(0.94f, 0.75f, 0.15f, 1.00f);

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

        if (_builder != null && _builder.DoorUnlocked)
        {
            _board.SetActive(false);
            return;
        }

        bool p1Near = player1 != null &&
                      Vector3.Distance(player1.transform.position, transform.position) <= showRadius;
        bool p2Near = player2 != null &&
                      Vector3.Distance(player2.transform.position, transform.position) <= showRadius;

        bool shouldShow = (p1Near || p2Near) &&
                          (_builder == null || !_builder.IsKeypadOpen);

        _board.SetActive(shouldShow);
        if (shouldShow)
            _board.transform.rotation = Quaternion.Euler(boardRotation);
    }

    // ═══════════════════════════════════════════════════
    //  BuildBoard
    //
    //  ┌─────────────────────────────────────────────┐
    //  ║▌  [E]  or  [Numpad7]  enter code   ╭────╮  ║
    //  ║                                    │ ◉  │  ║
    //  └─────────────────────────────────────────────┘
    // ═══════════════════════════════════════════════════

    void BuildBoard()
    {
        Vector3 worldPos = promptAnchor != null
            ? promptAnchor.position
            : transform.position + Vector3.up * heightAboveKeypad;

        _board = new GameObject("_PromptBoard");
        _board.transform.position   = worldPos;
        _board.transform.rotation   = Quaternion.identity;
        _board.transform.localScale = Vector3.one * 0.01f;

        var canvas        = _board.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        const float W = 320f;
        const float H =  72f;

        var rootRT       = _board.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(W, H);

        // Border
        Img(_board.transform, "Border", 0, 0, W, H, ColBorder);

        // BG
        const float bpx = 1.5f;
        Img(_board.transform, "BG", 0, 0, W - bpx * 2f, H - bpx * 2f, ColBg);

        // Accent
        const float acW = 5.5f, acH = H - bpx * 2f;
        float acX = -(W / 2f - bpx - acW / 2f);
        Img(_board.transform, "Accent", acX, 0, acW, acH, ColAccent);

        float cStartX = acX + acW / 2f + 8f;

        // Row 1: badges
        const float rowY = 13f, badgeH = 22f;

        const float eW = 30f;
        float eX = cStartX + eW / 2f;
        Badge(_board.transform, "E", eX, rowY, eW, badgeH);

        const float orW = 18f;
        float orX = eX + eW / 2f + 4f + orW / 2f;
        TMP(_board.transform, "Or", "or", orX, rowY, orW, badgeH, 8.5f, ColOr);

        const float nW = 58f;
        float nX = orX + orW / 2f + 4f + nW / 2f;
        Badge(_board.transform, "Numpad7", nX, rowY, nW, badgeH);

        // Row 2: subtext (เว้นที่วงขวา)
        const float ringAreaW = 58f;
        float subW = W - bpx * 2f - acW - 10f - ringAreaW - 4f;
        float subX = cStartX + subW / 2f;
        TMP(_board.transform, "Sub", "Hold E or Numpad7 to enter door code",
            subX, -12f, subW, 16f, 7f, ColTextSub);

        // ── Ring ──────────────────────────────────────
        const float ringOuter = 48f;
        const float ringThick =  7f;
        const float ringInner = ringOuter - ringThick * 2f;
        float ringCX = W / 2f - bpx - ringAreaW / 2f - 2f;

        var bgRingGO = new GameObject("RingBg", typeof(RectTransform), typeof(Image));
        bgRingGO.transform.SetParent(_board.transform, false);
        var bgRingRT = bgRingGO.GetComponent<RectTransform>();
        bgRingRT.anchorMin = bgRingRT.anchorMax = bgRingRT.pivot = new Vector2(0.5f, 0.5f);
        bgRingRT.sizeDelta        = new Vector2(ringOuter, ringOuter);
        bgRingRT.anchoredPosition = new Vector2(ringCX, 0f);
        bgRingGO.GetComponent<Image>().color = ColRingBg;

        var fillGO = new GameObject("RingFill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(_board.transform, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = fillRT.anchorMax = fillRT.pivot = new Vector2(0.5f, 0.5f);
        fillRT.sizeDelta        = new Vector2(ringOuter, ringOuter);
        fillRT.anchoredPosition = new Vector2(ringCX, 0f);
        var ringFill            = fillGO.GetComponent<Image>();
        ringFill.color          = ColRingFill;
        ringFill.type           = Image.Type.Filled;
        ringFill.fillMethod     = Image.FillMethod.Radial360;
        ringFill.fillOrigin     = (int)Image.Origin360.Top;
        ringFill.fillClockwise  = true;
        ringFill.fillAmount     = 0f;

        // ส่ง ref ให้ KeypadUIBuilder animate
        _builder?.SetRingRef(ringFill);

        var innerGO = new GameObject("RingInner", typeof(RectTransform), typeof(Image));
        innerGO.transform.SetParent(_board.transform, false);
        var innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = innerRT.anchorMax = innerRT.pivot = new Vector2(0.5f, 0.5f);
        innerRT.sizeDelta        = new Vector2(ringInner, ringInner);
        innerRT.anchoredPosition = new Vector2(ringCX, 0f);
        innerGO.GetComponent<Image>().color = ColBg;

        var lblGO = new GameObject("RingLabel", typeof(RectTransform));
        lblGO.transform.SetParent(_board.transform, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = lblRT.anchorMax = lblRT.pivot = new Vector2(0.5f, 0.5f);
        lblRT.sizeDelta        = new Vector2(ringInner, ringInner);
        lblRT.anchoredPosition = new Vector2(ringCX, 0f);
        var lbl    = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = "E\n<size=60%>/ 7</size>";
        lbl.fontSize  = 12f;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color     = ColTextKey;

        _board.SetActive(false);
    }

    // ─── UI Helpers ──────────────────────────────────

    void Img(Transform parent, string name, float x, float y, float w, float h, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
        go.GetComponent<Image>().color = col;
    }

    void TMP(Transform parent, string name, string text,
             float x, float y, float w, float h, float fontSize, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = col;
    }

    void Badge(Transform parent, string label, float x, float y, float w, float h)
    {
        var outer = new GameObject("Badge_" + label, typeof(RectTransform), typeof(Image));
        outer.transform.SetParent(parent, false);
        var oRT = outer.GetComponent<RectTransform>();
        oRT.anchorMin = oRT.anchorMax = oRT.pivot = new Vector2(0.5f, 0.5f);
        oRT.anchoredPosition = new Vector2(x, y);
        oRT.sizeDelta        = new Vector2(w, h);
        outer.GetComponent<Image>().color = ColKeyBdr;

        const float bp = 1.2f;
        var inner = new GameObject("BG", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(outer.transform, false);
        var iRT = inner.GetComponent<RectTransform>();
        iRT.anchorMin = Vector2.zero; iRT.anchorMax = Vector2.one;
        iRT.offsetMin = new Vector2(bp, bp); iRT.offsetMax = new Vector2(-bp, -bp);
        inner.GetComponent<Image>().color = ColKeyBg;

        var tGO = new GameObject("Lbl", typeof(RectTransform));
        tGO.transform.SetParent(outer.transform, false);
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 9f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = ColTextKey;
    }
}
