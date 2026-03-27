using UnityEngine;

/// <summary>
/// ใส่ Script นี้ใน GameObject ว่างๆ ใน Scene
/// แล้วลาก Camera ของ Player1 และ Player2 มาใส่
/// </summary>
public class SplitScreenSetup : MonoBehaviour
{
    [Header("Cameras")]
    public Camera cameraLeft;   // กล้องจอซ้าย (Player 1 - WASD)
    public Camera cameraRight;  // กล้องจอขวา (Player 2 - Arrow)

    void Awake()
    {
        if (cameraLeft != null)
        {
            // จอซ้าย: x=0, y=0, width=0.5, height=1
            cameraLeft.rect = new Rect(0f, 0f, 0.5f, 1f);
        }

        if (cameraRight != null)
        {
            // จอขวา: x=0.5, y=0, width=0.5, height=1
            cameraRight.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        }
    }
}