using UnityEngine;
using Cinemachine;

// วาง Script นี้บน GameObject ว่างๆ ชื่อ "GameManager"
public class SplitScreenManager : MonoBehaviour
{
    [Header("Cameras")]
    // ลาก Camera ของ Player1 และ Player2 มาวาง
    public Camera cameraPlayer1;
    public Camera cameraPlayer2;

    [Header("Cinemachine Virtual Cameras")]
    // ลาก CinemachineFreeLookCamera ของแต่ละ Player มาวาง
    public CinemachineFreeLook vcamPlayer1;
    public CinemachineFreeLook vcamPlayer2;

    void Start()
    {
        SetupSplitScreen();
    }

    void SetupSplitScreen()
    {
        // Viewport Rect คือสัดส่วนพื้นที่จอ (0-1)
        // (x, y, width, height)

        // Player1 = ครึ่งซ้าย: เริ่มจากซ้ายสุด (x=0), กว้างครึ่งจอ (w=0.5)
        cameraPlayer1.rect = new Rect(0f, 0f, 0.5f, 1f);

        // Player2 = ครึ่งขวา: เริ่มจากกึ่งกลาง (x=0.5), กว้างครึ่งจอ (w=0.5)
        cameraPlayer2.rect = new Rect(0.5f, 0f, 0.5f, 1f);

        // ตั้ง Priority ของ VCam ให้ถูก Camera รับ
        // VCam ที่ Priority สูงกว่าจะถูกใช้กับ Camera Brain ที่เชื่อมอยู่
        vcamPlayer1.Priority = 10;
        vcamPlayer2.Priority = 10;

        // ตั้ง Output Channel แยกกัน (Cinemachine ใหม่ใช้ระบบ Channel)
        // Player1 Camera Brain ฟัง Channel 1
        // Player2 Camera Brain ฟัง Channel 2
        SetCameraChannel(cameraPlayer1, 1);
        SetCameraChannel(cameraPlayer2, 2);

        SetVCamChannel(vcamPlayer1, 1);
        SetVCamChannel(vcamPlayer2, 2);
    }

    // ตั้ง Output Channel ของ CinemachineBrain (บน Main Camera)
    void SetCameraChannel(Camera cam, int channel)
    {
        CinemachineBrain brain = cam.GetComponent<CinemachineBrain>();
        if (brain != null)
            brain.ChannelMask = (OutputChannels)(1 << channel);
    }

    // ตั้ง Output Channel ของ Virtual Camera
    void SetVCamChannel(CinemachineFreeLook vcam, int channel)
    {
        vcam.OutputChannel = (OutputChannels)(1 << channel);
    }
}