using UnityEngine;

public class KeyCollect : MonoBehaviour
{
    private KeyCounterUI keyCounter;

    public AudioClip collectSound;
    public AudioClip unlockSound;

    private void Start()
    {
        // Find the KeyCounterUI in the scene
        keyCounter = FindObjectOfType<KeyCounterUI>();

        if (keyCounter == null)
        {
            Debug.LogWarning("KeyCollect: No KeyCounterUI found in the scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ----- KEY PICKUP -----
        if (other.CompareTag("Key"))
        {
            Debug.Log($"{name} collected a key!");
            keyCounter?.AddKey(); // Increment the key count
            AudioManager.Instance.PlaySFX(collectSound, 0.4f);
            Destroy(other.gameObject); // Remove the key from the scene
            return;
        }

        // ----- LOCKED DOOR INTERACTION -----
        if (other.CompareTag("LockedDoor"))
        {
            if (keyCounter != null && keyCounter.HasKey())
            {
                Debug.Log($"{name} used a key to unlock a door!");
                keyCounter.RemoveKey(); // Spend one key
                AudioManager.Instance.PlaySFX(unlockSound, 0.5f);
                Destroy(other.gameObject); // Open (destroy) the locked door
            }
            else
            {
                Debug.Log($"{name} tried to open a locked door but has no keys!");
            }
        }

        //----- TREASURE INTERACTION -----
        if (other.CompareTag("Treasure"))
        {
            Debug.Log($"{name} reached the Treasure!");
            
            GameManager.Instance.TriggerGameWin();
        }
    }
}