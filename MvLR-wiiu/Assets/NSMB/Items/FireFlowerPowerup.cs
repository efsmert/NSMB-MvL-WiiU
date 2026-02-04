using UnityEngine;

namespace NSMB.Items {
    [RequireComponent(typeof(Collider2D))]
    public sealed class FireFlowerPowerup : MonoBehaviour {
        public int scoreValue = 1000;
        public float collectSfxVolume = 0.8f;

        private void Awake() {
            EnsureVisuals();
            Collider2D c = GetComponent<Collider2D>();
            if (c != null) {
                c.isTrigger = true;
            }
        }

        private void EnsureVisuals() {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) {
                sr = gameObject.AddComponent<SpriteRenderer>();
            }

            bool placeholder = (sr.sprite == null) || (sr.sprite.texture == Texture2D.whiteTexture);
            if (!placeholder) {
                return;
            }

            Sprite flower = NSMB.Visual.GameplaySprites.GetFireFlower();
            if (flower != null) {
                sr.sprite = flower;
                sr.color = Color.white;
                sr.sortingOrder = 0;
            }
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other == null) {
                return;
            }

            NSMB.Player.PlayerMotor2D player = other.GetComponent<NSMB.Player.PlayerMotor2D>();
            if (player == null) {
                player = other.GetComponentInParent<NSMB.Player.PlayerMotor2D>();
            }
            if (player == null) {
                return;
            }

            NSMB.Player.PlayerPowerupState ps = player.GetComponent<NSMB.Player.PlayerPowerupState>();
            if (ps != null) {
                ps.CollectFireFlower();
            }

            NSMB.Gameplay.GameManager gm = NSMB.Gameplay.GameManager.Instance;
            if (gm != null) {
                gm.AddScore(scoreValue);
            }

            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root != null) {
                NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
                if (audio != null) {
                    audio.PlayOneShot(NSMB.Audio.SoundEffectId.Player_Sound_PowerupCollect, collectSfxVolume);
                }
            }

            Destroy(gameObject);
        }
    }
}

