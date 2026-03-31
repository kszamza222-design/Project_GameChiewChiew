using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// KeyInventory — HUD กุญแจ + ป้าย 3D หน้าประตูพร้อมวงกลม progress ฝั่งขวา
///
/// Setup:
///   1. ผูก player1, player2, targetCanvas, keySprite
///   2. ผูก doorTransform + ใส่ nextSceneName
///   3. ผูก doorPromptAnchor (ไม่บังคับ) + ปรับ boardRotation Y
/// </summary>
public class KeyInventory : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Players ─────────────────────────")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("── Canvas ───────────────────────────")]
    public Canvas targetCanvas;

    [Header("── Sprites ──────────────────────────")]
    public Sprite keySprite;

    [Header("── Door / Scene Transition ──────────")]
    public string    nextSceneName = "Map2";
    public Transform doorTransform;
    public float     doorRadius    = 3f;

    [Header("── Door Prompt Board (3D) ───────────")]
    public Transform doorPromptAnchor;
    public float     heightAbove   = 2.2f;
    public Vector3   boardRotation = new Vector3(0f, 180f, 0f);

    [Header("── Timing ───────────────────────────")]
    [Tooltip("วินาทีที่ต้องค้างปุ่ม")]
    public float holdDuration = 1.2f;

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
    static readonly Color ColGold     = new Color(1.00f, 0.80f, 0.20f, 1.00f);
    static readonly Color ColGray     = new Color(0.40f, 0.40f, 0.44f, 1.00f);
    static readonly Color ColRingBg   = new Color(0.12f, 0.12f, 0.18f, 1.00f);
    static readonly Color ColRingFill = new Color(0.94f, 0.75f, 0.15f, 1.00f);
    static readonly Color ColRingDone = new Color(0.25f, 0.95f, 0.45f, 1.00f);

    // ═══════════════════════════════════════════════════
    //  State
    // ═══════════════════════════════════════════════════

    int _keysP1 = 0;
    int _keysP2 = 0;

    GameObject      _hudP1, _hudP2;
    TextMeshProUGUI _countP1, _countP2;
    Image           _keyIconP1, _keyIconP2;

    GameObject _doorBoard;
    Image      _ringFill;

    bool  _sceneLoading = false;
    float _holdTimer    = 0f;
    bool  _fired        = false;

    // ═══════════════════════════════════════════════════
    //  Awake
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        BuildKeyHUD(out _hudP1, out _countP1, out _keyIconP1, isLeftSide: true);
        BuildKeyHUD(out _hudP2, out _countP2, out _keyIconP2, isLeftSide: false);
        BuildDoorBoard();
        RefreshHUD();
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        if (_sceneLoading) return;
        UpdateDoorBoard();
        HandleHold();
    }

    // ═══════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════

    public void AddKey(bool isP1)
    {
        if (isP1) _keysP1 = Mathf.Min(_keysP1 + 1, 99);
        else      _keysP2 = Mathf.Min(_keysP2 + 1, 99);
        RefreshHUD();
    }

    public int GetKeys(bool isP1) => isP1 ? _keysP1 : _keysP2;

    // ═══════════════════════════════════════════════════
    //  RefreshHUD
    // ═══════════════════════════════════════════════════

    void RefreshHUD()
    {
        UpdateHUD(_hudP1, _countP1, _keyIconP1, _keysP1);
        UpdateHUD(_hudP2, _countP2, _keyIconP2, _keysP2);
    }

    void UpdateHUD(GameObject hud, TextMeshProUGUI count, Image icon, int keys)
    {
        if (hud == null) return;
        hud.SetActive(keys > 0);
        if (count != null) count.text = "x " + keys;
        if (icon  != null) icon.color = keys > 0 ? Color.white : ColGray;
    }

    // ═══════════════════════════════════════════════════
    //  UpdateDoorBoard
    // ═══════════════════════════════════════════════════

    void UpdateDoorBoard()
    {
        if (_doorBoard == null || doorTransform == null) return;

        bool p1NearKey = _keysP1 > 0 && player1 != null &&
                         Vector3.Distance(player1.transform.position, doorTransform.position) <= doorRadius;
        bool p2NearKey = _keysP2 > 0 && player2 != null &&
                         Vector3.Distance(player2.transform.position, doorTransform.position) <= doorRadius;

        bool show = p1NearKey || p2NearKey;
        _doorBoard.SetActive(show);
        if (show) _doorBoard.transform.rotation = Quaternion.Euler(boardRotation);
    }

    // ═══════════════════════════════════════════════════
    //  HandleHold
    // ═══════════════════════════════════════════════════

    void HandleHold()
    {
        if (doorTransform == null) return;

        bool p1Near    = _keysP1 > 0 && player1 != null &&
                         Vector3.Distance(player1.transform.position, doorTransform.position) <= doorRadius;
        bool p2Near    = _keysP2 > 0 && player2 != null &&
                         Vector3.Distance(player2.transform.position, doorTransform.position) <= doorRadius;

        bool p1Holding = p1Near && Input.GetKey(KeyCode.E);
        bool p2Holding = p2Near && Input.GetKey(KeyCode.Keypad7);
        bool anyHold   = p1Holding || p2Holding;

        if (anyHold && !_fired)
        {
            _holdTimer += Time.deltaTime / holdDuration;
            _holdTimer  = Mathf.Clamp01(_holdTimer);
            SetRing(_holdTimer);

            if (_holdTimer >= 1f)
            {
                _fired = true;
                GoToNextScene();
            }
        }
        else if (!anyHold)
        {
            if (_holdTimer > 0f)
            {
                _holdTimer -= Time.deltaTime / holdDuration * 2.5f;
                _holdTimer  = Mathf.Max(0f, _holdTimer);
                SetRing(_holdTimer);
            }
            _fired = false;
        }
    }

    void SetRing(float t)
    {
        if (_ringFill == null) return;
        _ringFill.fillAmount = t;
        _ringFill.color      = Color.Lerp(ColRingFill, ColRingDone, t);
    }

    void GoToNextScene()
    {
        if (_sceneLoading) return;
        _sceneLoading = true;
        SceneManager.LoadScene(nextSceneName);
    }

    // ═══════════════════════════════════════════════════
    //  BuildDoorBoard
    // ═══════════════════════════════════════════════════

    void BuildDoorBoard()
    {
        if (doorTransform == null) return;

        Vector3 worldPos = doorPromptAnchor != null
            ? doorPromptAnchor.position
            : doorTransform.position + Vector3.up * heightAbove;

        _doorBoard = new GameObject("_DoorPromptBoard");
        _doorBoard.transform.position   = worldPos;
        _doorBoard.transform.rotation   = Quaternion.Euler(boardRotation);
        _doorBoard.transform.localScale = Vector3.one * 0.01f;

        var canvas        = _doorBoard.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        const float W = 320f, H = 72f;
        var rootRT       = _doorBoard.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(W, H);

        Img(_doorBoard.transform, "Border", 0, 0, W, H, ColBorder);

        const float bpx = 1.5f;
        Img(_doorBoard.transform, "BG", 0, 0, W - bpx * 2f, H - bpx * 2f, ColBg);

        const float acW = 5.5f, acH = H - bpx * 2f;
        float acX = -(W / 2f - bpx - acW / 2f);
        Img(_doorBoard.transform, "Accent", acX, 0, acW, acH, ColAccent);

        float cStartX = acX + acW / 2f + 8f;

        const float eW = 30f;
        float eX = cStartX + eW / 2f;
        Badge(_doorBoard.transform, "E", eX, 13f, eW, 22f);

        const float orW = 18f;
        float orX = eX + eW / 2f + 4f + orW / 2f;
        TMP(_doorBoard.transform, "Or", "or", orX, 13f, orW, 22f, 8.5f, ColOr);

        const float nW = 58f;
        float nX = orX + orW / 2f + 4f + nW / 2f;
        Badge(_doorBoard.transform, "Numpad7", nX, 13f, nW, 22f);

        const float ringAreaW = 58f;
        float subW = W - bpx * 2f - acW - 10f - ringAreaW - 4f;
        float subX = cStartX + subW / 2f;
        TMP(_doorBoard.transform, "Sub", "Hold E or Numpad7 to enter",
            subX, -12f, subW, 16f, 7f, ColTextSub);

        // ── Ring ──────────────────────────────────────
        const float ringOuter = 48f;
        const float ringThick =  7f;
        const float ringInner = ringOuter - ringThick * 2f;
        float ringCX = W / 2f - bpx - ringAreaW / 2f - 2f;

        var bgRingGO = new GameObject("RingBg", typeof(RectTransform), typeof(Image));
        bgRingGO.transform.SetParent(_doorBoard.transform, false);
        var bgRingRT = bgRingGO.GetComponent<RectTransform>();
        bgRingRT.anchorMin = bgRingRT.anchorMax = bgRingRT.pivot = new Vector2(0.5f, 0.5f);
        bgRingRT.sizeDelta        = new Vector2(ringOuter, ringOuter);
        bgRingRT.anchoredPosition = new Vector2(ringCX, 0f);
        bgRingGO.GetComponent<Image>().color = ColRingBg;

        var fillGO = new GameObject("RingFill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(_doorBoard.transform, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = fillRT.anchorMax = fillRT.pivot = new Vector2(0.5f, 0.5f);
        fillRT.sizeDelta        = new Vector2(ringOuter, ringOuter);
        fillRT.anchoredPosition = new Vector2(ringCX, 0f);
        _ringFill               = fillGO.GetComponent<Image>();
        _ringFill.color         = ColRingFill;
        _ringFill.type          = Image.Type.Filled;
        _ringFill.fillMethod    = Image.FillMethod.Radial360;
        _ringFill.fillOrigin    = (int)Image.Origin360.Top;
        _ringFill.fillClockwise = true;
        _ringFill.fillAmount    = 0f;

        var innerGO = new GameObject("RingInner", typeof(RectTransform), typeof(Image));
        innerGO.transform.SetParent(_doorBoard.transform, false);
        var innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = innerRT.anchorMax = innerRT.pivot = new Vector2(0.5f, 0.5f);
        innerRT.sizeDelta        = new Vector2(ringInner, ringInner);
        innerRT.anchoredPosition = new Vector2(ringCX, 0f);
        innerGO.GetComponent<Image>().color = ColBg;

        var lblGO = new GameObject("RingLabel", typeof(RectTransform));
        lblGO.transform.SetParent(_doorBoard.transform, false);
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

        _doorBoard.SetActive(false);
    }

    // ═══════════════════════════════════════════════════
    //  BuildKeyHUD
    // ═══════════════════════════════════════════════════

    void BuildKeyHUD(out GameObject hud, out TextMeshProUGUI countTMP,
                     out Image keyIcon, bool isLeftSide)
    {
        hud = null; countTMP = null; keyIcon = null;
        if (targetCanvas == null) return;

        const float W = 120f, H = 52f, PAD = 18f;

        var root = new GameObject("KeyHUD_" + (isLeftSide ? "P1" : "P2"),
                                  typeof(RectTransform));
        root.transform.SetParent(targetCanvas.transform, false);
        var rootRT = root.GetComponent<RectTransform>();

        float anchorX           = isLeftSide ? 0.5f : 1.0f;
        rootRT.anchorMin        = new Vector2(anchorX, 1f);
        rootRT.anchorMax        = new Vector2(anchorX, 1f);
        rootRT.pivot            = new Vector2(1f, 1f);
        rootRT.sizeDelta        = new Vector2(W, H);
        rootRT.anchoredPosition = new Vector2(-PAD, -PAD - 40f);

        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(root.transform, false);
        var bRT = border.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = Vector2.zero; bRT.offsetMax = Vector2.zero;
        border.GetComponent<Image>().color = ColBorder;

        const float bpx = 1.5f;
        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(bpx, bpx); bgRT.offsetMax = new Vector2(-bpx, -bpx);
        bg.GetComponent<Image>().color = ColBg;

        const float iconSize = 32f, iconX = 10f;
        var iconGO = new GameObject("KeyIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(root.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin        = new Vector2(0f, 0.5f);
        iconRT.anchorMax        = new Vector2(0f, 0.5f);
        iconRT.pivot            = new Vector2(0f, 0.5f);
        iconRT.sizeDelta        = new Vector2(iconSize, iconSize);
        iconRT.anchoredPosition = new Vector2(iconX, 0f);
        keyIcon = iconGO.GetComponent<Image>();
        if (keySprite != null) keyIcon.sprite = keySprite;
        keyIcon.color = ColGray;

        float textX = iconX + iconSize + 4f;
        float textW = W - textX - bpx - 4f;
        var textGO = new GameObject("Count", typeof(RectTransform));
        textGO.transform.SetParent(root.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin        = new Vector2(0f, 0.5f);
        textRT.anchorMax        = new Vector2(0f, 0.5f);
        textRT.pivot            = new Vector2(0f, 0.5f);
        textRT.sizeDelta        = new Vector2(textW, H - bpx * 2f);
        textRT.anchoredPosition = new Vector2(textX, 0f);
        countTMP           = textGO.AddComponent<TextMeshProUGUI>();
        countTMP.text      = "x 0";
        countTMP.fontSize  = 22f;
        countTMP.color     = ColGold;
        countTMP.alignment = TextAlignmentOptions.MidlineLeft;

        hud = root;
        hud.SetActive(false);
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
