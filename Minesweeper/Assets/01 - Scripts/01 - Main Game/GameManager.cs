using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Random = UnityEngine.Random;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform tilePrefab;
    [SerializeField] private Transform gameHolder;
    [SerializeField] private Sprite hintTile;

    private List<Tile> tiles = new();
    private Dictionary<int, List<int>> neighboursCache = new();
    public Dictionary<Tile, int> tileToPositionMap = new();

    // Blank chunk system
    private Dictionary<int, int> tileToChunkMap = new(); // Maps tile position to chunk ID
    private Dictionary<int, HashSet<int>> chunkToTilesMap = new(); // Maps chunk ID to all tiles in that chunk
    private int nextChunkId = 0;

    public int width;
    public int height;
    public float numMines;
    public int hintTiles = 2;
    int avHints = 0;
    int[] gridTiles;

    private GridGenerationProgressTracker progressTracker;

    private bool tilesDestroyed = false;
    private readonly float tileSize = 0.5f;

    // Job system arrays - track disposal state
    private NativeArray<int> shuffledIndices;
    private NativeArray<bool> mines;
    private bool isDisposed = false;
    private bool isCleaningUp = false;

    // Static cleanup flag to prevent multiple cleanup attempts
    private static bool isApplicationQuitting = false;

    // FIX 1: Add JobHandle tracking to prevent memory leaks from incomplete jobs
    private List<JobHandle> activeJobs = new List<JobHandle>();

    // FIX 2: Add coroutine tracking to stop them properly during cleanup
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    public bool gameOver = false;
    public bool gameWin = false;

    public static class MouseUsability
    {
        public static bool isMouseEnabled = true;
        public static bool timerEnabled = true;
    }

    [BurstCompile]
    struct NeighbourJob : IJob
    {
        public int width;
        public int height;
        [WriteOnly] public NativeArray<int> neighbourCounts;
        [WriteOnly] public NativeArray<int> neighbourData;

        public void Execute()
        {
            int totalTiles = width * height;

            for (int index = 0; index < totalTiles; index++)
            {
                int row = index / width;
                int col = index % width;
                int count = 0;
                int baseIndex = index * 8; // Max 8 neighbors per tile

                // Check all 8 directions
                if (row < height - 1) AddNeighbour(index + width, baseIndex, ref count); // North
                if (row > 0) AddNeighbour(index - width, baseIndex, ref count); //South
                if (col < width - 1) AddNeighbour(index + 1, baseIndex, ref count); //East
                if (col > 0) AddNeighbour(index - 1, baseIndex, ref count); //West
                if (row < height - 1 && col < width - 1) AddNeighbour(index + width + 1, baseIndex, ref count); //NE
                if (row < height - 1 && col > 0) AddNeighbour(index + width - 1, baseIndex, ref count); //NW
                if (row > 0 && col < width - 1) AddNeighbour(index - width + 1, baseIndex, ref count); //SE
                if (row > 0 && col > 0) AddNeighbour(index - width - 1, baseIndex, ref count); //SW

                neighbourCounts[index] = count;
            }
        }

        void AddNeighbour(int neighbourIndex, int baseIndex, ref int count)
        {
            if (neighbourIndex >= 0 && neighbourIndex < width * height)
            {
                neighbourData[baseIndex + count] = neighbourIndex;
                count++;
            }
        }
    }

    [BurstCompile]
    struct MineJob : IJob
    {
        [ReadOnly] public NativeArray<int> shuffledIndicies;
        [ReadOnly] public float numMines;
        public NativeArray<bool> mines;
        [ReadOnly] public NativeArray<int> neighbourCounts;
        [ReadOnly] public NativeArray<int> neighbourData;
        public NativeArray<int> mineCount;

        public void Execute()
        {
            int totalTiles = shuffledIndicies.Length;

            // First pass: place mines
            for (int i = 0; i < totalTiles; i++)
            {
                int actualIndex = shuffledIndicies[i];
                bool isMine = i < numMines;
                mines[actualIndex] = isMine;
            }

            // Second pass: calculate mine counts
            for (int i = 0; i < totalTiles; i++)
            {
                int actualIndex = shuffledIndicies[i];

                if (mines[actualIndex])
                {
                    mineCount[actualIndex] = -1;
                    continue;
                }

                int count = 0;
                int neighbourCount = neighbourCounts[actualIndex];
                int baseIndex = actualIndex * 8;

                for (int j = 0; j < neighbourCount; j++)
                {
                    int neighbourIndex = neighbourData[baseIndex + j];
                    if (mines[neighbourIndex]) count++;
                }
                mineCount[actualIndex] = count;
            }
        }
    }

    private void Awake()
    {
        width = SettingsManager.Current.width;
        height = SettingsManager.Current.height;
        numMines = SettingsManager.Current.numMines;
        avHints = SettingsManager.Current.hints;
        gameOver = false;

        // Reset static flag
        isApplicationQuitting = false;
    }

    /// <summary>
    /// Tracks progress and ETA for grid generation steps, printing to the Unity Console.
    /// </summary>
    public class GridGenerationProgressTracker
    {
        private class StepInfo
        {
            public string Name;
            public float StartTime;
            public float EndTime;
            public float Duration => EndTime > 0 ? EndTime - StartTime : Time.realtimeSinceStartup - StartTime;
        }

        private List<StepInfo> steps = new List<StepInfo>();
        private int currentStep = -1;
        private float overallStartTime;
        private float overallEndTime;
        private bool finished = false;

        public GridGenerationProgressTracker()
        {
            overallStartTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Call at the start of each step.
        /// </summary>
        public void StartStep(string stepName)
        {
            if (finished) return;
            StepInfo step = new StepInfo { Name = stepName, StartTime = Time.realtimeSinceStartup };
            steps.Add(step);
            currentStep = steps.Count - 1;
            PrintProgress();
        }

        /// <summary>
        /// Call at the end of each step.
        /// </summary>
        public void EndStep()
        {
            if (finished || currentStep < 0) return;
            steps[currentStep].EndTime = Time.realtimeSinceStartup;
            PrintProgress();
        }

        /// <summary>
        /// Call when all steps are done.
        /// </summary>
        public void Finish()
        {
            if (finished) return;
            overallEndTime = Time.realtimeSinceStartup;
            finished = true;
            PrintProgress(final: true);
        }

        /// <summary>
        /// Prints the current progress and ETA to the Unity Console.
        /// </summary>
        private void PrintProgress(bool final = false)
        {
            int totalSteps = steps.Count;
            int completedSteps = 0;
            float elapsed = Time.realtimeSinceStartup - overallStartTime;
            float avgStepTime = 0f;

            for (int i = 0; i < steps.Count; i++)
                if (steps[i].EndTime > 0) completedSteps++;

            if (completedSteps > 0)
            {
                float totalStepTime = 0f;
                for (int i = 0; i < completedSteps; i++)
                    totalStepTime += steps[i].Duration;
                avgStepTime = totalStepTime / completedSteps;
            }

            float eta = avgStepTime * (totalSteps - completedSteps);

            string progressMsg = $"[GridGen] Step {completedSteps}/{totalSteps}";
            if (currentStep >= 0 && currentStep < steps.Count)
                progressMsg += $" - Now: {steps[currentStep].Name}";

            if (!final)
            {
                if (completedSteps < totalSteps && completedSteps > 0)
                    progressMsg += $" | ETA: {FormatTime(eta)} | Elapsed: {FormatTime(elapsed)}";
                else if (completedSteps == 0)
                    progressMsg += $" | Starting...";
                else
                    progressMsg += $" | Finalizing...";
            }
            else
            {
                progressMsg = $"[GridGen] READY! All steps complete in {FormatTime(overallEndTime - overallStartTime)}";
            }

            Debug.Log(progressMsg);
        }

        private string FormatTime(float seconds)
        {
            if (seconds < 1f)
                return $"{(int)(seconds * 1000)}ms";
            int min = (int)(seconds / 60);
            int sec = (int)(seconds % 60);
            if (min > 0)
                return $"{min}m {sec}s";
            return $"{sec}s";
        }
    }

    void Start()
    {
        GameManager.MouseUsability.isMouseEnabled = false;
        GameManager.MouseUsability.timerEnabled = false;
        CleanupNativeArrays();

        int totalTiles = width * height;
        if (totalTiles < 5000)
        {
            progressTracker = new GridGenerationProgressTracker();

            progressTracker.StartStep("Create Game Board");
            CreateGameBoardSync();
            progressTracker.EndStep();

            progressTracker.StartStep("Pre-calculate Neighbours");
            PreCalculateNeighboursSync();
            progressTracker.EndStep();

            progressTracker.StartStep("Shuffle Tiles");
            FisherYatesShuffle();
            progressTracker.EndStep();

            progressTracker.StartStep("Reset Game State");
            ResetGameStateSync();
            progressTracker.EndStep();

            progressTracker.StartStep("Calculate Blank Chunks");
            CalculateBlankChunks();
            progressTracker.EndStep();

            if (SettingsManager.Current.hintsEnabled)
            {
                progressTracker.StartStep("Place Hint Tiles");
                HintTiles();
                progressTracker.EndStep();
            }

            progressTracker.Finish();
            GameManager.MouseUsability.isMouseEnabled = true;
            GameManager.MouseUsability.timerEnabled = true;
        }
        else
        {
            var coroutine = StartCoroutine(StartCoroutineOptimized());
            activeCoroutines.Add(coroutine);
        }
    }



    IEnumerator StartCoroutineOptimized()
    {
        progressTracker = new GridGenerationProgressTracker();

        progressTracker.StartStep("Create Game Board");
        var coroutine1 = StartCoroutine(CreateGameBoardOptimized());
        activeCoroutines.Add(coroutine1);
        yield return coroutine1;
        progressTracker.EndStep();

        progressTracker.StartStep("Pre-calculate Neighbours");
        var coroutine2 = StartCoroutine(PreCalculateNeighboursOptimized());
        activeCoroutines.Add(coroutine2);
        yield return coroutine2;
        progressTracker.EndStep();

        progressTracker.StartStep("Shuffle Tiles");
        FisherYatesShuffle();
        progressTracker.EndStep();

        progressTracker.StartStep("Reset Game State");
        var coroutine3 = StartCoroutine(ResetGameStateOptimized());
        activeCoroutines.Add(coroutine3);
        yield return coroutine3;
        progressTracker.EndStep();

        progressTracker.StartStep("Calculate Blank Chunks");
        var coroutine4 = StartCoroutine(CalculateBlankChunksCoroutine());
        activeCoroutines.Add(coroutine4);
        yield return coroutine4;
        progressTracker.EndStep();

        if (SettingsManager.Current.hintsEnabled)
        {
            progressTracker.StartStep("Place Hint Tiles");
            HintTiles();
            progressTracker.EndStep();
        }

        progressTracker.Finish();
        GameManager.MouseUsability.isMouseEnabled = true;
        GameManager.MouseUsability.timerEnabled = true;
    }

    // Synchronous version for fast loading of smaller grids
    void CreateGameBoardSync()
    {
        float percent = numMines / 100f;
        this.numMines = Mathf.RoundToInt(percent * width * height);
        print(numMines);
        SettingsManager.Current.numFlags = numMines;

        int totalTiles = width * height;
        tiles = new List<Tile>(totalTiles);
        tileToPositionMap = new Dictionary<Tile, int>(totalTiles);

        // Batch instantiate for better performance
        Transform[] tileTransforms = new Transform[totalTiles];

        for (int i = 0; i < totalTiles; i++)
        {
            tileTransforms[i] = Instantiate(tilePrefab, gameHolder);
        }

        // Position and setup tiles
        for (int i = 0; i < totalTiles; i++)
        {
            int row = i / width;
            int col = i % width;

            Transform tileTransform = tileTransforms[i];
            float xIndex = col - ((width - 1) / 2.0f);
            float yIndex = row - ((height - 1) / 2.0f);
            tileTransform.localPosition = new Vector2(xIndex * tileSize, yIndex * tileSize);

            Tile tile = tileTransform.GetComponent<Tile>();
            tiles.Add(tile);
            tile.gameManager = this;
            tileToPositionMap[tile] = i;
        }
    }

    // Optimized coroutine version with fewer yields
    IEnumerator CreateGameBoardOptimized()
    {
        float percent = Mathf.Clamp(numMines, 0, 100) / 100f;
        this.numMines = Mathf.RoundToInt(percent * width * height);
        print(numMines);
        SettingsManager.Current.numFlags = numMines;

        int totalTiles = width * height;
        tiles = new List<Tile>(totalTiles);
        tileToPositionMap = new Dictionary<Tile, int>(totalTiles);

        const int batchSize = 1000; // Reduced batch size
        int processed = 0;

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                // FIX 4: Check if cleaning up during instantiation to prevent leaks
                if (isCleaningUp || isDisposed) yield break;

                Transform tileTransform = Instantiate(tilePrefab, gameHolder);
                float xIndex = col - ((width - 1) / 2.0f);
                float yIndex = row - ((height - 1) / 2.0f);
                tileTransform.localPosition = new Vector2(xIndex * tileSize, yIndex * tileSize);

                Tile tile = tileTransform.GetComponent<Tile>();
                tiles.Add(tile);
                tile.gameManager = this;
                tileToPositionMap[tile] = row * width + col;

                processed++;
                if (processed % batchSize == 0) yield return null;
            }
        }
    }

    void PreCalculateNeighboursSync()
    {
        int totalTiles = width * height;
        var neighbourCounts = new NativeArray<int>(totalTiles, Allocator.TempJob);
        var neighbourData = new NativeArray<int>(totalTiles * 8, Allocator.TempJob);

        var neighbourJob = new NeighbourJob
        {
            width = width,
            height = height,
            neighbourCounts = neighbourCounts,
            neighbourData = neighbourData
        };

        JobHandle jobHandle = neighbourJob.Schedule();
        jobHandle.Complete(); // Complete immediately for sync version

        // Convert job results to dictionary cache
        neighboursCache = new Dictionary<int, List<int>>(totalTiles);
        for (int pos = 0; pos < totalTiles; pos++)
        {
            List<int> neighbourList = new List<int>();
            int count = neighbourCounts[pos];
            int baseIndex = pos * 8;

            for (int i = 0; i < count; i++)
            {
                neighbourList.Add(neighbourData[baseIndex + i]);
            }
            neighboursCache[pos] = neighbourList;
        }

        neighbourCounts.Dispose();
        neighbourData.Dispose();
    }

    IEnumerator PreCalculateNeighboursOptimized()
    {
        int totalTiles = width * height;
        var neighbourCounts = new NativeArray<int>(totalTiles, Allocator.TempJob);
        var neighbourData = new NativeArray<int>(totalTiles * 8, Allocator.TempJob);

        var neighbourJob = new NeighbourJob
        {
            width = width,
            height = height,
            neighbourCounts = neighbourCounts,
            neighbourData = neighbourData
        };

        JobHandle jobHandle = neighbourJob.Schedule();
        // FIX 5: Track job handles for proper cleanup
        activeJobs.Add(jobHandle);

        // More aggressive completion checking
        while (!jobHandle.IsCompleted)
        {
            // FIX 6: Exit early if cleaning up
            if (isCleaningUp || isDisposed)
            {
                jobHandle.Complete(); // Force complete to prevent leak
                neighbourCounts.Dispose();
                neighbourData.Dispose();
                yield break;
            }
            yield return null;
        }
        jobHandle.Complete();

        // Convert job results to dictionary cache
        neighboursCache = new Dictionary<int, List<int>>(totalTiles);
        for (int pos = 0; pos < totalTiles; pos++)
        {
            List<int> neighbourList = new List<int>();
            int count = neighbourCounts[pos];
            int baseIndex = pos * 8;

            for (int i = 0; i < count; i++)
            {
                neighbourList.Add(neighbourData[baseIndex + i]);
            }
            neighboursCache[pos] = neighbourList;
        }

        neighbourCounts.Dispose();
        neighbourData.Dispose();
    }

    private void FisherYatesShuffle()
    {
        // FIX: Add safety check to prevent memory leaks during cleanup
        if (isCleaningUp || isDisposed)
        {
            return;
        }

        int totalTiles = width * height;
        gridTiles = new int[totalTiles];
        for (int i = 0; i < totalTiles; i++)
            gridTiles[i] = i;

        // Optimized shuffle using System.Random for better performance
        System.Random rng = new System.Random();
        for (int i = gridTiles.Length - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (gridTiles[i], gridTiles[j]) = (gridTiles[j], gridTiles[i]);
        }

        // FIX: Clean up old shuffledIndices before creating new one
        if (shuffledIndices.IsCreated)
        {
            shuffledIndices.Dispose();
        }

        // FIX: Add another safety check before creating new NativeArray
        if (isCleaningUp || isDisposed)
        {
            return;
        }

        shuffledIndices = new NativeArray<int>(gridTiles, Allocator.Persistent);
    }

    void ResetGameStateSync()
    {
        // FIX: Add safety check to prevent memory leaks during cleanup
        if (isCleaningUp || isDisposed)
        {
            return;
        }

        int totalTiles = width * height;

        // FIX: Clean up old mines array before creating new one
        if (mines.IsCreated)
        {
            mines.Dispose();
        }

        // FIX: Add another safety check before creating new NativeArray
        if (isCleaningUp || isDisposed)
        {
            return;
        }

        mines = new NativeArray<bool>(totalTiles, Allocator.Persistent);

        var mineCount = new NativeArray<int>(totalTiles, Allocator.TempJob);
        var neighbourCounts = new NativeArray<int>(totalTiles, Allocator.TempJob);
        var neighbourData = new NativeArray<int>(totalTiles * 8, Allocator.TempJob);

        // Copy neighbor data for the job
        for (int pos = 0; pos < totalTiles; pos++)
        {
            var neighbours = neighboursCache[pos];
            neighbourCounts[pos] = neighbours.Count;
            int baseIndex = pos * 8;
            for (int i = 0; i < neighbours.Count; i++)
            {
                neighbourData[baseIndex + i] = neighbours[i];
            }
        }

        var mineJob = new MineJob
        {
            shuffledIndicies = shuffledIndices,
            numMines = numMines,
            mines = mines,
            neighbourCounts = neighbourCounts,
            neighbourData = neighbourData,
            mineCount = mineCount
        };

        JobHandle jobHandle = mineJob.Schedule();
        jobHandle.Complete(); // Complete immediately

        // Apply results to tiles
        for (int i = 0; i < totalTiles; i++)
        {
            tiles[i].isMine = mines[i];
            tiles[i].mineCount = mineCount[i] == -1 ? 0 : mineCount[i];
        }

        mineCount.Dispose();
        neighbourCounts.Dispose();
        neighbourData.Dispose();
    }

    IEnumerator ResetGameStateOptimized()
    {
        // FIX: Add safety check to prevent memory leaks during cleanup
        if (isCleaningUp || isDisposed)
        {
            yield break;
        }

        int totalTiles = width * height;

        // FIX: Clean up old mines array before creating new one
        if (mines.IsCreated)
        {
            mines.Dispose();
        }

        // FIX: Add another safety check before creating new NativeArray
        if (isCleaningUp || isDisposed)
        {
            yield break;
        }

        mines = new NativeArray<bool>(totalTiles, Allocator.Persistent);

        var mineCount = new NativeArray<int>(totalTiles, Allocator.TempJob);
        var neighbourCounts = new NativeArray<int>(totalTiles, Allocator.TempJob);
        var neighbourData = new NativeArray<int>(totalTiles * 8, Allocator.TempJob);

        // Copy neighbor data for the job
        for (int pos = 0; pos < totalTiles; pos++)
        {
            var neighbours = neighboursCache[pos];
            neighbourCounts[pos] = neighbours.Count;
            int baseIndex = pos * 8;
            for (int i = 0; i < neighbours.Count; i++)
            {
                neighbourData[baseIndex + i] = neighbours[i];
            }
        }

        var mineJob = new MineJob
        {
            shuffledIndicies = shuffledIndices,
            numMines = numMines,
            mines = mines,
            neighbourCounts = neighbourCounts,
            neighbourData = neighbourData,
            mineCount = mineCount
        };

        JobHandle jobHandle = mineJob.Schedule();
        // FIX: Track job handles for proper cleanup
        activeJobs.Add(jobHandle);

        while (!jobHandle.IsCompleted)
        {
            // FIX: Exit early if cleaning up
            if (isCleaningUp || isDisposed)
            {
                jobHandle.Complete(); // Force complete to prevent leak
                mineCount.Dispose();
                neighbourCounts.Dispose();
                neighbourData.Dispose();
                yield break;
            }
            yield return null;
        }
        jobHandle.Complete();

        // Apply results to tiles
        for (int i = 0; i < totalTiles; i++)
        {
            tiles[i].isMine = mines[i];
            tiles[i].mineCount = mineCount[i] == -1 ? 0 : mineCount[i];
        }

        mineCount.Dispose();
        neighbourCounts.Dispose();
        neighbourData.Dispose();
    }

    // New method: Calculate blank tile chunks synchronously
    private void CalculateBlankChunks()
    {
        // Clear existing data
        tileToChunkMap.Clear();
        chunkToTilesMap.Clear();
        nextChunkId = 0;

        HashSet<int> visited = new HashSet<int>();
        int totalTiles = width * height;

        // Process in smaller batches to avoid frame drops
        for (int pos = 0; pos < totalTiles; pos++)
        {
            if (!visited.Contains(pos) && IsBlankTile(pos))
            {
                // Start a new chunk with flood fill
                HashSet<int> currentChunk = new HashSet<int>();
                FloodFillBlankChunk(pos, visited, currentChunk);

                // Only create chunks with more than 1 tile (optimization)
                if (currentChunk.Count > 1)
                {
                    int chunkId = nextChunkId++;
                    chunkToTilesMap[chunkId] = currentChunk;

                    foreach (int tilePos in currentChunk)
                    {
                        tileToChunkMap[tilePos] = chunkId;
                    }
                }
            }
        }

        Debug.Log($"Calculated {nextChunkId} blank chunks. Largest chunk: {(chunkToTilesMap.Values.Count > 0 ? chunkToTilesMap.Values.Max(chunk => chunk.Count) : 0)} tiles");
    }

    // New method: Coroutine version for large grids
    private IEnumerator CalculateBlankChunksCoroutine()
    {
        // Clear existing data
        tileToChunkMap.Clear();
        chunkToTilesMap.Clear();
        nextChunkId = 0;

        HashSet<int> visited = new HashSet<int>();
        int totalTiles = width * height;
        int processed = 0;
        const int batchSize = 500; // Reduced batch size for better performance

        for (int pos = 0; pos < totalTiles; pos++)
        {
            // FIX 12: Check if cleaning up during processing
            if (isCleaningUp || isDisposed) yield break;

            if (!visited.Contains(pos) && IsBlankTile(pos))
            {
                // Start a new chunk with flood fill
                HashSet<int> currentChunk = new HashSet<int>();
                FloodFillBlankChunk(pos, visited, currentChunk);

                // Only create chunks with more than 1 tile (optimization)
                if (currentChunk.Count > 1)
                {
                    int chunkId = nextChunkId++;
                    chunkToTilesMap[chunkId] = currentChunk;

                    foreach (int tilePos in currentChunk)
                    {
                        tileToChunkMap[tilePos] = chunkId;
                    }
                }
            }

            processed++;
            if (processed % batchSize == 0) yield return null;
        }

        Debug.Log($"Calculated {nextChunkId} blank chunks. Largest chunk: {(chunkToTilesMap.Values.Count > 0 ? chunkToTilesMap.Values.Max(chunk => chunk.Count) : 0)} tiles");
    }

    // New method: Check if a tile is blank (no mine, mine count = 0)
    private bool IsBlankTile(int position)
    {
        return !tiles[position].isMine && tiles[position].mineCount == 0;
    }

    // New method: Flood fill algorithm to find connected blank tiles
    private void FloodFillBlankChunk(int startPos, HashSet<int> globalVisited, HashSet<int> currentChunk)
    {
        Stack<int> stack = new Stack<int>();
        stack.Push(startPos);

        while (stack.Count > 0)
        {
            int pos = stack.Pop();

            if (globalVisited.Contains(pos) || currentChunk.Contains(pos))
                continue;

            if (!IsBlankTile(pos))
                continue;

            globalVisited.Add(pos);
            currentChunk.Add(pos);

            // Add all neighbors to stack for processing
            var neighbors = neighboursCache[pos];
            foreach (int neighborPos in neighbors)
            {
                if (!globalVisited.Contains(neighborPos) && !currentChunk.Contains(neighborPos))
                {
                    stack.Push(neighborPos);
                }
            }
        }
    }

    // New method: Instantly reveal entire blank chunk
    public void RevealBlankChunk(int position)
    {
        if (tileToChunkMap.TryGetValue(position, out int chunkId))
        {
            if (chunkToTilesMap.TryGetValue(chunkId, out HashSet<int> chunkTiles))
            {
                // Reveal all tiles in the chunk instantly
                foreach (int tilePos in chunkTiles)
                {
                    tiles[tilePos].RevealTileInstant();
                }

                // Also reveal the border tiles (tiles with numbers adjacent to the blank area)
                HashSet<int> borderTiles = new HashSet<int>();
                foreach (int tilePos in chunkTiles)
                {
                    var neighbors = neighboursCache[tilePos];
                    foreach (int neighborPos in neighbors)
                    {
                        if (!chunkTiles.Contains(neighborPos) && !tiles[neighborPos].isMine && tiles[neighborPos].mineCount > 0)
                        {
                            borderTiles.Add(neighborPos);
                        }
                    }
                }

                // Reveal border tiles
                foreach (int borderPos in borderTiles)
                {
                    tiles[borderPos].RevealTileInstant();
                }
            }
        }
    }

    // New method: Check if a tile is part of a blank chunk
    public bool IsPartOfBlankChunk(int position)
    {
        return tileToChunkMap.ContainsKey(position);
    }

    private void HintTiles()
    {
        int hintsPlaced = 0;
        for (int i = 0; i < gridTiles.Length && hintsPlaced < avHints; i++)
        {
            int pos = gridTiles[i];
            if (!tiles[pos].isMine)
            {
                tiles[pos].isHint = true;
                tiles[pos].GetComponent<SpriteRenderer>().sprite = hintTile;
                hintsPlaced++;
            }
        }
    }

    // Optimized cleanup methods for faster scene transitions

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
        FastCleanup();
    }

    private void OnDestroy()
    {
        if (!isApplicationQuitting && !isDisposed)
        {
            FastCleanup();
        }
    }

    private void OnDisable()
    {
        if (!isApplicationQuitting && !isDisposed)
        {
            FastCleanup();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            print("Game Paused");
        }
    }

    // Fast cleanup method - optimized for scene transitions
    private void FastCleanup()
    {
        if (isDisposed || isCleaningUp || isApplicationQuitting) return;

        isCleaningUp = true;

        try
        {
            // FIX 13: Stop all active coroutines first
            StopAllActiveCoroutines();

            // FIX 14: Complete all active jobs before disposing
            CompleteAllActiveJobs();

            // NEW: Fast tile destruction - destroy parent container instead of individual tiles
            DestroyTilesFast();

            // Priority 1: Dispose NativeArrays immediately (most critical)
            CleanupNativeArrays();

            // Priority 2: Clear large collections without iteration
            ClearCollectionsFast();

            // Priority 3: Null references to help GC
            NullifyReferences();

            isDisposed = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error during cleanup: {e.Message}");
        }
        finally
        {
            isCleaningUp = false;
        }
    }

    private void DestroyTilesFast()
    {
        if (tilesDestroyed) return;

        try
        {
            // Method 1: If gameHolder exists, destroy it entirely (fastest)
            if (gameHolder != null && gameHolder.gameObject != null)
            {
                // Disable the parent first to prevent individual tile cleanup calls
                gameHolder.gameObject.SetActive(false);

                // Use DestroyImmediate during scene transitions for instant cleanup
                if (isApplicationQuitting)
                {
                    // During quit, just disable - Unity will handle cleanup
                    gameHolder.gameObject.SetActive(false);
                }
                else
                {
                    // During scene changes, use DestroyImmediate for instant removal
                    DestroyImmediate(gameHolder.gameObject);
                }

                tilesDestroyed = true;
                return;
            }

            // Method 2: Fallback - batch destroy tiles if gameHolder is null
            if (tiles != null && tiles.Count > 0)
            {
                // Disable all tiles first to prevent individual cleanup calls
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (tiles[i] != null && tiles[i].gameObject != null)
                    {
                        tiles[i].gameObject.SetActive(false);
                    }
                }

                // Then destroy in batches
                if (!isApplicationQuitting)
                {
                    const int batchSize = 100;
                    for (int i = 0; i < tiles.Count; i += batchSize)
                    {
                        int endIndex = Mathf.Min(i + batchSize, tiles.Count);
                        for (int j = i; j < endIndex; j++)
                        {
                            if (tiles[j] != null && tiles[j].gameObject != null)
                            {
                                DestroyImmediate(tiles[j].gameObject);
                            }
                        }
                    }
                }

                tilesDestroyed = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error during fast tile destruction: {e.Message}");
            tilesDestroyed = true; // Mark as destroyed to prevent retry
        }
    }

    // FIX 15: New method to stop all tracked coroutines
    private void StopAllActiveCoroutines()
    {
        try
        {
            // Stop all tracked coroutines
            foreach (var coroutine in activeCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            activeCoroutines.Clear();

            // Also stop all coroutines as a safety measure
            StopAllCoroutines();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error stopping coroutines: {e.Message}");
        }
    }

    // FIX 16: New method to complete all tracked jobs
    private void CompleteAllActiveJobs()
    {
        try
        {
            foreach (var job in activeJobs)
            {
                if (!job.IsCompleted)
                {
                    job.Complete();
                }
            }
            activeJobs.Clear();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error completing jobs: {e.Message}");
        }
    }

    private void CleanupNativeArrays()
    {
        // Use try-catch for each array to prevent cascade failures
        try
        {
            if (shuffledIndices.IsCreated)
            {
                shuffledIndices.Dispose();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error disposing shuffledIndices: {e.Message}");
        }

        try
        {
            if (mines.IsCreated)
            {
                mines.Dispose();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error disposing mines: {e.Message}");
        }
    }

    // Fast collection clearing without iteration
    private void ClearCollectionsFast()
    {
        try
        {
            // Clear collections without iterating (much faster)
            tiles?.Clear();
            neighboursCache?.Clear();
            tileToPositionMap?.Clear();
            tileToChunkMap?.Clear();
            chunkToTilesMap?.Clear();

            // FIX 17: Also clear tracking collections
            activeJobs?.Clear();
            activeCoroutines?.Clear();

            // Set capacity to 0 to free memory immediately
            if (tiles != null)
            {
                tiles.Capacity = 0;
                tiles.TrimExcess();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error clearing collections: {e.Message}");
        }
    }

    // Nullify references to help garbage collection
    private void NullifyReferences()
    {
        try
        {
            tiles = null;
            neighboursCache = null;
            tileToPositionMap = null;
            tileToChunkMap = null;
            chunkToTilesMap = null;
            gridTiles = null;
            tilePrefab = null;
            gameHolder = null;
            hintTile = null;
            // FIX 18: Nullify tracking collections
            activeJobs = null;
            activeCoroutines = null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error nullifying references: {e.Message}");
        }
    }

    // Public method for manual cleanup (call before scene changes)
    public void PrepareForSceneChange()
    {
        if (!isDisposed)
        {
            FastCleanup();
        }
    }

    // Given a location work out how many mines are surrounding it.
    private int HowManyMines(int location)
    {
        int count = 0;
        var neighbours = neighboursCache[location];

        for (int i = 0; i < neighbours.Count; i++)
        {
            if (tiles[neighbours[i]].isMine)
            {
                count++;
            }
        }
        return count;
    }

    public void ClickNeighbours(Tile tile)
    {
        int location = tileToPositionMap[tile];
        var neighbours = neighboursCache[location];
        for (int i = 0; i < neighbours.Count; i++)
        {
            tiles[neighbours[i]].ClickedTile();
        }
    }

    public void GameOver()
    {
        foreach (Tile tile in tiles)
        {
            tile.ShowGameOverState();
        }
        gameOver = true;
        MouseUsability.timerEnabled = false;
    }

    public void CheckGameOver()
    {
        //if there are numMines left active then we're done
        int count = 0;
        foreach (Tile tile in tiles)
        {
            if (tile.active)
            {
                count++;
            }
        }
        if (count == numMines)
        {
            gameOver = true;
            gameWin = true;
            MouseUsability.timerEnabled = false;
            //flag and disable everything, we're done
            Debug.Log("Winner");
            foreach (Tile tile in tiles)
            {
                tile.ShowGameOverState();                
            }
        }
    }

    public void ExpandIfFlagged(Tile tile)
    {

        int location = tileToPositionMap[tile];
        //get the number of flags
        int flag_count = 0;
        var neighbours = neighboursCache[location];
        for (int i = 0; i < neighbours.Count; i++)
        {
            if (tiles[neighbours[i]].flagged)
            {
                flag_count++;
            }
        }
        //if we have the right number of flags, click surrounding tiles
        if (flag_count == tile.mineCount)
        {
            //clicking a flag does nothing so this is safe
            ClickNeighbours(tile);
        }
    }
}