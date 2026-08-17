using Game.Core;
using R3;
using TMPro;
using UnityEngine;

namespace Game.Play
{
    public class CoinCountView: UIView
    {
        private GameState _gameState;
        
        [SerializeField] private TextMeshProUGUI timerText;
        
        public override void Initialize(GameState gameState)
        {
            _gameState = gameState;
            gameState.CollectedCoinCount.Subscribe(v =>
            {
                timerText.text = $"Coin Count : {v.ToString()}";
            }).AddTo(this);
        }
    }
}