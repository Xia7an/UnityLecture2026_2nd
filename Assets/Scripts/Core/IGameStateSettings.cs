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
        /// <summary>フィールドに配置するコインの枚数。</summary>
        int CoinCount { get; }
    }
}
