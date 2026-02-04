using UnityEngine;

namespace NSMB.Blocks {
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BlockBump : MonoBehaviour {
        public float bumpHeight = 0.18f;
        public float bumpDuration = 0.10f;
        public bool playBumpSfx = true;
        public NSMB.Audio.SoundEffectId bumpSfxId = NSMB.Audio.SoundEffectId.World_Block_Bump;
        public float bumpSfxVolume = 0.8f;

        private Vector3 _startPos;
        private bool _bumping;
        private float _t;

        private NSMB.Player.PlayerMotor2D _lastBumper;

        public NSMB.Player.PlayerMotor2D LastBumper {
            get { return _lastBumper; }
        }

        private void Awake() {
            _startPos = transform.position;
        }

        private void Update() {
            if (!_bumping) {
                return;
            }

            _t += Time.deltaTime;
            float half = bumpDuration;
            float yOffset;

            if (_t <= half) {
                float p = _t / half;
                yOffset = Mathf.Lerp(0f, bumpHeight, p);
            } else if (_t <= half * 2f) {
                float p = (_t - half) / half;
                yOffset = Mathf.Lerp(bumpHeight, 0f, p);
            } else {
                yOffset = 0f;
                _bumping = false;
                _t = 0f;
                SendMessage("OnBumpFinished", SendMessageOptions.DontRequireReceiver);
            }

            transform.position = _startPos + new Vector3(0f, yOffset, 0f);
        }

        public void TriggerBump(NSMB.Player.PlayerMotor2D bumper) {
            _lastBumper = bumper;
            TriggerBump();
        }

        public void TriggerBump() {
            if (_bumping) {
                return;
            }

            _bumping = true;
            _t = 0f;
            _startPos = transform.position;

            if (playBumpSfx) {
                NSMB.Core.GameRoot root = NSMB.Core.GameRoot.Instance;
                if (root != null) {
                    NSMB.Audio.AudioManager audio = root.GetComponent<NSMB.Audio.AudioManager>();
                    if (audio != null) {
                        audio.PlayOneShot(bumpSfxId, bumpSfxVolume);
                    }
                }
            }

            SendMessage("OnBumped", SendMessageOptions.DontRequireReceiver);
        }
    }
}
