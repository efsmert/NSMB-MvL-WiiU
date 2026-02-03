using System;
using System.Collections.Generic;
using UnityEngine;

namespace NSMB.World {
    [Serializable]
    public struct StageTile {
        public int x;
        public int y;
        public int spriteIndex;
        public bool flipX;
    }

    [Serializable]
    public sealed class StageTileLayer {
        public string name;
        public string resourcesAtlasPath;
        public int sortingOrder;
        public Vector3 position;
        public Vector3 scale;
        public List<StageTile> tiles = new List<StageTile>();
    }

    public enum StageEntityKind {
        Unknown = 0,
        Coin = 1,
        Goomba = 2,
        Koopa = 3,
    }

    [Serializable]
    public struct StageEntity {
        public StageEntityKind kind;
        public Vector2 position;
        public int variant;
    }

    public sealed class StageDefinition : ScriptableObject {
        public string stageKey;

        // World-space spawn point for Player 1 (Unity units).
        public Vector2 spawnPoint;

        public List<StageTileLayer> tileLayers = new List<StageTileLayer>();
        public List<StageEntity> entities = new List<StageEntity>();
    }
}

