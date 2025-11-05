using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 90f;
    public float smoothness = 5f;
    public bool startLocked = true;
    public int doorID = 1;
    
    [Header("Door Sounds")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    public AudioClip unlockSound;
    public AudioClip wrongKeySound;
    
    [Header("Visual Feedback")]
    public Material lockedMaterial;
    public Material unlockedMaterial;
    public Renderer doorRenderer;
    
    [Header("Player Restrictions")]
    public bool allowPlayerToClose = false; // Set this to false to restrict player from closing
    
    private bool isLocked = true;
    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Material originalMaterial;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        isLocked = startLocked;
        
        if (doorRenderer != null)
        {
            originalMaterial = doorRenderer.material;
            UpdateDoorAppearance();
        }
    }

    void Update()
    {
        if (isOpen)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, openRotation, smoothness * Time.deltaTime);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, closedRotation, smoothness * Time.deltaTime);
        }
    }

    public void ToggleDoor()
    {
        if (isLocked)
        {
            PlaySound(lockedSound);
            return;
        }

        // Restrict closing if door is open and player isn't allowed to close
        if (isOpen && !allowPlayerToClose)
        {
            Debug.Log("Player cannot close this door");
            return;
        }

        isOpen = !isOpen;
        
        if (isOpen)
        {
            PlaySound(openSound);
        }
        else
        {
            PlaySound(closeSound);
        }
    }

    // Public method to close door (can be called by other systems)
    public void CloseDoor()
    {
        if (isOpen)
        {
            isOpen = false;
            PlaySound(closeSound);
            Debug.Log("Door closed by system");
        }
    }

    // Public method to open door
    public void OpenDoor()
    {
        if (isLocked)
        {
            PlaySound(lockedSound);
            return;
        }

        if (!isOpen)
        {
            isOpen = true;
            PlaySound(openSound);
        }
    }

    public bool TryUnlockWithKey(int keyID)
    {
        if (!isLocked) 
        {
            return true;
        }
        
        if (keyID == doorID)
        {
            isLocked = false;
            UpdateDoorAppearance();
            PlaySound(unlockSound);
            return true;
        }
        else
        {
            PlaySound(wrongKeySound);
            Debug.Log("Wrong key! Door ID: " + doorID + ", Key ID: " + keyID);
            return false;
        }
    }

    public void UnlockDoor()
    {
        isLocked = false;
        UpdateDoorAppearance();
        PlaySound(unlockSound);
        Debug.Log("Door has been unlocked!");
    }

    public void LockDoor()
    {
        isLocked = true;
        UpdateDoorAppearance();
        Debug.Log("Door has been locked!");
    }

    void UpdateDoorAppearance()
    {
        if (doorRenderer != null && lockedMaterial != null)
        {
            doorRenderer.material = isLocked ? lockedMaterial : originalMaterial;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Method to allow other systems to control player closing permission
    public void SetPlayerCanClose(bool canClose)
    {
        allowPlayerToClose = canClose;
        Debug.Log("Player close permission set to: " + canClose);
    }

    // Getters for door state
    public bool IsLocked() { return isLocked; }
    public bool IsOpen() { return isOpen; }
    public int GetDoorID() { return doorID; }
    
    // Force open/close methods for system use
    public void ForceOpenDoor()
    {
        isOpen = true;
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
    }
    
    public void ForceCloseDoor()
    {
        isOpen = false;
        if (audioSource != null && closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
    }
}