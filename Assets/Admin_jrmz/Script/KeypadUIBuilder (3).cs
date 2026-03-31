using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// KeypadUIBuilder — UI กรอกรหัสผ่าน
/// ค้างปุ่ม E / Numpad7 (วงวิ่งในป้าย 3D) → ครบ → เปิด Keypad UI
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
    public Camera cameraP1;
    public Camera cameraP2;

    [Header("── Settings ────────────────────────")]
    public string correctCode   = "1234";
    public float  triggerRadius = 3f;

    [Header("── Timing ───────────────────────────")]
    [Tooltip("วินาทีที่ต้องค้างปุ่มเพื่อเปิด Keypad")]
    public float holdDuration = 1.2f;

    // ═══════════════════════════════════════════════════
    //  Public State
    // ═══════════════════════════════════════════════════

    public bool DoorUnlocked { get; private set; } = false;
    public bool IsKeypadOpen { get; private set; } = false;

    // ═══════════════════════════════════════════════════
    //  Colors
    // ═══════════════════════════════════════════════════

    static readonly Color ColRingBg   = new Color(0.12f, 0.12f, 0.18f, 1.00f);
    static readonly Color ColRingFill = new Color(0.94f, 0.75f, 0.15f, 1.00f);
    static readonly Color ColRingDone = new Color(0.25f, 0.95f, 0.45f, 1.00f);
    static readonly Color ColBg       = new Color(0.06f, 0.06f, 0.09f, 0.96f);
    static readonly Color ColTextKey  = new Color(1.00f, 0.80f, 0.20f, 1.00f);

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    RectTransform   _panelRT;
    GameObject      _panel;
    TextMeshProUGUI _display;
    TextMeshProUGUI _statusText;

    string           _input       = "";
    PlayerController _nearPlayer  = null;
    bool             _isP1Opening = false;

    SlidingDoor _door;

    // Hold ring (อยู่ใน KeypadPromptUI board — แต่ KeypadUIBuilder สร้าง ring เองใน panel)
    // *** ring ในป้าย 3D ถูกสร้างโดย KeypadPromptUI ***
    // KeypadUIBuilder รับ ref จาก KeypadPromptUI ผ่าน SetRingRef()
    Image _ringFill;
    float _holdTimer = 0f;
    bool  _fired     = false;

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        _door = FindObjectOfType<SlidingDoor>();
        BuildUI();
    }

    /// <summary>KeypadPromptUI เรียกเพื่อส่ง ref ของ ring fill ในป้าย 3D</summary>
    public void SetRingRef(Image ringFill) => _ringFill = ringFill;

    // ═══════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════

    void Update()
    {
        if (DoorUnlocked) return;
        HandleHoldInput();
        HandleKeypadInput();
    }

    // ═══════════════════════════════════════════════════
    //  HandleHoldInput — ค้างปุ่มเพื่อเปิด Keypad
    // ═══════════════════════════════════════════════════

    void HandleHoldInput()
    {
        if (IsKeypadOpen) return;

        bool p1Near    = player1 != null &&
                         Vector3.Distance(player1.transform.position, transform.position) <= triggerRadius;
        bool p2Near    = player2 != null &&
                         Vector3.Distance(player2.transform.position, transform.position) <= triggerRadius;

        bool p1Holding = p1Near && Input.GetKey(KeyCode.E);
        bool p2Holding = p2Near && Input.GetKey(KeyCode.Keypad7);
        bool anyHold   = p1Holding || p2Holding;

        if (p1Holding) _isP1Opening = true;
        if (p2Holding) _isP1Opening = false;

        if (anyHold && !_fired)
        {
            _holdTimer += Time.deltaTime / holdDuration;
            _holdTimer  = Mathf.Clamp01(_holdTimer);
            SetRing(_holdTimer);

            if (_holdTimer >= 1f)
            {
                _fired = true;
                ResetRing();
                OpenKeypad(_isP1Opening);
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

    void ResetRing()
    {
        _holdTimer = 0f;
        SetRing(0f);
    }

    // ═══════════════════════════════════════════════════
    //  OpenKeypad
    // ═══════════════════════════════════════════════════

    void OpenKeypad(bool isP1)
    {
        IsKeypadOpen = true;
        _nearPlayer  = isP1 ? player1 : player2;
        _input       = "";
        UpdateDisplay();
        SetStatus("", Color.white);

        float anchorX = isP1 ? 0.25f : 0.75f;
        if (_panelRT != null)
        {
            _panelRT.anchorMin        = new Vector2(anchorX, 0.5f);
            _panelRT.anchorMax        = new Vector2(anchorX, 0.5f);
            _panelRT.anchoredPosition = Vector2.zero;
        }
        _panel.SetActive(true);
    }

    // ═══════════════════════════════════════════════════
    //  HandleKeypadInput
    // ═══════════════════════════════════════════════════

    void HandleKeypadInput()
    {
        if (!IsKeypadOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) ||
                Input.GetKeyDown(KeyCode.Keypad0 + i))
                PressDigit(i);
        }
        if (Input.GetKeyDown(KeyCode.Backspace)) PressDelete();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) PressOK();
    }

    // ═══════════════════════════════════════════════════
    //  Keypad Logic
    // ═══════════════════════════════════════════════════

    void PressDigit(int n)
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
            SetStatus("✓  Unlocked!", new Color(0.2f, 0.9f, 0.4f));
            _door?.ForceOpen();
            DoorUnlocked = true;
            Invoke(nameof(Close), 1.2f);
        }
        else
        {
            SetStatus("✗  Wrong code", new Color(1f, 0.3f, 0.3f));
            _door?.PlayWrong();
            _input = "";
            UpdateDisplay();
        }
    }

    void Close()
    {
        IsKeypadOpen = false;
        _panel.SetActive(false);
        ResetRing();
    }

    void UpdateDisplay()
    {
        if (_display == null) return;
        if (_input.Length == 0)
        {
            string blank = "";
            for (int i = 0; i < correctCode.Length; i++)
                blank += "_ " ;
            _display.text = blank.TrimEnd();
            return;
        }
        string s = "";
        for (int i = 0; i < correctCode.Length; i++)
            s += (i < _input.Length ? "●" : "_") + (i < correctCode.Length - 1 ? " " : "");
        _display.text = s;
    }

    void SetStatus(string msg, Color col)
    {
        if (_statusText == null) return;
        _statusText.text  = msg;
        _statusText.color = col;
    }

    // ═══════════════════════════════════════════════════
    //  BuildUI
    // ═══════════════════════════════════════════════════

    void BuildUI()
    {
        if (targetCanvas == null) return;

        Transform root = targetCanvas.transform;

        _panel   = MakeImage(root, "KeypadPanel",
                             Vector2.zero, new Vector2(340, 580),
                             new Color(0.08f, 0.08f, 0.12f, 0.97f));
        _panelRT = _panel.GetComponent<RectTransform>();
        _panel.SetActive(false);

        Transform p = _panel.transform;

        MakeText(p, "Title", "[ KEYPAD ]",
                 new Vector2(0, 245), new Vector2(300, 50), 28,
                 new Color(0.4f, 0.8f, 1f));

        var dispGo = MakeImage(p, "DisplayBG",
                               new Vector2(0, 180), new Vector2(280, 60),
                               new Color(0.02f, 0.02f, 0.05f, 1f));
        _display = MakeText(dispGo.transform, "DisplayText", "_ _ _ _",
                            Vector2.zero, new Vector2(280, 60), 38, Color.white);

        _statusText = MakeText(p, "Status", "",
                               new Vector2(0, 125), new Vector2(280, 36), 20,
                               new Color(1f, 0.4f, 0.4f));

        MakeNumBtn(p, 1, new Vector2(-100,  65));
        MakeNumBtn(p, 2, new Vector2(   0,  65));
        MakeNumBtn(p, 3, new Vector2( 100,  65));
        MakeNumBtn(p, 4, new Vector2(-100, -25));
        MakeNumBtn(p, 5, new Vector2(   0, -25));
        MakeNumBtn(p, 6, new Vector2( 100, -25));
        MakeNumBtn(p, 7, new Vector2(-100,-115));
        MakeNumBtn(p, 8, new Vector2(   0,-115));
        MakeNumBtn(p, 9, new Vector2( 100,-115));

        MakeActionBtn(p, "DEL", new Vector2(-100,-205), new Color(0.6f,0.2f,0.2f), PressDelete);
        MakeNumBtn(p, 0, new Vector2(0,-205));
        MakeActionBtn(p, "OK",  new Vector2( 100,-205), new Color(0.2f,0.55f,0.2f), PressOK);

        MakeEscText(p, new Vector2(0, -275));
    }

    // ─── UI Helpers ──────────────────────────────────

    GameObject MakeImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
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
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = color;
        return tmp;
    }

    void MakeEscText(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Btn_ESC", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(280, 44);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "ESC  —  Close"; tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.65f, 0.65f, 0.75f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = null;
        btn.onClick.AddListener(Close);
    }

    void MakeNumBtn(Transform parent, int num, Vector2 pos)
    {
        int n = num;
        MakeActionBtn(parent, num.ToString(), pos,
                      new Color(0.22f, 0.22f, 0.32f), () => PressDigit(n));
    }

    void MakeActionBtn(Transform parent, string label, Vector2 pos,
                       Color bgColor, System.Action onClick)
    {
        var go = MakeImage(parent, "Btn_" + label, pos, new Vector2(80, 72), bgColor);
        MakeText(go.transform, "Lbl", label, Vector2.zero, new Vector2(80, 72), 26, Color.white);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        btn.onClick.AddListener(() => onClick());
        var colors = btn.colors;
        colors.normalColor      = bgColor;
        colors.highlightedColor = bgColor * 1.3f;
        colors.pressedColor     = bgColor * 0.7f;
        btn.colors = colors;
    }
}
