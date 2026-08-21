using Game.Core;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// ゲーム状態の既定値。変化しない設定値はすべてここに置き、GameState には持たせない。
    /// GameRoot が保持し、GameState.Reset に渡す。
    /// </summary>
    [CreateAssetMenu(fileName = "GameStateSettings", menuName = "Game/Game State Settings")]
    public sealed class GameStateSettings : ScriptableObject, IGameStateSettings
    {
        [SerializeField] private int maxHp = 100;
        [SerializeField] private float timeLimitSeconds = 60f;
        [SerializeField] private int coinCount = 30;
        [SerializeField] private int damageOnEnemyHit = 10;

        /// <summary>プレイヤーの初期 HP。</summary>
        public int MaxHp => maxHp;

        /// <summary>制限時間（秒）。</summary>
        public float TimeLimitSeconds => timeLimitSeconds;

        /// <summary>フィールドに配置するコインの枚数。</summary>
        public int CoinCount => coinCount;

        /// <summary>敵と衝突したときに減少する HP。</summary>
        public int DamageOnEnemyHit => damageOnEnemyHit;
    }
}
