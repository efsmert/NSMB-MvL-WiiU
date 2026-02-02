using UnityEngine;

namespace NSMB.Gameplay {
    public sealed class GameManager : MonoBehaviour {
        private static GameManager _instance;

        public static GameManager Instance {
            get { return _instance; }
        }

        private int _coins;
        private int _score;

        public int Coins {
            get { return _coins; }
        }

        public int Score {
            get { return _score; }
        }

        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddCoins(int amount) {
            if (amount <= 0) return;
            _coins += amount;
            SendMessage("OnCoinsChanged", SendMessageOptions.DontRequireReceiver);
        }

        public void AddScore(int amount) {
            if (amount <= 0) return;
            _score += amount;
            SendMessage("OnScoreChanged", SendMessageOptions.DontRequireReceiver);
        }
    }
}

