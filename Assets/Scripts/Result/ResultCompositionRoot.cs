using Game.Core;
using R3;
using UnityEngine;

namespace Game.Result
{
    /// <summary>
    /// Result シーンの組み立て役。Back 入力が押されたらシーンの終了を通知する。
    ///
    /// クリアだったか失敗だったかは、GameState から Evaluate で求める。
    /// 終了理由を状態として別に持たないので、Play シーンと判定がずれることがない。
    /// </summary>
    public sealed class ResultCompositionRoot : CompositionRoot
    {
        private readonly Subject<SceneResult> onFinishScene = new();

        private ResultInput input;

        public override Observable<SceneResult> OnFinishScene => onFinishScene;

        /// <summary>直前のゲームの決着状況。表示に使う。</summary>
        public GameOutcome Outcome { get; private set; } = GameOutcome.InProgress;

        private void Awake() => enabled = false;

        public override void Initialize(GameState gameState)
        {
            Outcome = GameOutcomeEvaluator.Evaluate(gameState);

            input = new ResultInput();
            input.Result.Enable();
            enabled = true;
        }

        private void Update()
        {
            if (!input.Result.Back.WasPressedThisFrame()) return;

            onFinishScene.OnNext(SceneResult.Normal);
            enabled = false;
        }

        private void OnDestroy()
        {
            if (input != null)
            {
                input.Result.Disable();
                input.Dispose();
            }

            onFinishScene.Dispose();
        }
    }
}
