using UnityEngine;

namespace Game.Play.Core
{
    /// <summary>
    /// キャラクターの移動ロジック。
    ///
    /// プレイヤーの入力移動も敵のランダムウォークも、この 1 つのインターフェースの
    /// 別実装として表す。見た目を扱う CharacterView は、渡されたのがどちらの実装かを知らない。
    ///
    /// そのため講習後半の「移動ロジックの差し替え」は、実装クラスを 1 つ追加して
    /// CompositionRoot の 1 行を変えるだけの作業になる。
    ///
    /// 戻り値を位置ではなく速度にしているのは、壁や敵との衝突を知らないロジック側が
    /// 位置を確定させてしまわないようにするためである。位置の更新は View 側の責務とする。
    /// </summary>
    public interface ICharacterMoveLogic
    {
        /// <summary>現在位置と経過時間から、このフレームの速度を返す。</summary>
        Vector3 EvaluateVelocity(Vector3 currentPosition, float deltaTime);
    }
}
