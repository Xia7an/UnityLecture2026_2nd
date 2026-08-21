using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// 終了判定のテスト。
    ///
    /// 終了理由を GameState のフィールドとして持たず、他の値から導出する純粋関数にしたので、
    /// このように状態を組み立てて呼ぶだけで検証できる。
    /// </summary>
    public sealed class GameOutcomeTest
    {
        /// <summary>テスト用の設定値。</summary>
        private sealed class StubSettings : IGameStateSettings
        {
            public int MaxHp { get; set; } = 100;
            public float TimeLimitSeconds { get; set; } = 120f;
            public int CoinCount { get; set; } = 30;
            public int SpecialCoinCount { get; set; } = 3;
            public int DamageOnEnemyHit { get; set; } = 10;
            public float InvincibleDuration { get; set; } = 10f;
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
        public void HPが0になると失敗()
        {
            var settings = new StubSettings { MaxHp = 20, DamageOnEnemyHit = 10 };
            using var state = CreateResetState(settings);

            state.ApplyEnemyHit(settings);
            Assert.That(GameOutcomeEvaluator.Evaluate(state), Is.EqualTo(GameOutcome.InProgress));

            state.ApplyEnemyHit(settings);
            Assert.That(GameOutcomeEvaluator.Evaluate(state), Is.EqualTo(GameOutcome.Failure));
        }

        [Test]
        public void 時間切れは失敗として扱う()
        {
            using var state = CreateResetState(new StubSettings { TimeLimitSeconds = 1f });

            state.Tick(1f);

            Assert.That(state.RemainingTimeSeconds.CurrentValue, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(GameOutcomeEvaluator.Evaluate(state), Is.EqualTo(GameOutcome.Failure));
        }

        [Test]
        public void 無敵中は敵と衝突してもHPが減らない()
        {
            var settings = new StubSettings { MaxHp = 100, DamageOnEnemyHit = 10, InvincibleDuration = 10f };
            using var state = CreateResetState(settings);

            state.CollectSpecialCoin(settings);
            Assert.That(state.IsInvincible, Is.True);

            state.ApplyEnemyHit(settings);
            Assert.That(state.Hp.CurrentValue, Is.EqualTo(100));
        }

        [Test]
        public void 無敵時間が切れるとHPが減るようになる()
        {
            var settings = new StubSettings { MaxHp = 100, DamageOnEnemyHit = 10, InvincibleDuration = 10f };
            using var state = CreateResetState(settings);

            state.CollectSpecialCoin(settings);
            state.Tick(10f);
            Assert.That(state.IsInvincible, Is.False);

            state.ApplyEnemyHit(settings);
            Assert.That(state.Hp.CurrentValue, Is.EqualTo(90));
        }

        [Test]
        public void Resetで前回の状態が残らない()
        {
            var settings = new StubSettings { MaxHp = 100, TimeLimitSeconds = 120f, CoinCount = 30 };
            using var state = CreateResetState(settings);

            state.CollectCoin();
            state.ApplyEnemyHit(settings);
            state.Tick(30f);

            state.Reset(settings);

            Assert.That(state.Hp.CurrentValue, Is.EqualTo(100));
            Assert.That(state.RemainingTimeSeconds.CurrentValue, Is.EqualTo(120f).Within(0.0001f));
            Assert.That(state.CollectedCoinCount.CurrentValue, Is.EqualTo(0));
            Assert.That(state.TotalCoinCount.CurrentValue, Is.EqualTo(30));
            Assert.That(state.InvincibleRemainingSeconds.CurrentValue, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void 決着状況はSceneResultへ変換できる()
        {
            Assert.That(GameOutcome.Clear.ToSceneResult(), Is.EqualTo(SceneResult.GameClear));
            Assert.That(GameOutcome.Failure.ToSceneResult(), Is.EqualTo(SceneResult.GameFailure));
            Assert.That(GameOutcome.InProgress.ToSceneResult(), Is.EqualTo(SceneResult.Normal));
        }
    }
}
