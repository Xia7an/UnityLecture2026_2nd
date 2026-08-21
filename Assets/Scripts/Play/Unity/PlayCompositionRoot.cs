using System.Collections.Generic;
using Game.Core;
using Game.Play.Core;
using R3;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// Play シーンの組み立て役。
    ///
    /// 入力アセットはこのシーンでしか意味を持たないので、ここで生成して OnDestroy で破棄する。
    /// 生存期間がシーンの生存期間と一致するため、SwitchCurrentActionMap のような
    /// 「今どのマップが有効か」を管理する必要がない。
    ///
    /// 一方でゲーム状態と設定はシーンをまたぐので、自分では作らず GameRoot から受け取る。
    /// </summary>
    public sealed class PlayCompositionRoot : CompositionRoot
    {
        [SerializeField] private PlaySettings playSettings;
        [SerializeField] private CharacterView playerView;
        [SerializeField] private UIView[] uiViews;

        [Header("コイン")]
        [SerializeField] private GameObject normalCoinPrefab;

        [Tooltip("生成したコインをまとめる親。未設定ならシーン直下に置く。")]
        [SerializeField] private Transform coinParent;

        private readonly Subject<SceneResult> onFinishScene = new();

        private PlayerInputAdapter playerInput;
        private GameState gameState;

        public override Observable<SceneResult> OnFinishScene => onFinishScene;

        public override void Initialize(GameState gameState, IGameStateSettings gameStateSettings)
        {
            // Inspector での割り当て漏れは、そのままだと NullReferenceException になって
            // どのフィールドが原因か分からない。先に確かめて名前を挙げる。
            if (!HasRequiredReferences()) return;

            this.gameState = gameState;

            playerInput = new PlayerInputAdapter(new PlayInput());

            // ここで渡す実装を差し替えるだけで、プレイヤーの動きを変えられる。
            playerView.Initialize(
                new PlayerMoveLogic(playerInput, playSettings.WalkSpeed, playSettings.DashSpeed));

            var random = playSettings.CreateCoinRandom();

            foreach (var uiView in uiViews)
            {
                uiView.Initialize(gameState, gameStateSettings);
            }

            SpawnCoins(gameState, gameStateSettings, random);
        }

        /// <summary>
        /// Inspector で割り当てるべき参照が揃っているかを確かめる。
        /// 足りないものがあれば、フィールド名を挙げてまとめて報告する。
        /// ログをクリックすればこの GameObject が選択される。
        /// </summary>
        private bool HasRequiredReferences()
        {
            var missing = new List<string>();

            if (playSettings == null) missing.Add(nameof(playSettings));
            if (playerView == null) missing.Add(nameof(playerView));
            if (normalCoinPrefab == null) missing.Add(nameof(normalCoinPrefab));

            if (missing.Count == 0) return true;

            Debug.LogError(
                $"{nameof(PlayCompositionRoot)}: Inspector で未設定のフィールドがあります " +
                $"→ {string.Join(", ", missing)}",
                this);

            return false;
        }

        /// <summary>
        /// コインをフィールド上に配置する。
        ///
        /// 「どこに置くか」の判断は ICoinPlacementGenerator に切り出してあり、
        /// ここは決まった位置にプレハブを実体化して結線するだけを担当する。
        /// 移動ロジックと見た目を分けたのと同じ切り分けである。
        /// </summary>
        private void SpawnCoins(
            GameState gameState,
            IGameStateSettings gameStateSettings,
            System.Random random)
        {
            var generator = new RandomCoinPlacementGenerator(
                random,
                playSettings.FieldBounds,
                playSettings.CoinHeight,
                playSettings.CoinMinDistance);

            var positions = generator.Generate(gameStateSettings.CoinCount);

            var playerCollider = playerView.Collider;

            foreach (var position in positions)
            {
                // プレハブの回転をそのまま使う。コインは立てて置きたいため。
                var coin = Instantiate(
                    normalCoinPrefab, position, normalCoinPrefab.transform.rotation, coinParent);

                // 取得したときに何が起きるかは、ここで決めて渡す。
                coin.GetComponent<CoinView>().Initialize(playerCollider, gameState.CollectCoin);
            }
        }

        /// <summary>
        /// 決着がついたかを毎フレーム確かめる。
        ///
        /// 判定そのものは Core 層の純粋関数に任せ、ここは結果に応じて
        /// シーンの終了を通知するだけにする。
        /// </summary>
        private void Update()
        {
            if (gameState == null) return;

            var outcome = GameOutcomeEvaluator.Evaluate(gameState);
            if (outcome == GameOutcome.InProgress) return;

            // リザルトシーンに移動
            onFinishScene.OnNext(outcome.ToSceneResult());

            // 二重に通知しないよう、決着したら以降は動かさない。
            enabled = false;
        }

        private void OnDestroy()
        {
            playerInput?.Dispose();
            onFinishScene.Dispose();
        }
    }
}
