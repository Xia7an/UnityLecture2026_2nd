namespace Game.Core
{
    /// <summary>ゲームの決着状況。</summary>
    public enum GameOutcome
    {
        /// <summary>まだ決着していない。</summary>
        InProgress,

        /// <summary>全コインを取得した。</summary>
        Clear,
    }

    public static class GameOutcomeEvaluator
    {
        /// <summary>
        /// 現在の状態から決着状況を求める。
        ///
        /// 「クリアしたかどうか」を GameState のフィールドとして別に持たないのは、
        /// コイン枚数から導出できる値を二重に管理しないためである。
        /// Play シーンは終了判定に、Result シーンは表示内容の決定に、同じこの関数を使う。
        ///
        /// 副作用のない純粋関数なので、シーンを開かずに EditMode テストで検証できる。
        /// </summary>
        public static GameOutcome Evaluate(GameState state)
        {
            if (state == null) return GameOutcome.InProgress;

            if (state.TotalCoinCount.CurrentValue > 0 &&
                state.CollectedCoinCount.CurrentValue >= state.TotalCoinCount.CurrentValue)
            {
                return GameOutcome.Clear;
            }

            return GameOutcome.InProgress;
        }

        /// <summary>決着状況をシーン遷移用の SceneResult へ変換する。</summary>
        public static SceneResult ToSceneResult(this GameOutcome outcome) => outcome switch
        {
            GameOutcome.Clear => SceneResult.GameClear,
            _ => SceneResult.Normal,
        };
    }
}
