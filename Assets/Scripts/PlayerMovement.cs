using UnityEngine;
using EmotivUnityPlugin;

public class PlayerMovement : MonoBehaviour
{
    EmotivUnityItf _eItf = EmotivUnityItf.Instance;
    public float speed;
    public float moveSpeed = 5f; // Movement speed
    public float gravity = -9.81f; // Gravity force (only needed for CharacterController)
    public bool headset = false;
    
    private CharacterController controller; 
    private Vector3 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!headset) {
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
        else {
            Vector3 dir = headsetParser();
            forwardBack(dir);
        }
    }

    void forwardBack(Vector3 input) {
        transform.Translate(input * speed * Time.deltaTime);
    }

    Vector3 headsetParser(){
        if(_eItf != null)
        {
            if(_eItf.LatestMentalCommand.act != "NULL")
            {
                string current_command = _eItf.LatestMentalCommand.act;
                switch (current_command){
                    case "push": return new Vector3(0,0,1);
                    case "pull": return new Vector3(0,0,-1);
                    case "left": return new Vector3(-1,0,0);
                    case "right": return new Vector3(1,0,0);
                }
            }
        }
        return new Vector3(0,0,0);
    }
}