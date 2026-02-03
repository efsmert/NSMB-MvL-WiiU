using UnityEngine;

namespace NSMB.Enemies {
    public sealed class PiranhaPlantEnemy : MonoBehaviour {
        public float animationFps = 10f;

        private void Awake() {
            Transform graphics;
            SpriteRenderer sr;
            NSMB.Visual.SimpleSpriteAnimator anim;
            Unity6EnemyPrototypes.ApplyPiranhaPlant(gameObject, out graphics, out sr, out anim);
            if (anim != null && animationFps > 0f) {
                anim.framesPerSecond = animationFps;
            }
        }
    }
}
