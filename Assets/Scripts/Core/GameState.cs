using System;
using R3;

namespace Game.Core
{
    /// <summary>
    /// シーンをまたいで保持されるゲームの状態。
    ///
    /// 保持するのは「変化する値」だけである。配置するコインの枚数といった
    /// 変化しない値は設定であり、IGameStateSettings 側に置く。
    ///
    /// 各フィールドを ReactiveProperty にしているのは、コイン枚数の表示が
    /// 毎フレーム読みに来るのではなく購読できるようにするためである。
    /// 「シーンが終わった」のような出来事は Subject で表すが、コイン枚数は状態なので
    /// ReactiveProperty を使う。この使い分けが本講習会の主題そのものにあたる。
    ///
    /// アプリの生存期間を通じて GameRoot が 1 インスタンスだけ保持し、
    /// ゲーム開始時に Reset を明示的に呼んで初期化する。
    /// </summary>
    public sealed class GameState : IDisposable
    {
        private readonly ReactiveProperty<int> collectedCoinCount = new();
        private readonly ReactiveProperty<int> totalCoinCount = new();

        /// <summary>取得済みのコイン枚数。</summary>
        public ReactiveProperty<int> CollectedCoinCount => collectedCoinCount;

        /// <summary>フィールドに配置されたコインの総数。Reset で設定から写す。</summary>
        public ReactiveProperty<int> TotalCoinCount => totalCoinCount;

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

            collectedCoinCount.Value = 0;
            totalCoinCount.Value = settings.CoinCount;
        }

        /// <summary>コインを取得したときに呼ぶ。</summary>
        public void CollectCoin()
        {
            collectedCoinCount.Value = collectedCoinCount.CurrentValue + 1;
        }

        public void Dispose()
        {
            collectedCoinCount.Dispose();
            totalCoinCount.Dispose();
        }
    }
}
