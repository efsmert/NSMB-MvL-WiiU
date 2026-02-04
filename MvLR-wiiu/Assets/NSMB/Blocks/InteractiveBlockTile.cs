using UnityEngine;

namespace NSMB.Blocks {
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class InteractiveBlockTile : MonoBehaviour {
        [System.Flags]
        private enum BreakableBy {
            SmallMario = 1 << 0,
            SmallMarioGroundpound = 1 << 1,
            SmallMarioDrill = 1 << 2,
            LargeMario = 1 << 3,
            LargeMarioGroundpound = 1 << 4,
            LargeMarioDrill = 1 << 5,
            MegaMario = 1 << 6,
            Shells = 1 << 7,
            Bombs = 1 << 8,
        }

        public NSMB.World.StageTileInteractionKind interactionKind = NSMB.World.StageTileInteractionKind.None;
        public int breakingRules;
        public bool bumpIfNotBroken = true;

        public string usedAtlasPath;
        public string usedSpriteName;

        public NSMB.World.StagePowerupKind smallPowerup = NSMB.World.StagePowerupKind.None;
        public NSMB.World.StagePowerupKind largePowerup = NSMB.World.StagePowerupKind.None;

        public bool isQuestionBlockVisual;
        public float questionAnimationFps = 8f;

        public int coinValue = 1;
        public int coinScoreValue = 200;

        private bool _used;
        private bool _swapToUsedOnFinish;
        private SpriteRenderer _sr;
        private NSMB.Visual.SimpleSpriteAnimator _anim;

        private void Awake() {
            _sr = GetComponent<SpriteRenderer>();

            if (isQuestionBlockVisual) {
                Sprite[] frames = NSMB.Visual.GameplaySprites.GetQuestionBlockFrames();
                if (frames != null && frames.Length > 0) {
                    _anim = GetComponent<NSMB.Visual.SimpleSpriteAnimator>();
                    if (_anim == null) {
                        _anim = gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                    }
                    _anim.SetFrames(frames, questionAnimationFps, true);
                    if (_sr != null) {
                        _sr.sprite = frames[0];
                    }
                }
            }
        }

        // Called by BlockBump via SendMessage.
        private void OnBumped() {
            if (_used && (interactionKind == NSMB.World.StageTileInteractionKind.CoinTile ||
                         interactionKind == NSMB.World.StageTileInteractionKind.PowerupTile ||
                         interactionKind == NSMB.World.StageTileInteractionKind.RouletteTile)) {
                PlaySfx(NSMB.Audio.SoundEffectId.World_Block_Bump, 0.8f);
                return;
            }

            NSMB.Player.PlayerMotor2D bumper = null;
            BlockBump bump = GetComponent<BlockBump>();
            if (bump != null) {
                bumper = bump.LastBumper;
            }

            NSMB.Player.PlayerPowerupState power = (bumper != null) ? bumper.GetComponent<NSMB.Player.PlayerPowerupState>() : null;
            bool isLarge = (power != null) && power.IsLarge;

            if (interactionKind == NSMB.World.StageTileInteractionKind.BreakableBrick ||
                interactionKind == NSMB.World.StageTileInteractionKind.CoinTile ||
                interactionKind == NSMB.World.StageTileInteractionKind.PowerupTile ||
                interactionKind == NSMB.World.StageTileInteractionKind.RouletteTile) {

                // Upwards interaction only (hit from below).
                BreakableBy rules = (BreakableBy)breakingRules;
                bool shouldBreak = false;
                if (isLarge) {
                    shouldBreak = (rules & BreakableBy.LargeMario) != 0;
                } else {
                    shouldBreak = (rules & BreakableBy.SmallMario) != 0;
                }

                if (shouldBreak) {
                    PlaySfx(NSMB.Audio.SoundEffectId.World_Block_Break, 0.85f);
                    BreakBlock();
                    return;
                }
            }

            // Not breaking.
            if (interactionKind == NSMB.World.StageTileInteractionKind.CoinTile) {
                PlaySfx(NSMB.Audio.SoundEffectId.World_Block_Bump, 0.8f);
                GiveCoin();
                SpawnCoinPopup();
                _swapToUsedOnFinish = true;
                return;
            }

            if (interactionKind == NSMB.World.StageTileInteractionKind.PowerupTile ||
                interactionKind == NSMB.World.StageTileInteractionKind.RouletteTile) {
                PlaySfx(NSMB.Audio.SoundEffectId.World_Block_Powerup, 0.85f);
                SpawnPowerup(isLarge);
                _swapToUsedOnFinish = true;
                return;
            }

            // Plain brick bump.
            if (interactionKind == NSMB.World.StageTileInteractionKind.BreakableBrick) {
                PlaySfx(NSMB.Audio.SoundEffectId.World_Block_Bump, 0.8f);
                return;
            }

            // Default bump sound for unknown interactive blocks.
            PlaySfx(NSMB.Audio.SoundEffectId.World_Block_Bump, 0.8f);
        }

        // Called by BlockBump when the bump animation ends.
        private void OnBumpFinished() {
            if (!_swapToUsedOnFinish) {
                return;
            }
            _swapToUsedOnFinish = false;
            _used = true;

            if (_anim != null) {
                _anim.enabled = false;
            }

            if (_sr != null && !string.IsNullOrEmpty(usedAtlasPath) && !string.IsNullOrEmpty(usedSpriteName)) {
                Sprite used = NSMB.Content.ResourceSpriteCache.FindSprite(usedAtlasPath, usedSpriteName);
                if (used != null) {
                    _sr.sprite = used;
                }
            }
        }

        private void GiveCoin() {
            NSMB.Gameplay.GameManager gm = NSMB.Gameplay.GameManager.Instance;
            if (gm != null) {
                gm.AddCoins(coinValue);
                gm.AddScore(coinScoreValue);
            }
            PlaySfx(NSMB.Audio.SoundEffectId.World_Coin_Collect, 0.8f);
        }

        private void SpawnCoinPopup() {
            GameObject go = new GameObject("CoinPopup");
            go.transform.position = transform.position + new Vector3(0f, 0.75f, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            if (_sr != null) {
                sr.sortingOrder = _sr.sortingOrder + 1;
            }

            Sprite[] frames = NSMB.Visual.GameplaySprites.GetCoinSpinFrames();
            if (frames != null && frames.Length > 0) {
                sr.sprite = frames[0];
                NSMB.Visual.SimpleSpriteAnimator anim = go.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                anim.SetFrames(frames, 12f, true);
            }

            go.AddComponent<NSMB.Items.CoinPopup>();
        }

        private void SpawnPowerup(bool isLarge) {
            NSMB.World.StagePowerupKind kind = isLarge ? largePowerup : smallPowerup;
            if (kind == NSMB.World.StagePowerupKind.None) {
                kind = NSMB.World.StagePowerupKind.Mushroom;
            }

            GameObject go = new GameObject(kind.ToString());
            go.transform.position = transform.position + new Vector3(0f, 0.25f, 0f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = false;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            if (_sr != null) {
                sr.sortingOrder = _sr.sortingOrder + 1;
            }

            switch (kind) {
                case NSMB.World.StagePowerupKind.FireFlower:
                    sr.sprite = NSMB.Visual.GameplaySprites.GetFireFlower();
                    go.AddComponent<NSMB.Items.FireFlowerPowerup>();
                    break;
                default:
                    sr.sprite = NSMB.Visual.GameplaySprites.GetMushroom();
                    go.AddComponent<NSMB.Items.MushroomPowerup>();
                    break;
            }

            go.AddComponent<NSMB.Items.PowerupEmerger>();
        }

        private void BreakBlock() {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) {
                col.enabled = false;
            }
            if (_sr != null) {
                _sr.enabled = false;
            }

            Destroy(gameObject);
        }

        private static void PlaySfx(NSMB.Audio.SoundEffectId id, float volume) {
            NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
            if (root == null) {
                return;
            }
            NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
            if (audio == null) {
                return;
            }
            audio.PlayOneShot(id, volume);
        }
    }
}
