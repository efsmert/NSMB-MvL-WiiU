using UnityEngine;

namespace NSMB.Items {
    [RequireComponent(typeof(Collider2D))]
    public sealed class CoinPickup : MonoBehaviour {
        public int coinValue = 1;
        public int scoreValue = 200;
        public float sfxVolume = 0.8f;

        private void Awake() {
            EnsureVisuals();
        }

        private void Reset() {
            Collider2D c = GetComponent<Collider2D>();
            c.isTrigger = true;
        }

        private void EnsureVisuals() {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) {
                return;
            }

            bool placeholder = (sr.sprite == null) || (sr.sprite.texture == Texture2D.whiteTexture);
            if (!placeholder) {
                return;
            }

            Sprite[] frames = NSMB.Visual.GameplaySprites.GetCoinSpinFrames();
            if (frames != null && frames.Length > 0) {
                sr.sprite = frames[0];
                sr.color = Color.white;
                sr.sortingOrder = 0;

                NSMB.Visual.SimpleSpriteAnimator anim = GetComponent<NSMB.Visual.SimpleSpriteAnimator>();
                if (anim == null) {
                    anim = gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                }
                anim.SetFrames(frames, 12f, true);
            }
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
                    audio.PlayOneShot(NSMB.Audio.SoundEffectId.World_Coin_Collect, sfxVolume);
                }
            }

            Destroy(gameObject);
        }
    }
}
