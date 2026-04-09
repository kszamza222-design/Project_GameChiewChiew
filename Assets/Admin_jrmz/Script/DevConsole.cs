using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// DevConsole v7 — แก้ไข:
///   1. CAM MOVE ปิด CameraFollow ก่อนขยับกล้อง
///   2. HideAllCanvases เก็บสถานะเดิมก่อนซ่อน ไม่คืนค่าให้อันที่ปิดอยู่ก่อน
/// </summary>
public class DevConsole : MonoBehaviour
{
    [Header("── References ──────────────────────")]
    public Canvas            targetCanvas;
    public PlayerController  player1;
    public PlayerController  player2;
    public HealthSystem      healthSystem;
    public CheckpointManager checkpointManager;
    public KeyInventory      keyInventory;

    [Header("── Toggle Key ──────────────────────")]
    public KeyCode toggleKey = KeyCode.F12;

    [Header("── Panel Position ───────────────────")]
    public Vector2 panelOffset = new Vector2(10f, -10f);

    [Header("── Cinematic Mode ───────────────────")]
    public Canvas[] extraCanvasesToHide;

    // ══════════════════════════════════════════════════════
    //  Palette
    // ══════════════════════════════════════════════════════

    static readonly Color C_BG         = new Color(0.080f, 0.062f, 0.040f, 0.96f);
    static readonly Color C_DARK       = new Color(0.052f, 0.040f, 0.025f, 1.00f);
    static readonly Color C_MID        = new Color(0.120f, 0.096f, 0.062f, 1.00f);
    static readonly Color C_LITE       = new Color(0.170f, 0.140f, 0.092f, 1.00f);
    static readonly Color C_GOLD       = new Color(0.92f,  0.70f,  0.18f,  1.00f);
    static readonly Color C_GOLD_DIM   = new Color(0.52f,  0.38f,  0.05f,  1.00f);
    static readonly Color C_GOLD_DARK  = new Color(0.26f,  0.18f,  0.02f,  1.00f);
    static readonly Color C_BLUE       = new Color(0.40f,  0.82f,  1.00f,  1.00f);
    static readonly Color C_BLUE_DIM   = new Color(0.12f,  0.28f,  0.52f,  1.00f);
    static readonly Color C_ORANGE     = new Color(1.00f,  0.78f,  0.22f,  1.00f);
    static readonly Color C_ORANGE_DIM = new Color(0.46f,  0.28f,  0.04f,  1.00f);
    static readonly Color C_GREEN      = new Color(0.18f,  0.90f,  0.32f,  1.00f);
    static readonly Color C_GREEN_DIM  = new Color(0.08f,  0.30f,  0.12f,  1.00f);
    static readonly Color C_RED_DIM    = new Color(0.38f,  0.07f,  0.07f,  1.00f);
    static readonly Color C_GHOST      = new Color(0.72f,  0.50f,  1.00f,  1.00f);
    static readonly Color C_GHOST_DIM  = new Color(0.26f,  0.14f,  0.48f,  1.00f);
    static readonly Color C_GHOST_DARK = new Color(0.13f,  0.07f,  0.24f,  1.00f);
    static readonly Color C_CINE       = new Color(0.20f,  0.85f,  0.85f,  1.00f);
    static readonly Color C_CINE_DIM   = new Color(0.05f,  0.28f,  0.28f,  1.00f);
    static readonly Color C_CINE_DARK  = new Color(0.02f,  0.14f,  0.14f,  1.00f);
    static readonly Color C_TEXT       = new Color(0.93f,  0.87f,  0.72f,  1.00f);
    static readonly Color C_TEXT_DIM   = new Color(0.50f,  0.44f,  0.30f,  1.00f);
    static readonly Color C_SEP        = new Color(0.92f,  0.70f,  0.18f,  0.16f);

    // ══════════════════════════════════════════════════════
    //  State
    // ══════════════════════════════════════════════════════

    bool  _open      = false;
    bool  _godP1     = false;
    bool  _godP2     = false;
    float _speedMult = 1f;
    float _timeScale = 1f;
    float _baseSpeedP1 = 5f, _baseSpeedP2 = 5f;
    float _fps = 0f, _fpsTimer = 0f;

    // Ghost
    bool              _ghostActive    = false;
    bool              _ghostInvisible = true;
    float             _ghostSpeed     = 14f;
    const float       GHOST_SPD_MIN   = 1f;
    const float       GHOST_SPD_MAX   = 80f;
    GameObject          _ghostCamGO  = null;
    Camera              _ghostCam    = null;
    CharacterController _ccP1        = null;
    Renderer[]          _p1Renderers = null;

    // Cinematic
    bool   _cinematic       = false;
    bool   _cineP1          = true;
    bool   _cineGhostActive = false;
    bool   _cineInvisible   = false;
    float  _cineGhostSpeed  = 14f;
    Rect   _savedRectP1     = new Rect(0f,   0f, 0.5f, 1f);
    Rect   _savedRectP2     = new Rect(0.5f, 0f, 0.5f, 1f);
    Camera _cineFocusCam    = null;

    // ── เก็บสถานะเดิมของ canvas children ก่อนซ่อน ──────
    Dictionary<GameObject, bool> _canvasChildStates = new Dictionary<GameObject, bool>();
    Dictionary<Canvas, bool>     _extraCanvasStates  = new Dictionary<Canvas, bool>();

    Renderer[] _cineP1Rend = null;
    Renderer[] _cineP2Rend = null;

    // ── CameraFollow ที่ต้อง disable ตอน CineGhost ──────
    CameraFollow _cineCamFollow = null;

