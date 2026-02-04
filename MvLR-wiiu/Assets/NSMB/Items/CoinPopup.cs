using UnityEngine;

namespace NSMB.Items {
    public sealed class CoinPopup : MonoBehaviour {
        public float riseSpeed = 4.5f;
        public float lifeSeconds = 0.45f;

        private float _t;

        private void Update() {
            transform.position += new Vector3(0f, riseSpeed * Time.deltaTime, 0f);
            _t += Time.deltaTime;
            if (_t >= lifeSeconds) {
                Destroy(gameObject);
            }
        }
    }
}

