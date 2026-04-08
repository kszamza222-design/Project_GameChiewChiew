using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// RespawnEffect — เอฟเฟกต์หน้าจอ Flash + ข้อความ "Respawning..." เมื่อตาย
///
/// วิธีใช้:
///   • Add Component → RespawnEffect บน GameManager หรือ Empty GameObject
///   • ผูก targetCanvas
///   • CheckpointManager จะเรียก RespawnEffect.Instance.Play() อัตโนมัติ
///
/// ไม่บังคับ — ถ้าไม่มีจะข้ามไปเลย
/// </summary>
public class RespawnEffect : MonoBehaviour
{
    public static RespawnEffect Instance { get; private set; }

    [Header("── Canvas ───────────────────────────")]
    public Canvas targetCanvas;

    [Header("── Timing ───────────────────────────")]
    public float fadeDuration   = 0.3f;
    public float holdDuration   = 0.6f;

    // ── UI ───────────────────────────────────────
    GameObject      _overlayP1, _overlayP2;
    TextMeshProUGUI _textP1,    _textP2;

    // ─────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildOverlay(out _overlayP1, out _textP1, isLeftSide: true);
        BuildOverlay(out _overlayP2, out _textP2, isLeftSide: false);
    }

    // ═══════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════

    /// <summary>เล่น Respawn effect ฝั่งที่กำหนด</summary>
    public void Play(bool isP1) =>
        StartCoroutine(PlayRoutine(isP1 ? _overlayP1 : _overlayP2,
                                   isP1 ? _textP1    : _textP2));

    // ═══════════════════════════════════════════════════
    //  Routine
    // ═══════════════════════════════════════════════════

    IEnumerator PlayRoutine(GameObject overlay, TextMeshProUGUI label)
    {
        if (overlay == null) yield break;
        overlay.SetActive(true);

        // ── Fade in (ดำ) ──────────────────────────────
        yield return FadeOverlay(overlay, label, 0f, 1f, fadeDuration);

        // ── Hold ──────────────────────────────────────
        if (label != null) label.text = "Respawning...";
        yield return new WaitForSeconds(holdDuration);

        // ── Fade out ──────────────────────────────────
        if (label != null) label.text = "";
        yield return FadeOverlay(overlay, label, 1f, 0f, fadeDuration);

        overlay.SetActive(false);
    }

    IEnumerator FadeOverlay(GameObject overlay, TextMeshProUGUI label,
                             float from, float to, float dur)
    {
        var imgs = overlay.GetComponentsInChildren<Image>();
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / dur);
            foreach (var img in imgs)
            {
                var c = img.color; c.a = a; img.color = c;
            }
            if (label != null)
            {
                var c = label.color; c.a = a; label.color = c;
            }
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════
    //  Build Overlay — ครึ่งจอดำ
    // ═══════════════════════════════════════════════════

    void BuildOverlay(out GameObject go, out TextMeshProUGUI label, bool isLeftSide)
    {
        go    = null;
        label = null;
        if (targetCanvas == null) return;

        go = new GameObject("RespawnOverlay_" + (isLeftSide ? "P1" : "P2"),
                            typeof(RectTransform), typeof(Image));
        go.transform.SetParent(targetCanvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        float minX = isLeftSide ? 0f : 0.5f;
        float maxX = isLeftSide ? 0.5f : 1.0f;
        rt.anchorMin = new Vector2(minX, 0f);
        rt.anchorMax = new Vector2(maxX, 1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);

        // Text "Respawning..."
        var tGO = new GameObject("Text", typeof(RectTransform));
        tGO.transform.SetParent(go.transform, false);
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = tRT.anchorMax = tRT.pivot = new Vector2(0.5f, 0.5f);
        tRT.sizeDelta        = new Vector2(300f, 60f);
        tRT.anchoredPosition = Vector2.zero;

        label               = tGO.AddComponent<TextMeshProUGUI>();
        label.text          = "";
        label.fontSize      = 24f;
        label.alignment     = TextAlignmentOptions.Center;
        label.color         = new Color(1f, 1f, 1f, 0f);
        label.fontStyle     = FontStyles.Bold;

        go.SetActive(false);
    }
}
