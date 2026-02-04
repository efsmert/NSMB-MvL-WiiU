using UnityEngine;

namespace NSMB.Gameplay {
    public sealed class GameManager : MonoBehaviour {
        private static GameManager _instance;

        public static GameManager Instance {
            get { return _instance; }
        }

        [Header("Objectives (HUD)")]
        [Tooltip("Coin requirement shown in HUD (Unity 6 default is often 8).")]
        [SerializeField] private int _coinRequirement = 8;
        [Tooltip("Star requirement shown in HUD (Unity 6 default is often 10).")]
        [SerializeField] private int _starRequirement = 10;

        private int _coins;
        private int _score;
        private int _stars;

        public int Coins {
            get { return _coins; }
        }

        public int Stars {
            get { return _stars; }
        }

        public int Score {
            get { return _score; }
        }

        public int CoinRequirement {
            get { return _coinRequirement; }
        }

        public int StarRequirement {
            get { return _starRequirement; }
        }

        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetRequirements(int coinRequirement, int starRequirement) {
            _coinRequirement = Mathf.Max(0, coinRequirement);
            _starRequirement = Mathf.Max(0, starRequirement);
            SendMessage("OnObjectivesChanged", SendMessageOptions.DontRequireReceiver);
        }

        public void AddCoins(int amount) {
            if (amount <= 0) return;
            _coins += amount;
            SendMessage("OnCoinsChanged", SendMessageOptions.DontRequireReceiver);
        }

        public void AddStars(int amount) {
            if (amount <= 0) return;
            _stars += amount;
            SendMessage("OnStarsChanged", SendMessageOptions.DontRequireReceiver);
        }

        public void AddScore(int amount) {
            if (amount <= 0) return;
            _score += amount;
            SendMessage("OnScoreChanged", SendMessageOptions.DontRequireReceiver);
        }
    }
}
