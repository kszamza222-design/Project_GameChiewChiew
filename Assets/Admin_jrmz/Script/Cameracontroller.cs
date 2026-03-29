using UnityEngine;

/// <summary>
/// กล้องล็อคอยู่หลังตัวละคร - แก้ปัญหากล้องมองจากบน
/// ติด Script นี้ที่ CameraTarget_P1 / CameraTarget_P2
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform playerBody;

    [Header("Camera Position")]
    public float distanceBack = 4f;   // ระยะถอยหลัง
    public float distanceUp   = 1.5f; // ความสูง

    [Header("Smoothing")]
    public float positionDamping = 6f;
    public float rotationDamping = 4f;

    void LateUpdate()
    {
        if (playerBody == null) return;

        // ใช้แค่ yaw (Y) ของตัวละคร ไม่เอา pitch/roll
        Quaternion yawOnly = Quaternion.Euler(0f, playerBody.eulerAngles.y, 0f);

        Vector3 desiredPos = playerBody.position
                           + yawOnly * Vector3.back  * distanceBack
                           + Vector3.up              * distanceUp;

        transform.position = Vector3.Lerp(
            transform.position, desiredPos, positionDamping * Time.deltaTime
        );

        // มองไปที่ตัวละคร (ระดับหน้าอก)
        Vector3 lookTarget = playerBody.position + Vector3.up * 0.8f;
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot, rotationDamping * Time.deltaTime
        );
    }

    public float GetYaw() => playerBody != null ? playerBody.eulerAngles.y : 0f;
}
