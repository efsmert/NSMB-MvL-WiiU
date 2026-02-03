using UnityEngine;

namespace NSMB.World {
    public sealed class DamageOnTouch : MonoBehaviour {
        private void OnTriggerEnter2D(Collider2D other) {
            if (other == null) {
                return;
            }

            NSMB.Player.PlayerHealth ph = other.GetComponent<NSMB.Player.PlayerHealth>();
            if (ph != null) {
                ph.TakeHit();
            }
        }
    }
}

