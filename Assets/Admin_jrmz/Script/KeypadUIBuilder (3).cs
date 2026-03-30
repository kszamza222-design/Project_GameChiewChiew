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
/// Public State (KeypadPromptUI อ่านได้):
///   DoorUnlocked  — ประตูถูกปลดล็อคแล้ว
///   IsKeypadOpen  — UI กรอกรหัสกำลังแสดงอยู่
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
    //  Public State — KeypadPromptUI อ่านได้
    // ═══════════════════════════════════════════════════

    /// <summary>true = ประตูเปิดสำเร็จแล้ว (ถาวร)</summary>
    public bool DoorUnlocked { get; private set; } = false;

    /// <summary>true = UI กรอกรหัสกำลังแสดงอยู่</summary>
    public bool IsKeypadOpen { get; private set; } = false;

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    RectTransform   _panelRT;
    GameObject      _panel;
    TextMeshProUGUI _display;
    TextMeshProUGUI _statusText;

    string           _input      = "";
    PlayerController _nearPlayer = null;

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

        // ── ESC — Text ล้วน ไม่มีกล่องพื้นหลัง ──
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

    void MakeEscText(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Btn_ESC", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(280, 44);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "ESC  —  Close";
        tmp.fontSize  = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(0.65f, 0.65f, 0.75f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = null;
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
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        // ── ประตูเปิดแล้ว = หยุดทำงานทั้งหมด ──
        if (DoorUnlocked) return;

        CheckNearby();
        HandleOpenKey();
    }

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

    void HandleOpenKey()
    {
        if (_nearPlayer == null || IsKeypadOpen) return;

        bool openPressed = (_nearPlayer == player1 && Input.GetKeyDown(KeyCode.E))
                        || (_nearPlayer == player2 && Input.GetKeyDown(KeyCode.Keypad7));

        if (openPressed) Open(_nearPlayer);
    }

    // ═══════════════════════════════════════════════════
    //  Open / Close
    // ═══════════════════════════════════════════════════

    void Open(PlayerController player)
    {
        if (DoorUnlocked) return;

        IsKeypadOpen = true;   // ← แจ้ง KeypadPromptUI ให้ซ่อน Prompt
        _input = "";
        UpdateDisplay();
        SetStatus("", Color.white);
        PositionPanel(player);
        _panel.SetActive(true);
    }

    void Close()
    {
        IsKeypadOpen = false;   // ← แจ้ง KeypadPromptUI ให้แสดง Prompt อีกครั้ง
        _panel.SetActive(false);
        _input = "";
    }

    void PositionPanel(PlayerController player)
    {
        bool isP1 = (player == player1);

        float screenX = isP1 ? Screen.width * 0.25f : Screen.width * 0.75f;
        float screenY = Screen.height * 0.5f;

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

            if (_door != null) _door.ForceOpen();

            // ── ล็อคถาวร ──
            DoorUnlocked = true;   // ← KeypadPromptUI จะซ่อน Prompt ถาวร
            IsKeypadOpen = false;

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

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < correctCode.Length; i++)
        {
            sb.Append(i < _input.Length ? _input[i] : '_');
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
        Gizmos.color = DoorUnlocked ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
