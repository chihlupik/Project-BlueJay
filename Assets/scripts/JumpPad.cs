using UnityEngine;
using System.Collections;

public class JumpPad : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpHeight = 15f;
    public bool destroyOnTouch = true;
    public float respawnTime = 2f;
    
    [Header("Visual")]
    public GameObject touchEffect;
    public Color gizmoColor = Color.magenta;
    public float gizmoRadius = 0.5f;
    public bool showGizmo = true;
    
    [Header("Sound")]
    public AudioClip jumpSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;
    public float soundPitch = 1.2f;
    
    private MeshRenderer meshRenderer;
    private Collider padCollider;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private AudioSource audioSource;
    private bool isAvailable = true;
    
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        padCollider = GetComponent<Collider>();
        originalScale = transform.localScale;
        originalPosition = transform.position;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && jumpSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        if (padCollider != null && !padCollider.isTrigger)
        {
            padCollider.isTrigger = true;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && isAvailable)
        {
            OnPlayerTouch(player);
        }
    }
    
    public void OnPlayerTouch(PlayerController player)
    {
        if (!isAvailable) return;
        
        // Apply jump (higher than normal)
        if (player != null)
        {
            player.ApplyJumpPadBoost(jumpHeight);
        }
        
        // Visual effect
        if (touchEffect != null)
        {
            GameObject effect = Instantiate(touchEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Sound effect
        if (audioSource != null && jumpSound != null)
        {
            audioSource.pitch = soundPitch;
            audioSource.PlayOneShot(jumpSound, soundVolume);
        }
        
        // Destroy or deactivate the jump pad
        if (destroyOnTouch)
        {
            StartCoroutine(DelayedDeactivate());
        }
    }
    
    IEnumerator DelayedDeactivate()
    {
        // Wait one frame to ensure jump is processed
        yield return null;
        Deactivate();
        if (respawnTime > 0)
        {
            Invoke(nameof(Reactivate), respawnTime);
        }
    }
    
    void Deactivate()
    {
        isAvailable = false;
        if (meshRenderer != null) meshRenderer.enabled = false;
        if (padCollider != null) padCollider.enabled = false;
    }
    
    void Reactivate()
    {
        isAvailable = true;
        if (meshRenderer != null) meshRenderer.enabled = true;
        if (padCollider != null) padCollider.enabled = true;
        transform.localScale = originalScale;
        transform.position = originalPosition;
    }
    
    public bool IsAvailable() => isAvailable;
    
    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        
        // Draw an upward arrow to indicate jump direction
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f);
        Vector3 arrowStart = transform.position + Vector3.up * 0.2f;
        Vector3 arrowEnd = transform.position + Vector3.up * 1f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        
        // Arrow head
        Vector3 arrowDirection = Vector3.up;
        Vector3 right = Vector3.Cross(Vector3.forward, arrowDirection).normalized;
        Vector3 up = Vector3.Cross(arrowDirection, right).normalized;
        Gizmos.DrawLine(arrowEnd, arrowEnd - (arrowDirection * 0.2f) + (right * 0.1f));
        Gizmos.DrawLine(arrowEnd, arrowEnd - (arrowDirection * 0.2f) - (right * 0.1f));
    }
}