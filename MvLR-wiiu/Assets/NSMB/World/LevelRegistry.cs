using System;
using UnityEngine;

namespace NSMB.World {
    public static class LevelRegistry {
        public static GameObject Spawn(string stageKey) {
            if (string.IsNullOrEmpty(stageKey)) {
                stageKey = "stage-grassland";
            }

            // Alias for stages not yet imported as distinct scenes.
            if (string.Equals(stageKey, "stage-bonus-2", StringComparison.InvariantCultureIgnoreCase)) {
                stageKey = "stage-bonus";
            }

            GameObject root = new GameObject("Level");

            // Preferred path: load an imported StageDefinition from Resources.
            NSMB.World.StageDefinition imported = Resources.Load<NSMB.World.StageDefinition>("NSMB/Levels/" + stageKey);
            if (imported != null) {
                NSMB.World.StageRuntimeBuilder.Build(imported, root.transform);
                return root;
            }

            // Map menu stage icons to simple bootstraps for now.
            // Next step: replace these with proper converted levels.
            switch (stageKey) {
                case "stage-grassland":
                case "stage-beach":
                case "stage-jungle":
                    root.AddComponent<TestLevelBootstrap>();
                    break;
                default:
                    root.AddComponent<FlatLevelBootstrap>();
                    break;
            }

            return root;
        }
    }
}
