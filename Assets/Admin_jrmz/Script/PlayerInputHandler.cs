using UnityEngine;

/// <summary>
/// จัดการ Input สำหรับ Player แต่ละคน
/// Player 1 : WASD + Space (กระโดด) + E (ยก/วาง) + Q (โยน)
/// Player 2 : Numpad 8/5/4/6 + Numpad0 (กระโดด) + NumpadEnter (ยก/วาง) + Numpad. (โยน)
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    public enum PlayerID { Player1, Player2 }

    [Header("Player Assignment")]
    public PlayerID playerID = PlayerID.Player1;

    // ──────────────────────────────────────────
    //  Read-only properties (ใช้จาก PlayerController)
    // ──────────────────────────────────────────
    public Vector2 MoveInput        { get; private set; }  // x = ซ้าย/ขวา, y = หน้า/หลัง
    public bool    JumpPressed      { get; private set; }  // กดปุ่มกระโดดในเฟรมนี้
    public bool    JumpHeld         { get; private set; }  // ค้างปุ่มกระโดด
    public bool    InteractPressed  { get; private set; }  // กด ยก/วาง
    public bool    ThrowPressed     { get; private set; }  // กด โยน

    // ──────────────────────────────────────────
    //  Update
    // ──────────────────────────────────────────
    void Update() => ReadInput();

    void ReadInput()
    {
        float mx = 0f, my = 0f;
        bool  jump = false, jumpHeld = false, interact = false, throwKey = false;

        if (playerID == PlayerID.Player1)
        {
            // ── WASD Movement ──
            if (Input.GetKey(KeyCode.D))     mx += 1f;
            if (Input.GetKey(KeyCode.A))     mx -= 1f;
            if (Input.GetKey(KeyCode.W))     my += 1f;
            if (Input.GetKey(KeyCode.S))     my -= 1f;

            // ── Actions ──
            jump     = Input.GetKeyDown(KeyCode.Space);
            jumpHeld = Input.GetKey(KeyCode.Space);
            interact = Input.GetKeyDown(KeyCode.E);           // ยก / วาง
            throwKey = Input.GetKeyDown(KeyCode.Q);           // โยน
        }
        else  // Player 2
        {
            // ── Numpad Movement ──
            if (Input.GetKey(KeyCode.Keypad6)) mx += 1f;
            if (Input.GetKey(KeyCode.Keypad4)) mx -= 1f;
            if (Input.GetKey(KeyCode.Keypad8)) my += 1f;
            if (Input.GetKey(KeyCode.Keypad5)) my -= 1f;

            // ── Actions ──
            jump     = Input.GetKeyDown(KeyCode.Keypad0);        // กระโดด
            jumpHeld = Input.GetKey(KeyCode.Keypad0);
            interact = Input.GetKeyDown(KeyCode.KeypadEnter);    // ยก / วาง
            throwKey = Input.GetKeyDown(KeyCode.KeypadPeriod);   // โยน  (Numpad .)
        }

        MoveInput       = new Vector2(mx, my).normalized;
        JumpPressed     = jump;
        JumpHeld        = jumpHeld;
        InteractPressed = interact;
        ThrowPressed    = throwKey;
    }
}