    // ── UI refs ──────────────────────────────────────────
    GameObject      _root;
    RectTransform   _panelRt;
    TextMeshProUGUI _txtFPS;
    TextMeshProUGUI _txtP1Pos, _txtP2Pos, _txtP1HP, _txtP2HP;
    TextMeshProUGUI _txtTimeScale, _txtSpeedMult, _txtGhostSpeed, _txtCineSpeed;
    Image           _imgGodP1, _imgGodP2;
    Image           _imgGhost, _imgInvisible;
    Image           _imgCine,  _imgCineP1, _imgCineP2;
    Image           _imgCineGhost, _imgCineInvis;
    TextMeshProUGUI _lblGodP1, _lblGodP2;
    TextMeshProUGUI _lblGhost, _lblInvisible;
    TextMeshProUGUI _lblCine,  _lblCineP1, _lblCineP2;
    TextMeshProUGUI _lblCineGhost, _lblCineInvis;
    Slider          _timeSlider, _speedSlider, _ghostSlider, _cineSpeedSlider;

    // ══════════════════════════════════════════════════════
    //  Init
    // ══════════════════════════════════════════════════════

    void Awake()
    {
        if (player1 != null)
        {
            _baseSpeedP1 = player1.moveSpeed;
            _ccP1 = player1.GetComponent<CharacterController>();
            _cineP1Rend = player1.GetComponentsInChildren<Renderer>();
        }
        if (player2 != null)
        {
            _baseSpeedP2 = player2.moveSpeed;
            _cineP2Rend = player2.GetComponentsInChildren<Renderer>();
        }
        if (targetCanvas == null) { Debug.LogError("[DevConsole] targetCanvas ยังไม่ผูก!"); return; }
        BuildUI();
        _root.SetActive(false);
    }

    void OnDestroy()
    {
        ExitGhost();
        if (_cinematic) ExitCinematic();
    }

