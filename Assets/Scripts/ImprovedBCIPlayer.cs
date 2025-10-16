using UnityEngine;

/// <summary>
/// Improved BCI Player with better left/right movement
/// Fixes movement issues where left/right commands don't work properly
/// </summary>
public class ImprovedBCIPlayer : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;           // Increased for better visibility
    public float jumpForce = 12f;          // Increased for better jumps
    public float movementForceMultiplier = 100f;  // NEW: Force multiplier
    
    [Header("Command Smoothing")]
    public float commandCooldown = 0.15f;   // Reduced for more responsive
    public float strengthThreshold = 0.3f;  // Lowered threshold
    public int maxCommandsPerSecond = 8;    // Increased for better response
    
    [Header("Visual Feedback")]
    public Material defaultMaterial;
    public Material activeMaterial;
    
    [Header("Physics Settings")]
    public float gravityMultiplier = 2f; // 1 = normal, >1 = stronger gravity
    
    private Rigidbody rb;
    private Renderer playerRenderer;
    private bool isGrounded = false;
    
    // Command debouncing and smoothing
    private string lastCommand = "neutral";
    private float lastCommandTime = 0f;
    private float commandTime = 0f;
    private System.Collections.Generic.Queue<float> commandTimestamps = new System.Collections.Generic.Queue<float>();
    private bool isCommandOnCooldown = false;

    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody for better movement
        rb.mass = 1f;
        rb.linearDamping = 5f;      // Stronger damping
        rb.angularDamping = 5f;     // Stronger angular damping
        rb.linearDamping = 0f;               // Set drag to 0 for normal gravity effect
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Make sure player starts above terrain (Y=1 for capsule, but for terrain, use Y=terrain height)
        transform.position = new Vector3(transform.position.x, 1f, transform.position.z);

        // For Unity Terrain, set Y=terrain height (usually 0)
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float terrainHeight = terrain.SampleHeight(transform.position);
            transform.position = new Vector3(transform.position.x, terrainHeight + 1f, transform.position.z);
        }

        // For testing, set gravityMultiplier to 1 (normal gravity)
        gravityMultiplier = 1f;

        // Adjust CapsuleCollider center if needed
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.center = new Vector3(0, 0, 0); // Center at origin
            col.height = 2f;
            col.radius = 0.5f;
        }

        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null)
        {
            playerRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // Subscribe to BCI events - try both event systems
        if (FindFirstObjectByType<WorkingBCIServer>() != null)
        {
            WorkingBCIServer.OnCommandReceived += HandleMentalCommand;
        }

        // Optionally, set global gravity (affects all objects):
        // Physics.gravity = new Vector3(0, -9.81f * gravityMultiplier, 0);
        // For per-player gravity, use FixedUpdate below.

        Debug.Log("✅ Improved BCI Player initialized with better movement");
    }
    
    void FixedUpdate()
    {
        // Apply custom gravity force per player
        if (rb != null && gravityMultiplier > 1f)
        {
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        }

        // Debug: print velocity every physics frame
        Debug.Log($"Player velocity: {rb.linearVelocity}");
    }
    
    void HandleMentalCommand(string command, float strength)
    {
        // Skip if command is too weak
        if (strength < strengthThreshold)
        {
            Debug.Log($"⚠️ Command too weak: {command} ({strength:F2}) < {strengthThreshold}");
            return;
        }
        
        // Skip neutral commands
        if (command.ToLower() == "neutral")
        {
            return;
        }
        
        // Rate limiting - check commands per second
        float currentTime = Time.time;
        commandTimestamps.Enqueue(currentTime);
        
        // Remove old timestamps (older than 1 second)
        while (commandTimestamps.Count > 0 && currentTime - commandTimestamps.Peek() > 1f)
        {
            commandTimestamps.Dequeue();
        }
        
        // Skip if too many commands in the last second
        if (commandTimestamps.Count > maxCommandsPerSecond)
        {
            Debug.Log($"⏳ Rate limiting: Skipping {command} command");
            return;
        }
        
        // Command cooldown - prevent rapid repeated commands
        if (isCommandOnCooldown || (currentTime - lastCommandTime < commandCooldown))
        {
            Debug.Log($"⏳ Cooldown: Skipping {command} command");
            return;
        }
        
        // Different cooldown for same vs different commands
        if (command == lastCommand && currentTime - lastCommandTime < commandCooldown * 1.5f)
        {
            Debug.Log($"⏳ Same command cooldown: {command}");
            return;
        }
        
        lastCommand = command;
        lastCommandTime = currentTime;
        commandTime = currentTime;
        
        // Execute command on main thread
        ExecuteCommand(command, strength);
        
        // Start cooldown
        StartCoroutine(CommandCooldownCoroutine());
    }
    
    private System.Collections.IEnumerator CommandCooldownCoroutine()
    {
        isCommandOnCooldown = true;
        yield return new WaitForSeconds(commandCooldown);
        isCommandOnCooldown = false;
    }
    
    void ExecuteCommand(string command, float strength)
    {
        // Scale strength for consistent movement
        float scaledStrength = Mathf.Clamp(strength, 0.3f, 1f);
        Vector3 force = Vector3.zero;
        
        switch (command.ToLower())
        {
            case "left":
                force = Vector3.left * moveSpeed * scaledStrength * movementForceMultiplier;
                Debug.Log($"🔴 LEFT FORCE: {force} (strength: {strength:F2})");
                break;
            case "right":
                force = Vector3.right * moveSpeed * scaledStrength * movementForceMultiplier;
                Debug.Log($"🔵 RIGHT FORCE: {force} (strength: {strength:F2})");
                break;
            case "push":
                force = Vector3.forward * moveSpeed * scaledStrength * movementForceMultiplier;
                Debug.Log($"🟢 FORWARD FORCE: {force} (strength: {strength:F2})");
                break;
            case "pull":
                force = Vector3.back * moveSpeed * scaledStrength * movementForceMultiplier;
                Debug.Log($"🟡 BACK FORCE: {force} (strength: {strength:F2})");
                break;
            case "lift":
                if (isGrounded)
                {
                    Vector3 jumpForceVector = Vector3.up * jumpForce * scaledStrength;
                    rb.AddForce(jumpForceVector, ForceMode.Impulse);
                    Debug.Log($"🚀 JUMP: {jumpForceVector} (grounded: {isGrounded})");
                }
                else
                {
                    Debug.Log("⚠️ Jump ignored - not grounded");
                }
                break;
        }
        
        // Apply horizontal movement force
        if (force != Vector3.zero)
        {
            // Use ForceMode.Force for continuous movement
            rb.AddForce(force, ForceMode.Force);
            Debug.Log($"✅ Applied force: {force.magnitude:F1} in direction {force.normalized}");
            Debug.Log($"[TRACE] ExecuteCommand called from:\n{System.Environment.StackTrace}");
        }
        
        // Visual feedback
        if (activeMaterial != null && playerRenderer != null)
        {
            playerRenderer.material = activeMaterial;
            Invoke("ResetMaterial", 0.3f);
        }
        
        Debug.Log($"🎮 BCI Command: {command.ToUpper()} | Strength: {strength:F2} | Position: {transform.position}");
    }
    
    void ResetMaterial()
    {
        if (defaultMaterial != null && playerRenderer != null)
        {
            playerRenderer.material = defaultMaterial;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            Debug.Log("✅ Player grounded");
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            Debug.Log("⚠️ Player airborne");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (FindFirstObjectByType<WorkingBCIServer>() != null)
        {
            WorkingBCIServer.OnCommandReceived -= HandleMentalCommand;
        }
    }
    
    // Public method for testing commands
    public void TestCommand(string command, float strength)
    {
        Debug.Log($"🧪 Testing command: {command} with strength {strength}");
        HandleMentalCommand(command, strength);
    }
    
    // Add these public methods to allow movement/jump from InputSystemPlayerController
    public void Move(Vector3 direction, float strength = 1f)
    {
        // Ignore tiny movement vectors (dead zone)
        if (direction.sqrMagnitude < 0.01f) return;

        float scaledStrength = Mathf.Clamp(strength, 0.3f, 1f);
        Vector3 force = direction.normalized * moveSpeed * scaledStrength;
        rb.AddForce(force, ForceMode.Force);
        Debug.Log($"Move called: direction={direction}, strength={strength}");
        Debug.Log($"[TRACE] Move called from:\n{System.Environment.StackTrace}");
        // Visual feedback (optional)
        if (activeMaterial != null && playerRenderer != null)
        {
            playerRenderer.material = activeMaterial;
            Invoke("ResetMaterial", 0.2f);
        }
    }

    public void Jump(float strength = 1f)
    {
        if (isGrounded)
        {
            float scaledStrength = Mathf.Clamp(strength, 0.3f, 1f);
            Vector3 jumpForceVector = Vector3.up * jumpForce * scaledStrength;
            rb.AddForce(jumpForceVector, ForceMode.Impulse);
            Debug.Log($"Jump called: strength={strength}");
            Debug.Log($"[TRACE] Jump called from:\n{System.Environment.StackTrace}");
            // Visual feedback (optional)
            if (activeMaterial != null && playerRenderer != null)
            {
                playerRenderer.material = activeMaterial;
                Invoke("ResetMaterial", 0.2f);
            }
        }
    }

    public bool GetIsGrounded()
    {
        return isGrounded;
    }

    // Debug info
    void OnGUI()
    {
        if (Time.time - commandTime < 2f)
        {
            GUI.Label(new Rect(10, 50, 300, 20), $"Last Command: {lastCommand.ToUpper()}");
            GUI.Label(new Rect(10, 70, 300, 20), $"Position: {transform.position}");
            GUI.Label(new Rect(10, 90, 300, 20), $"Velocity: {rb.linearVelocity}");
            GUI.Label(new Rect(10, 110, 300, 20), $"Grounded: {isGrounded}");
        }
    }

    void OnEnable()
    {
        WorkingBCIServer.OnCommandReceived += HandleBCICommand;
    }

    void OnDisable()
    {
        WorkingBCIServer.OnCommandReceived -= HandleBCICommand;
    }

    void HandleBCICommand(string command, float strength)
    {
        ExecuteCommand(command, strength);
    // Add more command handling as needed
    }
}