using System;
using UnityEngine;

namespace NSMB.World {
    public static class LevelRegistry {
        public static GameObject Spawn(string stageKey) {
            if (string.IsNullOrEmpty(stageKey)) {
                stageKey = "stage-grassland";
            }

            GameObject root = new GameObject("Level");

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

