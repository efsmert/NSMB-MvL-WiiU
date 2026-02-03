using System;
using System.Collections.Generic;
using UnityEngine;

namespace NSMB.World {
    [Serializable]
    public struct StageTile {
        public int x;
        public int y;
        public int spriteIndex;
        public string spriteName;
        public bool flipX;
        public bool flipY;
        public bool solid;
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
        BreakableBlock = 4,
        InvisibleBlock = 5,
        MovingPlatform = 6,
        BulletBillLauncher = 7,
        PiranhaPlant = 8,
        Boo = 9,
        Bobomb = 10,
        Spinner = 11,
        EnterablePipe = 12,
        MarioBrosPlatform = 13,
    }

    [Serializable]
    public struct StagePathNode {
        public Vector2 position;
        public float travelDurationSeconds;
        public bool easeIn;
        public bool easeOut;
    }

    [Serializable]
    public struct StageEntity {
        public StageEntityKind kind;
        public Vector2 position;
        public int variant;

        // Optional data for entities that need it.
        public Vector2 size;
        public Vector2 colliderOffset;
        public Vector2 velocity;
        public bool isTrigger;

        // Generic mover path (optional).
        public StagePathNode[] path;
        public int loopMode;
        public float startOffsetSeconds;

        // Optional behavior params (per-entity-kind).
        // BulletBillLauncher uses these for shoot cadence + range.
        public float param0;
        public float param1;
        public float param2;
    }

    public sealed class StageDefinition : ScriptableObject {
        public string stageKey;

        // World-space spawn point for Player 1 (Unity units).
        public Vector2 spawnPoint;

        // Camera clamp (world-space, Unity units). If min == max, bounds are considered unset.
        public Vector2 cameraMin;
        public Vector2 cameraMax;

        public bool isWrappingLevel;

        public List<StageTileLayer> tileLayers = new List<StageTileLayer>();
        public List<StageEntity> entities = new List<StageEntity>();
    }
}
