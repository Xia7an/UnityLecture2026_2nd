using System;
using R3;

namespace Game.Core
{
    /// <summary>
    /// シーンをまたいで保持されるゲームの状態。
    ///
    /// 保持するのは「変化する値」だけである。1 ヒットの被ダメージ量や無敵時間の長さといった
    /// 変化しない値は設定であり、IGameStateSettings 側に置く。
    ///
    /// 各フィールドを ReactiveProperty にしているのは、HP バーやスコア表示が
    /// 毎フレーム読みに来るのではなく購読できるようにするためである。
    /// 「シーンが終わった」のような出来事は Subject で表すが、HP や残り時間は状態なので
    /// ReactiveProperty を使う。この使い分けが本講習会の主題そのものにあたる。
    ///
    /// アプリの生存期間を通じて GameRoot が 1 インスタンスだけ保持し、
    /// ゲーム開始時に Reset を明示的に呼んで初期化する。
    /// </summary>
    public sealed class GameState : IDisposable
    {
        private readonly ReactiveProperty<int> hp = new();
        private readonly ReactiveProperty<float> remainingTimeSeconds = new();
        private readonly ReactiveProperty<int> collectedCoinCount = new();
        private readonly ReactiveProperty<int> totalCoinCount = new();
        private readonly ReactiveProperty<float> invincibleRemainingSeconds = new();

        /// <summary>プレイヤーの残り HP。</summary>
        public ReactiveProperty<int> Hp => hp;

        /// <summary>残り時間（秒）。0 になると時間切れ。</summary>
        public ReactiveProperty<float> RemainingTimeSeconds => remainingTimeSeconds;

        /// <summary>取得済みのコイン枚数。</summary>
        public ReactiveProperty<int> CollectedCoinCount => collectedCoinCount;

        /// <summary>フィールドに配置されたコインの総数。Reset で設定から写す。</summary>
        public ReactiveProperty<int> TotalCoinCount => totalCoinCount;

        /// <summary>無敵状態の残り時間（秒）。0 より大きい間は敵と衝突しても HP が減らない。</summary>
        public ReactiveProperty<float> InvincibleRemainingSeconds => invincibleRemainingSeconds;

        /// <summary>現在無敵状態かどうか。残り時間から導出するため、状態としては持たない。</summary>
        public bool IsInvincible => invincibleRemainingSeconds.CurrentValue > 0f;

        /// <summary>
        /// 設定値をもとに状態を初期化する。
        ///
        /// ゲームを 2 周目に入るときへ備えて新しいインスタンスを作るのではなく、
        /// 明示的にこのメソッドを呼ばせている。いつ状態を初期化するのかを
        /// コード上ではっきりさせること自体が、状態管理における重要な論点であるため。
        /// </summary>
        public void Reset(IGameStateSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            hp.Value = settings.MaxHp;
            remainingTimeSeconds.Value = settings.TimeLimitSeconds;
            collectedCoinCount.Value = 0;
            totalCoinCount.Value = settings.CoinCount;
            invincibleRemainingSeconds.Value = 0f;
        }

        /// <summary>
        /// 時間を進める。GameState は Pure C# であり Update を持たないので、
        /// Play シーンの CompositionRoot が毎フレーム呼ぶ。
        ///
        /// この「毎フレーム呼ぶ相手を手で登録する」形は、
        /// 次回講習で扱う DIContainer の ITickable が自動化してくれる作業にあたる。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (remainingTimeSeconds.CurrentValue > 0f)
            {
                remainingTimeSeconds.Value = Math.Max(0f, remainingTimeSeconds.CurrentValue - deltaTime);
            }

            if (invincibleRemainingSeconds.CurrentValue > 0f)
            {
                invincibleRemainingSeconds.Value =
                    Math.Max(0f, invincibleRemainingSeconds.CurrentValue - deltaTime);
            }
        }

        /// <summary>敵と衝突したときに呼ぶ。無敵状態なら何も起きない。</summary>
        public void ApplyEnemyHit(IGameStateSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (IsInvincible) return;

            hp.Value = Math.Max(0, hp.CurrentValue - settings.DamageOnEnemyHit);
        }

        /// <summary>通常コインを取得したときに呼ぶ。</summary>
        public void CollectCoin()
        {
            collectedCoinCount.Value = collectedCoinCount.CurrentValue + 1;
        }

        /// <summary>特殊コインを取得したときに呼ぶ。無敵時間を設定値まで戻す。</summary>
        public void CollectSpecialCoin(IGameStateSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            collectedCoinCount.Value = collectedCoinCount.CurrentValue + 1;
            invincibleRemainingSeconds.Value = settings.InvincibleDuration;
        }

        public void Dispose()
        {
            hp.Dispose();
            remainingTimeSeconds.Dispose();
            collectedCoinCount.Dispose();
            totalCoinCount.Dispose();
            invincibleRemainingSeconds.Dispose();
        }
    }
}
