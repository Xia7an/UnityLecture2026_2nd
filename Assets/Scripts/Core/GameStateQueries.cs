using System;
using R3;

namespace Game.Core
{
    /// <summary>
    /// 状態と設定から表示用の値を導出する関数を集めた場所。
    ///
    /// GameOutcomeEvaluator と同じ考え方で、導出できる値は状態として持たずここで求める。
    /// 副作用がないので、シーンを開かずに EditMode テストで検証できる。
    ///
    /// 「最大 HP」のように変化しない値は IGameStateSettings 側にあるため、
    /// 状態だけでは割合を求められない。両方を引数に取るのがこのクラスの役目である。
    /// </summary>
    public static class GameStateQueries
    {
        /// <summary>
        /// 最大 HP に対する残り HP の割合。0〜1 に収まるので、
        /// Image.fillAmount にそのまま渡せる。
        /// </summary>
        public static Observable<float> HpRatio(GameState state, IGameStateSettings settings)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            // 設定ミスで MaxHp が 0 でも 0 除算にならないようにしておく。
            var maxHp = Math.Max(1, settings.MaxHp);

            return state.Hp.Select(hp => Math.Clamp(hp / (float)maxHp, 0f, 1f));
        }
    }
}
