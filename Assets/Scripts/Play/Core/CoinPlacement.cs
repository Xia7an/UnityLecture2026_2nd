using UnityEngine;

namespace Game.Play.Core
{
    /// <summary>コインの種類。</summary>
    public enum CoinKind
    {
        /// <summary>通常コイン。取得すると枚数が増えるだけ。</summary>
        Normal,

        /// <summary>特殊コイン。取得すると一定時間無敵になる。</summary>
        Special,
    }

    /// <summary>コイン 1 枚をどこにどの種類で置くか。</summary>
    public readonly struct CoinPlacement
    {
        public CoinKind Kind { get; }
        public Vector3 Position { get; }

        public CoinPlacement(CoinKind kind, Vector3 position)
        {
            Kind = kind;
            Position = position;
        }
    }
}
