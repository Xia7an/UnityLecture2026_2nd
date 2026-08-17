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
        [SerializeField] private float timeLimitSeconds = 120f;
        [SerializeField] private int coinCount = 30;
        [SerializeField] private int specialCoinCount = 3;
        [SerializeField] private int damageOnEnemyHit = 10;
        [SerializeField] private float invincibleDuration = 10f;

        /// <summary>プレイヤーの初期 HP。</summary>
        public int MaxHp => maxHp;

        /// <summary>制限時間（秒）。</summary>
        public float TimeLimitSeconds => timeLimitSeconds;

        /// <summary>フィールドに配置するコインの枚数（通常・特殊の合計）。</summary>
        public int CoinCount => coinCount;

        /// <summary>CoinCount のうち特殊コインにする枚数。</summary>
        public int SpecialCoinCount => specialCoinCount;

        /// <summary>敵と衝突したときに減少する HP。</summary>
        public int DamageOnEnemyHit => damageOnEnemyHit;

        /// <summary>特殊コイン取得後に無敵でいられる時間（秒）。</summary>
        public float InvincibleDuration => invincibleDuration;
    }
}
