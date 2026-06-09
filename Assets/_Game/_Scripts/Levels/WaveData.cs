using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaouSamaTD.Levels
{
    [Serializable]
    public class WaveData
    {
        [Tooltip("Groups of enemies in this wave (can run in parallel with different delays)")]
        public List<WaveGroup> Groups = new List<WaveGroup>();
        
        [Tooltip("Time to wait after this wave finishes (all enemies dead?) or fixed duration? usually 'Time before NEXT wave'")]
        public float DelayBeforeNextWave = 5f; 
        
        [Tooltip("Message to display when wave starts")]
        public string WaveMessage;

        [Header("Dialogue")]
        [Tooltip("Story to play BEFORE the wave starts spawning")]
        public MaouSamaTD.Story.StoryDataSO PreWaveStory;
        
        [Tooltip("Story to play AFTER the wave is cleared (all enemies dead)")]
        public MaouSamaTD.Story.StoryDataSO PostWaveStory;

        [Header("Grid Alterations")]
        [Tooltip("List of tile alterations (spawns/exits addition, subtraction, or override) applied AFTER this wave is cleared")]
        public List<WaveTileAlteration> TileAlterations = new List<WaveTileAlteration>();
    }

    public enum TileAlterationAction
    {
        Add,
        Subtract,
        Override
    }

    public enum TilePointType
    {
        SpawnGround,
        SpawnHigh,
        ExitGround,
        ExitHigh,
        Walkable,
        HighGround,
        Decoration
    }

    [Serializable]
    public struct WaveTileAlteration
    {
        [Tooltip("Add, Subtract or Override existing active list of this point type")]
        public TileAlterationAction Action;

        [Tooltip("The point type: Spawn Ground, Spawn High Ground, Exit Ground, Exit High Ground")]
        public TilePointType PointType;

        [Tooltip("The coordinate of the tile to alter")]
        public Vector2Int Coordinate;

        [Tooltip("For Spawn types, the index of the exit in the active ExitPoints list. Set to -1 for Any/First.")]
        public int TargetExitIndex;
    }
}

