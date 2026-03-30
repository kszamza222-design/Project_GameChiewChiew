using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ImageViewerPromptUI — ป้ายแจ้งเตือนลอยในโลก 3D + แสดงรูปภาพกลางจอของผู้เล่นแต่ละคน
///
/// Split-Screen:
///   Player1 (จอซ้าย)  กด E       → รูปขึ้นกลางจอซ้าย
///   Player2 (จอขวา)   กด Numpad7 → รูปขึ้นกลางจอขวา
///   กด ESC หรือปุ่ม ESC → ปิดรูป
///
/// วิธีใช้:
///   1. ติด Script นี้กับ GameObject ใดก็ได้
///   2. ผูก player1, player2
///   3. ผูก targetCanvas (Screen Space – Overlay)
///   4. ใส่ viewImage (Sprite)
///   5. ผูก promptAnchor (ไม่บังคับ)
///   6. ปรับ boardRotation Y ให้ป้ายหันถูกทิศ
/// </summary>
public class ImageViewerPromptUI : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Players ─────────────────────────")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("── Canvas (Screen Space Overlay) ───")]
    [Tooltip("Canvas แบบ Screen Space – Overlay")]
    public Canvas targetCanvas;

    [Header("── รูปภาพที่จะแสดง ─────────────────")]
    [Tooltip("ลาก Sprite (1200x1200) มาใส่ตรงนี้")]
    public Sprite viewImage;

    [Header("── Prompt Position ─────────────────")]
    public Transform promptAnchor;
    public float heightAbove = 2.2f;

    [Header("── Show Radius ──────────────────────")]
    public float showRadius = 3f;

    [Header("── Board Rotation ─────────────────")]
    [Tooltip("ปรับ Y เพื่อหมุนป้ายให้หันถูกทิศ")]
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

    GameObject _board;
    GameObject _viewerP1;
    GameObject _viewerP2;
    bool       _viewerP1Open = false;
    bool       _viewerP2Open = false;

    // ═══════════════════════════════════════════════════
    //  Awake
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        BuildBoard();
        BuildViewer(out _viewerP1, isLeftSide: true);
        BuildViewer(out _viewerP2, isLeftSide: false);
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        HandleInput();
        UpdateBoard();
    }

    // ═══════════════════════════════════════════════════
    //  HandleInput
    // ═══════════════════════════════════════════════════

    void HandleInput()
    {
        bool p1Near = player1 != null &&
                      Vector3.Distance(player1.transform.position, transform.position) <= showRadius;
        bool p2Near = player2 != null &&
                      Vector3.Distance(player2.transform.position, transform.position) <= showRadius;

        // ESC ปิดทั้งคู่
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_viewerP1Open) { _viewerP1Open = false; _viewerP1.SetActive(false); }
            if (_viewerP2Open) { _viewerP2Open = false; _viewerP2.SetActive(false); }
            return;
        }

        // P1 กด E → เปิดจอซ้าย
        if (p1Near && !_viewerP1Open && Input.GetKeyDown(KeyCode.E))
        {
            _viewerP1Open = true;
            _viewerP1.SetActive(true);
        }

        // P2 กด Numpad7 → เปิดจอขวา
        if (p2Near && !_viewerP2Open && Input.GetKeyDown(KeyCode.Keypad7))
        {
            _viewerP2Open = true;
            _viewerP2.SetActive(true);
        }
    }

    // ═══════════════════════════════════════════════════
    //  UpdateBoard
    // ═══════════════════════════════════════════════════

    void UpdateBoard()
    {
        if (_board == null) return;

        bool p1Near = player1 != null &&
                      Vector3.Distance(player1.transform.position, transform.position) <= showRadius;
        bool p2Near = player2 != null &&
                      Vector3.Distance(player2.transform.position, transform.position) <= showRadius;

        bool shouldShow = (p1Near || p2Near) && !_viewerP1Open && !_viewerP2Open;
        _board.SetActive(shouldShow);
        if (shouldShow)
            _board.transform.rotation = Quaternion.Euler(boardRotation);
    }

    // ═══════════════════════════════════════════════════
    //  BuildBoard — ป้าย 3D
    // ═══════════════════════════════════════════════════

    void BuildBoard()
    {
        Vector3 worldPos = promptAnchor != null
            ? promptAnchor.position
            : transform.position + Vector3.up * heightAbove;

        _board = new GameObject("_ImagePromptBoard");
        _board.transform.position   = worldPos;
        _board.transform.rotation   = Quaternion.Euler(boardRotation);
        _board.transform.localScale = Vector3.one * 0.01f;

        var canvas        = _board.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        const float W = 260f, H = 68f;
        var rootRT = _board.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(W, H);

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
            "Press E or Numpad7 to view image",
            cStartX + subW / 2f, -12f, subW, 16f, 7f, ColTextSub);

        _board.SetActive(false);
    }

    // ═══════════════════════════════════════════════════
    //  BuildViewer — Image Viewer กลางจอของ Player นั้น
    //
    //  isLeftSide = true  → P1 จอซ้าย  anchor 0.0~0.5
    //  isLeftSide = false → P2 จอขวา   anchor 0.5~1.0
    // ═══════════════════════════════════════════════════

    void BuildViewer(out GameObject viewer, bool isLeftSide)
    {
        viewer = null;
        if (targetCanvas == null)
        {
            Debug.LogError("[ImageViewerPromptUI] ยังไม่ได้ผูก Target Canvas!");
            return;
        }

        string suffix = isLeftSide ? "P1" : "P2";

        // ── Overlay มืดครึ่งจอ ───────────────────────
        var overlayGo = new GameObject("_Viewer_" + suffix,
                                       typeof(RectTransform), typeof(Image));
        overlayGo.transform.SetParent(targetCanvas.transform, false);

        var overlayRT = overlayGo.GetComponent<RectTransform>();
        overlayRT.anchorMin = isLeftSide ? new Vector2(0.0f, 0f) : new Vector2(0.5f, 0f);
        overlayRT.anchorMax = isLeftSide ? new Vector2(0.5f, 1f) : new Vector2(1.0f, 1f);
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;
        overlayGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        Transform v = overlayGo.transform;

        // ── กรอบ + รูป ────────────────────────────────
        float size = Mathf.Min(Screen.height * 0.80f, 800f);

        var frameGo = MakeImg(v, "Frame_" + suffix,
                              Vector2.zero, new Vector2(size + 12f, size + 12f), ColBorder);

        var imgGo = MakeImg(frameGo.transform, "Photo_" + suffix,
                            Vector2.zero, new Vector2(size, size), Color.white);

        if (viewImage != null)
        {
            var imgComp = imgGo.GetComponent<Image>();
            imgComp.sprite         = viewImage;
            imgComp.type           = Image.Type.Simple;
            imgComp.preserveAspect = true;
        }
        else
        {
            imgGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
            MakeTMP(imgGo.transform, "NoImg_" + suffix, "[ No Image ]",
                    Vector2.zero, new Vector2(size, 60f), 32f, new Color(0.6f, 0.6f, 0.6f));
        }

        // ── ปุ่ม ESC ─────────────────────────────────
        var btnGo = MakeImg(v, "EscBtn_" + suffix,
                            new Vector2(0f, -(size / 2f + 44f)),
                            new Vector2(220f, 48f),
                            new Color(0.18f, 0.18f, 0.25f, 1f));

        MakeImg(btnGo.transform, "EscBdr", Vector2.zero, new Vector2(220f, 48f), ColBorder);
        MakeImg(btnGo.transform, "EscBg",  Vector2.zero, new Vector2(216f, 44f),
                new Color(0.18f, 0.18f, 0.25f, 1f));
        MakeTMP(btnGo.transform, "EscTxt", "[ ESC ]  Close",
                Vector2.zero, new Vector2(210f, 44f), 20f,
                new Color(0.95f, 0.70f, 0.20f));

        var btn = btnGo.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = Color.white;
        bc.highlightedColor = new Color(1f, 0.85f, 0.4f);
        bc.pressedColor     = new Color(0.7f, 0.5f, 0.1f);
        btn.colors = bc;

        bool leftCapture = isLeftSide; // capture ก่อนเข้า lambda
        btn.onClick.AddListener(() =>
        {
            if (leftCapture) { _viewerP1Open = false; _viewerP1.SetActive(false); }
            else             { _viewerP2Open = false; _viewerP2.SetActive(false); }
        });

        overlayGo.SetActive(false);
        viewer = overlayGo;
    }

    // ═══════════════════════════════════════════════════
    //  Badge helper
    // ═══════════════════════════════════════════════════

    void Badge(Transform parent, string label,
               float cx, float cy, float w, float h)
    {
        const float bi = 1.2f;
        Img(parent, "BBdr_" + label, cx, cy, w,           h,           ColKeyBdr);
        Img(parent, "BBg_"  + label, cx, cy, w - bi * 2f, h - bi * 2f, ColKeyBg);
        TMP(parent, "BTxt_" + label, label,
            cx, cy, w - bi * 2f, h, h * 0.52f, ColTextKey);
    }

    // ═══════════════════════════════════════════════════
    //  World-space helpers
    // ═══════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════
    //  Screen-space helpers
    // ═══════════════════════════════════════════════════

    GameObject MakeImg(Transform parent, string goName,
                       Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        go.GetComponent<Image>().color = color;
        return go;
    }

    TextMeshProUGUI MakeTMP(Transform parent, string goName, string text,
                            Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
        tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }
}
