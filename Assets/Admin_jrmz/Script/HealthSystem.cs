using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// HealthSystem — UI วงกลม Portrait + Arc HP สไตล์ RPG
/// ปรับทุกอย่างได้ใน Inspector
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

    // ═══════════════════════════════════════════
    //  ปรับขนาดได้ทั้งหมด
    // ═══════════════════════════════════════════

    [Header("── Size: Panel ─────────────────────")]
    [Tooltip("ความกว้าง Panel ทั้งหมด")]
    public float panelW = 185f;
    [Tooltip("ความสูง Panel ทั้งหมด")]
    public float panelH = 90f;

    [Header("── Size: Portrait Circle ───────────")]
    [Tooltip("เส้นผ่านศูนย์กลางวงกลม Portrait")]
    public float circleSize = 76f;
    [Tooltip("ความหนา HP Arc รอบวงกลม")]
    public float arcThickness = 10f;

    [Header("── Size: Portrait Image ───────────")]
    [Tooltip("ขนาดรูปภาพใน Portrait (0 = ใช้ circleSize อัตโนมัติ)")]
    public float portraitImageSize = 0f;

    [Header("── Size: Text ───────────────────────")]
    [Tooltip("ขนาด font ชื่อ Player")]
    public float fontSizeName = 13f;
    [Tooltip("ขนาด font ตัวเลข HP")]
    public float fontSizeHP   = 19f;
    [Tooltip("ขนาด font ป้าย HP")]
    public float fontSizeLabel = 10f;

    [Header("── Colors: Panel ───────────────────")]
    public Color colPanel    = new Color(0.12f, 0.08f, 0.04f, 0.95f);
    public Color colPanelBdr = new Color(0.80f, 0.55f, 0.10f, 1.00f);
    public Color colPanelIn  = new Color(0.20f, 0.13f, 0.06f, 0.92f);

    [Header("── Colors: HP Arc ──────────────────")]
    public Color colArcBg    = new Color(0.08f, 0.05f, 0.02f, 1.00f);
    public Color colArcFull  = new Color(0.18f, 0.90f, 0.32f, 1.00f);
    public Color colArcMid   = new Color(1.00f, 0.80f, 0.08f, 1.00f);
    public Color colArcLow   = new Color(0.95f, 0.15f, 0.15f, 1.00f);
    public Color colArcDrain = new Color(0.92f, 0.45f, 0.05f, 0.65f);

    [Header("── Colors: Portrait Ring ──────────")]
    public Color colPortBg   = new Color(0.15f, 0.10f, 0.05f, 1.00f);
    public Color colRingOut  = new Color(0.88f, 0.62f, 0.08f, 1.00f);
    public Color colRingIn   = new Color(0.50f, 0.32f, 0.04f, 1.00f);

    [Header("── Colors: Text ────────────────────")]
    public Color colNameP1  = new Color(0.40f, 0.82f, 1.00f, 1.00f);
    public Color colNameP2  = new Color(1.00f, 0.78f, 0.22f, 1.00f);
    public Color colHpNum   = new Color(1.00f, 0.95f, 0.80f, 1.00f);
    public Color colHpLabel = new Color(0.78f, 0.58f, 0.28f, 0.85f);

    [Header("── Test Keys ───────────────────────")]
    public bool enableTestKeys = true;
    public int  testDamage     = 15;

    // ── State ──────────────────────────────────
    float _hp1, _hp2, _drain1, _drain2, _regen1, _regen2;
    RectTransform   _rt1, _rt2;
    Image           _fill1, _fill2, _di1, _di2, _flash1, _flash2;
    Image           _port1, _port2;
    TextMeshProUGUI _txt1, _txt2;
    Sprite          _cSpr;

    bool _built = false;

    // ════════════════════════════════════════════
    void Awake()
    {
        _hp1=_drain1=_hp2=_drain2=maxHP;
        if (!targetCanvas){Debug.LogError("[HP] ไม่มี Canvas");return;}
        _cSpr = MakeCircle(128);
        RebuildAll();
    }

    void RebuildAll()
    {
        // ลบ HUD เก่าออกก่อน
        var old1 = targetCanvas.transform.Find("_HP_P1");
        var old2 = targetCanvas.transform.Find("_HP_P2");
        if (old1) Destroy(old1.gameObject);
        if (old2) Destroy(old2.gameObject);

        BuildHUD(true,  out _rt1, out _fill1, out _di1, out _flash1, out _txt1, out _port1);
        BuildHUD(false, out _rt2, out _fill2, out _di2, out _flash2, out _txt2, out _port2);
        Refresh(true); Refresh(false);
        _built = true;
    }

    void Update()
    {
        if (!_built) return;

        if (_rt1) _rt1.anchoredPosition = hudOffsetP1;
        if (_rt2) _rt2.anchoredPosition = hudOffsetP2;

        _drain1 = Mathf.MoveTowards(_drain1,_hp1,20f*Time.deltaTime);
        _drain2 = Mathf.MoveTowards(_drain2,_hp2,20f*Time.deltaTime);
        if (_di1) _di1.fillAmount=_drain1/maxHP;
        if (_di2) _di2.fillAmount=_drain2/maxHP;

        Regen(ref _hp1,ref _regen1,true);
        Regen(ref _hp2,ref _regen2,false);

        if (!enableTestKeys)return;
        if (Input.GetKeyDown(KeyCode.F1))TakeDamage(true, testDamage);
        if (Input.GetKeyDown(KeyCode.F2))TakeDamage(false,testDamage);
        if (Input.GetKeyDown(KeyCode.F3))Heal(true, testDamage);
        if (Input.GetKeyDown(KeyCode.F4))Heal(false,testDamage);
    }

    // ── ปรับขนาด/สี real-time ใน Inspector ─────
    void OnValidate()
    {
        if (!Application.isPlaying || !_built) return;
        // Rebuild เมื่อค่าเปลี่ยนใน Inspector ขณะ Play
        if (_cSpr == null) _cSpr = MakeCircle(128);
        RebuildAll();
    }

    void Regen(ref float hp,ref float timer,bool p1)
    {
        if(hp>=maxHP)return;
        timer-=Time.deltaTime;
        if(timer>0f)return;
        hp=Mathf.Min(maxHP,hp+regenPerSecond*Time.deltaTime);
        Refresh(p1);
    }

    // ── Public API ─────────────────────────────
    public void TakeDamage(bool p1,int amt)
    {
        if(p1){_hp1=Mathf.Max(0,_hp1-amt);_regen1=regenDelay;Refresh(true); StartCoroutine(DoFlash(_flash1));if(_hp1<=0)Die(true);}
        else  {_hp2=Mathf.Max(0,_hp2-amt);_regen2=regenDelay;Refresh(false);StartCoroutine(DoFlash(_flash2));if(_hp2<=0)Die(false);}
    }
    public void Heal(bool p1,int amt)
    {
        if(p1){_hp1=Mathf.Min(maxHP,_hp1+amt);Refresh(true);}
        else  {_hp2=Mathf.Min(maxHP,_hp2+amt);Refresh(false);}
    }
    public void ResetHP(bool p1)
    {
        if(p1){_hp1=_drain1=maxHP;Refresh(true);}
        else  {_hp2=_drain2=maxHP;Refresh(false);}
    }
    public int GetHP(bool p1)=>p1?(int)_hp1:(int)_hp2;
    void Die(bool p1){ResetHP(p1);CheckpointManager.Instance?.TriggerRespawn(p1);}

    void Refresh(bool p1)
    {
        float hp=p1?_hp1:_hp2;
        var fill=p1?_fill1:_fill2;
        var txt =p1?_txt1 :_txt2;
        float r=hp/maxHP;
        if(fill!=null){fill.fillAmount=r;fill.color=r>.55f?colArcFull:r>.28f?colArcMid:colArcLow;}
        if(txt!=null)txt.text=$"{(int)hp}<size=65%>/{maxHP}</size>";
    }

    IEnumerator DoFlash(Image img)
    {
        if(!img)yield break;
        img.gameObject.SetActive(true);
        for(float t=0;t<.3f;t+=Time.deltaTime)
        {img.color=new Color(1,.05f,.05f,Mathf.Lerp(.5f,0,t/.3f));yield return null;}
        img.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════
    //  BuildHUD
    // ════════════════════════════════════════════
    void BuildHUD(bool isLeft,
                  out RectTransform    rt,
                  out Image            fill,
                  out Image            drain,
                  out Image            flash,
                  out TextMeshProUGUI  txt,
                  out Image            portImg)
    {
        fill=drain=flash=portImg=null; txt=null;
        string s  = isLeft?"P1":"P2";
        Sprite port= isLeft?portraitP1:portraitP2;
        Color  nc  = isLeft?colNameP1:colNameP2;
        string pn  = isLeft?"PLAYER 1":"PLAYER 2";

        // ── Root ──────────────────────────────
        var root=new GameObject("_HP_"+s,typeof(RectTransform));
        root.transform.SetParent(targetCanvas.transform,false);
        rt=root.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=isLeft?new Vector2(0f,1f):new Vector2(.5f,1f);
        rt.pivot=new Vector2(0f,1f);
        rt.anchoredPosition=isLeft?hudOffsetP1:hudOffsetP2;
        rt.sizeDelta=new Vector2(panelW,panelH);
        var T=root.transform;

        // ── Panel ─────────────────────────────
        MkBox(T,"Bdr", 0,0,  panelW,     panelH,     colPanelBdr);
        MkBox(T,"Bg",  2,-2, panelW-4f,  panelH-4f,  colPanel);
        MkBox(T,"BgI", 4,-4, panelW-8f,  panelH-8f,  colPanelIn);

        // ── วงกลม Arc ─────────────────────────
        float cx = circleSize/2f + 10f;
        float cy = -panelH/2f;
        float D  = circleSize + arcThickness*2f + 6f;

        MkCircle(T,"ArcBg",  cx,cy, D,D, colArcBg);
        drain = MkArc(T,"Drain",   cx,cy, D,D, colArcDrain);
        fill  = MkArc(T,"Fill",    cx,cy, D,D, colArcFull);

        // ── Portrait ─────────────────────────
        MkCircle(T,"PortBg", cx,cy, circleSize,circleSize, colPortBg);

        // Ring (วาดก่อน portrait เพื่อซ่อนขอบ arc)
        MkCircle(T,"RingO",cx,cy,circleSize+arcThickness+4f,circleSize+arcThickness+4f,colRingOut);
        MkCircle(T,"RingI",cx,cy,circleSize+arcThickness,   circleSize+arcThickness,   colPanelIn);

        // Portrait Image (บนสุดในวงกลม)
        float pSize = portraitImageSize > 0f ? portraitImageSize : circleSize - 4f;
        var pGo=new GameObject("Portrait_"+s,typeof(RectTransform),typeof(Image));
        pGo.transform.SetParent(T,false);
        var pRt=pGo.GetComponent<RectTransform>();
        pRt.anchorMin=pRt.anchorMax=new Vector2(0f,1f);
        pRt.pivot=new Vector2(.5f,.5f);
        pRt.anchoredPosition=new Vector2(cx,cy);
        pRt.sizeDelta=new Vector2(pSize,pSize);
        portImg=pGo.GetComponent<Image>();
        portImg.raycastTarget=false;
        if(port!=null){portImg.sprite=port;portImg.type=Image.Type.Simple;portImg.preserveAspect=true;portImg.color=Color.white;}
        else portImg.color=colPortBg;

        // Flash
        var fGo=new GameObject("Flash_"+s,typeof(RectTransform),typeof(Image));
        fGo.transform.SetParent(T,false);
        var fRt=fGo.GetComponent<RectTransform>();
        fRt.anchorMin=fRt.anchorMax=new Vector2(0f,1f);
        fRt.pivot=new Vector2(.5f,.5f);
        fRt.anchoredPosition=new Vector2(cx,cy);
        fRt.sizeDelta=new Vector2(circleSize,circleSize);
        flash=fGo.GetComponent<Image>();
        flash.sprite=_cSpr;flash.type=Image.Type.Simple;
        flash.color=new Color(1,.05f,.05f,.5f);flash.raycastTarget=false;
        fGo.SetActive(false);

        // ── Text ──────────────────────────────
        float tx=cx+circleSize/2f+arcThickness+14f;
        float tw=panelW-tx-6f;
        MkTMP(T,"Name",pn,              tx,-10f,tw,fontSizeName+8f,fontSizeName,nc,       FontStyles.Bold);
        txt=MkTMP(T,"Hp",$"{maxHP}<size=65%>/{maxHP}</size>",tx,-10f-fontSizeName-4f,tw,fontSizeHP+8f,fontSizeHP,colHpNum,  FontStyles.Bold);
        MkTMP(T,"Lbl","HP",             tx,-10f-fontSizeName-4f-fontSizeHP-2f,tw,fontSizeLabel+6f,fontSizeLabel,colHpLabel,FontStyles.Normal);
    }

    // ════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════

    void MkBox(Transform p,string n,float x,float y,float w,float h,Color c)
    {
        var go=new GameObject(n,typeof(RectTransform),typeof(Image));
        go.transform.SetParent(p,false);
        var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=new Vector2(0f,1f);
        rt.pivot=new Vector2(0f,1f);
        rt.anchoredPosition=new Vector2(x,-y);
        rt.sizeDelta=new Vector2(w,h);
        go.GetComponent<Image>().color=c;
    }

    Image MkCircle(Transform p,string n,float cx,float cy,float w,float h,Color c)
    {
        var go=new GameObject(n,typeof(RectTransform),typeof(Image));
        go.transform.SetParent(p,false);
        var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=new Vector2(0f,1f);
        rt.pivot=new Vector2(.5f,.5f);
        rt.anchoredPosition=new Vector2(cx,cy);
        rt.sizeDelta=new Vector2(w,h);
        var img=go.GetComponent<Image>();
        img.sprite=_cSpr;img.type=Image.Type.Simple;
        img.color=c;img.raycastTarget=false;
        return img;
    }

    Image MkArc(Transform p,string n,float cx,float cy,float w,float h,Color c)
    {
        var img=MkCircle(p,n,cx,cy,w,h,c);
        img.type=Image.Type.Filled;
        img.fillMethod=Image.FillMethod.Radial360;
        img.fillOrigin=(int)Image.Origin360.Top;
        img.fillClockwise=false;
        img.fillAmount=1f;
        return img;
    }

    TextMeshProUGUI MkTMP(Transform p,string n,string text,
                          float x,float y,float w,float h,
                          float sz,Color c,FontStyles fs)
    {
        var go=new GameObject(n,typeof(RectTransform));
        go.transform.SetParent(p,false);
        var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=new Vector2(0f,1f);
        rt.pivot=new Vector2(0f,1f);
        rt.anchoredPosition=new Vector2(x,y);
        rt.sizeDelta=new Vector2(w,h);
        var tmp=go.AddComponent<TextMeshProUGUI>();
        tmp.text=text;tmp.fontSize=sz;tmp.fontStyle=fs;
        tmp.alignment=TextAlignmentOptions.Left;tmp.color=c;
        tmp.textWrappingMode=TextWrappingModes.NoWrap;
        tmp.overflowMode=TextOverflowModes.Overflow;
        return tmp;
    }

    Sprite MakeCircle(int res)
    {
        var tex=new Texture2D(res,res,TextureFormat.RGBA32,false);
        tex.filterMode=FilterMode.Bilinear;
        var px=new Color32[res*res];
        float h2=res*.5f,r2=(h2-1f)*(h2-1f);
        for(int y=0;y<res;y++)for(int x=0;x<res;x++)
        {float dx=x-h2+.5f,dy=y-h2+.5f;px[y*res+x]=new Color32(255,255,255,dx*dx+dy*dy<=r2?(byte)255:(byte)0);}
        tex.SetPixels32(px);tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,res,res),new Vector2(.5f,.5f),res);
    }
}
