using UnityEngine;

public class Key : MonoBehaviour
{
    [Header("Key Settings")]
    public int keyID = 1;
    
    [Header("Key Sounds")]
    public AudioSource audioSource;
    public AudioClip unlockSound;
    public AudioClip wrongKeySound;
    
    private bool isUsed = false;
    
    void OnCollisionEnter(Collision collision)
    {
        if (isUsed) return;
        
        if (collision.gameObject.name == "doormodel")
        {
            Door door = collision.gameObject.GetComponentInParent<Door>();
            if (door != null)
            {
                if (door.TryUnlockWithKey(keyID))
                {
                    PlaySound(unlockSound);
                    isUsed = true;
                    
                    gameObject.SetActive(false);
                    Destroy(gameObject, 2f);
                }
                else
                {
                    PlaySound(wrongKeySound);
                }
            }
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public int GetKeyID() { return keyID; }
}