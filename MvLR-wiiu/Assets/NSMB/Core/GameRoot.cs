using UnityEngine;

namespace NSMB.Core {
    public sealed class GameRoot : MonoBehaviour {
        private static GameRoot _instance;

        public static GameRoot Instance {
            get { return _instance; }
        }

        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (GetComponent<NSMB.Audio.AudioManager>() == null) {
                gameObject.AddComponent<NSMB.Audio.AudioManager>();
            }

            if (GetComponent<NSMB.Gameplay.GameManager>() == null) {
                gameObject.AddComponent<NSMB.Gameplay.GameManager>();
            }

            if (GetComponent<NSMB.Gameplay.GameFlow>() == null) {
                gameObject.AddComponent<NSMB.Gameplay.GameFlow>();
            }

            if (GetComponent<NSMB.UI.UIRootBootstrap>() == null) {
                gameObject.AddComponent<NSMB.UI.UIRootBootstrap>();
            }
        }
    }
}
