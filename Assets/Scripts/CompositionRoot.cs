using Game.Core;
using R3;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 各シーンの組み立て役。シーンのルート GameObject 直下に 1 つだけ置く。
    ///
    /// GameRoot はこの抽象クラスの型でシーン内から取得するので、
    /// シーンが増えても GameRoot 側を変更する必要はない。
    ///
    /// 依存の向きは GameRoot → 各シーンの一方向で、逆向きの参照は存在しない。
    /// シーン側は「終わった」という事実と理由を発するだけで、次に何が起きるかを知らない。
    /// この一方向性が、次回講習で扱う DIContainer の親スコープと子スコープの関係にあたる。
    /// </summary>
    public abstract class CompositionRoot : MonoBehaviour
    {
        /// <summary>
        /// 依存を配線する。GameRoot がシーンのロード直後に呼ぶ。
        ///
        /// 注意: このメソッドはシーン内オブジェクトの Awake より後に呼ばれる。
        /// 注入された依存を Awake や Start で使ってはいけない。
        /// </summary>
        /// <param name="gameState">シーンをまたいで保持される状態。</param>
        /// <param name="settings">
        /// 変化しない設定値。状態と同じくアプリ全体で 1 つなので GameRoot から渡す。
        /// 各シーンが自前で ScriptableObject を参照すると、GameRoot が持つものと
        /// 食い違いうるため、供給元を 1 箇所に絞っている。
        /// </param>
        public abstract void Initialize(GameState gameState, IGameStateSettings settings);

        /// <summary>
        /// このシーンの役目が終わったことを通知する。
        ///
        /// 「シーンが終わった」は状態ではなく出来事なので、ReactiveProperty ではなく
        /// Subject を実体として使う。ReactiveProperty は購読した瞬間に現在値を流すため、
        /// GameRoot が購読した途端に初期値で遷移が始まってしまう。
        /// </summary>
        public abstract Observable<SceneResult> OnFinishScene { get; }
    }
}
