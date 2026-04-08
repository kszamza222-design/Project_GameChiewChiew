using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SoundManager — จัดการ BGM และ SFX ทั้งเกม
///
/// วิธีใช้:
///   1. วาง Script นี้บน GameObject ชื่อ "SoundManager"
///   2. ผูก AudioClip ต่างๆ ใน Inspector
///   3. เรียก SoundManager.Instance.PlaySFX(...) จากที่ไหนก็ได้
///   4. ปรับ bgmVolume / sfxVolume ผ่าน PauseMenu หรือ Inspector
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════

    [Header("── BGM ──────────────────────────────")]
    [Tooltip("เพลงพื้นหลังขณะเล่นเกม")]
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 0.4f;
    public bool  playOnStart = true;

    [Header("── SFX Clips ────────────────────────")]
    [Tooltip("เสียงก้าวเดิน")]
    public AudioClip sfxFootstep;
    [Tooltip("เสียงกระโดด")]
    public AudioClip sfxJump;
    [Tooltip("เสียงลงพื้น")]
    public AudioClip sfxLand;
    [Tooltip("เสียงหยิบของ")]
    public AudioClip sfxPickup;
    [Tooltip("เสียงวางของ")]
    public AudioClip sfxDrop;
    [Tooltip("เสียงโยนของ")]
    public AudioClip sfxThrow;
    [Tooltip("เสียงรับ Damage")]
    public AudioClip sfxHurt;
    [Tooltip("เสียง Checkpoint")]
    public AudioClip sfxCheckpoint;
    [Tooltip("เสียงเปิด Pause")]
    public AudioClip sfxPauseOpen;
    [Tooltip("เสียงปิด Pause")]
    public AudioClip sfxPauseClose;
    [Tooltip("เสียงกดปุ่ม UI")]
    public AudioClip sfxButton;

    [Header("── SFX Volume ───────────────────────")]
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;

    // ═══════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════

    AudioSource _bgmSource;
    AudioSource _sfxSource;

    // ═══════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM Source
        _bgmSource             = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop        = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume      = bgmVolume;

        // SFX Source
        _sfxSource             = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop        = false;
        _sfxSource.playOnAwake = false;
        _sfxSource.volume      = sfxVolume;
    }

    void Start()
    {
        if (playOnStart && bgmClip != null)
            PlayBGM(bgmClip);
    }

    // ═══════════════════════════════════════════════════
    //  BGM
    // ═══════════════════════════════════════════════════

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void StopBGM()  => _bgmSource.Stop();
    public void PauseBGM() => _bgmSource.Pause();
    public void ResumeBGM() => _bgmSource.UnPause();

    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        _bgmSource.volume = bgmVolume;
    }

    public float GetBGMVolume() => bgmVolume;

    // ═══════════════════════════════════════════════════
    //  SFX
    // ═══════════════════════════════════════════════════

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        _sfxSource.volume = sfxVolume;
    }

    public float GetSFXVolume() => sfxVolume;

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ── Shortcut methods ─────────────────────────────
    public void PlayFootstep()   => PlaySFX(sfxFootstep);
    public void PlayJump()       => PlaySFX(sfxJump);
    public void PlayLand()       => PlaySFX(sfxLand);
    public void PlayPickup()     => PlaySFX(sfxPickup);
    public void PlayDrop()       => PlaySFX(sfxDrop);
    public void PlayThrow()      => PlaySFX(sfxThrow);
    public void PlayHurt()       => PlaySFX(sfxHurt);
    public void PlayCheckpoint() => PlaySFX(sfxCheckpoint);
    public void PlayButton()     => PlaySFX(sfxButton);
    public void PlayPauseOpen()  => PlaySFX(sfxPauseOpen);
    public void PlayPauseClose() => PlaySFX(sfxPauseClose);
}
