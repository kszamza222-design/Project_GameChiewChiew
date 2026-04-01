using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// HealthSystem — UI วงกลม Portrait + Arc HP สไตล์ RPG น้ำตาล-ทอง
/// Portrait วงกลม Mask + Radial Fill HP Arc
///
/// Setup:
///   1. ผูก targetCanvas, player1, player2
///   2. ผูก portraitP1, portraitP2 (Sprite)
///   3. ปรับ hudOffsetP1/P2
/// Test: F1=P1ลดHP  F2=P2ลดHP  F3=P1Heal  F4=P2Heal
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("── Canvas ───────────────────────────")]
    public Canvas targetCanvas;

    [Header("── Players ─────────────────────────")]
    public PlayerController player1;
    public PlayerController player2;

    [Header("── Portrait Sprites ───────────────")]
    public Sprite portraitP1;
    public Sprite portraitP2;

    [Header("── HP Settings ─────────────────────")]
    public int   maxHP          = 100;
    public float regenDelay     = 4f;
    public float regenPerSecond = 5f;

    [Header("── HUD Position ────────────────────")]
    public Vector2 hudOffsetP1 = new Vector2(16f, -16f);
    public Vector2 hudOffsetP2 = new Vector2(16f, -16f);

    [Header("── Test Keys ───────────────────────")]
    public bool enableTestKeys = true;
    public int  testDamage     = 15;

    // ═══════════════════════════════════════════
    //  Palette — เข้ม น้ำตาล-ทอง RPG
    // ═══════════════════════════════════════════
    static readonly Color ColPanelDark  = new Color(0.12f, 0.08f, 0.05f, 0.95f);
    static readonly Color ColPanelBdr   = new Color(0.75f, 0.50f, 0.10f, 1.00f);
    static readonly Color ColPanelInner = new Color(0.20f, 0.13f, 0.07f, 0.92f);

    static readonly Color ColArcBg      = new Color(0.10f, 0.06f, 0.03f, 1.00f);
    static readonly Color ColArcFull    = new Color(0.20f, 0.88f, 0.35f, 1.00f);  // เขียวสด
    static readonly Color ColArcMid     = new Color(0.98f, 0.80f, 0.10f, 1.00f);  // เหลืองทอง
    static readonly Color ColArcLow     = new Color(0.95f, 0.18f, 0.18f, 1.00f);  // แดง
    static readonly Color ColArcDrain   = new Color(0.90f, 0.45f, 0.05f, 0.65f);  // ส้ม drain
    static readonly Color ColArcGlow    = new Color(1.00f, 0.85f, 0.30f, 0.25f);  // ทองเรือง

    static readonly Color ColRingOuter  = new Color(0.85f, 0.60f, 0.10f, 1.00f);  // ทองเข้ม
    static readonly Color ColRingInner  = new Color(0.55f, 0.35f, 0.05f, 1.00f);  // น้ำตาลทอง
    static readonly Color ColPortBg     = new Color(0.15f, 0.10f, 0.06f, 1.00f);

    static readonly Color ColNameP1     = new Color(0.40f, 0.80f, 1.00f, 1.00f);  // ฟ้า
    static readonly Color ColNameP2     = new Color(0.98f, 0.75f, 0.25f, 1.00f);  // ทอง
    static readonly Color ColHpNum      = new Color(1.00f, 0.95f, 0.80f, 1.00f);  // ขาวครีม
    static readonly Color ColHpLabel    = new Color(0.80f, 0.60f, 0.30f, 0.85f);  // น้ำตาลอ่อน
    static readonly Color ColFlash      = new Color(1.00f, 0.05f, 0.05f, 0.50f);

    // ── ขนาด ────────────────────────────────────
    const float CIRC  = 76f;
    const float ARCW  = 10f;
    const float PANW  = 185f;
    const float PANH  = 90f;

    // ── State ────────────────────────────────────
    float _hp1, _hp2, _drain1, _drain2, _regen1, _regen2;
    RectTransform   _rt1, _rt2;
    Image           _fill1, _fill2, _drainImg1, _drainImg2, _flash1, _flash2;
    TextMeshProUGUI _txt1, _txt2;

    Sprite _circleSpr;

    // ════════════════════════════════════════════
    void Awake()
    {
        _hp1 = _drain1 = _hp2 = _drain2 = maxHP;
        if (!targetCanvas) { Debug.LogError("[HP] ไม่มี Canvas"); return; }

        // สร้าง circle sprite ครั้งเดียว ใช้ซ้ำ
        _circleSpr = BuildCircleSprite(128);

        BuildHUD(true,  out _rt1, out _fill1, out _drainImg1, out _flash1, out _txt1);
        BuildHUD(false, out _rt2, out _fill2, out _drainImg2, out _flash2, out _txt2);
        Refresh(true); Refresh(false);
    }

    void Update()
    {
        if (_rt1) _rt1.anchoredPosition = hudOffsetP1;
        if (_rt2) _rt2.anchoredPosition = hudOffsetP2;

        _drain1 = Mathf.MoveTowards(_drain1, _hp1, 20f * Time.deltaTime);
        _drain2 = Mathf.MoveTowards(_drain2, _hp2, 20f * Time.deltaTime);
        if (_drainImg1) _drainImg1.fillAmount = _drain1 / maxHP;
        if (_drainImg2) _drainImg2.fillAmount = _drain2 / maxHP;

        Regen(ref _hp1, ref _regen1, true);
        Regen(ref _hp2, ref _regen2, false);

        if (!enableTestKeys) return;
        if (Input.GetKeyDown(KeyCode.F1)) TakeDamage(true,  testDamage);
        if (Input.GetKeyDown(KeyCode.F2)) TakeDamage(false, testDamage);
        if (Input.GetKeyDown(KeyCode.F3)) Heal(true,  testDamage);
        if (Input.GetKeyDown(KeyCode.F4)) Heal(false, testDamage);
    }

    void Regen(ref float hp, ref float timer, bool p1)
    {
        if (hp >= maxHP) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        hp = Mathf.Min(maxHP, hp + regenPerSecond * Time.deltaTime);
        Refresh(p1);
    }

    // ── Public API ───────────────────────────────
    public void TakeDamage(bool p1, int amt)
    {
        if (p1) { _hp1=Mathf.Max(0,_hp1-amt); _regen1=regenDelay; Refresh(true);  StartCoroutine(DoFlash(_flash1)); if(_hp1<=0)Die(true);  }
        else    { _hp2=Mathf.Max(0,_hp2-amt); _regen2=regenDelay; Refresh(false); StartCoroutine(DoFlash(_flash2)); if(_hp2<=0)Die(false); }
    }
    public void Heal(bool p1, int amt)
    {
        if (p1) { _hp1=Mathf.Min(maxHP,_hp1+amt); Refresh(true);  }
        else    { _hp2=Mathf.Min(maxHP,_hp2+amt); Refresh(false); }
    }
    public void ResetHP(bool p1)
    {
        if (p1) { _hp1=_drain1=maxHP; Refresh(true);  }
        else    { _hp2=_drain2=maxHP; Refresh(false); }
    }
    public int GetHP(bool p1) => p1?(int)_hp1:(int)_hp2;

    void Die(bool p1) { ResetHP(p1); CheckpointManager.Instance?.TriggerRespawn(p1); }

    void Refresh(bool p1)
    {
        float hp   = p1 ? _hp1 : _hp2;
        var   fill = p1 ? _fill1 : _fill2;
        var   txt  = p1 ? _txt1  : _txt2;
        float r    = hp / maxHP;
        if (fill != null) { fill.fillAmount = r; fill.color = r>0.55f?ColArcFull:r>0.28f?ColArcMid:ColArcLow; }
        if (txt  != null) txt.text = $"{(int)hp}<size=65%>/{maxHP}</size>";
    }

    IEnumerator DoFlash(Image img)
    {
        if (!img) yield break;
        img.gameObject.SetActive(true);
        for (float t=0; t<0.3f; t+=Time.deltaTime)
        { img.color=new Color(1,.05f,.05f,Mathf.Lerp(.5f,0,t/.3f)); yield return null; }
        img.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════
    //  BuildHUD
    // ════════════════════════════════════════════
    void BuildHUD(bool isLeft,
                  out RectTransform    hudRt,
                  out Image            arcFill,
                  out Image            arcDrain,
                  out Image            flash,
                  out TextMeshProUGUI  hpTxt)
    {
        arcFill=arcDrain=flash=null; hpTxt=null;
        string s    = isLeft?"P1":"P2";
        Color  ncol = isLeft?ColNameP1:ColNameP2;
        string name = isLeft?"PLAYER 1":"PLAYER 2";
        Sprite port = isLeft?portraitP1:portraitP2;

        // ── Root ────────────────────────────────
        var root = new GameObject("_HP_"+s, typeof(RectTransform));
        root.transform.SetParent(targetCanvas.transform, false);
        hudRt = root.GetComponent<RectTransform>();
        hudRt.anchorMin = hudRt.anchorMax = isLeft ? new Vector2(0f,1f) : new Vector2(.5f,1f);
        hudRt.pivot     = new Vector2(0f,1f);
        hudRt.anchoredPosition = isLeft?hudOffsetP1:hudOffsetP2;
        hudRt.sizeDelta = new Vector2(PANW, PANH);
        Transform t = root.transform;

        // ── Panel (outer border → inner bg) ─────
        SI(t,"Bdr",  0f,0f, PANW, PANH, ColPanelBdr);
        SI(t,"Bg",   1.5f,-1.5f, PANW-3f, PANH-3f, ColPanelDark);
        SI(t,"BgIn", 3f,-3f, PANW-6f, PANH-6f, ColPanelInner);

        // ── Arc + Portrait ───────────────────────
        float cx = CIRC/2f + 10f;
        float cy = -PANH/2f;
        float D  = CIRC + ARCW*2f + 6f; // เส้นผ่านศูนย์กลาง arc

        // Arc background
        SC(t,"ArcBg", cx,cy, D,D, ColArcBg, _circleSpr);

        // Drain arc (ส้ม ลดช้า)
        arcDrain = SCArc(t,"ArcDrain", cx,cy, D,D, ColArcDrain);

        // HP arc (ลดเร็ว เปลี่ยนสี)
        arcFill = SCArc(t,"ArcFill", cx,cy, D,D, ColArcFull);

        // Glow ring (ทองเรือง얇)
        SC(t,"Glow", cx,cy, D+4f,D+4f, ColArcGlow, _circleSpr);

        // ── Portrait: วิธีที่ได้ผล 100% ─────────
        // ใช้ RawImage + RenderTexture ไม่ได้ใน runtime แบบนี้
        // ใช้วิธี: สร้าง parent empty → ใส่ Mask → ใส่ Image ลูก
        //
        // Parent (Mask holder) — ต้องมี Image component ด้วย
        var maskHolder = new GameObject("MaskHolder", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskHolder.transform.SetParent(t, false);
        var mhRt = maskHolder.GetComponent<RectTransform>();
        mhRt.anchorMin = mhRt.anchorMax = new Vector2(0f,1f);
        mhRt.pivot     = new Vector2(.5f,.5f);
        mhRt.anchoredPosition = new Vector2(cx, cy);
        mhRt.sizeDelta = new Vector2(CIRC, CIRC);
        // Image บน MaskHolder ต้องเป็น circle sprite และ showMaskGraphic=false
        var mhImg = maskHolder.GetComponent<Image>();
        mhImg.sprite          = _circleSpr;
        mhImg.color           = Color.white;
        mhImg.type            = Image.Type.Simple;
        mhImg.raycastTarget   = false;
        maskHolder.GetComponent<Mask>().showMaskGraphic = false;

        // Child 1: พื้นหลัง (ใน mask)
        var portBg = new GameObject("PortBg", typeof(RectTransform), typeof(Image));
        portBg.transform.SetParent(maskHolder.transform, false);
        var pbRt = portBg.GetComponent<RectTransform>();
        pbRt.anchorMin = Vector2.zero; pbRt.anchorMax = Vector2.one;
        pbRt.offsetMin = pbRt.offsetMax = Vector2.zero;
        portBg.GetComponent<Image>().color = ColPortBg;

        // Child 2: Portrait รูปตัวละคร (ใน mask) ← จะถูก clip เป็นวงกลมโดย Mask
        var portImg = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portImg.transform.SetParent(maskHolder.transform, false);
        var piRt = portImg.GetComponent<RectTransform>();
        piRt.anchorMin = Vector2.zero; piRt.anchorMax = Vector2.one;
        piRt.offsetMin = piRt.offsetMax = Vector2.zero;
        var piComp = portImg.GetComponent<Image>();
        piComp.raycastTarget = false;
        if (port != null)
        {
            piComp.sprite         = port;
            piComp.preserveAspect = true;
            piComp.color          = Color.white;
        }
        else piComp.color = new Color(.25f,.18f,.10f,1f);

        // Child 3: Flash (ใน mask)
        var flashGo = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        flashGo.transform.SetParent(maskHolder.transform, false);
        var flRt = flashGo.GetComponent<RectTransform>();
        flRt.anchorMin = Vector2.zero; flRt.anchorMax = Vector2.one;
        flRt.offsetMin = flRt.offsetMax = Vector2.zero;
        flash = flashGo.GetComponent<Image>();
        flash.color = ColFlash;
        flashGo.SetActive(false);

        // วงแหวนทอง (ชั้นบนสุด ล้อม mask)
        Ring(t,"RO", cx,cy, CIRC+8f,  3.5f, ColRingOuter);
        Ring(t,"RI", cx,cy, CIRC+14f, 2f,   ColRingInner);

        // ── Text ─────────────────────────────────
        float tx = cx + CIRC/2f + ARCW + 14f;
        float tw = PANW - tx - 6f;

        TMP(t,"Name", name, tx,-10f, tw,22f, 13f, ncol, FontStyles.Bold);
        hpTxt = TMP(t,"Hp", $"{maxHP}<size=65%>/{maxHP}</size>", tx,-33f, tw,26f, 19f, ColHpNum, FontStyles.Bold);
        TMP(t,"Lbl",  "HP",  tx,-58f, tw,16f, 10f, ColHpLabel, FontStyles.Normal);
    }

    // ════════════════════════════════════════════
    //  Image Helpers
    // ════════════════════════════════════════════

    // Stretch Image (pivot ซ้ายบน)
    Image SI(Transform p, string n, float x, float y, float w, float h, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(p, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f,1f);
        rt.pivot     = new Vector2(0f,1f);
        rt.anchoredPosition = new Vector2(x,-y);
        rt.sizeDelta = new Vector2(w,h);
        var img = go.GetComponent<Image>(); img.color=c; img.raycastTarget=false;
        return img;
    }

    // Circle Image (pivot center)
    Image SC(Transform p, string n, float cx, float cy, float w, float h, Color c, Sprite spr=null)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(p, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f,1f);
        rt.pivot     = new Vector2(.5f,.5f);
        rt.anchoredPosition = new Vector2(cx,cy);
        rt.sizeDelta = new Vector2(w,h);
        var img = go.GetComponent<Image>();
        img.color=c; img.raycastTarget=false;
        if (spr!=null) { img.sprite=spr; img.type=Image.Type.Simple; }
        return img;
    }

    // Radial Arc Image
    Image SCArc(Transform p, string n, float cx, float cy, float w, float h, Color c)
    {
        var img = SC(p,n,cx,cy,w,h,c,_circleSpr);
        img.type          = Image.Type.Filled;
        img.fillMethod    = Image.FillMethod.Radial360;
        img.fillOrigin    = (int)Image.Origin360.Top;
        img.fillClockwise = false;
        img.fillAmount    = 1f;
        return img;
    }

    // วงแหวนขอบ (ทอง)
    void Ring(Transform p, string n, float cx, float cy, float d, float thick, Color c)
    {
        SC(p,n+"_o",cx,cy, d+thick*2f, d+thick*2f, c, _circleSpr);
        SC(p,n+"_i",cx,cy, d,          d,          ColPanelInner, _circleSpr);
    }

    // ════════════════════════════════════════════
    //  TMP Helper
    // ════════════════════════════════════════════
    TextMeshProUGUI TMP(Transform p, string n, string text,
                        float x, float y, float w, float h,
                        float sz, Color c, FontStyles fs)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(p, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f,1f);
        rt.pivot     = new Vector2(0f,1f);
        rt.anchoredPosition = new Vector2(x,y);
        rt.sizeDelta = new Vector2(w,h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text; tmp.fontSize=sz; tmp.fontStyle=fs;
        tmp.alignment = TextAlignmentOptions.Left; tmp.color=c;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        return tmp;
    }

    // ════════════════════════════════════════════
    //  Circle Sprite
    // ════════════════════════════════════════════
    Sprite BuildCircleSprite(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[res*res];
        float h2 = res*.5f, r2 = (h2-1f)*(h2-1f);
        for (int y=0;y<res;y++) for (int x=0;x<res;x++)
        {
            float dx=x-h2+.5f, dy=y-h2+.5f;
            px[y*res+x] = new Color32(255,255,255, dx*dx+dy*dy<=r2?(byte)255:(byte)0);
        }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,res,res),new Vector2(.5f,.5f),res);
    }
}
