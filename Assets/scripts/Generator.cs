using UnityEngine;
using System.Collections.Generic;

public class Generator : MonoBehaviour
{
    [Header("Platform Prefabs")]
    public GameObject[] platformPrefabs;     // Array of regular platforms
    public GameObject spawnPlatform;         // Special spawn platform (assign in editor)
    public GameObject cornerPlatform;        // L-shaped corner piece
    public GameObject straightPlatform;      // Long straight platform
    public GameObject smallPlatform;         // Small jump platform
    
    [Header("Generation Settings")]
    public int width = 20;                    // Increased default width
    public int depth = 20;                    // Increased default depth
    public float tileSize = 10f;
    public float platformHeight = 5f;
    
    [Header("Platform Distribution")]
    [Range(0, 100)] public int straightChance = 40;
    [Range(0, 100)] public int cornerChance = 30;
    [Range(0, 100)] public int smallChance = 30;
    [Range(0, 100)] public int regularChance = 20; // Chance to use regular array platforms
    
    [Header("Generation Control")]
    public int targetPlatformCount = 200;      // How many platforms to generate
    public int maxBranchingDepth = 10;         // How far to branch out
    public float branchChance = 0.3f;          // Chance to create a branch
    
    [Header("Jump Challenge Settings")]
    public int minJumpGap = 2;
    public int maxJumpGap = 4;
    public int minJumpChainLength = 2;
    public int maxJumpChainLength = 5;
    public float jumpChallengeChance = 0.2f;
    
    private bool[,] grid;
    private List<Vector2Int> allPlatformPositions = new List<Vector2Int>();
    private Queue<Vector2Int> pendingExpansion = new Queue<Vector2Int>();
    private Vector2Int spawnPos;
    
    void Start()
    {
        GenerateLevel();
    }
    
    void GenerateLevel()
    {
        // Clear existing platforms
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }
        
        // Initialize grid
        grid = new bool[width, depth];
        allPlatformPositions.Clear();
        pendingExpansion.Clear();
        
        // Set spawn position at center (or approximately center)
        spawnPos = new Vector2Int(width / 2, depth / 2);
        
        // Place spawn platform
        allPlatformPositions.Add(spawnPos);
        grid[spawnPos.x, spawnPos.y] = true;
        
        // Instantiate spawn platform immediately
        Vector3 spawnWorldPos = new Vector3(spawnPos.x * tileSize, platformHeight, spawnPos.y * tileSize);
        Instantiate(spawnPlatform, spawnWorldPos, Quaternion.identity, transform);
        
        // Add spawn position to expansion queue
        pendingExpansion.Enqueue(spawnPos);
        
        // Generate the rest of the level
        GenerateFromSpawn();
        
        // Place all remaining platforms
        PlacePlatforms();
        
