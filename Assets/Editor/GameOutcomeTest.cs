using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// 終了判定のテスト。
    ///
    /// 決着状況を GameState のフィールドとして持たず、他の値から導出する純粋関数にしたので、
    /// このように状態を組み立てて呼ぶだけで検証できる。
    /// </summary>
    public sealed class GameOutcomeTest
    {
        /// <summary>テスト用の設定値。</summary>
        private sealed class StubSettings : IGameStateSettings
        {
            public int CoinCount { get; set; } = 30;
        }

        private static GameState CreateResetState(StubSettings settings = null)
        {
            var state = new GameState();
            state.Reset(settings ?? new StubSettings());
            return state;
        }

        [Test]
        public void 初期化直後は決着していない()
        {
            using var state = CreateResetState();

            Assert.That(GameOutcomeEvaluator.Evaluate(state), Is.EqualTo(GameOutcome.InProgress));
        }

        [Test]
        public void 全コインを取得するとクリア()
        {
            using var state = CreateResetState(new StubSettings { CoinCount = 3 });

            state.CollectCoin();
            state.CollectCoin();
            Assert.That(GameOutcomeEvaluator.Evaluate(state), Is.EqualTo(GameOutcome.InProgress));

            state.CollectCoin();
            Assert.That(GameOutcomeEvaluator.Evaluate(state), Is.EqualTo(GameOutcome.Clear));
        }

        [Test]
        public void Resetで前回の状態が残らない()
        {
            var settings = new StubSettings { CoinCount = 30 };
            using var state = CreateResetState(settings);

            state.CollectCoin();

            state.Reset(settings);

            Assert.That(state.CollectedCoinCount.CurrentValue, Is.EqualTo(0));
            Assert.That(state.TotalCoinCount.CurrentValue, Is.EqualTo(30));
        }

        [Test]
        public void 決着状況はSceneResultへ変換できる()
        {
            Assert.That(GameOutcome.Clear.ToSceneResult(), Is.EqualTo(SceneResult.GameClear));
            Assert.That(GameOutcome.InProgress.ToSceneResult(), Is.EqualTo(SceneResult.Normal));
        }
    }
}
