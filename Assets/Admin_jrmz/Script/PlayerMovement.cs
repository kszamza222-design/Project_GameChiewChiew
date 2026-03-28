using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Setup")]
    public int playerIndex = 0;

    [Header("Speed Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("References")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        if (playerIndex == 0)
        {
            inputActions.Player1.Enable();
            inputActions.Player1.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            inputActions.Player1.Move.canceled  += ctx => moveInput = Vector2.zero;
        }
        else
        {
            inputActions.Player2.Enable();
            inputActions.Player2.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            inputActions.Player2.Move.canceled  += ctx => moveInput = Vector2.zero;
        }
    }

    void OnDisable()
    {
        inputActions.Player1.Disable();
        inputActions.Player2.Disable();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 forward = cameraTransform.forward;
        Vector3 right   = cameraTransform.right;
        forward.y = 0f;
        right.y   = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

        Vector3 targetVel = moveDir * moveSpeed;
        targetVel.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVel;

        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
    }

    public bool IsMoving() => moveInput.sqrMagnitude > 0.01f;
}