using UnityEngine;
using System.Collections;

public class GrapplePoint : MonoBehaviour
{
    [Header("Settings")]
    public bool destroyOnTouch = true;
    public float respawnTime = 2f;
    
    [Header("Visual")]
    public GameObject touchEffect;
    public Color gizmoColor = Color.green;
    public float gizmoRadius = 0.5f;
    public bool showGizmo = true;
    
    [Header("Sound")]
    public AudioClip touchSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;
    
    private MeshRenderer meshRenderer;
    private Collider pointCollider;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private AudioSource audioSource;
    private bool isAvailable = true;
    
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        pointCollider = GetComponent<Collider>();
        originalScale = transform.localScale;
        originalPosition = transform.position;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && touchSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        if (pointCollider != null && !pointCollider.isTrigger)
        {
            pointCollider.isTrigger = true;
        }
    }
    
    public void OnPlayerTouch(PlayerController player)
    {
        if (!isAvailable) return;
        
        // Эффект
        if (touchEffect != null)
        {
            GameObject effect = Instantiate(touchEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Звук
        if (audioSource != null && touchSound != null)
        {
            audioSource.PlayOneShot(touchSound, soundVolume);
        }
        
        // Даем прыжок игроку
        player.GrantJump();
        
        // Удаляем точку
        if (destroyOnTouch)
        {
            Deactivate();
            if (respawnTime > 0)
            {
                Invoke(nameof(Reactivate), respawnTime);
            }
        }
    }
    
    void Deactivate()
    {
        isAvailable = false;
        if (meshRenderer != null) meshRenderer.enabled = false;
        if (pointCollider != null) pointCollider.enabled = false;
    }
    
    void Reactivate()
    {
        isAvailable = true;
        if (meshRenderer != null) meshRenderer.enabled = true;
        if (pointCollider != null) pointCollider.enabled = true;
        transform.localScale = originalScale;
        transform.position = originalPosition;
    }
    
    public bool IsAvailable() => isAvailable;
    
    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}