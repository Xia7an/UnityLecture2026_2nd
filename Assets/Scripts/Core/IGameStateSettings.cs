namespace Game.Core
{
    /// <summary>
    /// ゲーム状態の既定値。
    ///
    /// Core 層は ScriptableObject を知らない。実体は Unity 層の GameStateSettings が持ち、
    /// Core 層はこのインターフェース越しにだけ参照する。
    /// IPlayerInput と同じく「依存の向きを Unity → Core の一方向に保つ」ための境界である。
    /// </summary>
    public interface IGameStateSettings
    {
        /// <summary>プレイヤーの初期 HP。</summary>
        int MaxHp { get; }

        /// <summary>制限時間（秒）。</summary>
        float TimeLimitSeconds { get; }

        /// <summary>フィールドに配置するコインの枚数。</summary>
        int CoinCount { get; }

        /// <summary>敵と衝突したときに減少する HP。</summary>
        int DamageOnEnemyHit { get; }

        /// <summary>特殊コイン取得後に無敵でいられる時間（秒）。</summary>
        float InvincibleDuration { get; }
    }
}
