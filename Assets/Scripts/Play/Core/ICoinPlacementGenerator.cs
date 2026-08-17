using System.Collections.Generic;

namespace Game.Play.Core
{
    /// <summary>
    /// コインの配置を決める。
    ///
    /// ICharacterMoveLogic と同じ考え方で、「どこに置くか」という判断を
    /// シーン上のオブジェクト生成から切り離してある。
    /// 実装を差し替えれば、ランダム配置を格子状配置や手動配置に変えられる。
    ///
    /// UnityEngine の GameObject を作らない純粋な計算なので、
    /// シーンを開かずに EditMode テストで検証できる。
    /// </summary>
    public interface ICoinPlacementGenerator
    {
        /// <summary>
        /// コインの配置を決める。
        /// </summary>
        /// <param name="totalCount">配置する総枚数。</param>
        /// <param name="specialCount">そのうち特殊コインにする枚数。</param>
        IReadOnlyList<CoinPlacement> Generate(int totalCount, int specialCount);
    }
}
