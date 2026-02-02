using UnityEngine;

namespace NSMB.Items {
    [RequireComponent(typeof(Collider2D))]
    public sealed class CoinPickup : MonoBehaviour {
        public int coinValue = 1;
        public int scoreValue = 200;
        public string sfxResourcesPath = "NSMB/AudioClips/Resources/Sound/coin_collect";
        public float sfxVolume = 0.8f;

        private void Reset() {
            Collider2D c = GetComponent<Collider2D>();
            c.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.GetComponent<NSMB.Player.PlayerMotor2D>() == null) {
                return;
            }

            NSMB.Gameplay.GameManager gm = NSMB.Gameplay.GameManager.Instance;
            if (gm != null) {
                gm.AddCoins(coinValue);
                gm.AddScore(scoreValue);
            }

            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root != null) {
                NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
                if (audio != null) {
                    audio.PlayResourcesOneShot(sfxResourcesPath, sfxVolume);
                }
            }

            Destroy(gameObject);
        }
    }
}

