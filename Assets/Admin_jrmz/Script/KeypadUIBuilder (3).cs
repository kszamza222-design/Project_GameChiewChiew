using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// KeypadUIBuilder — UI กรอกรหัสผ่านกลางจอของ Player ที่กด
///
/// Split-Screen:
///   Player1 (ซ้าย) กด E        → UI ขึ้นกลางจอซ้าย
///   Player2 (ขวา)  กด Numpad7  → UI ขึ้นกลางจอขวา
///
/// แก้ไข:
///   • ปุ่ม ESC เปลี่ยนเป็น TextMeshProUGUI ล้วน (ไม่มีกล่องพื้นหลัง)
///   • เมื่อเปิดประตูสำเร็จแล้ว (_doorUnlocked = true) Keypad จะไม่ทำงานอีกเลย
/// </summary>
public class KeypadUIBuilder : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── Canvas ───────────────────────────")]
    public Canvas targetCanvas;

    [Header("── Players & Cameras ───────────────")]
    public PlayerController player1;
    public PlayerController player2;
    [Tooltip("Camera ของ Player1 (ฝั่งซ้าย)")]
    public Camera cameraP1;
    [Tooltip("Camera ของ Player2 (ฝั่งขวา)")]
    public Camera cameraP2;

    [Header("── Settings ────────────────────────")]
    public string correctCode   = "1234";
    public float  triggerRadius = 3f;

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    RectTransform   _panelRT;
    GameObject      _panel;
    TextMeshProUGUI _display;
    TextMeshProUGUI _statusText;

    string           _input      = "";
    bool             _isOpen     = false;
    PlayerController _nearPlayer = null;

    /// <summary>
    /// ประตูถูกปลดล็อคแล้ว — true = Keypad หยุดทำงานถาวร
    /// </summary>
    bool _doorUnlocked = false;

    SlidingDoor _door;

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        _door = FindObjectOfType<SlidingDoor>();
        if (_door == null)
            Debug.LogError("[Keypad] ไม่พบ SlidingDoor ในฉาก!");

        BuildUI();
    }

    // ═══════════════════════════════════════════════════
    //  Build UI
    // ═══════════════════════════════════════════════════

    void BuildUI()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[Keypad] ยังไม่ได้ผูก Target Canvas!");
            return;
        }

        Transform root = targetCanvas.transform;

        // Panel
        _panel   = MakeImage(root, "KeypadPanel",
                             Vector2.zero, new Vector2(340, 580),
                             new Color(0.08f, 0.08f, 0.12f, 0.97f));
        _panelRT = _panel.GetComponent<RectTransform>();
        _panel.SetActive(false);

        Transform p = _panel.transform;

        // หัวข้อ
        MakeText(p, "Title", "[ KEYPAD ]",
                 new Vector2(0, 245), new Vector2(300, 50), 28,
                 new Color(0.4f, 0.8f, 1f));

        // Display รหัส
        var dispGo = MakeImage(p, "DisplayBG",
                               new Vector2(0, 180), new Vector2(280, 60),
                               new Color(0.02f, 0.02f, 0.05f, 1f));
        _display = MakeText(dispGo.transform, "DisplayText", "_ _ _ _",
                            Vector2.zero, new Vector2(280, 60), 38, Color.white);

        // Status
        _statusText = MakeText(p, "Status", "",
                               new Vector2(0, 125), new Vector2(280, 36), 20,
                               new Color(1f, 0.4f, 0.4f));

        // ── ปุ่มตัวเลข ──
        MakeNumBtn(p, 1, new Vector2(-100,  65));
        MakeNumBtn(p, 2, new Vector2(   0,  65));
        MakeNumBtn(p, 3, new Vector2( 100,  65));
        MakeNumBtn(p, 4, new Vector2(-100, -25));
        MakeNumBtn(p, 5, new Vector2(   0, -25));
        MakeNumBtn(p, 6, new Vector2( 100, -25));
        MakeNumBtn(p, 7, new Vector2(-100,-115));
        MakeNumBtn(p, 8, new Vector2(   0,-115));
        MakeNumBtn(p, 9, new Vector2( 100,-115));

        // DEL  0  OK
        MakeActionBtn(p, "DEL", new Vector2(-100,-205),
                      new Color(0.6f,0.2f,0.2f), PressDelete);
        MakeNumBtn(p, 0, new Vector2(0,-205));
        MakeActionBtn(p, "OK",  new Vector2( 100,-205),
                      new Color(0.2f,0.55f,0.2f), PressOK);

        // ── ESC — Text ล้วน ไม่มีกล่องพื้นหลัง ──────────────
        MakeEscText(p, new Vector2(0, -275));
    }

    // ═══════════════════════════════════════════════════
    //  UI Helpers
    // ═══════════════════════════════════════════════════

    GameObject MakeImage(Transform parent, string name,
                         Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        go.GetComponent<Image>().color = color;
        return go;
    }

    TextMeshProUGUI MakeText(Transform parent, string name, string text,
                             Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
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
        return tmp;
    }

    // ── ESC Text ล้วน — ไม่มี Image พื้นหลัง ────────────────
    void MakeEscText(Transform parent, Vector2 pos)
    {
        // GameObject มีแค่ RectTransform + TextMeshProUGUI + Button
        // ไม่มี Image component → ไม่มีกล่องพื้นหลังสีใดๆ
        var go = new GameObject("Btn_ESC", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(280, 44);

        // Text
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "ESC  —  Close";          // ภาษาอังกฤษ (TMP ไม่ต้องการ Font เพิ่ม)
        tmp.fontSize  = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(0.65f, 0.65f, 0.75f, 1f);   // เทาอมม่วงอ่อน

        // Button — ไม่มี targetGraphic → ใช้ null-safe mode
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = null;   // ไม่มี Graphic → ไม่แสดง highlight กล่อง
        btn.onClick.AddListener(Close);
    }

    void MakeNumBtn(Transform parent, int num, Vector2 pos)
    {
        int n = num;
        MakeActionBtn(parent, num.ToString(), pos,
                      new Color(0.22f, 0.22f, 0.32f), () => PressNumber(n));
    }

    void MakeActionBtn(Transform parent, string label, Vector2 pos,
                       Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var sz = new Vector2(82, 72);
        float fz = label.Length > 2 ? 18f : 28f;

        var go = new GameObject("Btn_" + label,
                                typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = sz;

        var img = go.GetComponent<Image>();
        img.color = bgColor;

        var btn = go.GetComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = bgColor * 1.4f;
        cb.pressedColor     = bgColor * 0.6f;
        cb.fadeDuration     = 0.05f;
        btn.colors          = cb;
        btn.targetGraphic   = img;
        btn.onClick.AddListener(onClick);

        MakeText(go.transform, "Label", label,
                 Vector2.zero, sz, fz, Color.white);
    }

    // ═══════════════════════════════════════════════════
    //  Update — ตรวจ Player เข้าใกล้ + กดเปิด UI
    // ═══════════════════════════════════════════════════

    void Update()
    {
        // ── ประตูเปิดแล้ว = หยุดทำงานทั้งหมด ──────────────
        if (_doorUnlocked) return;

        CheckNearby();
        HandleOpenKey();
    }

    // ── หา Player ที่อยู่ใกล้ Keypad ────────────────────
    void CheckNearby()
    {
        _nearPlayer = null;

        if (player1 != null &&
            Vector3.Distance(transform.position, player1.transform.position) <= triggerRadius)
            _nearPlayer = player1;

        else if (player2 != null &&
                 Vector3.Distance(transform.position, player2.transform.position) <= triggerRadius)
            _nearPlayer = player2;
    }

    // ── ตรวจปุ่มเปิด UI ──────────────────────────────
    void HandleOpenKey()
    {
        if (_nearPlayer == null || _isOpen) return;

        bool openPressed = false;

        if      (_nearPlayer == player1 && Input.GetKeyDown(KeyCode.E))
            openPressed = true;
        else if (_nearPlayer == player2 && Input.GetKeyDown(KeyCode.Keypad7))
            openPressed = true;

        if (openPressed) Open(_nearPlayer);
    }

    // ═══════════════════════════════════════════════════
    //  Open / Close
    // ═══════════════════════════════════════════════════

    void Open(PlayerController player)
    {
        // ป้องกันเปิดซ้ำหลังปลดล็อค
        if (_doorUnlocked) return;

        _isOpen = true;
        _input  = "";
        UpdateDisplay();
        SetStatus("", Color.white);

        // จัดตำแหน่ง Panel ให้อยู่กลางจอของ Player นั้น
        PositionPanel(player);

        _panel.SetActive(true);
    }

    void Close()
    {
        _isOpen = false;
        _panel.SetActive(false);
        _input = "";
    }

    // ── คำนวณ anchoredPosition ให้กลางจอฝั่งนั้น ─────
    void PositionPanel(PlayerController player)
    {
        // Canvas ต้องเป็น Screen Space - Overlay
        // จอซ้าย = x ที่ 25% ของ Screen, จอขวา = x ที่ 75%
        bool isP1 = (player == player1);

        float screenX = isP1 ? Screen.width * 0.25f : Screen.width * 0.75f;
        float screenY = Screen.height * 0.5f;

        // แปลง Screen → Canvas local position
        RectTransform canvasRT = targetCanvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            new Vector2(screenX, screenY),
            targetCanvas.worldCamera,
            out Vector2 localPos);

        _panelRT.anchorMin = _panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRT.anchoredPosition = localPos;
    }

    // ═══════════════════════════════════════════════════
    //  Button Actions
    // ═══════════════════════════════════════════════════

    void PressNumber(int n)
    {
        if (_input.Length >= correctCode.Length) return;
        _input += n.ToString();
        UpdateDisplay();
        SetStatus("", Color.white);
    }

    void PressDelete()
    {
        if (_input.Length == 0) return;
        _input = _input.Substring(0, _input.Length - 1);
        UpdateDisplay();
        SetStatus("", Color.white);
    }

    void PressOK()
    {
        if (_input == correctCode)
        {
            SetStatus("CORRECT  —  UNLOCKED", new Color(0.3f, 1f, 0.4f));

            // เปิดประตู
            if (_door != null) _door.ForceOpen();

            // ── ล็อค Keypad ถาวร ──────────────────────────
            _doorUnlocked = true;

            // ปิด UI หลัง 1.5 วินาที
            Invoke(nameof(Close), 1.5f);
        }
        else
        {
            SetStatus("WRONG CODE", new Color(1f, 0.3f, 0.3f));
            if (_door != null) _door.PlayWrong();
            _input = "";
            Invoke(nameof(ClearStatusAndDisplay), 1.2f);
        }
    }

    void ClearStatusAndDisplay()
    {
        _input = "";
        UpdateDisplay();
        SetStatus("", Color.white);
    }

    // ═══════════════════════════════════════════════════
    //  Display Helpers
    // ═══════════════════════════════════════════════════

    void UpdateDisplay()
    {
        if (_display == null) return;

        // แสดงตัวเลขที่กดแล้ว + ขีดสำหรับช่องที่ยังว่าง
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < correctCode.Length; i++)
        {
            if (i < _input.Length)
                sb.Append(_input[i]);
            else
                sb.Append('_');

            if (i < correctCode.Length - 1) sb.Append(' ');
        }
        _display.text = sb.ToString();
    }

    void SetStatus(string msg, Color col)
    {
        if (_statusText == null) return;
        _statusText.text  = msg;
        _statusText.color = col;
    }

    // ═══════════════════════════════════════════════════
    //  Gizmo
    // ═══════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        Gizmos.color = _doorUnlocked ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