        Debug.Log($"Generated {allPlatformPositions.Count} platforms total");
    }
    
    void GenerateFromSpawn()
    {
        int platformsGenerated = 1; // Started with spawn platform
        
        while (platformsGenerated < targetPlatformCount && pendingExpansion.Count > 0)
        {
            Vector2Int currentPos = pendingExpansion.Dequeue();
            
            // Get available directions from current position
            List<Vector2Int> availableDirs = GetAvailableDirections(currentPos);
            
            // Shuffle directions for randomness
            ShuffleList(availableDirs);
            
            // Determine how many paths to create from this position (1-3 usually)
            int pathsToCreate = Mathf.Min(availableDirs.Count, Random.Range(1, 4));
            
            for (int i = 0; i < pathsToCreate && platformsGenerated < targetPlatformCount; i++)
            {
                if (i < availableDirs.Count)
                {
                    Vector2Int direction = availableDirs[i];
                    
                    // Decide if this should be a jump challenge or normal path
                    if (Random.value < jumpChallengeChance && platformsGenerated < targetPlatformCount - minJumpChainLength)
                    {
                        platformsGenerated = CreateJumpChain(currentPos, direction, platformsGenerated);
                    }
                    else
                    {
                        platformsGenerated = CreateNormalPath(currentPos, direction, platformsGenerated);
                    }
                }
            }
            
            // Occasionally create a branch from this position
            if (Random.value < branchChance && platformsGenerated < targetPlatformCount)
            {
                CreateBranch(currentPos, platformsGenerated);
            }
        }
        
        // If we still need more platforms, fill randomly but connected to existing ones
        while (platformsGenerated < targetPlatformCount)
        {
            FillRemainingPlatforms(ref platformsGenerated);
        }
    }
    
    int CreateNormalPath(Vector2Int startPos, Vector2Int direction, int platformsGenerated)
    {
        // Determine path length (3-8 platforms)
        int pathLength = Random.Range(3, 9);
        
        Vector2Int currentPos = startPos;
        
        for (int step = 1; step <= pathLength && platformsGenerated < targetPlatformCount; step++)
        {
            Vector2Int nextPos = currentPos + direction;
            
            // Check bounds and availability
            if (IsValidPosition(nextPos) && !grid[nextPos.x, nextPos.y])
            {
                // Add platform
                allPlatformPositions.Add(nextPos);
                grid[nextPos.x, nextPos.y] = true;
                platformsGenerated++;
                
                // Add to expansion queue for further generation
                pendingExpansion.Enqueue(nextPos);
                
                currentPos = nextPos;
                
                // Chance to change direction slightly
                if (step < pathLength && Random.value < 0.3f)
                {
                    direction = GetSlightDirectionChange(direction);
                }
            }
            else
            {
                break; // Stop if we hit boundary or existing platform
            }
        }
        
        return platformsGenerated;
    }
    
    int CreateJumpChain(Vector2Int startPos, Vector2Int direction, int platformsGenerated)
    {
        int gapSize = Random.Range(minJumpGap, maxJumpGap + 1);
        int chainLength = Random.Range(minJumpChainLength, maxJumpChainLength + 1);
        
        Vector2Int currentPos = startPos;
        
        for (int i = 0; i < chainLength && platformsGenerated < targetPlatformCount; i++)
        {
            Vector2Int jumpPos = currentPos + direction * (gapSize + i + 1);
            
            if (IsValidPosition(jumpPos) && !grid[jumpPos.x, jumpPos.y])
            {
                allPlatformPositions.Add(jumpPos);
                grid[jumpPos.x, jumpPos.y] = true;
                platformsGenerated++;
                
                // Mark as small platform (we'll handle type later)
                pendingExpansion.Enqueue(jumpPos);
            }
            else
            {
                break;
            }
        }
        
        return platformsGenerated;
    }
    
    void CreateBranch(Vector2Int startPos, int platformsGenerated)
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };
        
        // Find a free direction for branching
        foreach (Vector2Int dir in directions)
        {
            Vector2Int branchPos = startPos + dir;
            
            if (IsValidPosition(branchPos) && !grid[branchPos.x, branchPos.y])
            {
                allPlatformPositions.Add(branchPos);
                grid[branchPos.x, branchPos.y] = true;
                pendingExpansion.Enqueue(branchPos);
                break;
            }
        }
    }
    
    void FillRemainingPlatforms(ref int platformsGenerated)
    {
        // Try to add platforms adjacent to existing ones
        List<Vector2Int> possiblePositions = new List<Vector2Int>();
        
        foreach (Vector2Int pos in allPlatformPositions)
        {
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1)
            };
            
            foreach (Vector2Int neighbor in neighbors)
            {
                if (IsValidPosition(neighbor) && !grid[neighbor.x, neighbor.y])
                {
                    possiblePositions.Add(neighbor);
                }
            }
        }
        
        if (possiblePositions.Count > 0)
        {
            Vector2Int newPos = possiblePositions[Random.Range(0, possiblePositions.Count)];
            allPlatformPositions.Add(newPos);
            grid[newPos.x, newPos.y] = true;
            pendingExpansion.Enqueue(newPos);
            platformsGenerated++;
        }
    }
    
    List<Vector2Int> GetAvailableDirections(Vector2Int pos)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        
        Vector2Int[] possibleDirs = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };
        
        foreach (Vector2Int dir in possibleDirs)
        {
            Vector2Int nextPos = pos + dir;
            if (IsValidPosition(nextPos) && !grid[nextPos.x, nextPos.y])
            {
                // Check if this direction already has too many platforms
                int platformsInDirection = CountPlatformsInDirection(nextPos, dir);
                if (platformsInDirection < maxBranchingDepth)
                {
                    directions.Add(dir);
                }
            }
        }
        
        return directions;
    }
    
    int CountPlatformsInDirection(Vector2Int start, Vector2Int direction)
    {
        int count = 0;
        Vector2Int current = start;
        
        while (IsValidPosition(current) && count < maxBranchingDepth)
        {
            if (grid[current.x, current.y])
                count++;
            current += direction;
        }
        
        return count;
    }
    
    Vector2Int GetSlightDirectionChange(Vector2Int currentDir)
    {
        // 70% chance to continue straight, 30% chance to turn
        if (Random.value < 0.7f)
            return currentDir;
        
        // Turn perpendicular
        if (currentDir.x != 0) // Moving horizontally
        {
            return Random.value < 0.5f ? new Vector2Int(0, 1) : new Vector2Int(0, -1);
        }
        else // Moving vertically
        {
            return Random.value < 0.5f ? new Vector2Int(1, 0) : new Vector2Int(-1, 0);
        }
    }
    
    bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < depth;
    }
    
    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    
    void PlacePlatforms()
    {
        // Skip the first platform (spawn) since it's already placed
        for (int i = 1; i < allPlatformPositions.Count; i++)
        {
            Vector2Int pos = allPlatformPositions[i];
            Vector3 worldPos = new Vector3(pos.x * tileSize, platformHeight, pos.y * tileSize);
            
            // Determine platform type
            GameObject platformType = DeterminePlatformType(pos, i);
            Quaternion rotation = DetermineRotation(pos, i, platformType);
            
            Instantiate(platformType, worldPos, rotation, transform);
        }
    }
    
    GameObject DeterminePlatformType(Vector2Int pos, int index)
    {
        // Chance to use regular array platforms
        if (Random.value < regularChance / 100f && platformPrefabs.Length > 0)
        {
            return platformPrefabs[Random.Range(0, platformPrefabs.Length)];
        }
        
        // Check if this is part of a jump chain
        if (index > 0 && index < allPlatformPositions.Count - 1)
        {
            Vector2Int prev = allPlatformPositions[index - 1];
            Vector2Int next = allPlatformPositions[index + 1];
            
            float distanceToPrev = Vector2Int.Distance(pos, prev);
            float distanceToNext = Vector2Int.Distance(pos, next);
            
            if (distanceToPrev > 1.1f || distanceToNext > 1.1f)
            {
                return smallPlatform;
            }
        }
        
        // Check for corners
        if (index > 0 && index < allPlatformPositions.Count - 1)
        {
            Vector2Int prev = allPlatformPositions[index - 1];
            Vector2Int next = allPlatformPositions[index + 1];
            
            Vector2Int dir1 = prev - pos;
            Vector2Int dir2 = next - pos;
            
            if (dir1.x != dir2.x && dir1.y != dir2.y)
            {
                if (Mathf.Abs(dir1.x) <= 1 && Mathf.Abs(dir1.y) <= 1 &&
                    Mathf.Abs(dir2.x) <= 1 && Mathf.Abs(dir2.y) <= 1)
                {
                    return cornerPlatform;
                }
            }
        }
        
        // Random distribution
        int roll = Random.Range(0, 100);
        if (roll < straightChance)
            return straightPlatform;
        else if (roll < straightChance + cornerChance)
            return cornerPlatform;
        else
            return smallPlatform;
    }
    
    Quaternion DetermineRotation(Vector2Int pos, int index, GameObject platformType)
    {
        if (platformType == smallPlatform || (platformPrefabs.Length > 0 && System.Array.Exists(platformPrefabs, p => p == platformType)))
        {
            return Quaternion.identity;
        }
        
        if (index > 0 && index < allPlatformPositions.Count - 1)
        {
            Vector2Int prev = allPlatformPositions[index - 1];
            Vector2Int next = allPlatformPositions[index + 1];
            
            Vector2Int dirFromPrev = pos - prev;
            Vector2Int dirToNext = next - pos;
            
            if (platformType == straightPlatform)
            {
                if (Mathf.Abs(dirFromPrev.x) > 0 || Mathf.Abs(dirToNext.x) > 0)
                {
                    return Quaternion.Euler(0, 0, 0);
                }
                else
                {
                    return Quaternion.Euler(0, 90, 0);
                }
            }
            else if (platformType == cornerPlatform)
            {
                if (dirFromPrev.x > 0 && dirToNext.y > 0)
                    return Quaternion.Euler(0, 0, 0);
                else if (dirFromPrev.x > 0 && dirToNext.y < 0)
                    return Quaternion.Euler(0, 270, 0);
                else if (dirFromPrev.x < 0 && dirToNext.y > 0)
                    return Quaternion.Euler(0, 90, 0);
                else if (dirFromPrev.x < 0 && dirToNext.y < 0)
                    return Quaternion.Euler(0, 180, 0);
                else if (dirFromPrev.y > 0 && dirToNext.x > 0)
                    return Quaternion.Euler(0, 0, 0);
                else if (dirFromPrev.y > 0 && dirToNext.x < 0)
                    return Quaternion.Euler(0, 90, 0);
                else if (dirFromPrev.y < 0 && dirToNext.x > 0)
                    return Quaternion.Euler(0, 270, 0);
                else if (dirFromPrev.y < 0 && dirToNext.x < 0)
                    return Quaternion.Euler(0, 180, 0);
            }
        }
        
        return Quaternion.identity;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw grid
        Gizmos.color = Color.yellow;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < depth; y++)
            {
                Vector3 center = new Vector3(x * tileSize, platformHeight, y * tileSize);
                Vector3 size = new Vector3(tileSize * 0.9f, 0.5f, tileSize * 0.9f);
                Gizmos.DrawWireCube(center, size);
            }
        }
        
        // Draw spawn platform marker
        Vector3 spawnCenter = new Vector3((width / 2) * tileSize, platformHeight + 2f, (depth / 2) * tileSize);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(spawnCenter, 2f);
        
        // Draw path in play mode
        if (Application.isPlaying && allPlatformPositions != null)
        {
            Gizmos.color = Color.green;
            foreach (Vector2Int pos in allPlatformPositions)
            {
                Vector3 center = new Vector3(pos.x * tileSize, platformHeight + 1f, pos.y * tileSize);
                Gizmos.DrawSphere(center, 0.5f);
            }
        }
    }
}