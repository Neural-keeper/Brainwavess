using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Movement speed
    public float gravity = -9.81f; // Gravity force (only needed for CharacterController)
    
    private CharacterController controller; 
    private Vector3 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float moveZ = Input.GetAxis("Vertical");   // W/S or Up/Down

        Vector3 move = new Vector3(moveX, 0f, moveZ);
        move = transform.TransformDirection(move); // Moves relative to the player's facing direction

        // CharacterController approach
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Apply gravity (optional if you have physics-based gravity)
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}