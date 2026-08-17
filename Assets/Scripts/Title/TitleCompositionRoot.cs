using Game.Core;
using R3;
using UnityEngine;

namespace Game.Title
{
    /// <summary>
    /// Title シーンの組み立て役。Start 入力が押されたらシーンの終了を通知する。
    ///
    /// Play シーンと同じく、入力アセットはこのシーンが所有して破棄する。
    /// </summary>
    public sealed class TitleCompositionRoot : CompositionRoot
    {
        private readonly Subject<SceneResult> onFinishScene = new();

        private TitleInput input;

        public override Observable<SceneResult> OnFinishScene => onFinishScene;

        private void Awake() => enabled = false;

        public override void Initialize(GameState gameState, IGameStateSettings settings)
        {
            input = new TitleInput();
            input.Title.Enable();
            enabled = true;
        }

        private void Update()
        {
            // 「押した瞬間」だけが意味を持つ入力なので、状態としてではなく出来事として扱う。
            if (!input.Title.Start.WasPressedThisFrame()) return;

            onFinishScene.OnNext(SceneResult.Normal);
            enabled = false;
        }

        private void OnDestroy()
        {
            if (input != null)
            {
                input.Title.Disable();
                input.Dispose();
            }

            onFinishScene.Dispose();
        }
    }
}
