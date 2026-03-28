using UnityEngine;
using UnityEngine.InputSystem;

public class JumpController : MonoBehaviour
{
    [Header("Player Setup")]
    public int playerIndex = 0;

    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private bool isGrounded;
    private bool jumpPressed;

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
            inputActions.Player1.Jump.performed += ctx => TryJump();
            inputActions.Player1.Jump.canceled  += ctx => jumpPressed = false;
        }
        else
        {
            inputActions.Player2.Enable();
            inputActions.Player2.Jump.performed += ctx => TryJump();
            inputActions.Player2.Jump.canceled  += ctx => jumpPressed = false;
        }
    }

    void OnDisable()
    {
        inputActions.Player1.Disable();
        inputActions.Player2.Disable();
    }

    void FixedUpdate()
    {
        CheckGround();
        ApplyBetterGravity();
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    void TryJump()
    {
        if (!isGrounded) return;
        jumpPressed = true;

        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector3.up * Physics.gravity.y
                                 * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !jumpPressed)
            rb.linearVelocity += Vector3.up * Physics.gravity.y
                                 * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
    }

    public bool IsGrounded() => isGrounded;
}