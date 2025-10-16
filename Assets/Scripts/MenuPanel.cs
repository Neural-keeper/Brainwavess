using UnityEngine;

public class MenuPanel : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel; // Assign your menu panel in Inspector

    void Update()
    {
        // Check if Escape was pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Toggle the panel
            bool isActive = menuPanel.activeSelf;
            menuPanel.SetActive(!isActive);
        }
    }
}

