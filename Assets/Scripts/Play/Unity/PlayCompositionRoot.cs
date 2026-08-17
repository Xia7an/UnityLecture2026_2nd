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
    /// 一方でゲーム状態はシーンをまたぐので、自分では作らず GameRoot から受け取る。
    /// </summary>
    public sealed class PlayCompositionRoot : CompositionRoot
    {
        [SerializeField] private PlaySettings settings;
        [SerializeField] private CharacterView playerView;
        [SerializeField] private CharacterView[] enemyViews;

        private readonly Subject<SceneResult> onFinishScene = new();

        private PlayerInputAdapter playerInput;
        private GameState gameState;

        public override Observable<SceneResult> OnFinishScene => onFinishScene;

        public override void Initialize(GameState gameState)
        {
            this.gameState = gameState;

            playerInput = new PlayerInputAdapter(new PlayInput());

            // ここで渡す実装を差し替えるだけで、プレイヤーの動きを変えられる。
            // 例えば new RandomWalkMoveLogic(...) を渡せばプレイヤーが勝手に歩き回る。
            playerView.Initialize(
                new PlayerMoveLogic(playerInput, settings.WalkSpeed, settings.DashSpeed));

            var random = new System.Random();
            foreach (var enemyView in enemyViews)
            {
                enemyView.Initialize(new RandomWalkMoveLogic(
                    random,
                    settings.EnemySpeed,
                    settings.EnemyDirectionChangeInterval,
                    settings.FieldBounds));
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
