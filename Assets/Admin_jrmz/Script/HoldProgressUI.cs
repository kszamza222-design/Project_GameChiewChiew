using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HoldProgressUI — วงกลม Progress ค้างปุ่มเพื่อยืนยัน
///
/// ใช้ร่วมกับทุก script ที่ต้องการ hold-to-confirm:
///   TreasureBox, KeyInventory (door), KeypadUIBuilder
///
/// วิธีใช้:
///   1. ติด script นี้กับ GameObject ใดก็ได้ (เช่น GameManager)
///   2. ผูก targetCanvas (Screen Space – Overlay)
///   3. เรียก StartHold() เมื่อเริ่มค้างปุ่ม
///   4. เรียก StopHold() เมื่อปล่อยปุ่ม
///   5. Subscribe onComplete เพื่อรับ event เมื่อครบ
/// </summary>
public class HoldProgressUI : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Canvas ───────────────────────────")]
    public Canvas targetCanvas;

    [Header("── Timing ───────────────────────────")]
    [Tooltip("เวลาที่ต้องค้างปุ่ม (วินาที)")]
    public float holdDuration = 1.2f;

    // ═══════════════════════════════════════════════════
    //  Colors & Style
    // ═══════════════════════════════════════════════════

    static readonly Color ColRingBg   = new Color(0.10f, 0.10f, 0.14f, 0.85f);
    static readonly Color ColRingFill = new Color(0.94f, 0.75f, 0.15f, 1.00f);
    static readonly Color ColRingDone = new Color(0.25f, 0.95f, 0.45f, 1.00f);
    static readonly Color ColCircleBg = new Color(0.06f, 0.06f, 0.10f, 0.95f);
    static readonly Color ColKeyLabel = new Color(1.00f, 0.85f, 0.20f, 1.00f);
    static readonly Color ColSubText  = new Color(0.75f, 0.75f, 0.78f, 1.00f);
    static readonly Color ColBorder   = new Color(0.85f, 0.55f, 0.08f, 0.90f);

    // ═══════════════════════════════════════════════════
    //  Public Events
    // ═══════════════════════════════════════════════════

    /// <summary>เรียกเมื่อค้างปุ่มครบ holdDuration</summary>
    public System.Action onComplete;

    // ═══════════════════════════════════════════════════
    //  Private State
    // ═══════════════════════════════════════════════════

    // P1 ring (จอซ้าย)
    GameObject      _ringRootP1;
    Image           _ringFillP1;
    Image           _ringBgP1;
    TextMeshProUGUI _keyLabelP1;

    // P2 ring (จอขวา)
    GameObject      _ringRootP2;
    Image           _ringFillP2;
    Image           _ringBgP2;
    TextMeshProUGUI _keyLabelP2;

    float _progressP1 = 0f;
    float _progressP2 = 0f;
    bool  _holdingP1  = false;
    bool  _holdingP2  = false;
    bool  _firedP1    = false;
    bool  _firedP2    = false;

    // ═══════════════════════════════════════════════════
    //  Awake
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        BuildRing(out _ringRootP1, out _ringFillP1, out _ringBgP1, out _keyLabelP1,
                  isLeftSide: true);
        BuildRing(out _ringRootP2, out _ringFillP2, out _ringBgP2, out _keyLabelP2,
                  isLeftSide: false);
    }

    // ═══════════════════════════════════════════════════
    //  Update — ทำให้ progress วิ่ง
    // ═══════════════════════════════════════════════════

    void Update()
    {
        TickProgress(ref _progressP1, ref _holdingP1, ref _firedP1,
                     _ringRootP1, _ringFillP1, isP1: true);
        TickProgress(ref _progressP2, ref _holdingP2, ref _firedP2,
                     _ringRootP2, _ringFillP2, isP1: false);
    }

    void TickProgress(ref float progress, ref bool holding, ref bool fired,
                      GameObject root, Image fill, bool isP1)
    {
        if (!holding)
        {
            // decay กลับเร็วกว่าตอนกด
            if (progress > 0f)
            {
                progress -= Time.deltaTime * (1f / holdDuration) * 2.5f;
                progress  = Mathf.Max(progress, 0f);
                UpdateFill(fill, progress, root);
                if (progress <= 0f) root.SetActive(false);
            }
            fired = false;
            return;
        }

        if (fired) return;

        progress += Time.deltaTime / holdDuration;
        progress  = Mathf.Clamp01(progress);
        UpdateFill(fill, progress, root);

        if (progress >= 1f)
        {
            fired   = true;
            holding = false;
            // flash สีเขียว
            fill.color = ColRingDone;
            onComplete?.Invoke();
        }
    }

    void UpdateFill(Image fill, float t, GameObject root)
    {
        if (fill == null) return;
        fill.fillAmount = t;
        fill.color      = Color.Lerp(ColRingFill, ColRingDone, t);
        root.SetActive(t > 0f);
    }

    // ═══════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════

    /// <summary>เริ่มค้างปุ่ม — เรียกทุกเฟรมที่ค้างอยู่</summary>
    public void SetHolding(bool isP1, bool holding)
    {
        if (isP1)
        {
            _holdingP1 = holding;
            if (holding && _progressP1 <= 0f) _ringRootP1.SetActive(true);
        }
        else
        {
            _holdingP2 = holding;
            if (holding && _progressP2 <= 0f) _ringRootP2.SetActive(true);
        }
    }

    /// <summary>รีเซ็ต progress ทั้งคู่ (เช่น หลัง fire แล้ว)</summary>
    public void ResetAll()
    {
        _progressP1 = 0f; _progressP2 = 0f;
        _holdingP1  = false; _holdingP2 = false;
        _firedP1    = false; _firedP2   = false;
        _ringRootP1.SetActive(false);
        _ringRootP2.SetActive(false);
    }

    /// <summary>ตั้งข้อความบนปุ่ม (เช่น "E" หรือ "7")</summary>
    public void SetKeyLabel(string p1Label, string p2Label)
    {
        if (_keyLabelP1 != null) _keyLabelP1.text = p1Label;
        if (_keyLabelP2 != null) _keyLabelP2.text = p2Label;
    }

    // ═══════════════════════════════════════════════════
    //  BuildRing — สร้าง UI วงกลม progress
    //
    //  Layout (กลางจอฝั่งนั้น ล่างเล็กน้อย):
    //
    //       ╭──────╮
    //      /  [ E ]  \    ← วงแหวน progress (Image Type = Filled)
    //      \         /
    //       ╰──────╯
    //     Hold to confirm   ← subtext
    //
    // ═══════════════════════════════════════════════════

    void BuildRing(out GameObject ringRoot, out Image ringFill, out Image ringBg,
                   out TextMeshProUGUI keyLabel, bool isLeftSide)
    {
        ringRoot = null; ringFill = null; ringBg = null; keyLabel = null;
        if (targetCanvas == null) return;

        const float ringSize   = 110f;   // เส้นผ่านศูนย์กลางวงแหวน
        const float ringThick  =  10f;   // ความหนาขอบ (ลบออกจาก inner)
        const float innerSize  = ringSize - ringThick * 2f;
        const float subH       =  28f;
        const float totalH     = ringSize + subH + 6f;

        // ── Root ────────────────────────────────────────
        var root = new GameObject("HoldRing_" + (isLeftSide ? "P1" : "P2"),
                                  typeof(RectTransform));
        root.transform.SetParent(targetCanvas.transform, false);
        var rootRT = root.GetComponent<RectTransform>();

        // วางกลางจอฝั่งนั้น ล่างประมาณ 30% จากบน
        float anchorX = isLeftSide ? 0.25f : 0.75f;
        rootRT.anchorMin        = new Vector2(anchorX, 0.5f);
        rootRT.anchorMax        = new Vector2(anchorX, 0.5f);
        rootRT.pivot            = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta        = new Vector2(ringSize, totalH);
        rootRT.anchoredPosition = Vector2.zero;

        // ── วงนอก BG (วงเต็มสีเข้ม) ────────────────────
        var bgGO = new GameObject("RingBG", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(root.transform, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = bgRT.pivot = new Vector2(0.5f, 1f);
        bgRT.sizeDelta        = new Vector2(ringSize, ringSize);
        bgRT.anchoredPosition = new Vector2(0f, 0f);
        ringBg       = bgGO.GetComponent<Image>();
        ringBg.color = ColRingBg;
        ringBg.type  = Image.Type.Simple;
        // ทำให้เป็นวงกลม: ใช้ sprite กลม หรือ border radius
        // ใช้ Outline เป็น trick เพื่อให้เห็นชัด
        var outline = bgGO.AddComponent<Outline>();
        outline.effectColor    = ColBorder;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // ── Fill Ring (Image Type Filled) ───────────────
        var fillGO = new GameObject("RingFill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(root.transform, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = fillRT.anchorMax = fillRT.pivot = new Vector2(0.5f, 1f);
        fillRT.sizeDelta        = new Vector2(ringSize, ringSize);
        fillRT.anchoredPosition = new Vector2(0f, 0f);
        ringFill            = fillGO.GetComponent<Image>();
        ringFill.color      = ColRingFill;
        ringFill.type       = Image.Type.Filled;
        ringFill.fillMethod = Image.FillMethod.Radial360;
        ringFill.fillOrigin = (int)Image.Origin360.Top;
        ringFill.fillClockwise = true;
        ringFill.fillAmount = 0f;

        // ── Inner Circle (ปิดกลางวงแหวน) ───────────────
        var innerGO = new GameObject("InnerCircle", typeof(RectTransform), typeof(Image));
        innerGO.transform.SetParent(root.transform, false);
        var innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = innerRT.anchorMax = innerRT.pivot = new Vector2(0.5f, 1f);
        innerRT.sizeDelta        = new Vector2(innerSize, innerSize);
        innerRT.anchoredPosition = new Vector2(0f, -(ringThick));
        innerGO.GetComponent<Image>().color = ColCircleBg;

        // ── Border ring얇ๆ รอบวง ────────────────────────
        var borderGO = new GameObject("RingBorder", typeof(RectTransform), typeof(Image));
        borderGO.transform.SetParent(root.transform, false);
        var borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = borderRT.anchorMax = borderRT.pivot = new Vector2(0.5f, 1f);
        borderRT.sizeDelta        = new Vector2(innerSize - 4f, innerSize - 4f);
        borderRT.anchoredPosition = new Vector2(0f, -(ringThick + 2f));
        var borderImg   = borderGO.GetComponent<Image>();
        borderImg.color = new Color(0.85f, 0.55f, 0.08f, 0.25f);

        // ── Key Label กลางวง ────────────────────────────
        var lblGO = new GameObject("KeyLabel", typeof(RectTransform));
        lblGO.transform.SetParent(root.transform, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = lblRT.anchorMax = lblRT.pivot = new Vector2(0.5f, 1f);
        lblRT.sizeDelta        = new Vector2(innerSize, innerSize);
        lblRT.anchoredPosition = new Vector2(0f, -(ringThick));
        keyLabel           = lblGO.AddComponent<TextMeshProUGUI>();
        keyLabel.text      = isLeftSide ? "E" : "7";
        keyLabel.fontSize  = 32f;
        keyLabel.fontStyle = FontStyles.Bold;
        keyLabel.alignment = TextAlignmentOptions.Center;
        keyLabel.color     = ColKeyLabel;

        // ── Subtext ─────────────────────────────────────
        var subGO = new GameObject("SubText", typeof(RectTransform));
        subGO.transform.SetParent(root.transform, false);
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = subRT.anchorMax = subRT.pivot = new Vector2(0.5f, 1f);
        subRT.sizeDelta        = new Vector2(160f, subH);
        subRT.anchoredPosition = new Vector2(0f, -(ringSize + 4f));
        var subTMP    = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text      = "Hold to confirm";
        subTMP.fontSize  = 11f;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color     = ColSubText;

        root.SetActive(false);
        ringRoot = root;
    }
}
