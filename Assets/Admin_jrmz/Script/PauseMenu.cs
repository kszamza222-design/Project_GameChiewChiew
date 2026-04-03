using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// PauseMenu — กด ESC หยุดเกม / Resume / Restart / Quit + ปรับ Volume
///
/// วิธีใช้:
///   1. วาง Script นี้บน GameManager หรือ Empty GameObject
///   2. ผูก targetCanvas (Screen Space Overlay)
///   3. ผูก splitScreenManager (เพื่อซ่อนเส้นกลางตอน Pause)
///   4. ผูก soundIconSprite, musicIconSprite (ไม่บังคับ)
///   5. ปรับตำแหน่ง/ขนาดผ่าน Inspector ได้ทุกค่า
/// </summary>
public class PauseMenu : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── References ──────────────────────")]
    public Canvas             targetCanvas;
    public SplitScreenManager splitScreenManager;

    [Header("── Scene ───────────────────────────")]
    [Tooltip("ชื่อ Scene ที่จะโหลดเมื่อกด Restart")]
    public string restartSceneName = "";
    [Tooltip("ชื่อ Scene Main Menu (กด Quit)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("── Icons (ไม่บังคับ) ────────────────")]
    [Tooltip("รูปไอคอนเสียง SFX")]
    public Sprite soundIconSprite;
    [Tooltip("รูปไอคอนเพลง BGM")]
    public Sprite musicIconSprite;
    [Tooltip("รูปไอคอน Resume")]
    public Sprite resumeIconSprite;
    [Tooltip("รูปไอคอน Restart")]
    public Sprite restartIconSprite;
    [Tooltip("รูปไอคอน Quit")]
    public Sprite quitIconSprite;

    [Header("── Panel Position & Size ───────────")]
    [Tooltip("ตำแหน่ง Panel กลางจอ (anchoredPosition)")]
    public Vector2 panelPosition = Vector2.zero;
    [Tooltip("ขนาด Panel")]
    public Vector2 panelSize     = new Vector2(420f, 560f);

    [Header("── Button Size ─────────────────────")]
    [Tooltip("ขนาดปุ่ม Resume / Restart / Quit")]
    public Vector2 buttonSize    = new Vector2(320f, 58f);
    [Tooltip("ระยะห่างระหว่างปุ่ม")]
    public float   buttonSpacing = 14f;

    [Header("── Slider Size ─────────────────────")]
    [Tooltip("ขนาด Slider Volume")]
    public Vector2 sliderSize    = new Vector2(280f, 28f);
    [Tooltip("ขนาด Icon เสียง")]
    public Vector2 iconSize      = new Vector2(32f, 32f);

    [Header("── Title ────────────────────────────")]
    [Tooltip("ขนาด Font หัวข้อ PAUSED")]
    public float   titleFontSize = 36f;
    [Tooltip("ขนาด Font ปุ่ม")]
    public float   btnFontSize   = 18f;
    [Tooltip("ขนาด Font Label")]
    public float   labelFontSize = 13f;

    // ═══════════════════════════════════════════════════
    //  Colors
    // ═══════════════════════════════════════════════════

    [Header("── Colors ───────────────────────────")]
    public Color colOverlay   = new Color(0f,    0f,    0f,    0.65f);
    public Color colPanel     = new Color(0.08f, 0.06f, 0.04f, 0.98f);
    public Color colBorder    = new Color(0.85f, 0.58f, 0.08f, 1.00f);
    public Color colInner     = new Color(0.14f, 0.10f, 0.06f, 0.96f);
    public Color colTitle     = new Color(1.00f, 0.85f, 0.30f, 1.00f);
    public Color colBtnNormal = new Color(0.20f, 0.15f, 0.08f, 1.00f);
    public Color colBtnHover  = new Color(0.85f, 0.58f, 0.08f, 1.00f);
    public Color colBtnDanger = new Color(0.55f, 0.10f, 0.08f, 1.00f);
    public Color colBtnText   = new Color(1.00f, 0.95f, 0.80f, 1.00f);
    public Color colSep       = new Color(0.85f, 0.58f, 0.08f, 0.45f);
    public Color colSliderBg  = new Color(0.12f, 0.08f, 0.04f, 1.00f);
    public Color colSliderFg  = new Color(0.85f, 0.62f, 0.10f, 1.00f);
    public Color colLabel     = new Color(0.80f, 0.72f, 0.55f, 1.00f);
    public Color colAccent    = new Color(0.94f, 0.62f, 0.15f, 1.00f);

    // ═══════════════════════════════════════════════════
    //  State
    // ═══════════════════════════════════════════════════

    bool       _isPaused = false;
    GameObject _overlay;
    GameObject _panel;

    RectTransform _panelRt;

    // Sliders
    Slider _bgmSlider;
    Slider _sfxSlider;

    // Divider line reference (จาก SplitScreenManager)
    bool _dividerWasVisible = true;

    Sprite _circleSprite;

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        if (targetCanvas == null) { Debug.LogError("[PauseMenu] ไม่มี Canvas!"); return; }
        _circleSprite = MakeCircle(64);
        BuildUI();
    }

    void Start()
    {
        // ค่า Scene ปัจจุบัน (ถ้าไม่ได้กำหนด)
        if (string.IsNullOrEmpty(restartSceneName))
            restartSceneName = SceneManager.GetActiveScene().name;

        // Sync slider กับ SoundManager
        StartCoroutine(SyncSlidersDelayed());
    }

    IEnumerator SyncSlidersDelayed()
    {
        yield return null; // รอ 1 frame ให้ SoundManager Awake ก่อน
        if (SoundManager.Instance != null)
        {
            if (_bgmSlider) _bgmSlider.value = SoundManager.Instance.GetBGMVolume();
            if (_sfxSlider) _sfxSlider.value = SoundManager.Instance.GetSFXVolume();
        }
    }

    // ═══════════════════════════════════════════════════
    //  Update — ตรวจ ESC
    // ═══════════════════════════════════════════════════

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        // อัปเดตตำแหน่ง Panel real-time
        if (_panelRt != null)
            _panelRt.anchoredPosition = panelPosition;
    }

    // ═══════════════════════════════════════════════════
    //  Toggle Pause
    // ═══════════════════════════════════════════════════

    public void TogglePause()
    {
        if (_isPaused) Resume();
        else           Pause();
    }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        // ซ่อนเส้นแบ่งจอ
        if (splitScreenManager != null)
        {
            _dividerWasVisible = splitScreenManager.showDivider;
            splitScreenManager.showDivider = false;
        }

        if (_overlay) _overlay.SetActive(true);
        if (_panel)   _panel.SetActive(true);

        SoundManager.Instance?.PauseBGM();
        SoundManager.Instance?.PlayPauseOpen();
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        // คืนเส้นแบ่งจอ
        if (splitScreenManager != null)
            splitScreenManager.showDivider = _dividerWasVisible;

        if (_overlay) _overlay.SetActive(false);
        if (_panel)   _panel.SetActive(false);

        SoundManager.Instance?.ResumeBGM();
        SoundManager.Instance?.PlayPauseClose();
    }

    public void Restart()
    {
        SoundManager.Instance?.PlayButton();
        Time.timeScale = 1f;
        SceneManager.LoadScene(restartSceneName);
    }

    public void QuitToMenu()
    {
        SoundManager.Instance?.PlayButton();
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Application.Quit();
    }

    // ═══════════════════════════════════════════════════
    //  Build UI
    // ═══════════════════════════════════════════════════

    void BuildUI()
    {
        Transform root = targetCanvas.transform;

        // ── Overlay (พื้นหลังมืด เต็มจอ) ──────────────
        _overlay = MakeBox(root, "PauseOverlay",
            Vector2.zero, Vector2.zero, colOverlay, stretch: true);
        _overlay.SetActive(false);

        // ── Panel Container ──────────────────────────
        _panel = MakeBox(root, "PausePanel",
            panelPosition, panelSize, Color.clear);
        _panelRt = _panel.GetComponent<RectTransform>();
        _panelRt.anchorMin = _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRt.pivot     = new Vector2(0.5f, 0.5f);
        _panel.SetActive(false);

        var P = _panel.transform;

        // Border
        MkImg(P, "Border", Vector2.zero, panelSize, colBorder);
        // BG
        MkImg(P, "BG",     Vector2.zero, panelSize - new Vector2(4f, 4f), colPanel);
        // Inner
        MkImg(P, "Inner",  Vector2.zero, panelSize - new Vector2(12f, 12f), colInner);

        // ── Accent bar บนสุด ──────────────────────────
        var acH = 6f;
        MkImg(P, "AccentTop",
            new Vector2(0, panelSize.y * 0.5f - acH * 0.5f - 6f),
            new Vector2(panelSize.x - 20f, acH), colAccent);

        // ── Title ─────────────────────────────────────
        var titleY = panelSize.y * 0.5f - 55f;
        MkTMP(P, "Title", "⏸  PAUSED",
            new Vector2(0, titleY), new Vector2(panelSize.x - 30f, 50f),
            titleFontSize, FontStyles.Bold, colTitle, TextAlignmentOptions.Center);

        // Separator
        MkImg(P, "Sep1",
            new Vector2(0, titleY - 36f),
            new Vector2(panelSize.x - 40f, 1.5f), colSep);

        // ── Buttons ───────────────────────────────────
        float btnStartY = titleY - 70f;

        BuildButton(P, "Resume",  resumeIconSprite,  "▶   RESUME",
            new Vector2(0, btnStartY), colBtnNormal, () =>
            {
                SoundManager.Instance?.PlayButton();
                Resume();
            });

        BuildButton(P, "Restart", restartIconSprite, "↺   RESTART",
            new Vector2(0, btnStartY - (buttonSize.y + buttonSpacing)), colBtnNormal, () =>
            {
                SoundManager.Instance?.PlayButton();
                Restart();
            });

        BuildButton(P, "Quit",    quitIconSprite,    "✕   QUIT TO MENU",
            new Vector2(0, btnStartY - (buttonSize.y + buttonSpacing) * 2f), colBtnDanger, () =>
            {
                SoundManager.Instance?.PlayButton();
                QuitToMenu();
            });

        // Separator
        float sepY = btnStartY - (buttonSize.y + buttonSpacing) * 2f - buttonSize.y * 0.5f - 18f;
        MkImg(P, "Sep2",
            new Vector2(0, sepY),
            new Vector2(panelSize.x - 40f, 1.5f), colSep);

        // ── Volume Controls ───────────────────────────
        float volStartY = sepY - 30f;

        _bgmSlider = BuildVolumeRow(P, "BGM",
            musicIconSprite, "BGM",
            new Vector2(0, volStartY),
            0.4f,
            (v) => SoundManager.Instance?.SetBGMVolume(v));

        _sfxSlider = BuildVolumeRow(P, "SFX",
            soundIconSprite, "SFX",
            new Vector2(0, volStartY - 52f),
            0.8f,
            (v) => SoundManager.Instance?.SetSFXVolume(v));

        // ── Version / hint ─────────────────────────────
        MkTMP(P, "Hint", "Press ESC to resume",
            new Vector2(0, -(panelSize.y * 0.5f - 22f)),
            new Vector2(panelSize.x - 30f, 20f),
            10f, FontStyles.Normal,
            new Color(0.55f, 0.48f, 0.35f, 0.7f),
            TextAlignmentOptions.Center);
    }

    // ═══════════════════════════════════════════════════
    //  BuildButton
    // ═══════════════════════════════════════════════════

    void BuildButton(Transform parent, string id, Sprite icon,
                     string label, Vector2 pos, Color bgColor,
                     UnityEngine.Events.UnityAction onClick)
    {
        // Container
        var go = MakeBox(parent, "Btn_" + id, pos, buttonSize, bgColor);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        // Border ปุ่ม
        MkImg(go.transform, "Bdr", Vector2.zero,
            buttonSize + new Vector2(3f, 3f),
            new Color(colBorder.r, colBorder.g, colBorder.b, 0.6f));
        MkImg(go.transform, "Bg", Vector2.zero, buttonSize, bgColor);

        // Accent bar ซ้าย
        MkImg(go.transform, "Accent",
            new Vector2(-(buttonSize.x * 0.5f - 4f), 0),
            new Vector2(4f, buttonSize.y - 8f), colAccent);

        // Icon (ถ้ามี)
        if (icon != null)
        {
            var iGo = MakeBox(go.transform, "Icon",
                new Vector2(-(buttonSize.x * 0.5f - 28f), 0),
                iconSize, Color.white);
            var iImg = iGo.AddComponent<Image>();
            iImg.sprite          = icon;
            iImg.preserveAspect  = true;
            iImg.raycastTarget   = false;
        }

        // Text
        MkTMP(go.transform, "Label", label,
            new Vector2(8f, 0), new Vector2(buttonSize.x - 20f, buttonSize.y),
            btnFontSize, FontStyles.Bold, colBtnText, TextAlignmentOptions.Center);

        // Button Component + hover effect
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.1f, 0.9f);
        colors.pressedColor     = new Color(0.8f, 0.7f, 0.5f);
        colors.fadeDuration     = 0.1f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        // Hover color change on BG
        var bgImg = go.transform.Find("Bg")?.GetComponent<Image>();
        if (bgImg != null)
        {
            btn.targetGraphic = bgImg;
            var btnColors = btn.colors;
            btnColors.normalColor      = bgColor;
            btnColors.highlightedColor = bgColor * 1.4f;
            btnColors.pressedColor     = bgColor * 0.8f;
            btn.colors = btnColors;
        }
    }

    // ═══════════════════════════════════════════════════
    //  BuildVolumeRow — Icon + Label + Slider
    // ═══════════════════════════════════════════════════

    Slider BuildVolumeRow(Transform parent, string id,
                          Sprite icon, string label,
                          Vector2 pos, float initVal,
                          System.Action<float> onChange)
    {
        const float rowH = 44f;
        float rowW = panelSize.x - 40f;

        var row = new GameObject("VolumeRow_" + id, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot     = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = pos;
        rowRt.sizeDelta = new Vector2(rowW, rowH);

        float iconW  = iconSize.x + 4f;
        float labelW = 38f;
        float sliderW = rowW - iconW - labelW - 16f;

        // Icon
        float iconX = -(rowW * 0.5f) + iconW * 0.5f;
        if (icon != null)
        {
            var iGo = MakeBox(row.transform, "Icon",
                new Vector2(iconX, 2f), iconSize, Color.white);
            var iImg = iGo.AddComponent<Image>();
            iImg.sprite         = icon;
            iImg.preserveAspect = true;
            iImg.raycastTarget  = false;
        }
        else
        {
            // ถ้าไม่มี icon ใช้ text แทน
            MkTMP(row.transform, "IconTxt",
                id == "BGM" ? "♪" : "♫",
                new Vector2(iconX, 2f), iconSize,
                iconSize.y * 0.7f, FontStyles.Bold, colAccent,
                TextAlignmentOptions.Center);
        }

        // Label
        float labelX = iconX + iconW * 0.5f + 4f + labelW * 0.5f;
        MkTMP(row.transform, "Label", label,
            new Vector2(labelX, 0),
            new Vector2(labelW, rowH),
            labelFontSize, FontStyles.Bold, colLabel,
            TextAlignmentOptions.Left);

        // Slider
        float sliderX = labelX + labelW * 0.5f + 8f + sliderW * 0.5f;
        var slider = BuildSlider(row.transform, id,
            new Vector2(sliderX, 0), new Vector2(sliderW, sliderSize.y),
            initVal, onChange);

        return slider;
    }

    // ═══════════════════════════════════════════════════
    //  BuildSlider
    // ═══════════════════════════════════════════════════

    Slider BuildSlider(Transform parent, string id,
                       Vector2 pos, Vector2 size,
                       float initVal, System.Action<float> onChange)
    {
        var go = new GameObject("Slider_" + id, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // Background track
        var bgGo = MakeBox(go.transform, "BG",
            Vector2.zero, size, colSliderBg);

        // Fill area
        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.offsetMin = new Vector2(5f, 0f);
        faRt.offsetMax = new Vector2(-5f, 0f);

        var fillGo = MakeBox(fillArea.transform, "Fill",
            Vector2.zero, Vector2.zero, colSliderFg, stretch: true);
        fillGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        // Handle area
        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = haRt.offsetMax = Vector2.zero;

        var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(handleArea.transform, false);
        var hRt = handleGo.GetComponent<RectTransform>();
        hRt.anchorMin = hRt.anchorMax = new Vector2(0f, 0.5f);
        hRt.pivot     = new Vector2(0.5f, 0.5f);
        hRt.sizeDelta = new Vector2(size.y + 8f, size.y + 8f);
        var hImg = handleGo.GetComponent<Image>();
        hImg.sprite = _circleSprite;
        hImg.color  = colBorder;

        // Slider component
        var slider = go.AddComponent<Slider>();
        slider.fillRect   = fillGo.GetComponent<RectTransform>();
        slider.handleRect = handleGo.GetComponent<RectTransform>();
        slider.targetGraphic = hImg;
        slider.direction  = Slider.Direction.LeftToRight;
        slider.minValue   = 0f;
        slider.maxValue   = 1f;
        slider.value      = initVal;

        // Value display
        MkTMP(go.transform, "ValTxt", $"{(int)(initVal * 100)}",
            new Vector2(size.x * 0.5f + 22f, 0),
            new Vector2(36f, size.y + 8f),
            labelFontSize, FontStyles.Bold, colLabel,
            TextAlignmentOptions.Center);

        var valTmp = go.transform.Find("ValTxt")?.GetComponent<TextMeshProUGUI>();

        slider.onValueChanged.AddListener((v) =>
        {
            onChange?.Invoke(v);
            if (valTmp != null) valTmp.text = $"{(int)(v * 100)}";
        });

        return slider;
    }

    // ═══════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════

    GameObject MakeBox(Transform parent, string name,
                       Vector2 pos, Vector2 size,
                       Color color, bool stretch = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();

        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
        go.GetComponent<Image>().color = color;
        return go;
    }

    void MkImg(Transform p, string n, Vector2 pos, Vector2 size, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(p, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = c;
        go.GetComponent<Image>().raycastTarget = false;
    }

    TextMeshProUGUI MkTMP(Transform p, string n, string text,
                          Vector2 pos, Vector2 size, float fontSize,
                          FontStyles style, Color color,
                          TextAlignmentOptions align)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(p, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color     = color;
        tmp.enableWordWrapping   = false;
        tmp.overflowMode         = TextOverflowModes.Overflow;
        tmp.raycastTarget        = false;
        return tmp;
    }

    Sprite MakeCircle(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[res * res];
        float h = res * .5f, r2 = (h - 1f) * (h - 1f);
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = x - h + .5f, dy = y - h + .5f;
                px[y * res + x] = new Color32(255, 255, 255,
                    dx * dx + dy * dy <= r2 ? (byte)255 : (byte)0);
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(.5f, .5f), res);
    }
}
