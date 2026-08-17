using System;
using Game.Core;
using R3;
using TMPro;
using UnityEngine;

namespace Game.Play
{
    public class TimerView: UIView
    {
        private GameState _gameState;
        
        [SerializeField] private TextMeshProUGUI timerText;

        public override void Initialize(GameState gameState)
        {
            _gameState = gameState;

            _gameState.RemainingTimeSeconds.Subscribe(v =>
            {
                // 秒を分秒に変換
                timerText.text = TimeSpan.FromSeconds(v).ToString(@"mm\:ss");
            }).AddTo(this);
        }
    }
}