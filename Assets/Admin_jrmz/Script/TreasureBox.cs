using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// TreasureBox — ระบบกล่องสมบัติให้กุญแจ
///
/// เมื่อผู้เล่นเข้าใกล้ → ป้าย [ E ] or [ Numpad7 ] ขึ้น
/// เมื่อกด → กล่องเล่น Animation เปิด + แจ้งเตือน "Add Key" ล่างซ้ายฝั่งนั้น
///
/// Setup:
///   1. ติด Script นี้กับ GameObject กล่องสมบัติ
///   2. ผูก player1, player2, targetCanvas
///   3. ผูก boxAnimator (Animator ของกล่อง)
///      — สร้าง Trigger parameter ชื่อ "Open" ใน Animator
///   4. ผูก keySprite (รูปกุญแจ)
///   5. ผูก promptAnchor (Empty GameObject เหนือกล่อง) — ไม่บังคับ
///   6. ปรับ boardRotation Y ให้ป้ายหันถูกทิศ
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
    [Tooltip("Animator ของกล่องสมบัติ — ต้องมี Trigger 'Open'")]
    public Animator boxAnimator;
    [Tooltip("ชื่อ Trigger parameter ใน Animator")]
    public string openTrigger = "Open";

    [Header("── Key Sprite ──────────────────────")]
    [Tooltip("รูปกุญแจที่จะแสดงตอนได้รับ")]
    public Sprite keySprite;

    [Header("── Prompt Position ─────────────────")]
    public Transform promptAnchor;
    public float heightAbove = 2.0f;

    [Header("── Show Radius ──────────────────────")]
    public float showRadius = 3f;

    [Header("── Board Rotation ─────────────────")]
    [Tooltip("ปรับ Y ให้ป้ายหันถูกทิศ")]
    public Vector3 boardRotation = new Vector3(0f, 180f, 0f);

    [Header("── Key Inventory ───────────────────")]
    [Tooltip("ผูก GameObject ที่มี KeyInventory script")]
    public KeyInventory keyInventory;

    [Header("── Notification ─────────────────────")]
    [Tooltip("เวลาแสดง notification (วินาที)")]
    public float notifyDuration = 3.0f;

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
    static readonly Color ColGreen   = new Color(0.20f, 0.90f, 0.40f, 1.00f);

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    GameObject _board;           // ป้าย 3D
    GameObject _notifyP1;        // notification ล่างซ้ายฝั่ง P1
    GameObject _notifyP2;        // notification ล่างซ้ายฝั่ง P2

    bool _boxOpened  = false;    // เปิดกล่องแล้ว (ถาวร)

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
        HandleInput();
    }

    // ═══════════════════════════════════════════════════
    //  UpdateBoard
    // ═══════════════════════════════════════════════════

    void UpdateBoard()
    {
        if (_board == null) return;

        // ซ่อนถาวรหลังเปิดกล่องแล้ว
        if (_boxOpened)
        {
            _board.SetActive(false);
            return;
        }

        bool p1Near = IsNear(player1);
        bool p2Near = IsNear(player2);
        bool show   = p1Near || p2Near;

        _board.SetActive(show);
        if (show)
            _board.transform.rotation = Quaternion.Euler(boardRotation);
    }

    // ═══════════════════════════════════════════════════
    //  HandleInput
    // ═══════════════════════════════════════════════════

    void HandleInput()
    {
        if (_boxOpened) return;

        if (IsNear(player1) && Input.GetKeyDown(KeyCode.E))
            OpenBox(isP1: true);

        if (IsNear(player2) && Input.GetKeyDown(KeyCode.Keypad7))
            OpenBox(isP1: false);
    }

    // ═══════════════════════════════════════════════════
    //  OpenBox
    // ═══════════════════════════════════════════════════

    void OpenBox(bool isP1)
    {
        _boxOpened = true;

        // เล่น Animation กล่อง — ถ้าไม่ได้ผูกใน Inspector จะหาเองอัตโนมัติ
        if (boxAnimator == null)
            boxAnimator = GetComponentInChildren<Animator>();

        if (boxAnimator != null)
        {
            // ตรวจว่ามี Parameter จริงไหม
            bool found = false;
            foreach (var p in boxAnimator.parameters)
                if (p.name == openTrigger) { found = true; break; }

            if (found)
            {
                boxAnimator.SetTrigger(openTrigger);
                Debug.Log("[TreasureBox] SetTrigger: " + openTrigger + " ✓");
            }
            else
            {
                Debug.LogError("[TreasureBox] ไม่พบ Trigger '" + openTrigger +
                               "' ใน Animator — กรุณาเพิ่ม Trigger parameter ชื่อ 'Open'");
            }
        }
        else
        {
            Debug.LogError("[TreasureBox] ไม่พบ Animator บน GameObject นี้หรือ Children!");
        }

        // เพิ่มกุญแจใน Inventory
        if (keyInventory != null)
            keyInventory.AddKey(isP1);
        else
            Debug.LogWarning("[TreasureBox] ยังไม่ได้ผูก KeyInventory!");

        // แสดง notification ฝั่งที่เปิด
        StartCoroutine(ShowNotification(isP1 ? _notifyP1 : _notifyP2));
    }

    // ═══════════════════════════════════════════════════
    //  ShowNotification — fade in → รอ → fade out
    // ═══════════════════════════════════════════════════

    IEnumerator ShowNotification(GameObject notify)
    {
        if (notify == null) yield break;

        notify.SetActive(true);

        // Fade in (0.3 วินาที)
        yield return StartCoroutine(FadeNotify(notify, 0f, 1f, 0.3f));

        // รอแสดง
        yield return new WaitForSeconds(notifyDuration);

        // Fade out (0.5 วินาที)
        yield return StartCoroutine(FadeNotify(notify, 1f, 0f, 0.5f));

        notify.SetActive(false);
    }

    IEnumerator FadeNotify(GameObject notify, float from, float to, float duration)
    {
        float t = 0f;
        var   graphics = notify.GetComponentsInChildren<Graphic>();

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);
            foreach (var g in graphics)
            {
                var c = g.color;
                c.a   = c.a > 0.01f || to > 0f ? alpha * GetBaseAlpha(g) : 0f;
                g.color = c;
            }
            yield return null;
        }
    }

    // เก็บ alpha ดั้งเดิมไว้ก่อน fade
    float GetBaseAlpha(Graphic g)
    {
        // คืนค่า 1 เสมอ (alpha จัดการผ่าน lerp ด้านบน)
        return 1f;
    }

    // ═══════════════════════════════════════════════════
    //  BuildBoard — ป้าย 3D
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

        const float W = 260f, H = 68f;
        _board.GetComponent<RectTransform>().sizeDelta = new Vector2(W, H);

        Img(_board.transform, "Border", 0, 0, W, H, ColBorder);

        const float bpx = 1.5f;
        Img(_board.transform, "BG", 0, 0, W - bpx * 2f, H - bpx * 2f, ColBg);

        const float acW = 5.5f, acH = H - bpx * 2f;
        float acX = -(W / 2f - bpx - acW / 2f);
        Img(_board.transform, "Accent", acX, 0, acW, acH, ColAccent);

        float cStartX = acX + acW / 2f + 8f;

        float eW = 30f, eX = cStartX + eW / 2f;
        Badge(_board.transform, "E", eX, 13f, eW, 22f);

        float orW = 18f, orX = eX + eW / 2f + 4f + orW / 2f;
        TMP(_board.transform, "Or", "or", orX, 13f, orW, 22f, 8.5f, ColOr);

        float nW = 58f, nX = orX + orW / 2f + 4f + nW / 2f;
        Badge(_board.transform, "Numpad7", nX, 13f, nW, 22f);

        float subW = W - bpx * 2f - acW - 10f;
        TMP(_board.transform, "Sub",
            "Press E or Numpad7 to open",
            cStartX + subW / 2f, -12f, subW, 16f, 7f, ColTextSub);

        _board.SetActive(false);
    }

    // ═══════════════════════════════════════════════════
    //  BuildNotification
    //
    //  Layout (ล่างซ้ายของฝั่งนั้น):
    //
    //  ┌──────────────────────────────┐
    //  │  [🔑]   Add Key              │
    //  └──────────────────────────────┘
    //
    //  isLeftSide = true  → P1 จอซ้าย   anchor pivot ซ้ายล่าง
    //  isLeftSide = false → P2 จอขวา    anchor pivot ซ้ายล่าง (ของครึ่งขวา)
    // ═══════════════════════════════════════════════════

    void BuildNotification(out GameObject notify, bool isLeftSide)
    {
        notify = null;
        if (targetCanvas == null) return;

        string suffix = isLeftSide ? "P1" : "P2";

        const float W  = 260f;
        const float H  =  64f;
        const float PAD = 60f;  // ระยะห่างจากขอบจอ (ยกสูงขึ้น)

        // ── Root ────────────────────────────────────
        var root = new GameObject("_KeyNotify_" + suffix,
                                  typeof(RectTransform));
        root.transform.SetParent(targetCanvas.transform, false);

        var rt = root.GetComponent<RectTransform>();

        // วางล่างซ้ายของครึ่งจอนั้น
        if (isLeftSide)
        {
            // P1: ครึ่งซ้าย → pivot/anchor ซ้ายล่าง, x=PAD, y=PAD
            rt.anchorMin        = new Vector2(0f,    0f);
            rt.anchorMax        = new Vector2(0f,    0f);
            rt.pivot            = new Vector2(0f,    0f);
            rt.anchoredPosition = new Vector2(PAD,   PAD);
        }
        else
        {
            // P2: ครึ่งขวา → pivot/anchor ซ้ายล่างของครึ่งขวา
            rt.anchorMin        = new Vector2(0.5f,  0f);
            rt.anchorMax        = new Vector2(0.5f,  0f);
            rt.pivot            = new Vector2(0f,    0f);
            rt.anchoredPosition = new Vector2(PAD,   PAD);
        }

        rt.sizeDelta = new Vector2(W, H);

        Transform t = root.transform;

        // ── Border ──────────────────────────────────
        MakeImg(t, "NfBorder", Vector2.zero, new Vector2(W, H), ColBorder,
                pivot: new Vector2(0f, 0f));

        // ── BG ──────────────────────────────────────
        const float b = 1.5f;
        MakeImg(t, "NfBg",
                new Vector2(b, b), new Vector2(W - b * 2f, H - b * 2f), ColBg,
                pivot: new Vector2(0f, 0f));

        // ── Accent bar ซ้าย ──────────────────────────
        const float acW2 = 5f;
        MakeImg(t, "NfAccent",
                new Vector2(b, b), new Vector2(acW2, H - b * 2f), ColAccent,
                pivot: new Vector2(0f, 0f));

        // ── Icon กุญแจ ───────────────────────────────
        const float iconSize = 44f;
        const float iconX    = b + acW2 + 10f + iconSize / 2f;
        const float centerY  = H / 2f;

        if (keySprite != null)
        {
            var iconGo = new GameObject("NfIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(t, false);
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0f, 0f);
            irt.pivot     = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(iconX, centerY);
            irt.sizeDelta        = new Vector2(iconSize, iconSize);
            var ic = iconGo.GetComponent<Image>();
            ic.sprite         = keySprite;
            ic.preserveAspect = true;
            ic.color          = Color.white;
        }
        else
        {
            // Placeholder สี่เหลี่ยมทอง
            var phGo = new GameObject("NfIconPh", typeof(RectTransform), typeof(Image));
            phGo.transform.SetParent(t, false);
            var prt = phGo.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0f, 0f);
            prt.pivot     = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(iconX, centerY);
            prt.sizeDelta        = new Vector2(iconSize, iconSize);
            phGo.GetComponent<Image>().color = ColAccent;
        }

        // ── ข้อความ "Add Key" ────────────────────────
        float textX = iconX + iconSize / 2f + 10f;
        float textW = W - textX - b - 8f;

        // บรรทัด 1: "Add Key"
        var titleGo = new GameObject("NfTitle", typeof(RectTransform));
        titleGo.transform.SetParent(t, false);
        var trt = titleGo.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0f, 0f);
        trt.pivot     = new Vector2(0f, 0.5f);
        trt.anchoredPosition = new Vector2(textX, centerY + 6f);
        trt.sizeDelta        = new Vector2(textW, 26f);
        var ttmp = titleGo.AddComponent<TextMeshProUGUI>();
        ttmp.text               = "Add Key";
        ttmp.fontSize           = 20f;
        ttmp.fontStyle          = FontStyles.Bold;
        ttmp.alignment          = TextAlignmentOptions.Left;
        ttmp.color              = ColGreen;
        ttmp.enableWordWrapping = false;

        // บรรทัด 2: "obtained from treasure box"
        var subGo = new GameObject("NfSub", typeof(RectTransform));
        subGo.transform.SetParent(t, false);
        var srt = subGo.GetComponent<RectTransform>();
        srt.anchorMin = srt.anchorMax = new Vector2(0f, 0f);
        srt.pivot     = new Vector2(0f, 0.5f);
        srt.anchoredPosition = new Vector2(textX, centerY - 12f);
        srt.sizeDelta        = new Vector2(textW, 18f);
        var stmp = subGo.AddComponent<TextMeshProUGUI>();
        stmp.text               = "obtained from treasure box";
        stmp.fontSize           = 11f;
        stmp.fontStyle          = FontStyles.Normal;
        stmp.alignment          = TextAlignmentOptions.Left;
        stmp.color              = ColTextSub;
        stmp.enableWordWrapping = false;

        root.SetActive(false);
        notify = root;
    }

    // ═══════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════

    bool IsNear(PlayerController p)
        => p != null &&
           Vector3.Distance(p.transform.position, transform.position) <= showRadius;

    // ── Badge (World-space) ──────────────────────────
    void Badge(Transform parent, string label,
               float cx, float cy, float w, float h)
    {
        const float bi = 1.2f;
        Img(parent, "BBdr_" + label, cx, cy, w,           h,           ColKeyBdr);
        Img(parent, "BBg_"  + label, cx, cy, w - bi * 2f, h - bi * 2f, ColKeyBg);
        TMP(parent, "BTxt_" + label, label,
            cx, cy, w - bi * 2f, h, h * 0.52f, ColTextKey);
    }

    void Img(Transform parent, string goName,
             float cx, float cy, float w, float h, Color color)
    {
        var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(cx, cy);
        rt.sizeDelta        = new Vector2(w, h);
        go.GetComponent<Image>().color = color;
    }

    void TMP(Transform parent, string goName, string text,
             float cx, float cy, float w, float h,
             float fontSize, Color color)
    {
        var go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
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

    // ── Screen-space (Notification) ──────────────────
    GameObject MakeImg(Transform parent, string goName,
                       Vector2 pos, Vector2 size, Color color,
                       Vector2? pivot = null)
    {
        var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = pivot ?? new Vector2(0.5f, 0.5f);
        rt.pivot            = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        go.GetComponent<Image>().color = color;
        return go;
    }
}
