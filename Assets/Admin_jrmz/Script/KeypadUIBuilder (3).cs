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
/// Auto-close:
///   ถ้าผู้เล่นเดินออกนอกรัศมี triggerRadius → UI ปิดอัตโนมัติ
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
    //  Public State
    // ═══════════════════════════════════════════════════

    public bool DoorUnlocked { get; private set; } = false;
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
    //  Update — ตรวจ Auto-close
    // ═══════════════════════════════════════════════════

    void Update()
    {
        if (!IsKeypadOpen) return;

        // ── Auto-close เมื่อผู้เล่นที่เปิด UI เดินออกนอกรัศมี ──
        if (_nearPlayer != null)
        {
            float dist = Vector3.Distance(_nearPlayer.transform.position,
                                          transform.position);
            if (dist > triggerRadius)
            {
                Debug.Log("[Keypad] ผู้เล่นออกนอกรัศมี → ปิด UI อัตโนมัติ");
                Close();
                return;
            }
        }

        // ESC ปิด
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
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

        // ESC
        MakeEscText(p, new Vector2(0, -275));
    }

    // ═══════════════════════════════════════════════════
    //  Open — เรียกจาก KeypadPromptUI
    // ═══════════════════════════════════════════════════

    public void Open(PlayerController caller, bool isLeftSide)
    {
        if (DoorUnlocked || IsKeypadOpen) return;

        _nearPlayer = caller;
        IsKeypadOpen = true;
        _input = "";
        UpdateDisplay();
        if (_statusText != null) _statusText.text = "";

        // วางตำแหน่ง Panel กลางจอฝั่งที่เปิด
        if (_panelRT != null)
        {
            if (isLeftSide)
            {
                _panelRT.anchorMin = new Vector2(0.25f, 0.5f);
                _panelRT.anchorMax = new Vector2(0.25f, 0.5f);
            }
            else
            {
                _panelRT.anchorMin = new Vector2(0.75f, 0.5f);
                _panelRT.anchorMax = new Vector2(0.75f, 0.5f);
            }
            _panelRT.pivot            = new Vector2(0.5f, 0.5f);
            _panelRT.anchoredPosition = Vector2.zero;
        }

        _panel.SetActive(true);
    }

    // ═══════════════════════════════════════════════════
    //  Close
    // ═══════════════════════════════════════════════════

    void Close()
    {
        IsKeypadOpen = false;
        _input       = "";
        _nearPlayer  = null;
        if (_panel != null) _panel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════
    //  Input Handlers
    // ═══════════════════════════════════════════════════

    void PressNum(int n)
    {
        if (_input.Length >= correctCode.Length) return;
        _input += n.ToString();
        UpdateDisplay();
    }

    void PressDelete()
    {
        if (_input.Length == 0) return;
        _input = _input.Substring(0, _input.Length - 1);
        UpdateDisplay();
    }

    void PressOK()
    {
        if (_input == correctCode)
        {
            if (_statusText != null)
            {
                _statusText.color = new Color(0.2f, 0.9f, 0.3f);
                _statusText.text  = "✓ Correct!";
            }
            DoorUnlocked = true;
            _door?.ForceOpen();
            Invoke(nameof(Close), 0.8f);
        }
        else
        {
            if (_statusText != null)
            {
                _statusText.color = new Color(1f, 0.3f, 0.3f);
                _statusText.text  = "✗ Wrong code";
            }
            _door?.PlayWrong();
            _input = "";
            Invoke(nameof(ClearStatus), 1.2f);
        }
    }

    void ClearStatus()
    {
        if (_statusText != null) _statusText.text = "";
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (_display == null) return;
        string shown = "";
        for (int i = 0; i < correctCode.Length; i++)
            shown += i < _input.Length ? "●" : "_";
        _display.text = string.Join(" ", shown.ToCharArray());
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
                      new Color(0.22f, 0.22f, 0.32f), () => PressNum(n));
    }

    void MakeActionBtn(Transform parent, string label, Vector2 pos,
                       Color bgColor, UnityEngine.Events.UnityAction action)
    {
        var go = MakeImage(parent, "Btn_" + label, pos,
                           new Vector2(80, 72), bgColor);
        var txt = MakeText(go.transform, "Lbl", label,
                           Vector2.zero, new Vector2(80, 72), 26, Color.white);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var bc = btn.colors;
        bc.highlightedColor = new Color(0.4f, 0.4f, 0.55f);
        bc.pressedColor     = new Color(0.15f, 0.15f, 0.22f);
        btn.colors = bc;
        btn.onClick.AddListener(action);
    }
}
