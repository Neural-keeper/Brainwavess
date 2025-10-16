using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemPlayerController : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float movementForceMultiplier = 100f;
    public float jumpForce = 12f;

    private Rigidbody rb;
    private bool isGrounded;

    private InputAction moveAction;
    private InputAction jumpAction;

    public ImprovedBCIPlayer bciPlayer; // Reference to ImprovedBCIPlayer

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Find actions by name from the current Input System
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");

        // Enable actions
        moveAction?.Enable();
        jumpAction?.Enable();

        // Try to find ImprovedBCIPlayer on the same GameObject if not set
        if (bciPlayer == null)
            bciPlayer = GetComponent<ImprovedBCIPlayer>();
    }

    void Update()
    {
        // Poll movement input
        Vector2 movementVector = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 move = new Vector3(movementVector.x, 0, movementVector.y);

        // Only send movement if input is outside dead zone
        if (move.sqrMagnitude > 0.01f)
        {
            if (bciPlayer != null)
                bciPlayer.Move(move, 1f); // Forward to ImprovedBCIPlayer
            else if (rb != null)
                rb.AddForce(move * moveSpeed * movementForceMultiplier, ForceMode.Force);
        }

        // Poll jump input
        bool jumpPressed = jumpAction != null && jumpAction.triggered;
        if (jumpPressed && isGrounded)
        {
            if (bciPlayer != null)
                bciPlayer.Jump(1f); // Forward to ImprovedBCIPlayer
            else if (rb != null)
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}