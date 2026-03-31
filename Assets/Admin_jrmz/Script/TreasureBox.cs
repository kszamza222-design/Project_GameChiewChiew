using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// TreasureBox — กล่องสมบัติ
///
/// ป้าย 3D มีวงกลม progress ฝั่งขวา
/// ค้างปุ่ม E / Numpad7 → วงวิ่ง → ครบ → เปิดกล่อง
///
/// Setup:
///   1. ติด Script นี้กับ GameObject กล่องสมบัติ
///   2. ผูก player1, player2, targetCanvas
///   3. ผูก boxAnimator, keySprite, keyInventory
///   4. ผูก promptAnchor (ไม่บังคับ) + ปรับ boardRotation Y
/// </summary>
public class TreasureBox : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Players ─────────────────────────")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("── Canvas (Screen Space Overlay) ───")]
    public Canvas targetCanvas;

    [Header("── Animator ────────────────────────")]
    public Animator boxAnimator;
    public string   openTrigger = "Open";

    [Header("── Key Sprite ──────────────────────")]
    public Sprite keySprite;

    [Header("── Prompt Position ─────────────────")]
    public Transform promptAnchor;
    public float     heightAbove  = 2.0f;

    [Header("── Show Radius ──────────────────────")]
    public float showRadius = 3f;

    [Header("── Board Rotation ──────────────────")]
    public Vector3 boardRotation = new Vector3(0f, 180f, 0f);

    [Header("── Key Inventory ───────────────────")]
    public KeyInventory keyInventory;

    [Header("── Timing ───────────────────────────")]
    [Tooltip("วินาทีที่ต้องค้างปุ่ม")]
    public float holdDuration   = 1.2f;
    [Tooltip("เวลาแสดง notification (วินาที)")]
    public float notifyDuration = 3.0f;

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
    static readonly Color ColGreen    = new Color(0.20f, 0.90f, 0.40f, 1.00f);
    static readonly Color ColRingBg   = new Color(0.12f, 0.12f, 0.18f, 1.00f);
    static readonly Color ColRingFill = new Color(0.94f, 0.75f, 0.15f, 1.00f);
    static readonly Color ColRingDone = new Color(0.25f, 0.95f, 0.45f, 1.00f);

    // ═══════════════════════════════════════════════════
    //  Private — Board & Ring refs
    // ═══════════════════════════════════════════════════

    GameObject _board;
    Image      _ringFill;          // Image Filled วงกลม
    Image      _ringFillBg;        // วงพื้นหลังสีเข้ม

    GameObject _notifyP1;
    GameObject _notifyP2;

    bool  _boxOpened  = false;
    float _holdTimer  = 0f;        // 0 → 1
    bool  _fired      = false;     // ยิงแล้ว ป้องกัน double-fire
    bool  _isP1Last   = true;      // คนล่าสุดที่ค้าง

    // ═══════════════════════════════════════════════════
    //  Awake
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        BuildBoard();
        BuildNotification(out _notifyP1, isLeftSide: true);
        BuildNotification(out _notifyP2, isLeftSide: false);
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        UpdateBoard();
        HandleHold();
    }

    // ═══════════════════════════════════════════════════
    //  UpdateBoard
    // ═══════════════════════════════════════════════════

    void UpdateBoard()
    {
        if (_board == null) return;
        if (_boxOpened) { _board.SetActive(false); return; }

        bool show = IsNear(player1) || IsNear(player2);
        _board.SetActive(show);
        if (show) _board.transform.rotation = Quaternion.Euler(boardRotation);
    }

    // ═══════════════════════════════════════════════════
    //  HandleHold — ค้างปุ่มเพื่อเปิด
    // ═══════════════════════════════════════════════════

    void HandleHold()
    {
        if (_boxOpened) return;

        bool p1Near    = IsNear(player1);
        bool p2Near    = IsNear(player2);
        bool p1Holding = p1Near && Input.GetKey(KeyCode.E);
        bool p2Holding = p2Near && Input.GetKey(KeyCode.Keypad7);
        bool anyHold   = p1Holding || p2Holding;

        if (p1Holding) _isP1Last = true;
        if (p2Holding) _isP1Last = false;

        if (anyHold && !_fired)
        {
            _holdTimer += Time.deltaTime / holdDuration;
            _holdTimer  = Mathf.Clamp01(_holdTimer);
            SetRing(_holdTimer);

            if (_holdTimer >= 1f)
            {
                _fired = true;
                OpenBox(_isP1Last);
            }
        }
        else if (!anyHold)
        {
            // decay เร็วกว่าตอนกด 2.5x
            if (_holdTimer > 0f)
            {
                _holdTimer -= Time.deltaTime / holdDuration * 2.5f;
                _holdTimer  = Mathf.Max(0f, _holdTimer);
                SetRing(_holdTimer);
            }
            _fired = false;
        }
    }

    // ── อัปเดตสีและ fill ของวง ──────────────────────

    void SetRing(float t)
    {
        if (_ringFill == null) return;
        _ringFill.fillAmount = t;
        _ringFill.color      = Color.Lerp(ColRingFill, ColRingDone, t);
    }

    // ═══════════════════════════════════════════════════
    //  OpenBox
    // ═══════════════════════════════════════════════════

    void OpenBox(bool isP1)
    {
        _boxOpened = true;

        if (boxAnimator == null)
            boxAnimator = GetComponentInChildren<Animator>();

        if (boxAnimator != null)
        {
            bool found = false;
            foreach (var p in boxAnimator.parameters)
                if (p.name == openTrigger) { found = true; break; }
            if (found) boxAnimator.SetTrigger(openTrigger);
        }

        if (keyInventory != null) keyInventory.AddKey(isP1);

        StartCoroutine(ShowNotification(isP1 ? _notifyP1 : _notifyP2));
    }

    bool IsNear(PlayerController p) =>
        p != null && Vector3.Distance(p.transform.position, transform.position) <= showRadius;

    // ═══════════════════════════════════════════════════
    //  BuildBoard
    //
    //  Layout ป้าย 3D (320 × 72 px @ scale 0.01):
    //
    //  ┌─────────────────────────────────────────────┐
    //  ║▌  [E]  or  [Numpad7]          ╭───╮         ║
    //  ║   Hold E or Numpad7 to open   │ ◉ │         ║
    //  └─────────────────────────────────────────────┘
    //                                  └───┘  ← ring อยู่ฝั่งขวา
    // ═══════════════════════════════════════════════════

    void BuildBoard()
    {
        Vector3 worldPos = promptAnchor != null
            ? promptAnchor.position
            : transform.position + Vector3.up * heightAbove;

        _board = new GameObject("_TreasurePromptBoard");
        _board.transform.position   = worldPos;
        _board.transform.rotation   = Quaternion.Euler(boardRotation);
        _board.transform.localScale = Vector3.one * 0.01f;

        var canvas        = _board.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // ── ป้ายกว้างขึ้นเพื่อให้มีที่วางวง ──
        const float W = 320f;
        const float H =  72f;

        var rootRT       = _board.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(W, H);

        // ── Border ──────────────────────────────────
        Img(_board.transform, "Border", 0, 0, W, H, ColBorder);

        // ── BG ──────────────────────────────────────
        const float bpx = 1.5f;
        Img(_board.transform, "BG", 0, 0, W - bpx * 2f, H - bpx * 2f, ColBg);

        // ── Accent bar ───────────────────────────────
        const float acW = 5.5f;
        const float acH = H - bpx * 2f;
        float acX = -(W / 2f - bpx - acW / 2f);
        Img(_board.transform, "Accent", acX, 0, acW, acH, ColAccent);

        // ── Content เริ่มหลัง accent ─────────────────
        float cStartX = acX + acW / 2f + 8f;

        // ── Row 1: [E]  or  [Numpad7] ────────────────
        const float rowY   =  13f;
        const float badgeH =  22f;

        const float eW = 30f;
        float eX = cStartX + eW / 2f;
        Badge(_board.transform, "E", eX, rowY, eW, badgeH);

        const float orW = 18f;
        float orX = eX + eW / 2f + 4f + orW / 2f;
        TMP(_board.transform, "Or", "or", orX, rowY, orW, badgeH, 8.5f, ColOr);

        const float nW = 58f;
        float nX = orX + orW / 2f + 4f + nW / 2f;
        Badge(_board.transform, "Numpad7", nX, rowY, nW, badgeH);

        // ── Row 2: subtext ────────────────────────────
        // เว้นที่ให้วง 58 px ทางขวา
        const float ringAreaW = 58f;
        float subW = W - bpx * 2f - acW - 10f - ringAreaW - 4f;
        float subX = cStartX + subW / 2f;
        TMP(_board.transform, "Sub", "Hold E or Numpad7 to open",
            subX, -12f, subW, 16f, 7f, ColTextSub);

        // ════════════════════════════════════════════
        //  Ring — วางฝั่งขวา กลาง H
        // ════════════════════════════════════════════

        const float ringOuter = 48f;   // เส้นผ่านศูนย์กลางวงนอก
        const float ringThick =  7f;   // ความหนาขอบ
        const float ringInner = ringOuter - ringThick * 2f;

        // X กลางวง = ขอบขวา - padding - ครึ่งวง
        float ringCX = W / 2f - bpx - ringAreaW / 2f - 2f;
        float ringCY = 0f;  // กลาง H

        // วงพื้นหลัง (สีเข้ม)
        var bgRingGO = new GameObject("RingBg", typeof(RectTransform), typeof(Image));
        bgRingGO.transform.SetParent(_board.transform, false);
        var bgRingRT = bgRingGO.GetComponent<RectTransform>();
        bgRingRT.anchorMin = bgRingRT.anchorMax = bgRingRT.pivot = new Vector2(0.5f, 0.5f);
        bgRingRT.sizeDelta        = new Vector2(ringOuter, ringOuter);
        bgRingRT.anchoredPosition = new Vector2(ringCX, ringCY);
        _ringFillBg       = bgRingGO.GetComponent<Image>();
        _ringFillBg.color = ColRingBg;

        // วง Fill (Radial360)
        var fillGO = new GameObject("RingFill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(_board.transform, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = fillRT.anchorMax = fillRT.pivot = new Vector2(0.5f, 0.5f);
        fillRT.sizeDelta        = new Vector2(ringOuter, ringOuter);
        fillRT.anchoredPosition = new Vector2(ringCX, ringCY);
        _ringFill               = fillGO.GetComponent<Image>();
        _ringFill.color         = ColRingFill;
        _ringFill.type          = Image.Type.Filled;
        _ringFill.fillMethod    = Image.FillMethod.Radial360;
        _ringFill.fillOrigin    = (int)Image.Origin360.Top;
        _ringFill.fillClockwise = true;
        _ringFill.fillAmount    = 0f;

        // วงใน (ปิดกลาง ทำให้เห็นเป็นแหวน)
        var innerGO = new GameObject("RingInner", typeof(RectTransform), typeof(Image));
        innerGO.transform.SetParent(_board.transform, false);
        var innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = innerRT.anchorMax = innerRT.pivot = new Vector2(0.5f, 0.5f);
        innerRT.sizeDelta        = new Vector2(ringInner, ringInner);
        innerRT.anchoredPosition = new Vector2(ringCX, ringCY);
        innerGO.GetComponent<Image>().color = ColBg;

        // ตัวอักษร E / 7 กลางวง (แสดง key ที่ต้องกด)
        var lblGO = new GameObject("RingLabel", typeof(RectTransform));
        lblGO.transform.SetParent(_board.transform, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = lblRT.anchorMax = lblRT.pivot = new Vector2(0.5f, 0.5f);
        lblRT.sizeDelta        = new Vector2(ringInner, ringInner);
        lblRT.anchoredPosition = new Vector2(ringCX, ringCY);
        var lblTMP    = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text      = "E\n<size=60%>/ 7</size>";
        lblTMP.fontSize  = 12f;
        lblTMP.fontStyle = FontStyles.Bold;
        lblTMP.alignment = TextAlignmentOptions.Center;
        lblTMP.color     = ColTextKey;

        _board.SetActive(false);
    }

    // ═══════════════════════════════════════════════════
    //  BuildNotification
    // ═══════════════════════════════════════════════════

    void BuildNotification(out GameObject notify, bool isLeftSide)
    {
        notify = null;
        if (targetCanvas == null) return;

        const float W = 220f, H = 56f, PAD = 16f;

        var root = new GameObject("KeyNotify_" + (isLeftSide ? "P1" : "P2"),
                                  typeof(RectTransform));
        root.transform.SetParent(targetCanvas.transform, false);
        var rt = root.GetComponent<RectTransform>();

        float anchorX = isLeftSide ? 0f : 0.5f;
        rt.anchorMin        = new Vector2(anchorX, 0f);
        rt.anchorMax        = new Vector2(anchorX, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.sizeDelta        = new Vector2(W, H);
        rt.anchoredPosition = new Vector2(PAD, PAD);

        const float bpx = 1.5f;
        Img(root.transform, "Border", 0, 0, W, H, ColBorder);
        Img(root.transform, "BG",     0, 0, W - bpx * 2f, H - bpx * 2f, ColBg);

        const float acW = 5f, acH = H - bpx * 2f;
        float acX2 = -(W / 2f - bpx - acW / 2f);
        Img(root.transform, "Accent", acX2, 0, acW, acH, ColGreen);

        float cX      = acX2 + acW / 2f + 6f;
        const float iconS = 28f;
        float iconX   = cX + iconS / 2f;

        if (keySprite != null)
        {
            var iconGO = new GameObject("KeyIcon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(root.transform, false);
            var iRT = iconGO.GetComponent<RectTransform>();
            iRT.anchorMin = iRT.anchorMax = iRT.pivot = new Vector2(0.5f, 0.5f);
            iRT.sizeDelta        = new Vector2(iconS, iconS);
            iRT.anchoredPosition = new Vector2(iconX, 4f);
            iconGO.GetComponent<Image>().sprite = keySprite;
        }

        float txtX = iconX + iconS / 2f + 4f;
        TMP(root.transform, "Title", "Key obtained!",
            txtX + 40f, 8f,  160f, 22f, 11f, ColGreen);
        TMP(root.transform, "Sub",   "Added to inventory",
            txtX + 40f, -10f, 160f, 16f, 8f, ColTextSub);

        foreach (var img in root.GetComponentsInChildren<Image>())
        { var c = img.color; c.a = 0f; img.color = c; }
        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>())
        { var c = t.color; c.a = 0f; t.color = c; }

        root.SetActive(false);
        notify = root;
    }

    // ═══════════════════════════════════════════════════
    //  ShowNotification
    // ═══════════════════════════════════════════════════

    IEnumerator ShowNotification(GameObject notify)
    {
        if (notify == null) yield break;
        notify.SetActive(true);
        yield return StartCoroutine(FadeNotify(notify, 0f, 1f, 0.3f));
        yield return new WaitForSeconds(notifyDuration);
        yield return StartCoroutine(FadeNotify(notify, 1f, 0f, 0.5f));
        notify.SetActive(false);
    }

    IEnumerator FadeNotify(GameObject notify, float from, float to, float dur)
    {
        float t = 0f;
        var imgs = notify.GetComponentsInChildren<Image>();
        var tmps = notify.GetComponentsInChildren<TextMeshProUGUI>();
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / dur);
            foreach (var img in imgs) { var c = img.color; c.a = a; img.color = c; }
            foreach (var tmp in tmps) { var c = tmp.color; c.a = a; tmp.color = c; }
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════
    //  UI Helpers
    // ═══════════════════════════════════════════════════

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
