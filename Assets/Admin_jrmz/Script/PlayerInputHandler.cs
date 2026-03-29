using UnityEngine;

/// <summary>
/// จัดการ Input สำหรับ Player แต่ละคน
/// Player 1 : WASD + Space (กระโดด) + E ค้าง (ยกของ ปล่อย=วาง) + Q (โยน)
/// Player 2 : Numpad 8/5/4/6 + Numpad0 (กระโดด) + Numpad7 ค้าง (ยกของ ปล่อย=วาง) + Numpad. (โยน)
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    public enum PlayerID { Player1, Player2 }

    [Header("Player Assignment")]
    public PlayerID playerID = PlayerID.Player1;

    // ──────────────────────────────────────────
    //  Read-only properties
    // ──────────────────────────────────────────
    public Vector2 MoveInput       { get; private set; }  // x = ซ้าย/ขวา, y = หน้า/หลัง
    public bool    JumpPressed     { get; private set; }  // กดปุ่มกระโดดในเฟรมนี้
    public bool    JumpHeld        { get; private set; }  // ค้างปุ่มกระโดด
    public bool    CarryHeld       { get; private set; }  // ค้างปุ่มยก = กำลังอุ้มของ
    public bool    CarryPressed    { get; private set; }  // กดปุ่มยกในเฟรมนี้ (เริ่มยก)
    public bool    CarryReleased   { get; private set; }  // ปล่อยปุ่มยกในเฟรมนี้ (วาง)
    public bool    ThrowPressed    { get; private set; }  // กดโยน

    // ──────────────────────────────────────────
    //  Update
    // ──────────────────────────────────────────
    void Update() => ReadInput();

    void ReadInput()
    {
        float mx = 0f, my = 0f;
        bool jump = false, jumpHeld = false;
        bool carryHeld = false, carryPressed = false, carryReleased = false;
        bool throwKey = false;

        if (playerID == PlayerID.Player1)
        {
            // ── WASD Movement ──
            if (Input.GetKey(KeyCode.D)) mx += 1f;
            if (Input.GetKey(KeyCode.A)) mx -= 1f;
            if (Input.GetKey(KeyCode.W)) my += 1f;
            if (Input.GetKey(KeyCode.S)) my -= 1f;

            // ── Actions ──
            jump         = Input.GetKeyDown(KeyCode.Space);
            jumpHeld     = Input.GetKey(KeyCode.Space);

            // E ค้าง = ยกของ, ปล่อย = วาง
            carryHeld     = Input.GetKey(KeyCode.E);
            carryPressed  = Input.GetKeyDown(KeyCode.E);
            carryReleased = Input.GetKeyUp(KeyCode.E);

            throwKey = Input.GetKeyDown(KeyCode.Q);
        }
        else // Player 2
        {
            // ── Numpad Movement ──
            if (Input.GetKey(KeyCode.Keypad6)) mx += 1f;
            if (Input.GetKey(KeyCode.Keypad4)) mx -= 1f;
            if (Input.GetKey(KeyCode.Keypad8)) my += 1f;
            if (Input.GetKey(KeyCode.Keypad5)) my -= 1f;

            // ── Actions ──
            jump         = Input.GetKeyDown(KeyCode.Keypad0);
            jumpHeld     = Input.GetKey(KeyCode.Keypad0);

            // Numpad7 ค้าง = ยกของ, ปล่อย = วาง
            carryHeld     = Input.GetKey(KeyCode.Keypad7);
            carryPressed  = Input.GetKeyDown(KeyCode.Keypad7);
            carryReleased = Input.GetKeyUp(KeyCode.Keypad7);

            throwKey = Input.GetKeyDown(KeyCode.KeypadPeriod);
        }

        MoveInput     = new Vector2(mx, my).normalized;
        JumpPressed   = jump;
        JumpHeld      = jumpHeld;
        CarryHeld     = carryHeld;
        CarryPressed  = carryPressed;
        CarryReleased = carryReleased;
        ThrowPressed  = throwKey;
    }
}
