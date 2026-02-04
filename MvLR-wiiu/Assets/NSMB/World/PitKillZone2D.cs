using UnityEngine;

namespace NSMB.World {
    // Kills/respawns players that fall below a stage's "void" threshold.
    public sealed class PitKillZone2D : MonoBehaviour {
        [Tooltip("World-space Y below which players are killed.")]
        public float killY = -8f;

        [Tooltip("Seconds between repeated pit kills for the same player.")]
        public float perPlayerCooldownSeconds = 0.5f;

        private float _nextAllowedTime;

        private void Update() {
            if (Time.time < _nextAllowedTime) {
                return;
            }

            NSMB.Player.PlayerHealth[] players = Object.FindObjectsOfType(typeof(NSMB.Player.PlayerHealth)) as NSMB.Player.PlayerHealth[];
            if (players == null || players.Length == 0) {
                return;
            }

            bool killedAny = false;
            for (int i = 0; i < players.Length; i++) {
                NSMB.Player.PlayerHealth ph = players[i];
                if (ph == null) continue;

                Transform t = ph.transform;
                if (t != null && t.position.y < killY) {
                    ph.ForceRespawnFromPit();
                    killedAny = true;
                }
            }

            if (killedAny) {
                _nextAllowedTime = Time.time + Mathf.Max(0.05f, perPlayerCooldownSeconds);
            }
        }
    }
}

