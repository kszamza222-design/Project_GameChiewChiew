using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// WallTransparency — ทำให้กำแพง/หลังคาโปร่งใสเมื่อบังตัวละคร
///
/// วิธีทำงาน:
///   ยิง Raycast จากตัวละครไปหากล้อง
///   ถ้าชน Object ใดก็ตาม → ทำ Material โปร่งใส
///   เมื่อ Raycast ไม่ชนแล้ว → คืน Material ต้นฉบับ
///
/// วิธีใช้:
///   1. Add Component → WallTransparency บน Camera GameObject
///   2. ผูก target = ตัวละคร
///   3. ตั้ง wallLayer = Layer ของกำแพง/บ้าน
///   4. กำแพงต้องใช้ URP Lit Shader (หรือ Standard Shader)
/// </summary>
public class WallTransparency : MonoBehaviour
{
    [Header("── Target ─────────────────────────")]
    [Tooltip("ตัวละครของ Camera นี้")]
    public Transform target;

    [Header("── Layer ───────────────────────────")]
    [Tooltip("Layer ของกำแพง/หลังคาที่ต้องการให้โปร่งใส\n" +
             "สร้าง Layer ชื่อ 'Wall' แล้วตั้งให้กับ Mesh ของบ้าน")]
    public LayerMask wallLayer;

    [Header("── Transparency ────────────────────")]
    [Tooltip("ความโปร่งใสของกำแพง (0 = มองทะลุ, 0.2 = เห็นเค้า)")]
    [Range(0f, 0.5f)]
    public float fadeAlpha = 0.15f;

    [Tooltip("ความเร็ว Fade In/Out")]
    public float fadeSpeed = 8f;

    // ─────────────────────────────────────────────────
    //  เก็บ Material ต้นฉบับ
    // ─────────────────────────────────────────────────

    class MatInfo
    {
        public Renderer  renderer;
        public Material  original;       // Material ต้นฉบับ
        public Material  fadeMat;        // Material copy สำหรับ fade
        public bool      isFading;
    }

    readonly Dictionary<Renderer, MatInfo> _tracked = new();
    readonly HashSet<Renderer>             _hitThisFrame = new();

    // ─────────────────────────────────────────────────

    void LateUpdate()
    {
        if (target == null) return;

        _hitThisFrame.Clear();

        // ── Raycast จากตัวละครไปกล้อง ────────────────
        Vector3 from  = target.position + Vector3.up * 1.2f;
        Vector3 to    = transform.position;
        Vector3 dir   = to - from;
        float   dist  = dir.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(from, dir.normalized, dist, wallLayer);

        foreach (var hit in hits)
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend == null)
                rend = hit.collider.GetComponentInParent<Renderer>();
            if (rend == null) continue;

            _hitThisFrame.Add(rend);

            if (!_tracked.ContainsKey(rend))
                StartTracking(rend);

            // Fade out (โปร่งใส)
            FadeRenderer(rend, fadeAlpha);
        }

        // ── คืน Renderer ที่ไม่ถูกบังแล้ว ────────────
        foreach (var kv in _tracked)
        {
            if (!_hitThisFrame.Contains(kv.Key))
                FadeRenderer(kv.Key, 1f);   // Fade กลับ opaque
        }

        // ── ลบ Renderer ที่ Opaque แล้วออกจาก track ──
        var toRemove = new List<Renderer>();
        foreach (var kv in _tracked)
        {
            if (!_hitThisFrame.Contains(kv.Key))
            {
                float a = kv.Value.fadeMat.color.a;
                if (a >= 0.99f)
                {
                    // คืน Material ต้นฉบับและลบออก
                    kv.Value.renderer.material = kv.Value.original;
                    Destroy(kv.Value.fadeMat);
                    toRemove.Add(kv.Key);
                }
            }
        }
        foreach (var r in toRemove) _tracked.Remove(r);
    }

    // ─────────────────────────────────────────────────
    //  เริ่ม Track Renderer ใหม่
    // ─────────────────────────────────────────────────

    void StartTracking(Renderer rend)
    {
        var info = new MatInfo
        {
            renderer = rend,
            original = rend.material,
            fadeMat  = new Material(rend.material),
            isFading = false,
        };

        // ── ตั้ง Shader Mode เป็น Transparent ──────────
        SetTransparentMode(info.fadeMat);

        _tracked[rend] = info;
    }

    // ─────────────────────────────────────────────────
    //  Fade Alpha ของ Renderer
    // ─────────────────────────────────────────────────

    void FadeRenderer(Renderer rend, float targetAlpha)
    {
        if (!_tracked.TryGetValue(rend, out var info)) return;

        // ใส่ fadeMat ถ้ายังไม่ได้ใส่
        if (rend.material != info.fadeMat)
            rend.material = info.fadeMat;

        Color c = info.fadeMat.color;
        c.a = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
        info.fadeMat.color = c;
    }

    // ─────────────────────────────────────────────────
    //  ตั้ง Material เป็น Transparent Mode
    //  รองรับทั้ง URP Lit และ Standard Shader
    // ─────────────────────────────────────────────────

    static void SetTransparentMode(Material mat)
    {
        // ── URP Lit ────────────────────────────────────
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);               // 1 = Transparent
            mat.SetFloat("_Blend", 0f);                 // Alpha blend
            mat.SetFloat("_AlphaClip", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;

            mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        // ── Standard Shader ────────────────────────────
        else if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3f);                  // 3 = Transparent
            mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        // ── Fallback: ตั้ง render queue ตรงๆ ───────────
        else
        {
            mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.renderQueue = 3000;
        }
    }

    // ─────────────────────────────────────────────────
    //  OnDestroy — คืน Material ทั้งหมด
    // ─────────────────────────────────────────────────

    void OnDestroy()
    {
        foreach (var kv in _tracked)
        {
            if (kv.Value.renderer != null)
                kv.Value.renderer.material = kv.Value.original;
            if (kv.Value.fadeMat != null)
                Destroy(kv.Value.fadeMat);
        }
        _tracked.Clear();
    }

    // ─────────────────────────────────────────────────
    //  Gizmo
    // ─────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(target.position + Vector3.up * 1.2f, transform.position);
    }
}
