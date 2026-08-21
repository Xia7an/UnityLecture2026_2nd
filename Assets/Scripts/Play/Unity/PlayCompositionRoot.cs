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

        [Header("敵")]
        [SerializeField] private GameObject enemyPrefab;

        [Tooltip("生成した敵をまとめる親。未設定ならシーン直下に置く。")]
        [SerializeField] private Transform enemyParent;

        [Header("コイン")]
        [SerializeField] private GameObject normalCoinPrefab;
        [SerializeField] private GameObject specialCoinPrefab;

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
            // 例えば new RandomWalkMoveLogic(...) を渡せばプレイヤーが勝手に歩き回る。
            playerView.Initialize(
                new PlayerMoveLogic(playerInput, playSettings.WalkSpeed, playSettings.DashSpeed));

            var random = playSettings.CreateCoinRandom();

            SpawnEnemies(gameState, gameStateSettings, random);

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
            if (enemyPrefab == null) missing.Add(nameof(enemyPrefab));
            if (enemyPrefab != null && enemyPrefab.GetComponent<CharacterView>() == null)
                missing.Add($"{nameof(enemyPrefab)}.{nameof(CharacterView)}");
            if (enemyPrefab != null && enemyPrefab.GetComponent<EnemyContactView>() == null)
                missing.Add($"{nameof(enemyPrefab)}.{nameof(EnemyContactView)}");
            if (normalCoinPrefab == null) missing.Add(nameof(normalCoinPrefab));
            if (specialCoinPrefab == null) missing.Add(nameof(specialCoinPrefab));

            if (missing.Count == 0) return true;

            Debug.LogError(
                $"{nameof(PlayCompositionRoot)}: Inspector で未設定のフィールドがあります " +
                $"→ {string.Join(", ", missing)}",
                this);

            return false;
        }

        /// <summary>
        /// 敵をフィールド上に生成する。
        ///
        /// 敵 1 体ごとに RandomWalkMoveLogic を新しく作って渡す。
        /// 進行方向と向きを変えるまでの残り時間はロジックが自分で持つので、
        /// 同じインスタンスを使い回すと全員が同じ方向へ動いてしまう。
        ///
        /// 一方で乱数はコインと共有している。シードを固定すれば
        /// 敵の初期位置も動きも毎回同じになり、講習中に再現しやすくなる。
        /// </summary>
        private void SpawnEnemies(
            GameState gameState,
            IGameStateSettings gameStateSettings,
            System.Random random)
        {
            for (var i = 0; i < playSettings.EnemyCount; i++)
            {
                var enemy = Instantiate(
                    enemyPrefab, RandomFieldPoint(random), Quaternion.identity, enemyParent);

                enemy.GetComponent<CharacterView>().Initialize(new RandomWalkMoveLogic(
                    random,
                    playSettings.EnemySpeed,
                    playSettings.EnemyDirectionChangeInterval,
                    playSettings.FieldBounds));

                // どの Collider をプレイヤーとみなし、接触時に何を起こすかをここで結線する。
                // EnemyContactView は HP という状態を知らず、GameState だけが HP を変更する。
                enemy.GetComponent<EnemyContactView>().Initialize(
                    playerView.Collider,
                    () => gameState.ApplyEnemyHit(gameStateSettings));
            }
        }

        /// <summary>フィールド内のランダムな一点を返す。敵の初期位置に使う。</summary>
        private Vector3 RandomFieldPoint(System.Random random)
        {
            var bounds = playSettings.FieldBounds;
            var x = Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble());
            var z = Mathf.Lerp(bounds.min.z, bounds.max.z, (float)random.NextDouble());

            return new Vector3(x, bounds.center.y, z);
        }

        /// <summary>
        /// コインをフィールド上に配置する。
        ///
        /// 「どこに何を置くか」の判断は ICoinPlacementGenerator に切り出してあり、
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

            var placements = generator.Generate(
                gameStateSettings.CoinCount,
                gameStateSettings.SpecialCoinCount);

            var playerCollider = playerView.Collider;

            foreach (var placement in placements)
            {
                var isSpecial = placement.Kind == CoinKind.Special;
                var prefab = isSpecial ? specialCoinPrefab : normalCoinPrefab;

                // プレハブの回転をそのまま使う。コインは立てて置きたいため。
                var coin = Instantiate(prefab, placement.Position, prefab.transform.rotation, coinParent);

                // 取得したときに何が起きるかは、ここで決めて渡す。
                // CoinView 自身は自分が通常なのか特殊なのかを知らない。
                if (isSpecial)
                {
                    coin.GetComponent<CoinView>().Initialize(playerCollider, () => gameState.CollectSpecialCoin(gameStateSettings));
                }
                else
                {
                    coin.GetComponent<CoinView>().Initialize(playerCollider, gameState.CollectCoin);
                }
            }
        }

        /// <summary>
        /// 時間を進め、決着がついたかを確かめる。
        ///
        /// GameState は Pure C# で Update を持たないため、こうして毎フレーム呼んでやる。
        /// 次回講習で扱う DIContainer には、この登録を肩代わりする仕組みがある。
        /// </summary>
        private void Update()
        {
            if (gameState == null) return;

            gameState.Tick(Time.deltaTime);

            var outcome = GameOutcomeEvaluator.Evaluate(gameState);
            if (outcome == GameOutcome.InProgress) return;

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
