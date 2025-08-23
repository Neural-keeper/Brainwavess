using UnityEngine;
using TMPro;

public class EndPointTrigger : MonoBehaviour
{
    public TMP_Text winText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (winText != null) {
            winText.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (winText != null)
            {
                winText.gameObject.SetActive(true);
            }

            Debug.Log("You win!");
            Time.timeScale = 0f;
        }
    }
}