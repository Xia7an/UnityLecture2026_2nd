namespace Game.Core
{
    /// <summary>ゲームの決着状況。</summary>
    public enum GameOutcome
    {
        /// <summary>まだ決着していない。</summary>
        InProgress,

        /// <summary>全コインを取得した。</summary>
        Clear,

        /// <summary>HP が 0 になった、または時間切れになった。</summary>
        Failure,
    }

    public static class GameOutcomeEvaluator
    {
        /// <summary>
        /// 現在の状態から決着状況を求める。
        ///
        /// 終了理由を GameState のフィールドとして別に持たないのは、
        /// HP・残り時間・コイン枚数から導出できる値を二重に管理しないためである。
        /// Play シーンは終了判定に、Result シーンは表示内容の決定に、同じこの関数を使う。
        ///
        /// 副作用のない純粋関数なので、シーンを開かずに EditMode テストで検証できる。
        /// </summary>
        public static GameOutcome Evaluate(GameState state)
        {
            if (state == null) return GameOutcome.InProgress;

            // 全コイン取得が最優先。取り切った瞬間に時間切れが重なってもクリア扱いとする。
            if (state.TotalCoinCount.CurrentValue > 0 &&
                state.CollectedCoinCount.CurrentValue >= state.TotalCoinCount.CurrentValue)
            {
                return GameOutcome.Clear;
            }

            if (state.Hp.CurrentValue <= 0) return GameOutcome.Failure;

            // コインを集めきれずに時間切れになった状態なので失敗として扱う。
            if (state.RemainingTimeSeconds.CurrentValue <= 0f) return GameOutcome.Failure;

            return GameOutcome.InProgress;
        }

        /// <summary>決着状況をシーン遷移用の SceneResult へ変換する。</summary>
        public static SceneResult ToSceneResult(this GameOutcome outcome) => outcome switch
        {
            GameOutcome.Clear => SceneResult.GameClear,
            GameOutcome.Failure => SceneResult.GameFailure,
            _ => SceneResult.Normal,
        };
    }
}
