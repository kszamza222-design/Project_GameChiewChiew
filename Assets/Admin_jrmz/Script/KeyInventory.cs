using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// KeyInventory — แสดงกุญแจมุมขวาบนของแต่ละฝั่ง + ใช้กุญแจเข้า Scene ถัดไป
///
/// Setup:
///   1. ติด Script นี้กับ GameObject ใดก็ได้ (เช่น "GameManager")
///   2. ผูก player1, player2
///   3. ผูก targetCanvas (Screen Space Overlay)
///   4. ผูก keySprite (รูปกุญแจ)
///   5. ใส่ nextSceneName ชื่อ Scene ถัดไป
///   6. วาง GameObject ที่มี DoorTrigger ใกล้ประตู แล้วผูก doorTransform
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
    [Tooltip("รูปกุญแจ")]
    public Sprite keySprite;

    [Header("── Scene Transition ─────────────────")]
    [Tooltip("ชื่อ Scene ถัดไป (ต้องเพิ่มใน Build Settings)")]
    public string nextSceneName = "Map2";
    [Tooltip("ตำแหน่งประตู/จุดที่ใช้กุญแจ")]
    public Transform doorTransform;
    [Tooltip("รัศมีที่จะใช้กุญแจได้")]
    public float doorRadius = 3f;
    [Tooltip("ข้อความบนประตู เช่น 'Press E to enter'")]
    public string doorPromptText = "Press E to enter next area";

    // ═══════════════════════════════════════════════════
    //  Colors
    // ═══════════════════════════════════════════════════

    static readonly Color ColBg     = new Color(0.06f, 0.06f, 0.09f, 0.92f);
    static readonly Color ColBorder = new Color(0.85f, 0.55f, 0.08f, 1.00f);
    static readonly Color ColGold   = new Color(1.00f, 0.80f, 0.20f, 1.00f);
    static readonly Color ColGreen  = new Color(0.20f, 0.90f, 0.40f, 1.00f);
    static readonly Color ColGray   = new Color(0.40f, 0.40f, 0.44f, 1.00f);

    // ═══════════════════════════════════════════════════
    //  State
    // ═══════════════════════════════════════════════════

    // กุญแจของแต่ละคน (ตอนนี้ max = 1)
    int _keysP1 = 0;
    int _keysP2 = 0;

    // HUD elements
    GameObject      _hudP1;
    GameObject      _hudP2;
    TextMeshProUGUI _countP1;
    TextMeshProUGUI _countP2;
    Image           _keyIconP1;
    Image           _keyIconP2;

    // Door prompt
    GameObject _doorPromptP1;
    GameObject _doorPromptP2;

    bool _sceneLoading = false;

    // ═══════════════════════════════════════════════════
    //  Awake
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        BuildKeyHUD(out _hudP1, out _countP1, out _keyIconP1, isLeftSide: true);
        BuildKeyHUD(out _hudP2, out _countP2, out _keyIconP2, isLeftSide: false);
        BuildDoorPrompt(out _doorPromptP1, isLeftSide: true);
        BuildDoorPrompt(out _doorPromptP2, isLeftSide: false);
        RefreshHUD();
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        if (_sceneLoading) return;
        HandleDoorPrompt();
        HandleDoorInput();
    }

    // ═══════════════════════════════════════════════════
    //  Public — เรียกจาก TreasureBox เมื่อเก็บกุญแจ
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
        if (count != null) count.text = keys.ToString();
        if (icon  != null) icon.color = keys > 0 ? Color.white : ColGray;
    }

    // ═══════════════════════════════════════════════════
    //  HandleDoorPrompt — แสดงป้าย "Press E" ใกล้ประตู
    // ═══════════════════════════════════════════════════

    void HandleDoorPrompt()
    {
        if (doorTransform == null) return;

        bool p1NearDoor = _keysP1 > 0 && player1 != null &&
                          Vector3.Distance(player1.transform.position,
                                           doorTransform.position) <= doorRadius;
        bool p2NearDoor = _keysP2 > 0 && player2 != null &&
                          Vector3.Distance(player2.transform.position,
                                           doorTransform.position) <= doorRadius;

        if (_doorPromptP1 != null) _doorPromptP1.SetActive(p1NearDoor);
        if (_doorPromptP2 != null) _doorPromptP2.SetActive(p2NearDoor);
    }

    // ═══════════════════════════════════════════════════
    //  HandleDoorInput — กด E / Numpad7 ใกล้ประตู
    // ═══════════════════════════════════════════════════

    void HandleDoorInput()
    {
        if (doorTransform == null) return;

        bool p1Near = player1 != null &&
                      Vector3.Distance(player1.transform.position,
                                       doorTransform.position) <= doorRadius;
        bool p2Near = player2 != null &&
                      Vector3.Distance(player2.transform.position,
                                       doorTransform.position) <= doorRadius;

        if (p1Near && _keysP1 > 0 && Input.GetKeyDown(KeyCode.E))
            GoToNextScene();

        if (p2Near && _keysP2 > 0 && Input.GetKeyDown(KeyCode.Keypad7))
            GoToNextScene();
    }

    void GoToNextScene()
    {
        if (_sceneLoading) return;
        _sceneLoading = true;
        Debug.Log("[KeyInventory] Loading scene: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    // ═══════════════════════════════════════════════════
    //  BuildKeyHUD — รูปกุญแจ + ตัวเลข มุมขวาบน
    //
    //  isLeftSide = true  → P1 ครึ่งซ้าย  anchor ขวาบน
    //  isLeftSide = false → P2 ครึ่งขวา   anchor ขวาบน
    //
    //  Layout:
    //  ┌────────────────┐
    //  │  [🔑]  x1      │   ← มุมขวาบนของฝั่งนั้น
    //  └────────────────┘
    // ═══════════════════════════════════════════════════

    void BuildKeyHUD(out GameObject hud, out TextMeshProUGUI countTMP,
                     out Image keyIcon, bool isLeftSide)
    {
        hud      = null;
        countTMP = null;
        keyIcon  = null;

        if (targetCanvas == null) return;

        string suffix = isLeftSide ? "P1" : "P2";
        const float W   = 110f;
        const float H   =  52f;
        const float PAD =  16f;

        // ── Root ────────────────────────────────────
        var root = new GameObject("_KeyHUD_" + suffix, typeof(RectTransform));
        root.transform.SetParent(targetCanvas.transform, false);
        var rt = root.GetComponent<RectTransform>();

        // anchor ขวาบนของครึ่งจอนั้น
        if (isLeftSide)
        {
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(1f,   1f);
            rt.anchoredPosition = new Vector2(-PAD, -PAD);
        }
        else
        {
            rt.anchorMin        = new Vector2(1f,   1f);
            rt.anchorMax        = new Vector2(1f,   1f);
            rt.pivot            = new Vector2(1f,   1f);
            rt.anchoredPosition = new Vector2(-PAD, -PAD);
        }
        rt.sizeDelta = new Vector2(W, H);

        Transform t = root.transform;

        // ── Border ──────────────────────────────────
        MakeImgAnchored(t, "HudBdr_" + suffix,
                        Vector2.zero, new Vector2(W, H), ColBorder,
                        new Vector2(0f, 0f), new Vector2(1f, 1f));

        // ── BG ──────────────────────────────────────
        const float b = 1.5f;
        MakeImgAnchored(t, "HudBg_" + suffix,
                        new Vector2(b, b), new Vector2(-b * 2f, -b * 2f), ColBg,
                        Vector2.zero, Vector2.one);

        // ── Accent bar ซ้าย ──────────────────────────
        MakeImgAnchored(t, "HudAcc_" + suffix,
                        new Vector2(b, b), new Vector2(5f, -(b * 2f)), ColBorder,
                        Vector2.zero, new Vector2(0f, 1f));

        // ── Icon กุญแจ ───────────────────────────────
        const float iconSize = 34f;
        const float iconX    = b + 5f + 8f + iconSize / 2f;
        const float iconY    = H / 2f;

        var iconGo = new GameObject("HudIcon_" + suffix,
                                    typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(t, false);
        var irt = iconGo.GetComponent<RectTransform>();
        irt.anchorMin = irt.anchorMax = new Vector2(0f, 0f);
        irt.pivot     = new Vector2(0.5f, 0.5f);
        irt.anchoredPosition = new Vector2(iconX, iconY);
        irt.sizeDelta        = new Vector2(iconSize, iconSize);
        var ic = iconGo.GetComponent<Image>();
        ic.sprite         = keySprite;
        ic.preserveAspect = true;
        ic.color          = keySprite != null ? Color.white : ColGold;
        keyIcon = ic;

        // ── ตัวเลข ───────────────────────────────────
        float numX = iconX + iconSize / 2f + 6f;
        float numW = W - numX - b - 4f;

        // "x" เล็ก
        var xGo = new GameObject("HudX_" + suffix, typeof(RectTransform));
        xGo.transform.SetParent(t, false);
        var xrt = xGo.GetComponent<RectTransform>();
        xrt.anchorMin = xrt.anchorMax = new Vector2(0f, 0f);
        xrt.pivot     = new Vector2(0f, 0.5f);
        xrt.anchoredPosition = new Vector2(numX, H / 2f + 2f);
        xrt.sizeDelta        = new Vector2(12f, 20f);
        var xtmp = xGo.AddComponent<TextMeshProUGUI>();
        xtmp.text      = "x";
        xtmp.fontSize  = 13f;
        xtmp.color     = ColGold;
        xtmp.alignment = TextAlignmentOptions.Left;

        // ตัวเลขจำนวนกุญแจ
        var numGo = new GameObject("HudCount_" + suffix, typeof(RectTransform));
        numGo.transform.SetParent(t, false);
        var nrt = numGo.GetComponent<RectTransform>();
        nrt.anchorMin = nrt.anchorMax = new Vector2(0f, 0f);
        nrt.pivot     = new Vector2(0f, 0.5f);
        nrt.anchoredPosition = new Vector2(numX + 12f, H / 2f);
        nrt.sizeDelta        = new Vector2(numW, 34f);
        var ntmp = numGo.AddComponent<TextMeshProUGUI>();
        ntmp.text      = "0";
        ntmp.fontSize  = 26f;
        ntmp.fontStyle = FontStyles.Bold;
        ntmp.color     = ColGold;
        ntmp.alignment = TextAlignmentOptions.Left;
        countTMP = ntmp;

        root.SetActive(false);
        hud = root;
    }

    // ═══════════════════════════════════════════════════
    //  BuildDoorPrompt — ป้ายบอกใช้กุญแจ (กลางล่างของฝั่งนั้น)
    // ═══════════════════════════════════════════════════

    void BuildDoorPrompt(out GameObject prompt, bool isLeftSide)
    {
        prompt = null;
        if (targetCanvas == null) return;

        string suffix = isLeftSide ? "P1" : "P2";
        const float W   = 320f;
        const float H   =  48f;
        const float PAD =  40f;

        var root = new GameObject("_DoorPrompt_" + suffix, typeof(RectTransform));
        root.transform.SetParent(targetCanvas.transform, false);
        var rt = root.GetComponent<RectTransform>();

        if (isLeftSide)
        {
            rt.anchorMin        = new Vector2(0.25f, 0f);
            rt.anchorMax        = new Vector2(0.25f, 0f);
        }
        else
        {
            rt.anchorMin        = new Vector2(0.75f, 0f);
            rt.anchorMax        = new Vector2(0.75f, 0f);
        }
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, PAD);
        rt.sizeDelta        = new Vector2(W, H);

        Transform t = root.transform;

        MakeImgAnchored(t, "DpBdr_" + suffix,
                        Vector2.zero, new Vector2(W, H),
                        new Color(0.85f, 0.55f, 0.08f, 1f),
                        new Vector2(0f, 0f), new Vector2(1f, 1f));

        const float b = 1.5f;
        MakeImgAnchored(t, "DpBg_" + suffix,
                        new Vector2(b, b), new Vector2(-b * 2f, -b * 2f),
                        new Color(0.06f, 0.06f, 0.09f, 0.96f),
                        Vector2.zero, Vector2.one);

        var go = new GameObject("DpTxt_" + suffix, typeof(RectTransform));
        go.transform.SetParent(t, false);
        var trt = go.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = doorPromptText;
        tmp.fontSize           = 16f;
        tmp.fontStyle          = FontStyles.Bold;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.color              = new Color(1f, 0.85f, 0.3f);
        tmp.enableWordWrapping = false;

        root.SetActive(false);
        prompt = root;
    }

    // ═══════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════

    void MakeImgAnchored(Transform parent, string goName,
                         Vector2 offsetMin, Vector2 offsetMax,
                         Color color,
                         Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0f, 0f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        go.GetComponent<Image>().color = color;
    }
}
