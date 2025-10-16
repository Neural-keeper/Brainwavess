using UnityEngine;

/// <summary>
/// Complete BCI scene setup - fixes falling through ground
/// Run this script to automatically setup player and ground
/// </summary>
public class BCISceneSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool setupOnStart = true;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupBCIScene();
        }
    }
    
    [ContextMenu("Setup BCI Scene")]
    public void SetupBCIScene()
    {
        Debug.Log("Setting up BCI Scene...");
        
        // 1. Create ground
        CreateGround();
        
        // 2. Find or create player
        GameObject player = SetupPlayer();
        
        // 3. Setup camera
        SetupCamera(player);
        
        // 4. Add BCI components
        SetupBCIComponents(player);
        
        Debug.Log("✅ BCI Scene setup complete!");
    }
    
    void CreateGround()
    {
        // Check if ground already exists
        GameObject existingGround = GameObject.FindGameObjectWithTag("Ground");
        if (existingGround != null)
        {
            Debug.Log("Ground already exists");
            return;
        }
        
        // Create ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.tag = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
        
        // Ground material
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        groundRenderer.material.color = new Color(0.2f, 0.6f, 0.2f);
        
        // Ensure proper collider
        Collider groundCollider = ground.GetComponent<Collider>();
        groundCollider.isTrigger = false;
        
        Debug.Log("✅ Ground created");
    }
    
    GameObject SetupPlayer()
    {
        // Find existing player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            // Create new player
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "BCIPlayer";
            player.tag = "Player";
        }
        
        // Position player above ground so capsule sits on ground
        player.transform.position = new Vector3(0, 1f, 0); // Y = half capsule height
        
        // Setup physics
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = player.AddComponent<Rigidbody>();
        }
        
        // Proper Rigidbody settings
        rb.mass = 1f;
        rb.linearDamping = 0.3f;
        rb.angularDamping = 0.05f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Ensure collider is not trigger and sits on ground
        CapsuleCollider col = player.GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.isTrigger = false;
            col.center = new Vector3(0, 0, 0); // Center at origin
            col.height = 2f;                   // Default Unity capsule height
            col.radius = 0.5f;                 // Default Unity capsule radius
        }
        
        Debug.Log("✅ Player setup complete");
        return player;
    }
    
    void SetupCamera(GameObject player)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
        }
        
        // Position camera to follow player
        mainCamera.transform.position = new Vector3(0, 5f, -10f);
        mainCamera.transform.LookAt(player.transform);
        
        Debug.Log("✅ Camera setup complete");
    }
    
    void SetupBCIComponents(GameObject player)
    {
        // Add BCI server if not exists
        GameObject serverObj = GameObject.Find("BCIServer");
        if (serverObj == null)
        {
            serverObj = new GameObject("BCIServer");
            serverObj.AddComponent<WorkingBCIServer>();
        }
        
        // (Removed SimpleBCIPlayer references)
        
        Debug.Log("✅ BCI components added");
    }
}