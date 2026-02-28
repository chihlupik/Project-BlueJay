using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Generator : MonoBehaviour
{
    [Header("Platform Prefabs")]
    public GameObject[] platformPrefabs;     // Array of regular platforms
    public GameObject spawnPlatform;         // Special spawn platform (assign in editor)
    public GameObject cornerPlatform;        // L-shaped corner piece
    public GameObject straightPlatform;      // Long straight platform
    public GameObject smallPlatform;         // Small jump platform
    
    [Header("Player")]
    public GameObject playerPrefab;          // Player prefab to instantiate
    public Vector3 playerSpawnOffset = new Vector3(0, 1f, 0); // Offset from platform center
    
    [Header("Generation Settings")]
    public int width = 20;                    // Increased default width
    public int depth = 20;                    // Increased default depth
    public float tileSize = 10f;
    public float platformHeight = 5f;
    
    [Header("Platform Distribution")]
    [Range(0, 100)] public int straightChance = 60;  // Increased for more regular paths
    [Range(0, 100)] public int cornerChance = 25;    // Slightly reduced
    [Range(0, 100)] public int smallChance = 15;      // Reduced for less parkour
    [Range(0, 100)] public int regularChance = 40;    // Increased for more regular platforms
    
    [Header("Generation Control")]
    public int targetPlatformCount = 200;      // How many platforms to generate
    public int maxBranchingDepth = 10;         // How far to branch out
    public float branchChance = 0.2f;          // Reduced branch chance
    
    [Header("Jump Challenge Settings")]
    public int minJumpGap = 2;
    public int maxJumpGap = 3;                  // Reduced max gap
    public int minJumpChainLength = 1;           // Reduced minimum
    public int maxJumpChainLength = 2;           // Reduced maximum
    public float jumpChallengeChance = 0.1f;     // Reduced chance
    
    [Header("Path Settings")]
    public int minPathLength = 5;                // Minimum platforms in main path
    public int maxPathLength = 15;               // Maximum platforms in main path
    public float cornerConnectionChance = 0.8f;  // Chance corners connect to long platforms
    
    private bool[,] grid;
    private List<Vector2Int> allPlatformPositions = new List<Vector2Int>();
    private Queue<Vector2Int> pendingExpansion = new Queue<Vector2Int>();
    private Vector2Int spawnPos;
    
    // Track platform types and their connections for validation
    private Dictionary<Vector2Int, PlatformType> platformTypes = new Dictionary<Vector2Int, PlatformType>();
    private Dictionary<Vector2Int, List<Vector2Int>> platformConnections = new Dictionary<Vector2Int, List<Vector2Int>>();
    
    private enum PlatformType
    {
        Spawn,
        Regular,
        Straight,
        Corner,
        Small
    }
    
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
        platformTypes.Clear();
        platformConnections.Clear();
        
        // Set spawn position at center (or approximately center)
        spawnPos = new Vector2Int(width / 2, depth / 2);
        
        // Place spawn platform
        allPlatformPositions.Add(spawnPos);
        grid[spawnPos.x, spawnPos.y] = true;
        platformTypes[spawnPos] = PlatformType.Spawn;
        platformConnections[spawnPos] = new List<Vector2Int>();
        
        // Instantiate spawn platform immediately
        Vector3 spawnWorldPos = new Vector3(spawnPos.x * tileSize, platformHeight, spawnPos.y * tileSize);
        Instantiate(spawnPlatform, spawnWorldPos, Quaternion.identity, transform);
        
        // Spawn the player on the spawn platform
        SpawnPlayer(spawnWorldPos);
        
        // Add spawn position to expansion queue
        pendingExpansion.Enqueue(spawnPos);
        
        // Generate the rest of the level
        GenerateFromSpawn();
        
        // Validate and fix corner platforms
        ValidateAndFixCorners();
        
        // Place all remaining platforms
        PlacePlatforms();
        
        Debug.Log($"Generated {allPlatformPositions.Count} platforms total");
        LogPlatformStatistics();
    }
    
    void SpawnPlayer(Vector3 spawnPlatformPosition)
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("Player prefab not assigned! Cannot spawn player.");
            return;
        }
        
        // Calculate player position (on top of the spawn platform with offset)
        Vector3 playerPosition = spawnPlatformPosition + playerSpawnOffset;
        
        // Check if a player already exists and destroy it
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            DestroyImmediate(existingPlayer);
        }
        
        // Instantiate the player
        GameObject player = Instantiate(playerPrefab, playerPosition, Quaternion.identity);
        player.tag = "Player"; // Ensure the player has the "Player" tag
        
        Debug.Log($"Player spawned at {playerPosition}");
    }
    
    void GenerateFromSpawn()
    {
        int platformsGenerated = 1; // Started with spawn platform
        
        while (platformsGenerated < targetPlatformCount && pendingExpansion.Count > 0)
        {
            Vector2Int currentPos = pendingExpansion.Dequeue();
            
            // Get available directions from current position
            List<Vector2Int> availableDirs = GetAvailableDirections(currentPos);
            
            // Shuffle directions for randomness - FIXED: Explicitly specify type
            ShuffleList<Vector2Int>(availableDirs);
            
            // Determine how many paths to create from this position (prefer 1-2 for more focused paths)
            int pathsToCreate = Mathf.Min(availableDirs.Count, Random.Range(1, 3));
            
            for (int i = 0; i < pathsToCreate && platformsGenerated < targetPlatformCount; i++)
            {
                if (i < availableDirs.Count)
                {
                    Vector2Int direction = availableDirs[i];
                    
                    // Prefer regular paths over jump challenges
                    if (Random.value < jumpChallengeChance && platformsGenerated < targetPlatformCount - minJumpChainLength)
                    {
                        platformsGenerated = CreateJumpChain(currentPos, direction, platformsGenerated);
                    }
                    else
                    {
                        platformsGenerated = CreateRegularPath(currentPos, direction, platformsGenerated);
                    }
                }
            }
            
            // Occasionally create a branch, but make it smaller
            if (Random.value < branchChance && platformsGenerated < targetPlatformCount)
            {
                CreateSmallBranch(currentPos, ref platformsGenerated);
            }
        }
        
        // If we still need more platforms, fill with connected straight platforms
        while (platformsGenerated < targetPlatformCount)
        {
            FillWithStraightPlatforms(ref platformsGenerated);
        }
    }
    
    int CreateRegularPath(Vector2Int startPos, Vector2Int direction, int platformsGenerated)
    {
        // Longer paths for more regular gameplay
        int pathLength = Random.Range(minPathLength, maxPathLength + 1);
        
        Vector2Int currentPos = startPos;
        Vector2Int currentDir = direction;
        int straightCount = 0;
        int lastCornerPos = -1;
        
        for (int step = 1; step <= pathLength && platformsGenerated < targetPlatformCount; step++)
        {
            Vector2Int nextPos = currentPos + currentDir;
            
            // Check bounds and availability
            if (IsValidPosition(nextPos) && !grid[nextPos.x, nextPos.y])
            {
                // Add platform
                allPlatformPositions.Add(nextPos);
                grid[nextPos.x, nextPos.y] = true;
                platformsGenerated++;
                
                // Record connection
                RecordConnection(currentPos, nextPos);
                
                // Initially mark as straight
                platformTypes[nextPos] = PlatformType.Straight;
                straightCount++;
                
                pendingExpansion.Enqueue(nextPos);
                currentPos = nextPos;
                
                // Decide if we should create a corner (but ensure it connects properly)
                if (step < pathLength && straightCount >= 3 && Random.value < 0.25f)
                {
                    if (CanCreateValidCorner(currentPos, currentDir, step - lastCornerPos > 3))
                    {
                        Vector2Int newDir = GetPerpendicularDirection(currentDir);
                        
                        // Mark current as corner
                        platformTypes[currentPos] = PlatformType.Corner;
                        lastCornerPos = step;
                        
                        // Ensure the next platform will be straight
                        currentDir = newDir;
                        straightCount = 0;
                    }
                }
            }
            else
            {
                break; // Stop if we hit boundary or existing platform
            }
        }
        
        return platformsGenerated;
    }
    
    bool CanCreateValidCorner(Vector2Int pos, Vector2Int currentDir, bool farEnoughFromLastCorner)
    {
        if (!farEnoughFromLastCorner) return false;
        
        Vector2Int newDir = GetPerpendicularDirection(currentDir);
        Vector2Int nextPos = pos + newDir;
        Vector2Int prevPos = pos - currentDir;
        
        // Check if we can create a corner that connects to existing platforms properly
        bool hasPrevConnection = IsValidPosition(prevPos) && grid[prevPos.x, prevPos.y];
        bool hasSpaceForNext = IsValidPosition(nextPos) && !grid[nextPos.x, nextPos.y];
        
        // Also check if we could create a corner pair
        bool couldBeCornerPair = false;
        Vector2Int potentialPairPos = pos + currentDir + newDir;
        if (IsValidPosition(potentialPairPos) && !grid[potentialPairPos.x, potentialPairPos.y])
        {
            couldBeCornerPair = true;
        }
        
        return (hasPrevConnection && hasSpaceForNext) || couldBeCornerPair;
    }
    
    void CreateSmallBranch(Vector2Int startPos, ref int platformsGenerated)
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };
        
        // FIXED: Explicitly specify type for ShuffleList
        List<Vector2Int> directionsList = new List<Vector2Int>(directions);
        ShuffleList<Vector2Int>(directionsList);
        
        foreach (Vector2Int dir in directionsList)
        {
            Vector2Int branchPos = startPos + dir;
            
            if (IsValidPosition(branchPos) && !grid[branchPos.x, branchPos.y])
            {
                // Create a small branch with 2-3 platforms
                int branchLength = Random.Range(2, 4);
                Vector2Int currentPos = startPos;
                Vector2Int currentDir = dir;
                
                for (int i = 0; i < branchLength; i++)
                {
                    Vector2Int nextPos = currentPos + currentDir;
                    
                    if (IsValidPosition(nextPos) && !grid[nextPos.x, nextPos.y])
                    {
                        allPlatformPositions.Add(nextPos);
                        grid[nextPos.x, nextPos.y] = true;
                        platformsGenerated++;
                        
                        RecordConnection(currentPos, nextPos);
                        
                        // Mark as straight or small
                        if (i == branchLength - 1)
                        {
                            platformTypes[nextPos] = PlatformType.Small;
                        }
                        else
                        {
                            platformTypes[nextPos] = PlatformType.Straight;
                        }
                        
                        pendingExpansion.Enqueue(nextPos);
                        currentPos = nextPos;
                    }
                    else
                    {
                        break;
                    }
                }
                break;
            }
        }
    }
    
    void FillWithStraightPlatforms(ref int platformsGenerated)
    {
        // Try to extend existing straight paths
        List<Vector2Int> straightPositions = platformTypes
            .Where(kvp => kvp.Value == PlatformType.Straight)
            .Select(kvp => kvp.Key)
            .ToList();
        
        // FIXED: Explicitly specify type for ShuffleList
        ShuffleList<Vector2Int>(straightPositions);
        
        foreach (Vector2Int pos in straightPositions)
        {
            // Try to extend in the direction of the straight platform
            Vector2Int[] directions = GetPlatformDirections(pos);
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int nextPos = pos + dir;
                
                if (IsValidPosition(nextPos) && !grid[nextPos.x, nextPos.y])
                {
                    allPlatformPositions.Add(nextPos);
                    grid[nextPos.x, nextPos.y] = true;
                    platformsGenerated++;
                    
                    RecordConnection(pos, nextPos);
                    platformTypes[nextPos] = PlatformType.Straight;
                    pendingExpansion.Enqueue(nextPos);
                    return;
                }
            }
        }
    }
    
    void ValidateAndFixCorners()
    {
        bool cornersFixed = false;
        int maxIterations = 10;
        int iteration = 0;
        
        while (!cornersFixed && iteration < maxIterations)
        {
            cornersFixed = true;
            List<Vector2Int> cornersToCheck = platformTypes
                .Where(kvp => kvp.Value == PlatformType.Corner)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (Vector2Int cornerPos in cornersToCheck)
            {
                if (!IsCornerValid(cornerPos))
                {
                    FixCornerPlacement(cornerPos);
                    cornersFixed = false;
                }
            }
            iteration++;
        }
    }
    
    bool IsCornerValid(Vector2Int cornerPos)
    {
        List<Vector2Int> connections = GetConnections(cornerPos);
        
        // Corner needs at least 2 connections
        if (connections.Count < 2)
            return false;
        
        // Check if connected to at least 2 long platforms (straight or corner)
        int longPlatformConnections = 0;
        foreach (Vector2Int connectedPos in connections)
        {
            if (platformTypes.ContainsKey(connectedPos))
            {
                PlatformType type = platformTypes[connectedPos];
                if (type == PlatformType.Straight || type == PlatformType.Corner)
                {
                    longPlatformConnections++;
                }
            }
        }
        
        // Valid if connected to 2+ long platforms OR in a pair with another corner
        if (longPlatformConnections >= 2)
            return true;
        
        // Check for corner pair (two corners connected to each other)
        if (connections.Count == 1)
        {
            Vector2Int otherPos = connections[0];
            if (platformTypes.ContainsKey(otherPos) && platformTypes[otherPos] == PlatformType.Corner)
            {
                // Check if the other corner also connects to at least one long platform
                List<Vector2Int> otherConnections = GetConnections(otherPos);
                foreach (Vector2Int otherConnected in otherConnections)
                {
                    if (otherConnected != cornerPos && 
                        platformTypes.ContainsKey(otherConnected) && 
                        platformTypes[otherConnected] == PlatformType.Straight)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    void FixCornerPlacement(Vector2Int cornerPos)
    {
        // Change invalid corner to straight platform
        if (platformTypes.ContainsKey(cornerPos))
        {
            platformTypes[cornerPos] = PlatformType.Straight;
            Debug.Log($"Fixed invalid corner at {cornerPos}");
        }
    }
    
    int CreateJumpChain(Vector2Int startPos, Vector2Int direction, int platformsGenerated)
    {
        // Reduced frequency and size of jump chains
        if (Random.value > 0.3f) // Only create jump chains 30% of the time when selected
        {
            return CreateRegularPath(startPos, direction, platformsGenerated);
        }
        
        int gapSize = Random.Range(minJumpGap, maxJumpGap + 1);
        int chainLength = Random.Range(minJumpChainLength, maxJumpChainLength + 1);
        
        Vector2Int currentPos = startPos;
        
        for (int i = 0; i < chainLength && platformsGenerated < targetPlatformCount; i++)
        {
            Vector2Int jumpPos = currentPos + direction * (gapSize + i);
            
            if (IsValidPosition(jumpPos) && !grid[jumpPos.x, jumpPos.y])
            {
                allPlatformPositions.Add(jumpPos);
                grid[jumpPos.x, jumpPos.y] = true;
                platformsGenerated++;
                
                RecordConnection(currentPos, jumpPos);
                platformTypes[jumpPos] = PlatformType.Small;
                pendingExpansion.Enqueue(jumpPos);
                
                currentPos = jumpPos;
            }
            else
            {
                break;
            }
        }
        
        return platformsGenerated;
    }
    
    void RecordConnection(Vector2Int from, Vector2Int to)
    {
        if (!platformConnections.ContainsKey(from))
            platformConnections[from] = new List<Vector2Int>();
        if (!platformConnections.ContainsKey(to))
            platformConnections[to] = new List<Vector2Int>();
        
        if (!platformConnections[from].Contains(to))
            platformConnections[from].Add(to);
        if (!platformConnections[to].Contains(from))
            platformConnections[to].Add(from);
    }
    
    List<Vector2Int> GetConnections(Vector2Int pos)
    {
        if (platformConnections.ContainsKey(pos))
            return platformConnections[pos];
        return new List<Vector2Int>();
    }
    
    Vector2Int[] GetPlatformDirections(Vector2Int pos)
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
            Vector2Int neighbor = pos + dir;
            if (IsValidPosition(neighbor) && grid[neighbor.x, neighbor.y])
            {
                directions.Add(dir);
            }
        }
        
        return directions.ToArray();
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
        // 80% chance to continue straight (increased from 70%)
        if (Random.value < 0.8f)
            return currentDir;
        
        return GetPerpendicularDirection(currentDir);
    }
    
    Vector2Int GetPerpendicularDirection(Vector2Int dir)
    {
        if (dir.x != 0) // Moving horizontally
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
            GameObject platformType = DetermineFinalPlatformType(pos, i);
            Quaternion rotation = DetermineRotation(pos, i, platformType);
            
            Instantiate(platformType, worldPos, rotation, transform);
        }
    }
    
    GameObject DetermineFinalPlatformType(Vector2Int pos, int index)
    {
        // Use the pre-determined type if available
        if (platformTypes.ContainsKey(pos))
        {
            switch (platformTypes[pos])
            {
                case PlatformType.Straight:
                    return straightPlatform;
                case PlatformType.Corner:
                    return cornerPlatform;
                case PlatformType.Small:
                    return smallPlatform;
                case PlatformType.Regular:
                    if (platformPrefabs.Length > 0)
                        return platformPrefabs[Random.Range(0, platformPrefabs.Length)];
                    break;
            }
        }
        
        // Fallback to original determination logic
        return DeterminePlatformType(pos, index);
    }
    
    GameObject DeterminePlatformType(Vector2Int pos, int index)
    {
        // Chance to use regular array platforms
        if (Random.value < regularChance / 100f && platformPrefabs.Length > 0)
        {
            return platformPrefabs[Random.Range(0, platformPrefabs.Length)];
        }
        
        // Random distribution favoring straight platforms
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
                // Calculate corner rotation based on directions
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
    
    void LogPlatformStatistics()
    {
        int straightCount = platformTypes.Values.Count(t => t == PlatformType.Straight);
        int cornerCount = platformTypes.Values.Count(t => t == PlatformType.Corner);
        int smallCount = platformTypes.Values.Count(t => t == PlatformType.Small);
        int regularCount = platformTypes.Values.Count(t => t == PlatformType.Regular);
        int spawnCount = platformTypes.Values.Count(t => t == PlatformType.Spawn);
        
        Debug.Log($"Platform Statistics - Spawn: {spawnCount}, Straight: {straightCount}, Corner: {cornerCount}, Small: {smallCount}, Regular: {regularCount}");
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
        
        // Draw path in play mode with color coding
        if (Application.isPlaying && allPlatformPositions != null)
        {
            foreach (Vector2Int pos in allPlatformPositions)
            {
                Vector3 center = new Vector3(pos.x * tileSize, platformHeight + 1f, pos.y * tileSize);
                
                // Color code by platform type
                if (platformTypes.ContainsKey(pos))
                {
                    switch (platformTypes[pos])
                    {
                        case PlatformType.Straight:
                            Gizmos.color = Color.blue;
                            break;
                        case PlatformType.Corner:
                            Gizmos.color = Color.magenta;
                            break;
                        case PlatformType.Small:
                            Gizmos.color = Color.yellow;
                            break;
                        default:
                            Gizmos.color = Color.green;
                            break;
                    }
                }
                else
                {
                    Gizmos.color = Color.green;
                }
                
                Gizmos.DrawSphere(center, 0.5f);
            }
        }
    }
}