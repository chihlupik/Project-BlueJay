using UnityEngine;

public class Anomaly : MonoBehaviour
{
    [Header("Anomaly Settings")]
    [SerializeField] private string anomalyType = "Unknown";
    [SerializeField] private int threatLevel = 0;
    
    [Header("Threat Level Range")]
    [SerializeField] private int minThreatLevel = 0;
    [SerializeField] private int maxThreatLevel = 6;
    
    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private bool showDebugRange = true;
    
    public string AnomalyType => anomalyType;
    public int ThreatLevel => threatLevel;
    public float DetectionRange => detectionRange;
    public Vector3 Position => transform.position;
    
    private void OnValidate()
    {
        // Clamp threat level to valid range
        threatLevel = Mathf.Clamp(threatLevel, minThreatLevel, maxThreatLevel);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (showDebugRange)
        {
            // Draw detection range in editor
            Gizmos.color = GetThreatColor();
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Draw threat level indicator
            #if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.normal.textColor = GetThreatColor();
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                                    $"{anomalyType}\nThreat: {GetThreatLevelSymbols()}", style);
            #endif
        }
    }
    
    public void SetAnomalyData(string type, int level, float range = 10f)
    {
        anomalyType = type;
        threatLevel = Mathf.Clamp(level, minThreatLevel, maxThreatLevel);
        detectionRange = Mathf.Max(0f, range);
    }
    
    public string GetThreatLevelSymbols()
    {
        return GetThreatLevelSymbols(threatLevel);
    }
    
    public static string GetThreatLevelSymbols(int level)
    {
        string symbols = "";
        int clampedLevel = Mathf.Clamp(level, 0, 6);
        for (int i = 0; i < clampedLevel; i++)
        {
            symbols += "■";
        }
        return symbols;
    }
    
    private Color GetThreatColor()
    {
        switch (threatLevel)
        {
            case 0: return Color.green;
            case 1: return Color.cyan;
            case 2: return Color.blue;
            case 3: return Color.yellow;
            case 4: return new Color(1f, 0.5f, 0f); // Orange
            case 5: return Color.red;
            case 6: return Color.magenta;
            default: return Color.white;
        }
    }
    
    // Getters for UI
    public string GetDisplayThreatLevel() => GetThreatLevelSymbols();
    public string GetDisplayType() => anomalyType;
    
    // Method to check if position is within detection range
    public bool IsInRange(Vector3 position)
    {
        return Vector3.Distance(transform.position, position) <= detectionRange;
    }
    
    // Method to get distance to position
    public float GetDistance(Vector3 position)
    {
        return Vector3.Distance(transform.position, position);
    }
}