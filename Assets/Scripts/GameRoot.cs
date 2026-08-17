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

        [Tooltip("Title を通らずにシーンへ直接入ったときの状態。単独テスト用。")]
        [SerializeField] private DebugStartState debugStartState = new();

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

            StartGameAsync().Forget();
        }

        private void OnDestroy()
        {
            sceneSubscription.Dispose();
            gameState.Dispose();
        }

        /// <summary>
        /// 開始シーンを決めて立ち上げる。
        ///
        /// Root 以外のシーンがすでにロードされていれば、そのシーンから始める。
        /// エディタで Play シーンを開いたまま Root を Additive で開いて再生すると、
        /// Title を経由せずにそのシーンだけを確かめられる。
        /// ビルドでは Root しかロードされていないので、常に Title から始まる。
        /// つまり単独テストのために本番の経路を分岐させていない。
        /// </summary>
        private async UniTask StartGameAsync()
        {
            if (TryFindLoadedGameScene(out var scene, out var sceneName))
            {
                // Play はどう入っても Reset から始まるので、下の BeginScene に任せる。
                // Title と Result は Play を通っていないぶん、ここで状態を作ってやる。
                // Result の CompositionRoot は Initialize の中で状態を読むので、順序が重要。
                if (sceneName != SceneName.Play)
                {
                    gameState.Reset(settings);
                    debugStartState.ApplyTo(gameState);
                }

                BeginScene(scene, sceneName, resetState: sceneName == SceneName.Play);
                return;
            }

            await LoadSceneAsync(SceneName.Title);
        }

        /// <summary>シーンを Additive でロードし、その CompositionRoot に依存を注入する。</summary>
        private async UniTask LoadSceneAsync(SceneName sceneName)
        {
            var assetName = sceneName.ToAssetName();

            await SceneManager.LoadSceneAsync(assetName, LoadSceneMode.Additive);

            // 明示的な状態初期化。いつ初期化するのかをコード上ではっきりさせている。
            BeginScene(SceneManager.GetSceneByName(assetName), sceneName,
                resetState: sceneName == SceneName.Play);
        }

        /// <summary>
        /// ロード済みのシーンに依存を配線する。
        /// 通常の遷移でロードした直後と、単独再生でそのシーンから始めるときの両方から呼ぶ。
        /// </summary>
        private void BeginScene(Scene scene, SceneName sceneName, bool resetState)
        {
            // ライティングやスカイボックスは「アクティブなシーン」の設定が使われる。
            // これを呼ばないと Root シーンの設定のままになる。
            SceneManager.SetActiveScene(scene);

            var compositionRoot = FindCompositionRoot(scene);

            // Initialize より先に購読しておく。初期化の途中で終了条件が満たされても取りこぼさない。
            compositionRoot.OnFinishScene
                .Subscribe(result => OnSceneFinished(scene, sceneName, result))
                .AddTo(sceneSubscription);

            if (resetState) gameState.Reset(settings);

            compositionRoot.Initialize(gameState, settings);
        }

        /// <summary>
        /// Root 以外のロード済みシーンを探す。単独再生かどうかの判定に使う。
        /// SceneName に対応しない名前のシーン（テスト用に開いたものなど）は無視する。
        /// </summary>
        private static bool TryFindLoadedGameScene(out Scene scene, out SceneName sceneName)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var loaded = SceneManager.GetSceneAt(i);

                if (!Enum.TryParse(loaded.name, out sceneName)) continue;
                if (sceneName == SceneName.Root) continue;

                scene = loaded;
                return true;
            }

            scene = default;
            sceneName = default;
            return false;
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

        /// <summary>
        /// Title を通らずにシーンへ直接入ったときに使う、ゲーム状態の初期値。
        ///
        /// Result シーンを単独で確かめるためにある。決着状況は Evaluate が状態から導出するので、
        /// 「クリアした状態」を作るにはコイン枚数を揃えてやる必要がある。
        /// 終了理由をフィールドとして持たない設計の裏返しである。
        ///
        /// 既定値は全コイン取得（クリア）。失敗の表示を確かめたいときは、コインを取り切って
        /// いない状態にしたうえで hp を 0 にする。クリア判定はコイン枚数が優先されるため。
        /// Play シーンには効かない。Play は常に Reset された状態から始まるため。
        /// </summary>
        [Serializable]
        private sealed class DebugStartState
        {
            [Tooltip("Title / Result へ直接入ったとき、下の値で状態を上書きする。ビルドでは使われない。")]
            [SerializeField] private bool overrideState = true;

            [SerializeField] private int hp = 100;
            [SerializeField] private float remainingTimeSeconds = 60f;
            [SerializeField] private int collectedCoinCount = 30;
            [SerializeField] private int totalCoinCount = 30;

            public void ApplyTo(GameState gameState)
            {
                if (!overrideState) return;

                gameState.Hp.Value = hp;
                gameState.RemainingTimeSeconds.Value = remainingTimeSeconds;
                gameState.TotalCoinCount.Value = totalCoinCount;
                gameState.CollectedCoinCount.Value = collectedCoinCount;
            }
        }
    }
}
