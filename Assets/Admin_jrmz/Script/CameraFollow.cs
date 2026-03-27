using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public Vector3 offset      = new Vector3(0f, 3f, -6f);
    public float   smoothSpeed = 8f;

    [Header("Look At Offset")]
    public float lookHeightOffset = 1f;   // ← ปรับได้ใน Inspector

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position, desired, smoothSpeed * Time.deltaTime);

        transform.LookAt(target.position + Vector3.up * lookHeightOffset);
    }
}