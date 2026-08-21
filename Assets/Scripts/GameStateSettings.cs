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
        [SerializeField] private int coinCount = 30;

        /// <summary>フィールドに配置するコインの枚数。</summary>
        public int CoinCount => coinCount;
    }
}
