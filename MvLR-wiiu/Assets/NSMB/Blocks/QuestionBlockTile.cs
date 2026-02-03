using UnityEngine;

namespace NSMB.Blocks {
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class QuestionBlockTile : MonoBehaviour {
        public string usedSpriteName = "animation_4";
        public float animationFps = 8f;

        private bool _used;
        private SpriteRenderer _sr;
        private NSMB.Visual.SimpleSpriteAnimator _anim;

        private void Awake() {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) {
                _sr = gameObject.AddComponent<SpriteRenderer>();
            }

            // Play the standard question block animation (first 4 frames).
            Sprite[] frames = NSMB.Visual.GameplaySprites.GetQuestionBlockFrames();
            if (frames != null && frames.Length > 0) {
                _anim = GetComponent<NSMB.Visual.SimpleSpriteAnimator>();
                if (_anim == null) {
                    _anim = gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                }
                _anim.SetFrames(frames, animationFps, true);
            }
        }

        // Called by BlockBump via SendMessage
        private void OnBumped() {
            if (_used) {
                return;
            }

            _used = true;

            // Stop anim and swap to a "used" sprite if available.
            if (_anim != null) {
                _anim.enabled = false;
            }

            if (_sr != null && !string.IsNullOrEmpty(usedSpriteName)) {
                Sprite used = NSMB.Content.ResourceSpriteCache.FindSprite(NSMB.Content.GameplayAtlasPaths.AnimatedBlocks, usedSpriteName);
                if (used != null) {
                    _sr.sprite = used;
                }
            }
        }
    }
}

