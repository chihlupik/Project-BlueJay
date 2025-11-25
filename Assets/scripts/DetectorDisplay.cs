using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class DetectorDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI threatLevelText;
    [SerializeField] private TextMeshProUGUI anomalyTypeText;
    [SerializeField] private TextMeshProUGUI anomalyDirectionText;
    [SerializeField] private TextMeshProUGUI distanceText;
    
    [Header("UI Canvases")]
    [SerializeField] private GameObject detectorCanvas;
    [SerializeField] private GameObject listCanvas;
    
    [Header("List UI References")]
    [SerializeField] private TextMeshProUGUI[] listSlots = new TextMeshProUGUI[10]; // A1 to A10
    [SerializeField] private TextMeshProUGUI[] listLevelSlots = new TextMeshProUGUI[10]; // For threat levels
    [SerializeField] private TextMeshProUGUI[] listDistanceSlots = new TextMeshProUGUI[10]; // For distances
    [SerializeField] private TextMeshProUGUI counterText;
    
    [Header("Detection Settings")]
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private float maxDetectionRange = 50f;
    
    [Header("Mode Switching")]
    [SerializeField] private KeyCode holdKey = KeyCode.Mouse1; // Right mouse button (hold)
    [SerializeField] private KeyCode switchKey = KeyCode.Mouse0; // Left mouse button (press)
    [SerializeField] private bool startWithDetector = true;
    
    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip beepSound;
    [SerializeField] private AudioClip criticalSound; // Only for threat level 6
    [SerializeField] private float listModeBeepVolume = 0.7f;
    [SerializeField] private float detectorModeBeepVolume = 1.0f;
    [SerializeField] private float criticalSoundVolume = 1.0f;
    
    private List<Anomaly> allAnomalies = new List<Anomaly>();
    private List<Anomaly> nearbyAnomalies = new List<Anomaly>();
    private List<Anomaly> previousNearbyAnomalies = new List<Anomaly>();
    private Anomaly closestAnomaly = null;
    private Anomaly previousClosestAnomaly = null;
    private bool wasLevel6LastFrame = false;
    private float updateTimer = 0f;
    private Transform playerTransform;
    private bool isDetectorMode = true;
    private bool canSwitchMode = false;

    private void Start()
    {
        // Find all anomalies in the scene
        FindAllAnomalies();
        
        // Try to find player transform automatically
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                playerTransform = Camera.main?.transform;
            }
        }
        
        // Try to find audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Set initial mode
        isDetectorMode = startWithDetector;
        UpdateUIMode();
        
        // Initialize UI - call the correct method based on mode
        if (isDetectorMode)
        {
            UpdateDetectorDisplay();
        }
        else
        {
            UpdateListDisplay();
        }
    }

    private void Update()
    {
        // Check if right mouse button is held down
        bool isRightMouseHeld = Input.GetKey(holdKey);
        
        // Enable mode switching only when right mouse is held
        if (isRightMouseHeld)
        {
            if (!canSwitchMode)
            {
                canSwitchMode = true;
                // Optional: Add visual feedback here
            }
            
            // Check for left mouse button press while right mouse is held
            if (Input.GetKeyDown(switchKey))
            {
                ToggleUIMode();
            }
        }
        else
        {
            if (canSwitchMode)
            {
                canSwitchMode = false;
                // Optional: Remove visual feedback here
            }
        }
        
        // Update detection based on current mode
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            FindNearbyAnomalies();
            
            if (isDetectorMode)
            {
                FindClosestAnomaly();
                UpdateDetectorDisplay();
                HandleDetectorModeBeep();
            }
            else
            {
                UpdateListDisplay();
                HandleListModeBeep();
            }
        }
    }

    private void FindAllAnomalies()
    {
        allAnomalies.Clear();
        Anomaly[] anomalies = FindObjectsOfType<Anomaly>();
        allAnomalies.AddRange(anomalies);
    }

    private void FindNearbyAnomalies()
    {
        if (playerTransform == null) return;

        // Store previous anomalies for comparison
        previousNearbyAnomalies = new List<Anomaly>(nearbyAnomalies);
        nearbyAnomalies.Clear();
        
        foreach (Anomaly anomaly in allAnomalies)
        {
            if (anomaly == null) continue;

            float distance = anomaly.GetDistance(playerTransform.position);
            
            if (distance <= anomaly.DetectionRange && distance <= maxDetectionRange)
            {
                nearbyAnomalies.Add(anomaly);
            }
        }
        
        // Sort by distance (closest first)
        nearbyAnomalies = nearbyAnomalies.OrderBy(a => a.GetDistance(playerTransform.position)).ToList();
    }

    private void FindClosestAnomaly()
    {
        previousClosestAnomaly = closestAnomaly;
        closestAnomaly = nearbyAnomalies.Count > 0 ? nearbyAnomalies[0] : null;
    }

    private string FormatDetectorTypeText(string type)
    {
        // Limit to 12 characters for detector mode
        if (type.Length > 12)
        {
            return type.Substring(0, 12);
        }
        else
        {
            return type;
        }
    }

    private string FormatListTypeText(string type)
    {
        // Limit to 9 characters for list mode
        if (type.Length > 9)
        {
            return type.Substring(0, 9);
        }
        else
        {
            // Pad with spaces to make it exactly 9 characters
            return type.PadRight(9);
        }
    }

    private string GetThreatLevelDisplay(int threatLevel)
    {
        // For level 6, show "RUN!!!", otherwise show squares
        if (threatLevel >= 6)
        {
            return "RUN!!!";
        }
        else
        {
            string symbols = "";
            for (int i = 0; i < threatLevel; i++)
            {
                symbols += "■";
            }
            return symbols;
        }
    }

    private string GetListThreatLevelDisplay(int threatLevel)
    {
        // For level 6, show "!!", otherwise show "Lx"
        if (threatLevel >= 6)
        {
            return "!!!";
        }
        else
        {
            return $"L{threatLevel}";
        }
    }

    private void UpdateDetectorDisplay()
    {
        if (!isDetectorMode) return;
        
        if (closestAnomaly == null)
        {
            threatLevelText.text = "";
            anomalyTypeText.text = "";
            anomalyDirectionText.text = "";
            distanceText.text = "";
        }
        else
        {
            float distance = closestAnomaly.GetDistance(playerTransform.position);
            
            // Show threat level with special display for level 6
            threatLevelText.text = GetThreatLevelDisplay(closestAnomaly.ThreatLevel);
            
            // Show formatted anomaly type in type text (12 character limit)
            string formattedType = FormatDetectorTypeText(closestAnomaly.GetDisplayType());
            anomalyTypeText.text = formattedType;
            
            // Show direction in direction text
            string direction = GetDirectionToAnomaly(closestAnomaly);
            anomalyDirectionText.text = direction;
            
            distanceText.text = GetDistanceSymbols(distance);
        }
    }

    private string GetDirectionToAnomaly(Anomaly anomaly)
    {
        if (playerTransform == null) return "";
        
        Vector3 directionToAnomaly = anomaly.Position - playerTransform.position;
        directionToAnomaly.y = 0; // Ignore vertical difference
        
        if (directionToAnomaly.magnitude < 0.1f) return "HERE";
        
        Vector3 localDirection = playerTransform.InverseTransformDirection(directionToAnomaly.normalized);
        
        // Get angle in degrees
        float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        
        // Convert angle to direction text
        if (angle >= -22.5f && angle < 22.5f) return "FRONT";
        if (angle >= 22.5f && angle < 67.5f) return "FRONT-RIGHT";
        if (angle >= 67.5f && angle < 112.5f) return "RIGHT";
        if (angle >= 112.5f && angle < 157.5f) return "BACK-RIGHT";
        if (angle >= 157.5f || angle < -157.5f) return "BACK";
        if (angle >= -157.5f && angle < -112.5f) return "BACK-LEFT";
        if (angle >= -112.5f && angle < -67.5f) return "LEFT";
        if (angle >= -67.5f && angle < -22.5f) return "FRONT-LEFT";
        
        return "UNKNOWN";
    }

    private void UpdateListDisplay()
    {
        if (isDetectorMode) return;
        
        // Clear all list slots first
        foreach (TextMeshProUGUI slot in listSlots)
        {
            if (slot != null)
                slot.text = "";
        }
        
        // Clear level slots
        foreach (TextMeshProUGUI slot in listLevelSlots)
        {
            if (slot != null)
                slot.text = "";
        }
        
        // Clear distance slots
        foreach (TextMeshProUGUI slot in listDistanceSlots)
        {
            if (slot != null)
                slot.text = "";
        }
        
        // Clear counter
        if (counterText != null)
            counterText.text = "";
        
        // Display formatted anomaly type with separate level and distance slots
        int slotsToFill = Mathf.Min(nearbyAnomalies.Count, listSlots.Length);
        
        for (int i = 0; i < slotsToFill; i++)
        {
            if (listSlots[i] != null)
            {
                Anomaly anomaly = nearbyAnomalies[i];
                float distance = anomaly.GetDistance(playerTransform.position);
                
                // Format type text (9 character limit)
                string formattedType = FormatListTypeText(anomaly.GetDisplayType());
                listSlots[i].text = formattedType;
                
                // Set level in separate slot
                if (i < listLevelSlots.Length && listLevelSlots[i] != null)
                {
                    string threatDisplay = GetListThreatLevelDisplay(anomaly.ThreatLevel);
                    listLevelSlots[i].text = threatDisplay;
                }
                
                // Set distance in separate slot
                if (i < listDistanceSlots.Length && listDistanceSlots[i] != null)
                {
                    listDistanceSlots[i].text = $"D{Mathf.RoundToInt(distance)}";
                }
            }
        }
        
        // Show counter if there are more anomalies than slots
        if (nearbyAnomalies.Count > listSlots.Length)
        {
            int extraCount = nearbyAnomalies.Count - listSlots.Length;
            if (counterText != null)
                counterText.text = $"+{extraCount}";
        }
    }

    private void HandleListModeBeep()
    {
        if (beepSound == null || audioSource == null) return;
        
        // Check for new anomalies that appeared in the list
        foreach (Anomaly newAnomaly in nearbyAnomalies)
        {
            if (!previousNearbyAnomalies.Contains(newAnomaly))
            {
                // Check if it's a level 6 threat
                if (newAnomaly.ThreatLevel >= 6)
                {
                    // Play critical sound for level 6 threats
                    if (criticalSound != null)
                    {
                        audioSource.PlayOneShot(criticalSound, criticalSoundVolume);
                    }
                    else
                    {
                        audioSource.PlayOneShot(beepSound, listModeBeepVolume);
                    }
                }
                else
                {
                    // Normal anomaly - play regular beep
                    audioSource.PlayOneShot(beepSound, listModeBeepVolume);
                }
                break; // Play only one beep per update to avoid multiple sounds at once
            }
        }
    }

    private void HandleDetectorModeBeep()
    {
        if (beepSound == null || audioSource == null) return;
        
        bool isLevel6Now = IsLevel6Threat();
        
        // Play beep when closest anomaly changes or appears
        if (closestAnomaly != previousClosestAnomaly)
        {
            if (closestAnomaly != null)
            {
                // Check if it's a level 6 threat
                if (isLevel6Now)
                {
                    // Play critical sound for level 6 threats
                    if (criticalSound != null)
                    {
                        audioSource.PlayOneShot(criticalSound, criticalSoundVolume);
                    }
                    else
                    {
                        audioSource.PlayOneShot(beepSound, detectorModeBeepVolume);
                    }
                }
                else
                {
                    // Normal anomaly change - play regular beep
                    audioSource.PlayOneShot(beepSound, detectorModeBeepVolume);
                }
            }
        }
        // Also play critical sound when level 6 threat appears (even if same anomaly type)
        else if (isLevel6Now && !wasLevel6LastFrame)
        {
            if (criticalSound != null && closestAnomaly != null)
            {
                audioSource.PlayOneShot(criticalSound, criticalSoundVolume);
            }
        }
        
        wasLevel6LastFrame = isLevel6Now;
    }

    private bool IsLevel6Threat()
    {
        return closestAnomaly != null && closestAnomaly.ThreatLevel >= 6;
    }

    private string GetDistanceSymbols(float distance)
    {
        if (closestAnomaly == null) return "";
        
        float normalizedDistance = 1f - Mathf.Clamp01(distance / closestAnomaly.DetectionRange);
        int symbolCount = Mathf.RoundToInt(normalizedDistance * 6f);
        
        string symbols = "";
        for (int i = 0; i < symbolCount; i++)
        {
            symbols += "■";
        }
        
        return symbols;
    }

    // Mode switching methods
    public void ToggleUIMode()
    {
        isDetectorMode = !isDetectorMode;
        UpdateUIMode();
    }
    
    public void SetDetectorMode()
    {
        isDetectorMode = true;
        UpdateUIMode();
    }
    
    public void SetListMode()
    {
        isDetectorMode = false;
        UpdateUIMode();
    }
    
    private void UpdateUIMode()
    {
        if (detectorCanvas != null)
        {
            detectorCanvas.SetActive(isDetectorMode);
        }
        
        if (listCanvas != null)
        {
            listCanvas.SetActive(!isDetectorMode);
        }
        
        // Reset previous anomaly tracking when switching modes
        previousClosestAnomaly = null;
        previousNearbyAnomalies.Clear();
        wasLevel6LastFrame = false;
        
        // Update display based on current mode
        if (isDetectorMode)
        {
            UpdateDetectorDisplay();
        }
        else
        {
            UpdateListDisplay();
        }
    }

    // Public methods
    public void RegisterAnomaly(Anomaly anomaly)
    {
        if (anomaly != null && !allAnomalies.Contains(anomaly))
        {
            allAnomalies.Add(anomaly);
        }
    }

    public void UnregisterAnomaly(Anomaly anomaly)
    {
        if (allAnomalies.Contains(anomaly))
        {
            allAnomalies.Remove(anomaly);
            if (closestAnomaly == anomaly)
            {
                closestAnomaly = null;
            }
        }
    }

    public void SetPlayerTransform(Transform newPlayerTransform)
    {
        playerTransform = newPlayerTransform;
    }

    public bool IsDetectorMode() => isDetectorMode;
    public bool IsListMode() => !isDetectorMode;
    public bool CanSwitchMode() => canSwitchMode;
    
    // Sound methods
    public void SetBeepSound(AudioClip newBeepSound)
    {
        beepSound = newBeepSound;
    }
    
    public void SetCriticalSound(AudioClip newCriticalSound)
    {
        criticalSound = newCriticalSound;
    }
    
    public void SetListModeBeepVolume(float volume)
    {
        listModeBeepVolume = Mathf.Clamp01(volume);
    }
    
    public void SetDetectorModeBeepVolume(float volume)
    {
        detectorModeBeepVolume = Mathf.Clamp01(volume);
    }
    
    public void SetCriticalSoundVolume(float volume)
    {
        criticalSoundVolume = Mathf.Clamp01(volume);
    }
    
    // Getters for debug information
    public int GetTotalAnomalyCount() => allAnomalies.Count;
    public int GetNearbyAnomalyCount() => nearbyAnomalies.Count;
    public List<Anomaly> GetNearbyAnomalies() => nearbyAnomalies;
    public bool IsLevel6ThreatActive() => IsLevel6Threat();
}