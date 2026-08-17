using UnityEngine;

namespace Game.Play.Core
{
    /// <summary>
    /// プレイヤーの入力。
    ///
    /// Core 層は UnityEngine.InputSystem を知らない。実体は Unity 層の
    /// PlayerInputAdapter が持ち、Core 層はこのインターフェース越しにだけ参照する。
    ///
    /// この境界があることで、PlayerMoveLogic はシーンを開かずにテストできる。
    /// テストではこのインターフェースを実装したスタブを渡せばよい。
    ///
    /// Move も Dash も「押されている状態」を表すため、イベントではなく
    /// 毎フレームの参照（ポーリング）で扱う。押した瞬間だけが意味を持つ入力
    /// （Title の Start、Result の Back）は、状態ではなく出来事なので別に扱う。
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>移動方向。長さが 1 を超えないよう正規化済み。</summary>
        Vector2 MoveDirection { get; }

        /// <summary>ダッシュ入力が押され続けているか。</summary>
        bool IsDashing { get; }
    }
}
