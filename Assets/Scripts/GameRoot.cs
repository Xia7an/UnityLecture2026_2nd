using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Core;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    /// <summary>
    /// Root シーンに常駐する、アプリ全体の組み立て役。
    ///
    /// ゲーム状態を保持し、Title / Play / Result を Additive でロード・アンロードする。
    /// シーンをまたぐ状態を static なしで持てるのは、このオブジェクトが常駐しているためである。
    ///
    /// 各シーンをロードした直後に、そのシーン内の CompositionRoot を取得して
    /// 明示的に Initialize を呼ぶ。シーン側から GameRoot を探しに来ることはない。
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private GameStateSettings settings;

        private readonly GameState gameState = new();

        /// <summary>今ロードしているシーンの購読。遷移のたびに解除して張り直す。</summary>
        private readonly CompositeDisposable sceneSubscription = new();

        private void Start()
        {
            if (settings == null)
            {
                Debug.LogError($"{nameof(GameRoot)}: {nameof(GameStateSettings)} が設定されていません。", this);
                return;
            }

            LoadSceneAsync(SceneName.Title).Forget();
        }

        private void OnDestroy()
        {
            sceneSubscription.Dispose();
            gameState.Dispose();
        }

        /// <summary>シーンを Additive でロードし、その CompositionRoot に依存を注入する。</summary>
        private async UniTask LoadSceneAsync(SceneName sceneName)
        {
            var assetName = sceneName.ToAssetName();

            await SceneManager.LoadSceneAsync(assetName, LoadSceneMode.Additive);

            var scene = SceneManager.GetSceneByName(assetName);

            // ライティングやスカイボックスは「アクティブなシーン」の設定が使われる。
            // これを呼ばないと Root シーンの設定のままになる。
            SceneManager.SetActiveScene(scene);

            var compositionRoot = FindCompositionRoot(scene);

            // Initialize より先に購読しておく。初期化の途中で終了条件が満たされても取りこぼさない。
            compositionRoot.OnFinishScene
                .Subscribe(result => OnSceneFinished(scene, sceneName, result))
                .AddTo(sceneSubscription);

            // 明示的な状態初期化。いつ初期化するのかをコード上ではっきりさせている。
            if (sceneName == SceneName.Play) gameState.Reset(settings);

            compositionRoot.Initialize(gameState);
        }

        private void OnSceneFinished(Scene current, SceneName from, SceneResult result)
        {
            TransitionAsync(current, from, result).Forget();
        }

        private async UniTask TransitionAsync(Scene current, SceneName from, SceneResult result)
        {
            var next = DecideNextScene(from, result);

            // アンロードより先に解除する。解除が後だと、アンロード中に届いた通知で
            // 遷移が二重に走る。購読を残したままにすると累積して多重遷移になる。
            sceneSubscription.Clear();

            await SceneManager.UnloadSceneAsync(current);
            await LoadSceneAsync(next);
        }

        /// <summary>
        /// 次に進むシーンを決める。UnityEngine に触れない純粋関数なので、
        /// 遷移のルールだけを取り出して読める。
        /// </summary>
        private static SceneName DecideNextScene(SceneName from, SceneResult result) => (from, result) switch
        {
            (SceneName.Title, SceneResult.Normal) => SceneName.Play,
            (SceneName.Play, SceneResult.GameClear) => SceneName.Result,
            (SceneName.Play, SceneResult.GameFailure) => SceneName.Result,
            (SceneName.Result, SceneResult.Normal) => SceneName.Title,
            _ => throw new ArgumentOutOfRangeException(
                nameof(result),
                $"{from} シーンからの遷移として {result} は想定されていません。"),
        };

        /// <summary>
        /// シーン内の CompositionRoot を探す。探索範囲をこのシーンに限定しているので、
        /// 遷移中に 2 つのシーンが一瞬同時に存在しても取り違えない。
        /// </summary>
        private static CompositionRoot FindCompositionRoot(Scene scene)
        {
            var compositionRoot = scene
                .GetRootGameObjects()
                .Select(gameObject => gameObject.GetComponent<CompositionRoot>())
                .FirstOrDefault(found => found != null);

            if (compositionRoot == null)
            {
                throw new InvalidOperationException(
                    $"シーン '{scene.name}' のルート GameObject に {nameof(CompositionRoot)} が見つかりません。" +
                    $"ルート直下に配置してください（子オブジェクトは探索しません）。");
            }

            return compositionRoot;
        }
    }
}
