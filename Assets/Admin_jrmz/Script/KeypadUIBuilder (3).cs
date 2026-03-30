using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// KeypadUIBuilder — UI กรอกรหัสผ่านกลางจอของ Player ที่กด
///
/// Split-Screen:
///   Player1 (ซ้าย) กด E  → UI ขึ้นกลางจอซ้าย
///   Player2 (ขวา) กด Numpad7 → UI ขึ้นกลางจอขวา
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

        // Panel — anchorMin/Max จะถูกตั้งใน Open() ตามฝั่ง Player
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
        _display   = MakeText(dispGo.transform, "DisplayText", "_ _ _ _",
                              Vector2.zero, new Vector2(280, 60), 38, Color.white);

        // Status
        _statusText = MakeText(p, "Status", "",
                               new Vector2(0, 125), new Vector2(280, 36), 20,
                               new Color(1f, 0.4f, 0.4f));

        // ปุ่มตัวเลข
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
        MakeActionBtn(p, "ESC  ออกจากหน้านี้", new Vector2(0,-275),
                      new Color(0.35f,0.35f,0.45f), Close);
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
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return tmp;
    }

    void MakeNumBtn(Transform parent, int num, Vector2 pos)
    {
        int n = num;
        MakeActionBtn(parent, num.ToString(), pos,
                      new Color(0.22f,0.22f,0.32f), () => PressNumber(n));
    }

    void MakeActionBtn(Transform parent, string label, Vector2 pos,
                       Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        bool isEsc = label.StartsWith("ESC");
        var sz = isEsc ? new Vector2(280,50) : new Vector2(82,72);
        float fz = isEsc ? 20f : (label.Length > 2 ? 18f : 28f);

        var go = new GameObject("Btn_"+label,
                                typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sz;

        var img = go.GetComponent<Image>();
        img.color = bgColor;
        var btn = go.GetComponent<Button>();
        var cb = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = bgColor * 1.4f;
        cb.pressedColor     = bgColor * 0.6f;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        MakeText(go.transform, "Label", label,
                 Vector2.zero, sz, fz, Color.white);
    }

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        CheckDistance();

        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
        { Close(); return; }

        if (_nearPlayer == null) return;

        var inp = _nearPlayer.GetComponent<PlayerInputHandler>();
        if (inp == null) return;

        bool toggle = inp.playerID == PlayerInputHandler.PlayerID.Player1
                    ? Input.GetKeyDown(KeyCode.E)
                    : Input.GetKeyDown(KeyCode.Keypad7);

        if (toggle)
        {
            if (_isOpen) Close();
            else         Open();
        }
    }

    // ═══════════════════════════════════════════════════
    //  ตรวจระยะ
    // ═══════════════════════════════════════════════════

    void CheckDistance()
    {
        PlayerController closest = null;
        float minD = triggerRadius;
        TryCloser(player1, ref closest, ref minD);
        TryCloser(player2, ref closest, ref minD);

        if (_isOpen && _nearPlayer != null)
        {
            float d = Vector3.Distance(transform.position,
                                       _nearPlayer.transform.position);
            if (d > triggerRadius + 1f) Close();
        }

        _nearPlayer = closest;
    }

    void TryCloser(PlayerController pc,
                   ref PlayerController best, ref float minD)
    {
        if (pc == null) return;
        float d = Vector3.Distance(transform.position, pc.transform.position);
        if (d < minD) { minD = d; best = pc; }
    }

    // ═══════════════════════════════════════════════════
    //  Open — เลื่อน Panel ไปกลางจอของ Player ที่กด
    // ═══════════════════════════════════════════════════

    void Open()
    {
        _isOpen = true;
        _input  = "";
        SetStatus("", Color.white);
        RefreshDisplay();

        // ── เลื่อน Panel ให้ตรงกลางจอของ Player ──────
        PositionPanelForPlayer(_nearPlayer);

        _panel.SetActive(true);

        if (_nearPlayer != null) _nearPlayer.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    /// <summary>
    /// ย้าย Panel ให้อยู่กลาง Viewport ของ Player ที่กด
    /// Split-Screen ซ้าย  = anchorX 0.0 – 0.5  → กลาง = 0.25
    /// Split-Screen ขวา   = anchorX 0.5 – 1.0  → กลาง = 0.75
    /// </summary>
    void PositionPanelForPlayer(PlayerController pc)
    {
        if (_panelRT == null) return;

        var inp = pc != null ? pc.GetComponent<PlayerInputHandler>() : null;
        bool isP1 = inp == null || inp.playerID == PlayerInputHandler.PlayerID.Player1;

        if (isP1)
        {
            // ฝั่งซ้าย: Viewport X = 0.0 – 0.5
            _panelRT.anchorMin = new Vector2(0f,   0f);
            _panelRT.anchorMax = new Vector2(0.5f, 1f);
        }
        else
        {
            // ฝั่งขวา: Viewport X = 0.5 – 1.0
            _panelRT.anchorMin = new Vector2(0.5f, 0f);
            _panelRT.anchorMax = new Vector2(1f,   1f);
        }

        _panelRT.pivot             = new Vector2(0.5f, 0.5f);
        _panelRT.anchoredPosition  = Vector2.zero;   // กลาง Viewport
        _panelRT.sizeDelta         = new Vector2(340, 580);
    }

    // ═══════════════════════════════════════════════════
    //  Close
    // ═══════════════════════════════════════════════════

    public void Close()
    {
        _isOpen = false;
        _panel.SetActive(false);

        if (_nearPlayer != null) _nearPlayer.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _input = "";
        RefreshDisplay();
    }

    // ═══════════════════════════════════════════════════
    //  ปุ่ม Logic
    // ═══════════════════════════════════════════════════

    void PressNumber(int n)
    {
        if (!_isOpen || _input.Length >= 4) return;
        _input += n.ToString();
        SetStatus("", Color.white);
        RefreshDisplay();
    }

    void PressDelete()
    {
        if (!_isOpen || _input.Length == 0) return;
        _input = _input.Substring(0, _input.Length - 1);
        SetStatus("", Color.white);
        RefreshDisplay();
    }

    void PressOK()
    {
        if (!_isOpen) return;
        if (_input.Length < 4)
        { SetStatus("กรอกให้ครบ 4 หลัก!", new Color(1f,0.6f,0.2f)); return; }

        if (_input == correctCode)
        {
            SetStatus("✓ รหัสถูกต้อง!", new Color(0.3f,1f,0.4f));
            Invoke(nameof(SuccessClose), 0.6f);
        }
        else
        {
            _input = "";
            RefreshDisplay();
            SetStatus("✗ รหัสไม่ถูกต้อง!", new Color(1f,0.3f,0.3f));
            if (_door != null) _door.PlayWrong();
        }
    }

    void SuccessClose()
    {
        if (_door != null) _door.ForceOpen();
        Close();
    }

    // ═══════════════════════════════════════════════════
    //  Display
    // ═══════════════════════════════════════════════════

    void RefreshDisplay()
    {
        if (_display == null) return;
        string s = "";
        for (int i = 0; i < 4; i++)
        {
            s += (i < _input.Length) ? "●" : "_";
            if (i < 3) s += "  ";
        }
        _display.text = s;
    }

    void SetStatus(string msg, Color color)
    {
        if (_statusText == null) return;
        _statusText.text  = msg;
        _statusText.color = color;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
