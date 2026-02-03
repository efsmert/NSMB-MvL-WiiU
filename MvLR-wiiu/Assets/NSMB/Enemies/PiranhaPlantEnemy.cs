using UnityEngine;

namespace NSMB.Enemies {
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PiranhaPlantEnemy : MonoBehaviour {
        public float animationFps = 10f;

        private void Awake() {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) {
                sr = gameObject.AddComponent<SpriteRenderer>();
            }

            // Default to the imported atlas frames.
            Sprite[] frames = NSMB.Content.ResourceSpriteCache.FindSpritesByPrefix(NSMB.Content.GameplayAtlasPaths.PiranhaPlant, "piranhaplant_");
            if (frames != null && frames.Length > 0) {
                sr.sprite = frames[0];
                sr.color = Color.white;
                sr.sortingOrder = 0;

                NSMB.Visual.SimpleSpriteAnimator anim = GetComponent<NSMB.Visual.SimpleSpriteAnimator>();
                if (anim == null) {
                    anim = gameObject.AddComponent<NSMB.Visual.SimpleSpriteAnimator>();
                }
                anim.SetFrames(frames, animationFps, true);
            }
        }
    }
}