    // ══════════════════════════════════════════════════════
    //  Update
    // ══════════════════════════════════════════════════════

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) ToggleConsole();
        if (_open)
        {
            TickFPS();
            TickInfo();
            ApplyGodMode();
            if (_panelRt != null) _panelRt.anchoredPosition = panelOffset;
        }
        if (_ghostActive) GhostMove();
        if (_cinematic && _cineGhostActive) CineGhostMove();
    }

    void ToggleConsole() { _open = !_open; _root.SetActive(_open); }

    void TickFPS()
    {
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer < 0.3f) return;
        _fps = 1f / Time.unscaledDeltaTime;
        _fpsTimer = 0f;
        if (_txtFPS == null) return;
        Color c = _fps >= 55 ? C_GREEN : _fps >= 30 ? C_GOLD : new Color(1f, 0.3f, 0.3f);
        _txtFPS.text = $"<color=#{H(C_TEXT_DIM)}>FPS</color> <color=#{H(c)}><b>{_fps:F0}</b></color>";
    }

    void TickInfo()
    {
        if (player1 != null && _txtP1Pos != null)
        { var p = player1.transform.position; _txtP1Pos.text = $"{p.x:F1}, {p.y:F1}, {p.z:F1}"; }
        if (player2 != null && _txtP2Pos != null)
        { var p = player2.transform.position; _txtP2Pos.text = $"{p.x:F1}, {p.y:F1}, {p.z:F1}"; }
        if (healthSystem != null)
        {
            if (_txtP1HP != null) _txtP1HP.text = $"{healthSystem.GetHP(true)} / {healthSystem.maxHP}";
            if (_txtP2HP != null) _txtP2HP.text = $"{healthSystem.GetHP(false)} / {healthSystem.maxHP}";
        }
    }

    void ApplyGodMode()
    {
        if (_godP1 && healthSystem != null && healthSystem.GetHP(true)  < healthSystem.maxHP) healthSystem.ResetHP(true);
        if (_godP2 && healthSystem != null && healthSystem.GetHP(false) < healthSystem.maxHP) healthSystem.ResetHP(false);
    }

    // ══════════════════════════════════════════════════════
    //  CINEMATIC MODE
    // ══════════════════════════════════════════════════════

    void ToggleCinematic()
    {
        _cinematic = !_cinematic;
        if (_cinematic) EnterCinematic(); else ExitCinematic();
        RefreshCineUI();
    }

    void EnterCinematic()
    {
        var ssm = FindObjectOfType<SplitScreenManager>();
        if (ssm == null) { Debug.LogWarning("[DevConsole] ไม่พบ SplitScreenManager"); return; }

        if (ssm.cameraP1 != null) _savedRectP1 = ssm.cameraP1.rect;
        if (ssm.cameraP2 != null) _savedRectP2 = ssm.cameraP2.rect;

        if (_cineP1)
        {
            if (ssm.cameraP1 != null) { ssm.cameraP1.rect = new Rect(0f, 0f, 1f, 1f); _cineFocusCam = ssm.cameraP1; }
            if (ssm.cameraP2 != null) ssm.cameraP2.enabled = false;
        }
        else
        {
            if (ssm.cameraP2 != null) { ssm.cameraP2.rect = new Rect(0f, 0f, 1f, 1f); _cineFocusCam = ssm.cameraP2; }
            if (ssm.cameraP1 != null) ssm.cameraP1.enabled = false;
        }

        ssm.showDivider = false;

        // ── เก็บ CameraFollow ของกล้องที่โฟกัส ──────────
        if (_cineFocusCam != null)
            _cineCamFollow = _cineFocusCam.GetComponent<CameraFollow>();

        // ── เปิด CineGhost ถ้าเปิดอยู่ ──────────────────
        if (_cineGhostActive) EnableCineGhost(true);

        // ── บันทึกสถานะ canvas แล้วซ่อน ─────────────────
        SaveAndHideCanvases();

        Debug.Log($"[DevConsole] Cinematic ON — Focus: {(_cineP1 ? "P1" : "P2")}");
    }

    void ExitCinematic()
    {
        var ssm = FindObjectOfType<SplitScreenManager>();
        if (ssm != null)
        {
            if (ssm.cameraP1 != null) { ssm.cameraP1.rect = _savedRectP1; ssm.cameraP1.enabled = true; }
            if (ssm.cameraP2 != null) { ssm.cameraP2.rect = _savedRectP2; ssm.cameraP2.enabled = true; }
            ssm.showDivider = true;
        }

        // คืน CameraFollow
        EnableCineGhost(false);
        if (_cineCamFollow != null) { _cineCamFollow.enabled = true; _cineCamFollow = null; }

        // คืน player movement
        SetFocusPlayerCC(true);

        // คืน visibility
        SetCinePlayerVis(true);

        // ── คืนสถานะ canvas เฉพาะที่เปิดอยู่ก่อนหน้า ───
        RestoreCanvases();

        _cineFocusCam = null;
        Debug.Log("[DevConsole] Cinematic OFF");
    }

    // ── บันทึกสถานะเดิมแล้วซ่อน ─────────────────────────
    void SaveAndHideCanvases()
    {
        _canvasChildStates.Clear();
        foreach (Transform child in targetCanvas.transform)
        {
            if (child.gameObject == _root) continue;
            _canvasChildStates[child.gameObject] = child.gameObject.activeSelf;
            child.gameObject.SetActive(false);
        }

        _extraCanvasStates.Clear();
        if (extraCanvasesToHide != null)
        {
            foreach (var c in extraCanvasesToHide)
            {
                if (c == null) continue;
                _extraCanvasStates[c] = c.enabled;
                c.enabled = false;
            }
        }
    }

    // ── คืนเฉพาะที่เปิดอยู่ก่อนหน้า ─────────────────────
    void RestoreCanvases()
    {
        foreach (var kv in _canvasChildStates)
            if (kv.Key != null) kv.Key.SetActive(kv.Value);

        foreach (var kv in _extraCanvasStates)
            if (kv.Key != null) kv.Key.enabled = kv.Value;

        _canvasChildStates.Clear();
        _extraCanvasStates.Clear();
    }

    void SetCineFocus(bool focusP1)
    {
        bool wasActive = _cinematic;
        if (wasActive) ExitCinematic();
        _cineP1 = focusP1;
        if (wasActive) EnterCinematic();
        RefreshCineUI();
    }

    // ══════════════════════════════════════════════════════
    //  CINEMATIC GHOST MOVEMENT
    // ══════════════════════════════════════════════════════

    void ToggleCineGhost()
    {
        _cineGhostActive = !_cineGhostActive;
        if (_cinematic) EnableCineGhost(_cineGhostActive);
        RefreshCineUI();
    }

    void EnableCineGhost(bool enable)
    {
        // disable/enable CameraFollow ของกล้องที่โฟกัส
        if (_cineCamFollow != null)
            _cineCamFollow.enabled = !enable;

        // disable/enable CharacterController ของ player ที่โฟกัส
        SetFocusPlayerCC(!enable);
    }

    void SetFocusPlayerCC(bool enable)
    {
        PlayerController fp = _cineP1 ? player1 : player2;
        if (fp == null) return;
        var cc = fp.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = enable;
    }

    void ToggleCineInvisible()
    {
        _cineInvisible = !_cineInvisible;
        if (_cinematic) SetCinePlayerVis(!_cineInvisible);
        RefreshCineUI();
    }

    void SetCinePlayerVis(bool vis)
    {
        var rends = _cineP1 ? _cineP1Rend : _cineP2Rend;
        if (rends == null) return;
        foreach (var r in rends) if (r != null) r.enabled = vis;
    }

    void CineGhostMove()
    {
        if (_cineFocusCam == null)
        {
            var ssm = FindObjectOfType<SplitScreenManager>();
            if (ssm != null)
                _cineFocusCam = _cineP1 ? ssm.cameraP1 : ssm.cameraP2;
        }
        if (_cineFocusCam == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _cineGhostSpeed = Mathf.Clamp(_cineGhostSpeed + Mathf.Sign(scroll) * 2f, GHOST_SPD_MIN, GHOST_SPD_MAX);
            UpdateCineSpeedUI();
        }

        float spd = Input.GetKey(KeyCode.LeftShift) ? _cineGhostSpeed * 3f : _cineGhostSpeed;
        Transform t = _cineFocusCam.transform;
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += t.forward;
        if (Input.GetKey(KeyCode.S)) move -= t.forward;
        if (Input.GetKey(KeyCode.A)) move -= t.right;
        if (Input.GetKey(KeyCode.D)) move += t.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down;

        if (move.sqrMagnitude > 0.001f)
            t.position += move.normalized * spd * Time.unscaledDeltaTime;

        if (Input.GetMouseButton(1))
        {
            float mx = Input.GetAxis("Mouse X") * 3.5f;
            float my = Input.GetAxis("Mouse Y") * 3.5f;
            Vector3 e = t.eulerAngles + new Vector3(-my, mx, 0f);
            e.z = 0f;
            t.eulerAngles = e;
        }
    }

    void UpdateCineSpeedUI()
    {
        if (_txtCineSpeed != null)
            _txtCineSpeed.text = $"Speed  <b>{_cineGhostSpeed:F0}</b>  <color=#{H(C_TEXT_DIM)}>(Shift×3  Scroll±2)</color>";
        if (_cineSpeedSlider != null && Mathf.Abs(_cineSpeedSlider.value - _cineGhostSpeed) > 0.01f)
            _cineSpeedSlider.SetValueWithoutNotify(Mathf.Clamp(_cineGhostSpeed, GHOST_SPD_MIN, GHOST_SPD_MAX));
    }

    void RefreshCineUI()
    {
        SetTog(_imgCine, _lblCine, _cinematic,
               _cinematic ? "CINEMATIC  ON" : "CINEMATIC  OFF", C_CINE, C_CINE_DIM);
        SetTog(_imgCineP1, _lblCineP1, _cineP1,  "FOCUS P1", C_BLUE,   C_BLUE_DIM);
        SetTog(_imgCineP2, _lblCineP2, !_cineP1, "FOCUS P2", C_ORANGE, C_ORANGE_DIM);
        SetTog(_imgCineGhost, _lblCineGhost, _cineGhostActive,
               _cineGhostActive ? "CAM MOVE  ●  ON" : "CAM MOVE  ○  OFF", C_CINE, C_CINE_DIM);
        SetTog(_imgCineInvis, _lblCineInvis, _cineInvisible,
               _cineInvisible ? "INVISIBLE  ✓" : "INVISIBLE", C_CINE, C_CINE_DIM);
    }

    // ══════════════════════════════════════════════════════
    //  GHOST MODE
    // ══════════════════════════════════════════════════════

    void ToggleGhost()
    {
        _ghostActive = !_ghostActive;
        if (_ghostActive) EnterGhost(); else ExitGhost();
        RefreshGhostToggle();
    }

    void EnterGhost()
    {
        if (player1 == null) return;
        if (_ccP1 != null) _ccP1.enabled = false;
        if (_ghostInvisible) SetP1Vis(false);

        if (_ghostCamGO == null)
        {
            _ghostCamGO = new GameObject("_DevGhostCam");
            _ghostCam   = _ghostCamGO.AddComponent<Camera>();
            _ghostCam.clearFlags    = CameraClearFlags.Skybox;
            _ghostCam.rect          = new Rect(0f, 0f, 0.5f, 1f);
            _ghostCam.fieldOfView   = 60f;
            _ghostCam.nearClipPlane = 0.1f;
            _ghostCam.farClipPlane  = 2000f;
            _ghostCam.depth         = 10;
        }

        _ghostCamGO.transform.position = player1.transform.position + Vector3.up * 2f;
        var ssm = FindObjectOfType<SplitScreenManager>();
        if (ssm != null && ssm.cameraP1 != null)
            _ghostCamGO.transform.rotation = ssm.cameraP1.transform.rotation;
        else
            _ghostCamGO.transform.rotation = player1.transform.rotation;

        _ghostCamGO.SetActive(true);
        if (ssm != null && ssm.cameraP1 != null) ssm.cameraP1.enabled = false;
    }

    void ExitGhost()
    {
        if (_ghostCamGO != null) _ghostCamGO.SetActive(false);
        if (_ccP1 != null && player1 != null) _ccP1.enabled = true;
        SetP1Vis(true);
        var ssm = FindObjectOfType<SplitScreenManager>();
        if (ssm != null && ssm.cameraP1 != null) ssm.cameraP1.enabled = true;
    }

    void GhostMove()
    {
        if (_ghostCamGO == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _ghostSpeed = Mathf.Clamp(_ghostSpeed + Mathf.Sign(scroll), GHOST_SPD_MIN, GHOST_SPD_MAX);
            UpdateGhostSpeedUI();
        }

        float spd = Input.GetKey(KeyCode.LeftShift) ? _ghostSpeed * 3f : _ghostSpeed;
        Transform t = _ghostCamGO.transform;
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += t.forward;
        if (Input.GetKey(KeyCode.S)) move -= t.forward;
        if (Input.GetKey(KeyCode.A)) move -= t.right;
        if (Input.GetKey(KeyCode.D)) move += t.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down;

        if (move.sqrMagnitude > 0.001f)
        {
            Vector3 delta = move.normalized * spd * Time.unscaledDeltaTime;
            t.position += delta;
            if (player1 != null) player1.transform.position += delta;
        }

        if (Input.GetMouseButton(1))
        {
            float mx = Input.GetAxis("Mouse X") * 3.5f;
            float my = Input.GetAxis("Mouse Y") * 3.5f;
            Vector3 e = t.eulerAngles + new Vector3(-my, mx, 0f);
            e.z = 0f;
            t.eulerAngles = e;
        }
    }

    void ToggleInvisible()
    {
        _ghostInvisible = !_ghostInvisible;
        if (_ghostActive) SetP1Vis(!_ghostInvisible);
        SetTog(_imgInvisible, _lblInvisible, _ghostInvisible,
               _ghostInvisible ? "INVISIBLE  ✓" : "INVISIBLE", C_GHOST, C_GHOST_DIM);
    }

    void SetGhostSpeedFromSlider(float v) { _ghostSpeed = v; UpdateGhostSpeedUI(); }

    void UpdateGhostSpeedUI()
    {
        if (_txtGhostSpeed != null)
            _txtGhostSpeed.text = $"Speed  <b>{_ghostSpeed:F0}</b>  <color=#{H(C_TEXT_DIM)}>(Shift×3  Scroll±1)</color>";
        if (_ghostSlider != null && Mathf.Abs(_ghostSlider.value - _ghostSpeed) > 0.01f)
            _ghostSlider.SetValueWithoutNotify(Mathf.Clamp(_ghostSpeed, GHOST_SPD_MIN, GHOST_SPD_MAX));
    }

    void RefreshGhostToggle()
    {
        SetTog(_imgGhost, _lblGhost, _ghostActive,
               _ghostActive ? "GHOST  ●  ON" : "GHOST  ○  OFF", C_GHOST, C_GHOST_DIM);
    }

    void SetP1Vis(bool vis)
    {
        if (player1 == null) return;
        if (_p1Renderers == null) _p1Renderers = player1.GetComponentsInChildren<Renderer>();
        foreach (var r in _p1Renderers) if (r != null) r.enabled = vis;
    }

    // ══════════════════════════════════════════════════════
    //  Actions
    // ══════════════════════════════════════════════════════

    void ToggleGodP1() { _godP1 = !_godP1; SetTog(_imgGodP1, _lblGodP1, _godP1, _godP1 ? "GOD  P1  ✓" : "GOD  P1", C_GREEN, C_GREEN_DIM); }
    void ToggleGodP2() { _godP2 = !_godP2; SetTog(_imgGodP2, _lblGodP2, _godP2, _godP2 ? "GOD  P2  ✓" : "GOD  P2", C_GREEN, C_GREEN_DIM); }

    void TpP1P2()  { if (player1 != null && player2 != null) player1.transform.position = player2.transform.position + Vector3.right * 1.2f; }
    void TpP2P1()  { if (player1 != null && player2 != null) player2.transform.position = player1.transform.position - Vector3.right * 1.2f; }
    void HealAll() { healthSystem?.ResetHP(true); healthSystem?.ResetHP(false); }
    void RespP1()  { checkpointManager?.TriggerRespawn(true); }
    void RespP2()  { checkpointManager?.TriggerRespawn(false); }
    void KeyP1()   { keyInventory?.AddKey(true); }
    void KeyP2()   { keyInventory?.AddKey(false); }
    void Reload()  { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    void SetTimeScale(float v)
    {
        _timeScale = v; Time.timeScale = v;
        if (_txtTimeScale != null) _txtTimeScale.text = $"Time Scale  <b>{v:F2}x</b>";
    }

    void SetSpeedMult(float v)
    {
        _speedMult = v;
        if (player1 != null) player1.moveSpeed = _baseSpeedP1 * v;
        if (player2 != null) player2.moveSpeed = _baseSpeedP2 * v;
        if (_txtSpeedMult != null) _txtSpeedMult.text = $"Walk Speed  <b>{v:F1}x</b>";
    }

    // ══════════════════════════════════════════════════════
    //  BUILD UI
    // ══════════════════════════════════════════════════════

    const float PW  = 340f;
    const float PH  = 620f;
    const float PAD = 10f;
    const float LH  = 18f;
    const float G   = 6f;

    void BuildUI()
    {
        _root = MkGO("DevConsole_Root", targetCanvas.transform);
        FullStr(_root);
        _root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);

        var panelCont = MkGO("PanelContainer", _root.transform);
        var pcRt = panelCont.AddComponent<RectTransform>();
        pcRt.anchorMin = pcRt.anchorMax = new Vector2(0f, 1f);
        pcRt.pivot     = new Vector2(0f, 1f);
        pcRt.anchoredPosition = panelOffset;
        pcRt.sizeDelta = new Vector2(PW, PH);
        _panelRt = pcRt;

        var glow = MkGO("Glow", panelCont.transform);
        var glRt = glow.AddComponent<RectTransform>();
        glRt.anchorMin = Vector2.zero; glRt.anchorMax = Vector2.one;
        glRt.offsetMin = new Vector2(-2f,-2f); glRt.offsetMax = new Vector2(2f,2f);
        glow.AddComponent<Image>().color = C_GOLD_DIM;

        var panel = MkGO("Panel", panelCont.transform);
        var pnRt = panel.AddComponent<RectTransform>();
        pnRt.anchorMin = Vector2.zero; pnRt.anchorMax = Vector2.one;
        pnRt.offsetMin = pnRt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = C_BG;
        Transform P = panel.transform;
        Stripe(P, 4f, PH, C_GOLD, 0f, 0f);

        float cy = 0f;

        // HEADER
        {
            const float HH = 40f;
            MkImg(P,"HdrBg", 0f,0f,  PW,  HH,   C_DARK, new Vector2(0f,1f));
            MkImg(P,"HdrBot",0f,-HH, PW,  1.5f, C_GOLD, new Vector2(0f,1f));
            var title = T(P,"Title","◈ DEV CONSOLE",PAD+8f,-HH*0.5f,220f,HH,14f,C_GOLD,TextAlignmentOptions.Left,new Vector2(0f,0.5f));
            title.fontStyle = FontStyles.Bold;
            _txtFPS = T(P,"FPS","FPS --",PW-8f,-HH*0.5f,80f,HH,11f,C_TEXT_DIM,TextAlignmentOptions.Right,new Vector2(1f,0.5f));
            cy = -HH-G;
        }

        // PLAYER INFO
        {
            cy -= 2f; SecLbl(P,"PLAYER INFO",cy); cy -= 14f;
            float cardH = LH*2f+G+8f;
            MkImg(P,"InfoBg",PAD,cy,PW-PAD*2f,cardH,C_DARK,new Vector2(0f,1f));
            float lx=PAD+6f,rx=PW*0.5f+4f,cw=PW*0.5f-PAD-14f;
            Stripe(P,2.5f,cardH-4f,C_BLUE,  lx,cy-2f);
            InfoPair(P,"P1 POS","P1 HP",lx+5f,cy-6f,cw,out _txtP1Pos,out _txtP1HP,C_BLUE,  C_GREEN);
            Stripe(P,2.5f,cardH-4f,C_ORANGE,rx,cy-2f);
            InfoPair(P,"P2 POS","P2 HP",rx+5f,cy-6f,cw,out _txtP2Pos,out _txtP2HP,C_ORANGE,C_GREEN);
            cy -= cardH+G; Sep(P,cy);
        }

        // TOGGLES
        {
            cy -= G; SecLbl(P,"TOGGLES",cy); cy -= 14f;
            float TW=(PW-PAD*2f-G)*0.5f,TH=30f,tx=PAD;
            (_imgGodP1,_lblGodP1)=MkTog(P,"GOD  P1",tx,      cy,TW,TH,false,ToggleGodP1,C_GREEN,C_GREEN_DIM);
            (_imgGodP2,_lblGodP2)=MkTog(P,"GOD  P2",tx+TW+G, cy,TW,TH,false,ToggleGodP2,C_GREEN,C_GREEN_DIM);
            cy -= TH+G; Sep(P,cy);
        }

        // CINEMATIC
        {
            cy -= G; SecLblColor(P,"CINEMATIC MODE",cy,C_CINE); cy -= 14f;
            float hbH=17f;
            MkImg(P,"CineHelpBg",PAD,cy,PW-PAD*2f,hbH,C_CINE_DARK,new Vector2(0f,1f));
            T(P,"CineHelp","<color=#55cccc>Cinematic</color>  ซ่อน UI + เส้นแบ่งจอ  |  <color=#55cccc>CAM MOVE</color>  WASD+Q/E+RMB",
              PAD+6f,cy-2f,PW-PAD*2f-8f,hbH,7.5f,C_CINE,TextAlignmentOptions.Left,new Vector2(0f,1f));
            cy -= hbH+G*0.5f;

            float cineW=PW-PAD*2f,cineH=32f;
            (_imgCine,_lblCine)=MkTog(P,"CINEMATIC  OFF",PAD,cy,cineW,cineH,false,ToggleCinematic,C_CINE,C_CINE_DIM,11f);
            cy -= cineH+G*0.5f;

            float fw=(PW-PAD*2f-G)*0.5f,fh=28f,fx=PAD;
            (_imgCineP1,_lblCineP1)=MkTog(P,"FOCUS P1",fx,      cy,fw,fh,true, ()=>SetCineFocus(true), C_BLUE,  C_BLUE_DIM,  10.5f);
            (_imgCineP2,_lblCineP2)=MkTog(P,"FOCUS P2",fx+fw+G, cy,fw,fh,false,()=>SetCineFocus(false),C_ORANGE,C_ORANGE_DIM,10.5f);
            cy -= fh+G*0.5f;

            float gw=(PW-PAD*2f-G)*0.55f,iw=(PW-PAD*2f-G)*0.45f,gh=28f;
            (_imgCineGhost,_lblCineGhost)=MkTog(P,"CAM MOVE  ○  OFF",PAD,      cy,gw,gh,false,ToggleCineGhost,    C_CINE,C_CINE_DIM,10f);
            (_imgCineInvis,_lblCineInvis)=MkTog(P,"INVISIBLE",        PAD+gw+G, cy,iw,gh,false,ToggleCineInvisible,C_CINE,C_CINE_DIM,10f);
            cy -= gh+G*0.5f;

            _txtCineSpeed=T(P,"CineSpdLbl",$"Speed  <b>{_cineGhostSpeed:F0}</b>  <color=#{H(C_TEXT_DIM)}>(Shift×3  Scroll±2)</color>",
                PAD+2f,cy-1f,195f,LH,9f,C_TEXT,TextAlignmentOptions.Left,new Vector2(0f,0.5f));
            _cineSpeedSlider=MkSlider(P,"CineSpd",PAD+200f,cy,PW-PAD*2f-204f,LH,
                GHOST_SPD_MIN,GHOST_SPD_MAX,_cineGhostSpeed,v=>{_cineGhostSpeed=v;UpdateCineSpeedUI();});
            cy -= LH+G; Sep(P,cy);
        }

        // GHOST
        {
            cy -= G; SecLblColor(P,"GHOST MODE",cy,C_GHOST); cy -= 14f;
            float hbH=17f;
            MkImg(P,"GhostHelpBg",PAD,cy,PW-PAD*2f,hbH,C_GHOST_DARK,new Vector2(0f,1f));
            T(P,"GhostHelp","<color=#9977cc>WASD</color> เดิน  <color=#9977cc>Q/E</color> ขึ้น/ลง  <color=#9977cc>SHIFT</color> เร็ว  <color=#9977cc>RMB</color> มุมมอง  <color=#55cc88>★ ตัวละครตาม</color>",
              PAD+6f,cy-2f,PW-PAD*2f-8f,hbH,7.5f,C_GHOST,TextAlignmentOptions.Left,new Vector2(0f,1f));
            cy -= hbH+G*0.5f;

            float GW=185f,IW=PW-PAD*2f-G-185f,BTH=32f,bx=PAD;
            (_imgGhost,    _lblGhost)    =MkTog(P,"GHOST  ○  OFF",bx,      cy,GW, BTH,false,ToggleGhost,    C_GHOST,C_GHOST_DIM,10.5f);
            (_imgInvisible,_lblInvisible)=MkTog(P,"INVISIBLE  ✓", bx+GW+G, cy,IW, BTH,true, ToggleInvisible,C_GHOST,C_GHOST_DIM,10f);
            cy -= BTH+G*0.5f;

            _txtGhostSpeed=T(P,"GhostSpdLbl",$"Speed  <b>{_ghostSpeed:F0}</b>  <color=#{H(C_TEXT_DIM)}>(Shift×3  Scroll±1)</color>",
                PAD+2f,cy-1f,195f,LH,9f,C_TEXT,TextAlignmentOptions.Left,new Vector2(0f,0.5f));
            _ghostSlider=MkSlider(P,"GhostSpd",PAD+200f,cy,PW-PAD*2f-204f,LH,
                GHOST_SPD_MIN,GHOST_SPD_MAX,_ghostSpeed,SetGhostSpeedFromSlider);
            cy -= LH+G; Sep(P,cy);
        }

        // ACTIONS
        {
            cy -= G; SecLbl(P,"ACTIONS",cy); cy -= 14f;
            float bw=(PW-PAD*2f-G*3f)/4f,bh=28f,bx=PAD;
            MkBtn(P,"TP P1→P2",bx,          cy,bw,bh,C_BLUE_DIM, TpP1P2);
            MkBtn(P,"TP P2→P1",bx+bw+G,     cy,bw,bh,C_BLUE_DIM, TpP2P1);
            MkBtn(P,"HEAL ALL", bx+2*(bw+G), cy,bw,bh,C_GREEN_DIM,HealAll);
            MkBtn(P,"RELOAD",   bx+3*(bw+G), cy,bw,bh,C_RED_DIM,  Reload);
            cy -= bh+G*0.6f;
            MkBtn(P,"KEY→P1",  bx,           cy,bw,bh,C_GOLD_DARK,KeyP1);
            MkBtn(P,"KEY→P2",  bx+bw+G,      cy,bw,bh,C_GOLD_DARK,KeyP2);
            MkBtn(P,"RESP P1", bx+2*(bw+G),  cy,bw,bh,C_RED_DIM,  RespP1);
            MkBtn(P,"RESP P2", bx+3*(bw+G),  cy,bw,bh,C_RED_DIM,  RespP2);
            cy -= bh+G; Sep(P,cy);
        }

        // TIME & SPEED
        {
            cy -= G; SecLbl(P,"TIME & SPEED",cy); cy -= 14f;
            float lx=PAD+2f,sw=PW-PAD*2f-122f;
            _txtTimeScale=T(P,"TsLbl",$"Time Scale  <b>{_timeScale:F2}x</b>",lx,cy-1f,115f,LH,9.5f,C_TEXT,TextAlignmentOptions.Left,new Vector2(0f,0.5f));
            _timeSlider=MkSlider(P,"TimeSlider",lx+120f,cy,sw,LH,0.05f,3f,_timeScale,SetTimeScale);
            cy -= LH+G*0.6f;
            _txtSpeedMult=T(P,"SpdLbl",$"Walk Speed  <b>{_speedMult:F1}x</b>",lx,cy-1f,115f,LH,9.5f,C_TEXT,TextAlignmentOptions.Left,new Vector2(0f,0.5f));
            _speedSlider=MkSlider(P,"SpeedSlider",lx+120f,cy,sw,LH,0.5f,5f,_speedMult,SetSpeedMult);
        }

        RefreshCineUI();
    }

    // ══════════════════════════════════════════════════════
    //  UI Primitives
    // ══════════════════════════════════════════════════════

    static GameObject MkGO(string n,Transform p){var g=new GameObject(n);g.transform.SetParent(p,false);return g;}
    static void FullStr(GameObject go){var rt=go.AddComponent<RectTransform>();rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}

    Image MkImg(Transform p,string n,float x,float y,float w,float h,Color col,Vector2? piv=null)
    {var go=MkGO(n,p);var rt=go.AddComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=piv??new Vector2(0f,1f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);var img=go.AddComponent<Image>();img.color=col;return img;}

    TextMeshProUGUI T(Transform p,string n,string txt,float x,float y,float w,float h,float size,Color col,TextAlignmentOptions align,Vector2? piv=null)
    {var go=MkGO(n,p);var rt=go.AddComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=piv??new Vector2(0f,1f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);var tmp=go.AddComponent<TextMeshProUGUI>();tmp.text=txt;tmp.fontSize=size;tmp.color=col;tmp.richText=true;tmp.enableWordWrapping=false;tmp.alignment=align;return tmp;}

    void SecLbl(Transform p,string txt,float cy)=>SecLblColor(p,txt,cy,C_GOLD);
    void SecLblColor(Transform p,string txt,float cy,Color col){var t=T(p,"Sec_"+txt,txt,PAD+6f,cy,240f,13f,8.5f,col,TextAlignmentOptions.Left,new Vector2(0f,1f));t.fontStyle=FontStyles.Bold;t.characterSpacing=1.5f;}
    void Sep(Transform p,float y)=>MkImg(p,"Sep",0f,y,PW,1f,C_SEP,new Vector2(0f,0.5f));
    void Stripe(Transform p,float w,float h,Color col,float x,float y)=>MkImg(p,"Stripe",x,y,w,h,col,new Vector2(0f,1f));

    void InfoPair(Transform p,string lbl1,string lbl2,float x,float y,float cw,out TextMeshProUGUI v1,out TextMeshProUGUI v2,Color c1,Color c2)
    {T(p,lbl1+"_L",lbl1,x,y,50f,LH-2f,8.5f,C_TEXT_DIM,TextAlignmentOptions.Left,new Vector2(0f,1f));v1=T(p,lbl1+"_V","—",x+52f,y,cw-52f,LH-2f,8.5f,c1,TextAlignmentOptions.Left,new Vector2(0f,1f));T(p,lbl2+"_L",lbl2,x,y-LH,50f,LH-2f,8.5f,C_TEXT_DIM,TextAlignmentOptions.Left,new Vector2(0f,1f));v2=T(p,lbl2+"_V","—",x+52f,y-LH,cw-52f,LH-2f,8.5f,c2,TextAlignmentOptions.Left,new Vector2(0f,1f));}

    void MkBtn(Transform parent,string label,float x,float y,float w,float h,Color col,System.Action onClick,float fs=9.5f)
    {var go=MkGO("Btn_"+label,parent);var rt=go.AddComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0f,1f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);var img=go.AddComponent<Image>();img.color=col;var btn=go.AddComponent<Button>();btn.targetGraphic=img;var cs=btn.colors;cs.normalColor=col;cs.highlightedColor=Color.Lerp(col,C_TEXT,0.25f);cs.pressedColor=Color.Lerp(col,Color.black,0.3f);cs.fadeDuration=0.06f;btn.colors=cs;btn.onClick.AddListener(()=>onClick?.Invoke());EdgeLine(go.transform,C_GOLD_DIM,true);var lGO=MkGO("Lbl",go.transform);var lRT=lGO.AddComponent<RectTransform>();lRT.anchorMin=Vector2.zero;lRT.anchorMax=Vector2.one;lRT.offsetMin=lRT.offsetMax=Vector2.zero;var tmp=lGO.AddComponent<TextMeshProUGUI>();tmp.text=label;tmp.fontSize=fs;tmp.color=C_TEXT;tmp.fontStyle=FontStyles.Bold;tmp.alignment=TextAlignmentOptions.Center;tmp.richText=true;}

    (Image img,TextMeshProUGUI lbl) MkTog(Transform parent,string label,float x,float y,float w,float h,bool state,System.Action onClick,Color onCol,Color offCol,float fs=10.5f)
    {var go=MkGO("Tog_"+label,parent);var rt=go.AddComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0f,1f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);Color bg=state?new Color(onCol.r*0.22f,onCol.g*0.22f,onCol.b*0.22f,1f):C_MID;var img=go.AddComponent<Image>();img.color=bg;var btn=go.AddComponent<Button>();btn.targetGraphic=img;var cs=btn.colors;cs.normalColor=bg;cs.highlightedColor=Color.Lerp(bg,C_TEXT,0.14f);cs.pressedColor=Color.Lerp(bg,Color.black,0.22f);cs.fadeDuration=0.06f;btn.colors=cs;btn.onClick.AddListener(()=>onClick?.Invoke());EdgeLine(go.transform,state?onCol:offCol,true);var bar=MkGO("Bar",go.transform);var bRT=bar.AddComponent<RectTransform>();bRT.anchorMin=new Vector2(0f,0f);bRT.anchorMax=new Vector2(0f,1f);bRT.pivot=new Vector2(0f,0.5f);bRT.anchoredPosition=Vector2.zero;bRT.sizeDelta=new Vector2(3f,0f);bar.AddComponent<Image>().color=state?onCol:offCol;var lGO=MkGO("Lbl",go.transform);var lRT=lGO.AddComponent<RectTransform>();lRT.anchorMin=Vector2.zero;lRT.anchorMax=Vector2.one;lRT.offsetMin=new Vector2(6f,0f);lRT.offsetMax=Vector2.zero;var lbl=lGO.AddComponent<TextMeshProUGUI>();lbl.text=label;lbl.fontSize=fs;lbl.color=state?Color.white:C_TEXT_DIM;lbl.fontStyle=FontStyles.Bold;lbl.alignment=TextAlignmentOptions.Center;lbl.richText=true;return(img,lbl);}

    void SetTog(Image img,TextMeshProUGUI lbl,bool state,string newLabel,Color onCol,Color offCol)
    {if(img==null)return;Color bg=state?new Color(onCol.r*0.22f,onCol.g*0.22f,onCol.b*0.22f,1f):C_MID;img.color=bg;var top=img.transform.Find("TopLine")?.GetComponent<Image>();if(top)top.color=state?onCol:offCol;var bar=img.transform.Find("Bar")?.GetComponent<Image>();if(bar)bar.color=state?onCol:offCol;if(lbl!=null){lbl.text=newLabel;lbl.color=state?Color.white:C_TEXT_DIM;}}

    static void EdgeLine(Transform parent,Color col,bool top)
    {var go=MkGO("TopLine",parent);var rt=go.AddComponent<RectTransform>();rt.anchorMin=new Vector2(0f,top?1f:0f);rt.anchorMax=new Vector2(1f,top?1f:0f);rt.pivot=new Vector2(0.5f,top?1f:0f);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(0f,1.5f);go.AddComponent<Image>().color=col;}

    Slider MkSlider(Transform p,string name,float x,float y,float w,float h,float min,float max,float val,System.Action<float> onChange)
    {var go=MkGO(name,p);var rt=go.AddComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0f,0.5f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);var s=go.AddComponent<Slider>();s.minValue=min;s.maxValue=max;s.value=val;s.direction=Slider.Direction.LeftToRight;var bgGO=MkGO("BG",go.transform);var bgRT=bgGO.AddComponent<RectTransform>();bgRT.anchorMin=new Vector2(0f,0.3f);bgRT.anchorMax=new Vector2(1f,0.7f);bgRT.offsetMin=bgRT.offsetMax=Vector2.zero;bgGO.AddComponent<Image>().color=C_LITE;var faGO=MkGO("FillArea",go.transform);var faRT=faGO.AddComponent<RectTransform>();faRT.anchorMin=new Vector2(0f,0.3f);faRT.anchorMax=new Vector2(1f,0.7f);faRT.offsetMin=faRT.offsetMax=Vector2.zero;var fGO=MkGO("Fill",faGO.transform);var fRT=fGO.AddComponent<RectTransform>();fRT.anchorMin=Vector2.zero;fRT.anchorMax=Vector2.one;fRT.offsetMin=fRT.offsetMax=Vector2.zero;fGO.AddComponent<Image>().color=C_GOLD;var haGO=MkGO("HandleArea",go.transform);var haRT=haGO.AddComponent<RectTransform>();haRT.anchorMin=Vector2.zero;haRT.anchorMax=Vector2.one;haRT.offsetMin=haRT.offsetMax=Vector2.zero;var hGO=MkGO("Handle",haGO.transform);var hRT=hGO.AddComponent<RectTransform>();hRT.sizeDelta=new Vector2(12f,12f);var hImg=hGO.AddComponent<Image>();hImg.color=C_TEXT;s.fillRect=fRT;s.handleRect=hRT;s.targetGraphic=hImg;s.onValueChanged.AddListener(v=>onChange?.Invoke(v));return s;}

    static string H(Color c)=>ColorUtility.ToHtmlStringRGB(c);
}
