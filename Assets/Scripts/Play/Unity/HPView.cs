using Game.Core;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Play
{
    public class HPView: UIView
    {
        [SerializeField] private Image hpGauge;

        public override void Initialize(GameState gameState, IGameStateSettings settings)
        {
            // 割合の計算は Core の純粋関数に任せ、View は流れてきた値を反映するだけにする。
            GameStateQueries.HpRatio(gameState, settings)
                .Subscribe(ratio => hpGauge.fillAmount = ratio)
                .AddTo(this);
        }
    }
}
