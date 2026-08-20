using Game.Core;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// Play シーンの HUD 表示の基底クラス。PlayCompositionRoot がまとめて初期化する。
    ///
    /// 状態だけでなく設定も受け取るのは、HP バーのように
    /// 「最大 HP に対する割合」を出すには両方が必要になるためである。
    /// 供給元を GameRoot 1 箇所に絞るという CompositionRoot の方針をここまで延長している。
    /// 設定を使わない View は引数を無視してよい。
    /// </summary>
    public abstract class UIView: MonoBehaviour
    {
        public abstract void Initialize(GameState gameState, IGameStateSettings settings);
    }
}