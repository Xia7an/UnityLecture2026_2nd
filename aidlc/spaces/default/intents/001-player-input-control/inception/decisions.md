# アーキテクチャ決定

対象 Intent: `INT-001`（`Main.inputactions` による `Character.prefab` の操作）

以下の ADR はいずれも提案であり、承認ゲートは未通過である。

## ADR-001: 今回は DIContainer を導入せず、手動 DI（Composition Root）で構成する

### Context

講習会の趣旨上、ゲームのコアロジックの状態管理は Pure C# に置きたい。
一方、受講者は DIContainer の存在自体を未習であり、`[Inject]` を提示すると混乱を招きうる。

当初、選択肢が「DIContainer を『おまじない』として導入する」か
「DIContainer 無しで設計する（＝`static` シングルトンを作る）」の二択として提示された。
利用者の考えは「未来永劫使わないコードを教えるくらいなら、分からないなりにも
これから触れていくコードに触れてもらうほうがマシ」というものであった。

この二択は網羅的ではない。**第三の選択肢として手動 DI（Composition Root パターン）がある。**
すなわち、ドメイン層のクラスはすべてコンストラクタで依存を受け取り、
`new` を書いてよい場所をシーンに 1 つ置く「組み立て役」に限定する方式である。

### Decision

今回の講習では DIContainer を導入しない。かつ `static` シングルトンも用いない。
シーンごとに Composition Root（`PlaySceneRoot`）を 1 つ置き、そこで依存を生成・結線する。
ドメイン層のクラスは最初からコンストラクタインジェクションで記述する。

### Consequences

- 利用者の「未来永劫使わないコードを教えたくない」という原則は満たされる。
  手動 DI は DIContainer が自動化する作業を明示的に書いたものであり、捨てるコードではない。
  移行時に変わるのは `PlaySceneRoot` → `LifetimeScope` の部分だけで、
  ドメイン層は 1 行も変わらない（NFR2）。
- 次回講習の動機付けが強くなる。DIContainer は「手で書いていた配線を自動化する道具」であるため、
  先に手で配線して面倒さを体感してもらうのが最短の説明経路になる。
  逆に `[Inject]` から入ると「何を解決する道具なのか」が分からないまま使うことになりやすい。
- 規模上の問題はない。1 シーン・10 個程度のオブジェクトであれば手動配線の記述量は許容範囲であり、
  「20〜30 個になったら？」という問いが次回への橋渡しになる。
- 弱点はシーンを跨ぐ状態の保持である。ADR-006（Root シーン + Additive ロード）で対処する。
- 受講者が講習外で他プロジェクトを見たときに `[Inject]` を知らない状態が続くが、
  次回講習で解消される。

### Alternatives Rejected

- **VContainer を「おまじない」として今回から導入する**: 却下。
  MonoBehaviour への注入には結局 `[Inject]` または `RegisterComponentInHierarchy` の説明が必要になり、
  NFR1 に反する。また、手動 DI が捨てるコードではない以上、前倒しの利得が小さい。
  ただし ADR-006 の結論次第では再検討の余地がある。
- **`static` なシングルトン（`GameManager.Instance` 等）**: 却下。
  グローバル可変状態は本講習会が問題として提示したい「状態管理の難しさ」そのものであり、
  教材として悪い型を教えることになる。
- **すべてを MonoBehaviour で管理する**: 却下。利用者が明示的に趣旨に反すると判断済み。

### トレーサビリティ

FR6、NFR1、NFR2

---

## ADR-002: Core（Pure C#）層と Unity 層をディレクトリと名前空間で分離し、asmdef は用いない

改訂: 当初、asmdef 2 枚（`Game.Play.Core` / `Game.Play`）で層をコンパイラに強制する案を記録していた。
利用者の判断（2026-08-17）により、便益と受講者の混乱のトレードオフでは混乱の影響がわずかに上回るとして
asmdef は不採用となった。以下は改訂後の内容である。

### Context

「状態管理を Pure C# で行う」方針を構造として担保したい。
Unity では、参照可能である限り受講者は容易に MonoBehaviour へ依存を戻してしまう。

一方、asmdef は受講者にとって新しい概念であり、
「なぜ参照できないのか」「参照を足すにはどこを編集するのか」の説明が必要になる。
本講習で新規に説明したい概念は interface とコンストラクタ引数の 2 つに絞りたい（NFR1）。
asmdef はここに 3 つ目を持ち込む。

### Decision

asmdef を作成しない。プロジェクト固有スクリプトは predefined assembly
（`Assembly-CSharp`）に置いたままとする。

層の分離はディレクトリと名前空間で表現する。

- `Assets/Scripts/Play/Core/`（`Game.Play.Core`）: Pure C#。
  `IPlayerInput`、`ICharacterMoveLogic`、`ICharacterView`、`PlayerMoveLogic`、`RandomWalkMoveLogic` 等。
- `Assets/Scripts/Play/Unity/`（`Game.Play`）: `MainInputPlayerInput`、`CharacterView`、`PlaySceneRoot`。

層の逸脱はコンパイラではなく、規約とレビューで検出する。
`Core/` 配下に `UnityEngine.InputSystem` の `using` と `MonoBehaviour` が出現しないことを
`grep` で機械的に確認できるようにする（NFR3）。

### Consequences

- 受講者に説明すべき新概念が減る。「なぜ参照できないのか」で講習が止まるリスクを避けられる。
- 層の逸脱がコンパイルエラーにならない。受講者が Core から `CharacterView` を参照しても
  ビルドは通ってしまうため、講師によるレビューと `grep` チェックが必要になる。
  これは今回受け入れるリスクである。
- EditMode テスト（FR5）は引き続き記述できる。Unity Test Framework は predefined assembly
  でのテストに対応しており、テストコードは `Assets/Editor/`（`Assembly-CSharp-Editor`）配下に置く。
  根拠: `Library/PackageCache/com.unity.test-framework@bd7f943e9647/UnityEngine.TestRunner/AssemblyInfo.cs`
  に `InternalsVisibleTo("Assembly-CSharp-testable")` および
  `InternalsVisibleTo("Assembly-CSharp-Editor-testable")` が存在する。
  ただし Editor 上での実行は未検証であり、着手時に確認が必要である（Q10）。
- Core 層は `UnityEngine.Vector3` / `Vector2` に依存する。これは
  `UnityEngine.CoreModule` の struct であり、Editor もシーンも不要なため
  EditMode テストの妨げにならない。完全な UnityEngine 非依存は目的ではないと整理する。
- コンパイル時間の分離という asmdef 本来の利得も得られないが、本プロジェクトの規模では影響しない。
- 将来 DIContainer を導入する際、asmdef の追加は独立した変更として後から行える。
  今回 asmdef を入れないことは、次回講習の妨げにならない。

### Alternatives Rejected

- **asmdef 2 枚で層をコンパイラに強制する**: 却下（当初案）。
  「参照できないから書けない」という強制力は魅力的だが、受講者の混乱コストが上回ると判断した。
- **asmdef をより細かく分ける（Input / Domain / View / Composition）**: 却下。同上、より強い理由で。
- **層を分けず 1 ディレクトリに置く**: 却下。
  Pure C# と MonoBehaviour の境界は本講習の主題であり、ディレクトリ上は明示する必要がある。

### トレーサビリティ

FR5、NFR1、NFR3、Q10

---

## ADR-003: プレイヤーと敵の移動を単一インターフェース `ICharacterMoveLogic` に統一する

### Context

ゲーム仕様では、敵の移動ロジックを講習後半で差し替えることが求められており、
移動ロジックと見た目制御 MonoBehaviour の分離が要求されている（FR4）。
プレイヤーの入力移動を別構造で作ると、この差し替えの型が 1 種類しか示せない。

### Decision

プレイヤーにも敵と同じ構造を適用する。

```csharp
public interface ICharacterMoveLogic
{
    // 現在位置と経過時間から、このフレームの速度を返す
    Vector3 EvaluateVelocity(Vector3 currentPosition, float deltaTime);
}
```

`PlayerMoveLogic`（`IPlayerInput` を受け取る）と `RandomWalkMoveLogic` を実装として用意し、
見た目制御 MonoBehaviour は `CharacterView` 1 本でプレイヤー・敵の双方に用いる。

### Consequences

- 「入力で動く」と「ランダムウォーク」が同じ差し替えポイントに乗る。
  後半の移動ロジック差し替えは、実装クラスを 1 つ追加して
  `PlaySceneRoot` の 1 行を変えるだけの作業になる。
- `CharacterView` はプレイヤーか敵かを知らない。責務が「速度を受け取って Transform と
  Animator に反映する」だけに閉じる。
- 戻り値を「速度」としたことで、位置の更新責任は View 側（`CharacterController.Move`）に残る。
  ロジック側は位置を直接書き換えられない。
- 将来、位置以外の状態（無敵時間中の点滅など）を反映するときは、
  `ICharacterMoveLogic` を膨らませず別のインターフェースを足す方針とする。

### Alternatives Rejected

- **戻り値を「次フレームの絶対位置」にする**: 却下。
  ロジック側が Collider や壁との衝突を知らないまま位置を確定させることになり、
  物理と整合しない。速度を返し、適用は View に委ねるほうが責務が素直である。
- **プレイヤーだけ専用の `PlayerController` を作る**: 却下。
  差し替えの型が 2 種類になり、教材としての一貫性が失われる。

### トレーサビリティ

FR1、FR2、FR4

---

## ADR-004: 入力は「状態はポーリング、瞬間はイベント」で使い分ける

### Context

`Main.inputactions` には、押しっぱなしで意味を持つ入力（`Play/Move`、`Play/Dash`）と、
押した瞬間だけ意味を持つ入力（`Title/Start`、`Result/Back`）が混在する。
また Input System 依存を Core から切り離す必要がある（NFR3）。

### Decision

Core 層に以下の抽象を置く。

```csharp
public interface IPlayerInput
{
    Vector2 MoveDirection { get; }  // 正規化済み
    bool IsDashing { get; }
}
```

`Move` は Value アクションのためポーリング（`ReadValue<Vector2>()`）、
`Dash` は押下継続の判定のためポーリング（`IsPressed()`）とする。
`Title/Start`、`Result/Back` は瞬間の入力であるため、イベントとして扱う。

実装は Unity 層に置き、MonoBehaviour ではない Pure C# クラス（`IDisposable`）とする。

改訂（2026-08-17）:

- 実装クラス名は `MainInputPlayerInput` から `PlayerInputAdapter` に変更する。
  InputActions をシーンごとに分割し（ADR-010）、包む対象が `Main` から `PlayInput` になるため。
- 「瞬間の入力」の表現は、R3 の `Observable<T>` を用いる（ADR-009）。
  本 ADR の「状態はポーリング、瞬間はイベント」という判断軸は変更しない。
  同じ軸がシーン遷移の通知にも適用され、`OnFinishScene` を
  `ReactiveProperty` ではなく `Subject` 由来の `Observable` とする根拠になっている。

### Consequences

- Input System が MonoBehaviour 無しで使えることを示せる。
  「Unity の機能＝MonoBehaviour」という思い込みを崩す教材になる。
- ポーリング主体のため、イベント購読・解除の寿命管理に受講者がつまずく箇所を減らせる。
  一方で `event` を使う箇所（Title / Result）では解除の必要性を明示的に教える機会になる。
- `IPlayerInput` のスタブ実装により `PlayerMoveLogic` の EditMode テストが可能になる（FR5）。
  これは「なぜ Pure C# に切り出すのか」に対する最も説得力のある回答であり、
  講習で 1 本テストを書いて見せる価値がある。
- アクションマップの有効・無効切り替え（Title / Play / Result）は状態管理の題材として使える。
  どこで切り替えるかは Composition Root またはシーン遷移側の責務とする。

### Alternatives Rejected

- **すべてを `event Action<Vector2>` で通知する**: 却下。
  移動のような連続値は毎フレーム参照するほうが素直で、購読解除漏れのリスクも避けられる。
- **`PlayerInput` コンポーネント（Send Messages / Unity Events）を使う**: 却下。
  文字列や Inspector 経由の結合になり、Core 層への依存の受け渡しが不透明になる。

### トレーサビリティ

FR1、FR2、FR5、NFR3

---

## ADR-005: 生成 C# ラッパークラスを使用し、移動は `CharacterController` で適用する

### Context

`Main.inputactions.meta` は当初 `generateWrapperCode: 0` であり、
アクションへのアクセスは文字列キー経由になる状態だった。
また `Character.prefab` には追加コンポーネントが無く、移動適用手段が決まっていない。

### Decision

- `.inputactions` の "Generate C# Class" を有効にし、生成されたクラスを
  `new PlayInput()` して `input.Play.Move` の形で参照する（本 intent 記録中に
  `generateWrapperCode: 1` へ変更済み）。
  当初は単一の `Main` クラスを想定していたが、ADR-010 により
  シーンごとの `TitleInput` / `PlayInput` / `ResultInput` に分割する。
- `Character.prefab`（Variant）に `CharacterController` を追加し、
  `CharacterController.Move(velocity * deltaTime)` で移動を適用する。

### Consequences

- 文字列キーによるアクセスを排除でき、型安全になる。Pure C# 側からの利用にも都合が良い。
- 生成コードは `.gitignore` の対象外にあるため、コミット対象になるかを確認する必要がある。
- `CharacterController` は接地・押し出しを自前で扱わずに済み、Rigidbody の物理設定
  （Freeze Rotation、Drag 等）の説明を省ける。初学者向けの説明コストが低い。
- 一方、敵との衝突を物理的な反発として表現したくなった場合は Rigidbody への変更が必要になる
  （未解決事項 Q4）。今回は接触検知が目的（HP 減少・コイン取得）であり、
  `OnTriggerEnter` で足りる想定である。
- Animator のパラメータ名は未確認であり（Q5）、`CharacterView` の実装前に確認が必要である。

### Alternatives Rejected

- **`InputActionReference` を `SerializeField` で持つ**: 却下。
  Inspector 経由の結合が増え、Composition Root で依存を一望する方針と噛み合わない。
- **`Rigidbody` + `Collider` で移動**: 保留。Q4 として残す。
- **`transform.position` を直接書き換える**: 却下。Collider をすり抜け、
  壁・敵との接触判定が成立しない。

### トレーサビリティ

FR1、FR3、Q2、Q3、Q4、Q5

---

## ADR-006: シーン跨ぎ状態は Root シーン + Additive ロードで保持する

改訂: 当初は提案・未承認として記録していた。利用者の判断（2026-08-17）により正式採用となった。

### Context

Title → Play → Result の 3 シーン構成では、スコアや終了理由を Result へ渡す必要がある。
ここが手動 DI（ADR-001）の唯一の弱点であり、多くの実装が `static` シングルトンに流れる箇所である。

### Decision

Root シーンを 1 枚作り、Title / Play / Result を Additive でロードする構成を採用する。

- Root シーンは常駐する。起動シーンであり、Build Settings の先頭に置く。
- Root シーンに `GameRoot`（アプリケーション全体の Composition Root）を置く。
  アプリの生存期間を通じて保持する依存（シーンを跨ぐゲーム状態、`IPlayerInput`、シーン遷移の実行役）は
  ここで生成する。
- Title / Play / Result は Additive でロードし、遷移時に前のシーンをアンロードする。
- 各シーンの Composition Root（例: `PlaySceneRoot`）は、そのシーン内で完結する依存のみを生成し、
  シーンを跨ぐ依存は `GameRoot` から受け取る。

### Consequences

- `static` シングルトンを避けるという ADR-001 の方針を、シーン遷移を含めて貫ける。
- Root シーンが親スコープとして振る舞う構成は、VContainer の親 `LifetimeScope` が
  行っていることと同じであるため、次回講習への接続が良い。
  ADR-001 で唯一の弱点としていた箇所が、次回講習への 2 本目の橋になる。
- Additive ロードとシーンのアンロード手順を受講者に説明する必要があり、
  単純な `SceneManager.LoadScene` より手数が増える。
  ただしこの手数自体が「グローバル状態を使わずに状態を持ち回る」ことの実演になる。
- `GameRoot` から各シーンの Composition Root へ依存を渡す経路が必要になる。
  シーンは Inspector 参照で結べないため、ロード完了後に `GameRoot` 側から
  シーン内の Composition Root を見つけて `Initialize()` を呼ぶ形になる。
  その具体的な手段は未決である（Q11）。
- `ProjectSettings/EditorBuildSettings.asset` の有効シーン設定の全面的な見直しが必要になる。
  Root を先頭に、Title / Play / Result を続けて登録する（Q8）。
- Camera / AudioListener は Root シーンに集約する（Q12、決定済み）。
  各シーンは単独で完結しないため、シーンごとに置く利点がなく、双方に存在すると重複警告が出る。
  EventSystem は uGUI の対話操作を使わないため配置しない（ADR-010 追記、2026-08-17）。
- 各シーンを単独で開いて Play したときに `GameRoot` が存在せず動作しない。
  開発時の利便のため、Root シーンから起動する運用を講習資料に明記する必要がある。

### Alternatives Rejected

- **`static` フィールドで結果を受け渡す**: 却下。ADR-001 と矛盾する。
- **`DontDestroyOnLoad` の MonoBehaviour に結果を持たせる**: 却下。
  Root シーン案より手数は少ないが、実体としてはグローバル可変状態に近く、
  参照の取得方法が `FindObjectOfType` 等に流れやすい。
- **今回から VContainer を導入し、親 `LifetimeScope` で解決する**: 却下。ADR-001 の NFR1 と衝突する。
- **シーンを分けず単一シーンで画面を切り替える**: 却下。
  既存の `Title.unity` / `Play.unity` / `Result.unity` の構成を壊す。

### トレーサビリティ

FR6、NFR2、Q7、Q8、Q11、Q12

## ADR-007: シーンをまたぐ依存は、ロード直後に `GameRoot` から `CompositionRoot` へ明示的に注入する

### Context

ADR-006 により Root シーン + Additive ロードを採用した。
その結果、Root シーンの `GameRoot` が保持する状態を、Additive ロードした各シーンへ
渡す経路が必要になった（旧 Q11）。シーンをまたぐため Inspector 参照は使えない。

### Decision

シーン管理の責務を `GameRoot` に置く。`GameRoot` はシーンをロードした直後に、
そのシーン内の `CompositionRoot` を取得して明示的に `Initialize` を呼ぶ。

```csharp
public sealed class GameRoot : MonoBehaviour
{
    private readonly GameState gameState = new();

    public async UniTask LoadSceneAsync(string sceneName)
    {
        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        var scene = SceneManager.GetSceneByName(sceneName);

        var compositionRoot = scene
            .GetRootGameObjects()
            .Select(x => x.GetComponent<CompositionRoot>())
            .First(x => x != null);

        compositionRoot.Initialize(gameState);
    }
}
```

付随して以下を決定する。

1. **`CompositionRoot` は abstract class とする。** シーンごとに
   `TitleCompositionRoot`、`PlayCompositionRoot`、`ResultCompositionRoot` を実装する。
   `GameRoot` は基底型で取得するため、シーンが増えても `GameRoot` は変更不要である。

   ```csharp
   public abstract class CompositionRoot : MonoBehaviour
   {
       public abstract void Initialize(GameState gameState);
   }

   public sealed class PlayCompositionRoot : CompositionRoot
   {
       [SerializeField] private Player player;
       [SerializeField] private ScoreView scoreView;

       public override void Initialize(GameState gameState)
       {
           player.Initialize(gameState);
           scoreView.Initialize(gameState);
       }
   }
   ```

2. **`CompositionRoot` はシーンのルート GameObject 直下に置く。**
   `GetRootGameObjects()` に対する `GetComponent` は子階層を探索しないため、規約として明記する。

3. **`GameState` は分割せず、そのまま渡す。** 参照・変更範囲に応じたインターフェースの切り出しは
   行わない。本講習で扱う範囲はゲームにおける状態管理であり、
   インターフェース分離は次回の VContainer 講習へ持ち越す（利用者判断、2026-08-17）。

4. **`GameState` はアプリ生存期間を通じて 1 インスタンスとし、`Reset()` で初期化する。**
   Play シーンのロード時に新規生成するのではなく、明示的に初期化を呼ばせる。
   明示的な状態初期化そのものが状態管理における重要な論点であるため（利用者判断、2026-08-17）。

5. **`GameState` の既定値は ScriptableObject（`GameStateSettings`）で管理する**
   （利用者判断、2026-08-17。旧 Q16）。
   HP 100、制限時間 2 分、コイン 30 枚、無敵 10 秒といった既定値を Inspector で調整できる。
   `GameRoot` が `SerializeField` で保持し、Play シーンのロード直前に
   `gameState.Reset(settings)` を呼ぶ。既定値の所在が Root シーン 1 箇所に集まる。

6. **シーンの終了は `CompositionRoot` が公開する `Observable<SceneResult>` で通知する。**
   詳細は ADR-009 を参照。`CompositionRoot` の定義は以下となる。

   ```csharp
   public abstract class CompositionRoot : MonoBehaviour
   {
       public abstract void Initialize(GameState gameState);
       public abstract Observable<SceneResult> OnFinishScene { get; }
   }
   ```

### Consequences

- 依存の向きが `GameRoot` → 各シーンの一方向に固定される。
  シーンから `GameRoot` への逆参照が存在しないため、`static` もサービスロケータも不要になる。
  この一方向性が、次回講習の親スコープ → 子スコープの関係にそのまま対応する。
  （当初「DIP を遵守」と記述していたが、`Initialize(GameState)` も
  `GetComponent<CompositionRoot>()` も具象型への依存であり DIP の定義には当たらない。
  本方式の利点は依存方向の単方向化である。利用者との確認済み、2026-08-17。）
- 探索範囲が対象シーン内に限定される。`FindFirstObjectByType` と異なり、
  遷移中に 2 シーンが同時に存在する瞬間でも取得対象が曖昧にならない。
- **Additive ロードしたシーン内オブジェクトの `Awake()` は `Initialize()` より先に実行される。**
  したがって、注入された依存を `Awake()` / `Start()` で使ってはならない。
  依存を必要とする MonoBehaviour は `Awake()` で `enabled = false` とし、
  `Initialize()` の末尾で `enabled = true` に戻す（design-note 参照）。
  これにより毎フレームの null チェックが不要になり、
  「未初期化という状態を持たない」形になる。
- `.First(x => x != null)` は該当なしのとき `Sequence contains no matching element` を投げる。
  教材では原因が読み取れないため、シーン名を含む例外メッセージに置き換える。
- `GameState` を丸ごと渡すため、ビュー側からも状態を書き換えられる。
  これは今回意図的に受け入れる制約であり、次回講習で解消する。
- 遷移時の `UnloadSceneAsync` も `GameRoot` の責務とする（旧 Q14）。
  シーン管理を独立クラスへ切り出すことは後から可能だが、今回は分けない。
- 各シーンを単独で開いて Play すると `GameRoot` が存在せず `Initialize` が呼ばれない。
  Root シーンから起動する運用を講習資料に明記する。

### Alternatives Rejected

- **`FindFirstObjectByType<CompositionRoot>()`**: 却下。
  ロード済みの全シーンを横断するため、遷移中の一時的な 2 シーン共存時に取得対象が曖昧になる。
- **`CompositionRoot` 側から `GameRoot` を探す**: 却下。
  依存の向きが双方向になり、`static` またはサービスロケータが必要になる。本方式の利点を失う。
- **`Initialize` に共通の入れ物（`GameContext` 等）を渡し、受け手が必要なものを取り出す**: 却下。
  実体はサービスロケータであり、何に依存しているかがシグネチャに現れなくなる。
- **`CompositionRoot` をプレハブ化して `GameRoot` から Instantiate する**: 却下。
  プレハブはシーン内オブジェクトを `SerializeField` で参照できない。

### トレーサビリティ

FR6、FR7、FR8、NFR2、ADR-001、ADR-006、ADR-009、Q11、Q12、Q14、Q16

---

## ADR-008: UniTask を導入する

### Context

ADR-007 の `GameRoot.LoadSceneAsync` は非同期メソッドである。
`Packages/manifest.json` に UniTask は含まれていない（調査日 2026-08-17）。

Unity `6000.3.21f1` は `await AsyncOperation` を標準でサポートしており、
UniTask を導入せずとも `UnityEngine.Awaitable` を戻り値型として同等のコードが書ける。
根拠: `/Applications/Unity/Hub/Editor/6000.3.21f1/.../UnityEngine.CoreModule.dll` に
`AsyncOperationAwaitableExtensions`（`GetAwaiter`）、`UnityEngine.Awaitable`、
`Awaitable+AwaitableAsyncMethodBuilder` が存在することを `strings` で確認済み。

### Decision

`UnityEngine.Awaitable` ではなく UniTask を導入する。
実際の開発でも UniTask を用いるため、受講者に触れてもらう対象として適切であるとの判断による
（利用者判断、2026-08-17）。ADR-001 の「未来永劫使わないコードを教えない」という原則と一致する。

### Consequences

- 依存パッケージが 1 つ増える。受講者の環境構築手順に UniTask の導入が加わる。
- `GetAwaiter` の競合（`CS0121`）は発生しない。UniTask 2.5.11 の
  `Runtime/UnityAsyncExtensions.cs` において `GetAwaiter(this AsyncOperation)` は
  `#if !UNITY_2023_1_OR_NEWER` で囲まれており、Unity `6000.3.21f1` ではコンパイル対象外となる。
  したがって `await SceneManager.LoadSceneAsync(...)` は Unity 標準の
  `AsyncOperationAwaitableExtensions` 側が使われ、戻り値型 `UniTask` は
  UniTask の async メソッドビルダが担う。確認日 2026-08-17、当該ファイルの 18 〜 25 行目。
- UniTask は Unity Package Registry には無く、Git URL 経由での導入となるため、
  `Packages/manifest.json` に外部 URL が入る。受講者の環境に Git が必要になる。
  講習の環境構築手順に明記が必要である。
- 導入完了。`Packages/manifest.json` に
  `"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11"`
  を追記し、Unity Editor による解決を確認した（2026-08-17）。
  `Packages/packages-lock.json` に `source: git` / `hash: 2e993ff1...` として記録され、
  `Library/PackageCache/com.cysharp.unitask@d648f5692cf2/` に展開されている（Q19、解決）。

### Alternatives Rejected

- **`UnityEngine.Awaitable` を使い、依存を増やさない**: 却下。
  依存は増えないが、実開発で使う道具に触れてもらう方針を優先した。
- **コルーチンまたは `AsyncOperation.completed` コールバック**: 却下。
  `async` / `await` を避けられるが、実開発の書き方から遠ざかる。

### トレーサビリティ

ADR-007、Q15、Q19

## ADR-009: シーン遷移は R3 によるイベント駆動で実装する

### Context

ADR-007 により、`GameRoot` は依存を注入するが、シーンから `GameRoot` への参照は持たない。
そのため「このシーンの役目が終わった」ことをシーン側から `GameRoot` へ伝える経路が必要になる（旧 Q17）。

検討時、`CompositionRoot` に `UniTask WaitForExitAsync(CancellationToken)` を持たせ、
`GameRoot` が `await` する案も挙がった。

### Decision

R3 を導入し、`CompositionRoot` が `Observable<SceneResult>` 型の `OnFinishScene` を公開する。
各シーンの Composition Root は終了条件を満たしたときにこれを発火し、
`GameRoot` はシーンのロード時に購読して、受信時に次のシーンへの遷移を開始する。

```csharp
public abstract class CompositionRoot : MonoBehaviour
{
    public abstract void Initialize(GameState gameState);
    public abstract Observable<SceneResult> OnFinishScene { get; }
}

public sealed class PlayCompositionRoot : CompositionRoot
{
    private readonly Subject<SceneResult> onFinishScene = new();
    public override Observable<SceneResult> OnFinishScene => onFinishScene;
}
```

`SceneResult` は 3 値とする（利用者判断、2026-08-17）。

```csharp
public enum SceneResult
{
    Normal,        // 通常の遷移（Title → Play、Result → Title）
    GameClear,     // Play → Result
    GameFailure,   // Play → Result
}
```

これにより `DecideNextScene` は `(SceneName, SceneResult)` から遷移先を返す純粋関数になる。

**`ReactiveProperty<T>` は用いない。** `ReactiveProperty<T>` は購読時に現在値を即座に流すため、
`GameRoot` が購読した瞬間に初期値で遷移が発火してしまう。
「シーンが終わった」は状態ではなく出来事であるため `Subject` が適切である。
この「状態か、出来事か」の判断軸は、ADR-004 で入力について決めた原則
（状態はポーリング、瞬間はイベント）と同一である。

### Consequences

- **R3 が実開発で用いる道具である**ため、受講者が触れる価値がある。
  ADR-001 の「未来永劫使わないコードを教えない」という原則と一致する。
- **分岐のある遷移を宣言的に記述できる。** `SceneResult` を enum とすることで、
  Play の終了理由（時間切れ / HP 0 / 全コイン取得）に応じた遷移先の切り替えが自然に書ける。
- 「次はどこか」を決める処理を純粋関数として切り出せる。
  ただしこれは R3 固有の利点ではなく、「次はどこか」と「どう遷移するか」を
  分離したことによる利点である。`WaitForExitAsync` 案でも同じことが実現できた。
  R3 を採る理由は上記 2 点である。
- **購読の解除が必要になる。** `GameRoot` はアンロードされるシーンのオブジェクトを購読するため、
  シーンごとに `CompositeDisposable` を保持し、遷移時に解除する。
  `await` 案には無かった失敗モードであり、購読が累積すると多重遷移を引き起こす。
  ただし「購読の解除も状態管理のうち」として教材にできる範囲である。
  **解除は `UnloadSceneAsync` の前に行う**（利用者判断、2026-08-17。旧 Q22）。
  アンロード中に発火が届いて二重に遷移が走ることを防ぐため。
- **導入コストが UniTask より重い。** R3 本体は NuGet 配布のため NuGetForUnity の導入が先に必要で、
  加えて `R3.Unity` を Git URL で導入する。受講者の環境構築手順が 2 段階増える。
  これが本決定の実質的な唯一のコストである（Q20）。
- 導入済み（2026-08-17）。`Packages/manifest.json` に
  `com.cysharp.r3`（`https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity#1.3.1`）と
  `com.github-glitchenzo.nugetforunity`
  （`https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity#v4.5.0`）を追記し、
  `Assets/packages.config` に R3 本体 `1.3.1` を記載した。
  タグ・パス・`package.json` の `name` / `version`、および nuget.org 上の R3 `1.3.1` の
  存在は確認済み。Unity Editor による UPM 解決と NuGetForUnity の自動復元は未実行である（Q20）。
- `OnFinishScene` の発火はシーン内オブジェクトのコールスタック上で起きるため、
  その中で同じシーンをアンロードすることになる。
  `UnloadSceneAsync` は非同期であるため実害は出ない見込みだが、確認が必要である（Q24）。
- UniTask は `LoadSceneAsync` / `UnloadSceneAsync` の待機で引き続き必要であり、R3 と併用する。

### Alternatives Rejected

- **`UniTask WaitForExitAsync(CancellationToken)` を `await` する**: 却下。
  `GameRoot` 側が `while` ループの直列な流れになり購読管理も不要という利点があるが、
  R3 が実開発で用いる道具であることを優先した。
- **`ReactiveProperty<SceneResult>`**: 却下。上記のとおり購読時に初期値が流れる。
- **`GameState` に終了フラグを持たせ `GameRoot` が毎フレーム監視する**: 却下。
  ポーリングであり、遷移理由の受け渡しも別途必要になる。

### トレーサビリティ

FR10、ADR-001、ADR-004、ADR-007、Q17、Q20、Q21、Q22、Q24

---

## ADR-010: InputActions をシーンごとに分割する

### Context

`Assets/Scripts/Main.inputactions` は Title / Play / Result の 3 マップを 1 ファイルで保持している。
ADR-007 により各シーンの Composition Root が自身の依存を管理する方針が決まったため、
入力アセットの所有者をどこに置くかを決める必要が生じた（旧 Q18）。

### Decision

InputActions をシーンごとのファイルに分割し、各シーンの Composition Root が所有する。

```
Assets/Scripts/Title/Title.inputactions      → 生成クラス TitleInput
Assets/Scripts/Play/Play.inputactions        → 生成クラス PlayInput
Assets/Scripts/Result/Result.inputactions    → 生成クラス ResultInput
```

各シーンのスクリプトと同じ場所に置き、所有者を自明にする。
生成クラス名は `wrapperClassName` で明示指定する（既定名はファイル名依存となるため）。

`Assets/Scripts/Main.inputactions` と生成物 `Assets/Scripts/Main.cs` は削除する。

### Consequences

- **`SwitchCurrentActionMap` が不要になる。** 単一ファイル設計では
  「今どのマップが有効か」というシーンをまたぐ可変状態が必要になり、
  誰がいつ切り替えたかを追う必要が生まれ、切り替え忘れがバグになる。
  これは本講習会が問題として提示したい構造そのものである。
  分割によりこの状態は消滅し、切り替えという概念自体が不要になる。
  ADR-007 の「シーンをまたぐものは `GameRoot` から、シーン内で完結するものはその場で」という
  原則が、そのまま入力にも適用される。
- 入力アセットの寿命がシーンの寿命と一致する。
  シーンのロードで生成され、`OnDestroy` で破棄される（旧 Q18 の解決）。
- **変更の影響範囲がファイル単位で切れる。** 単一ファイルでは、Play の入力を触ったつもりが
  Title のバインディングを壊しうる、レビュー時に差分から影響範囲を判断できない、
  といった問題があった。共同開発における変更衝突のリスクも下がる。
  ただし `.inputactions` は JSON でマップごとに別オブジェクトであるため、
  異なるマップの編集であればテキストマージが成立する場合もあり、
  衝突回避は分割の主たる論拠ではない。
- 複数シーンで共有したい入力（Pause、UI 操作など）が将来出た場合、
  共有用の 4 つ目のアセットが必要になる。
  現状の Title / Play / Result は共有すべき入力を持たないため、今は問題にならない。
- `controlSchemes` が空であるため、デバイス定義の重複コストは現時点でゼロである。
  将来ゲームパッド対応を入れる場合は 3 ファイルに同じスキームを記述することになる。
- ADR-005 で確定した「生成 C# ラッパークラスを使う」方針は維持される。対象が 3 つに増えるのみ。
- `MainInputPlayerInput` は `PlayInput` を包む形になるため、`PlayerInputAdapter` へ改名する。
- `Assets/InputSystem_Actions.inputactions` は削除する（追記、2026-08-17。旧 Q23）。

  調査の結果、本ファイル（GUID `052faaac586de48259a63d0c4782560b`）は
  `ProjectSettings/EditorBuildSettings.asset` の `m_configObjects` に
  `com.unity.input.settings.actions` として登録されており、Input System の
  **Project-wide Actions** として全シーンで暗黙に有効になる状態だった。
  内容は Unity テンプレート由来の `Player` マップ（Move / Look / Attack / Jump 等、未使用）と
  `UI` マップ（`InputSystemUIInputModule` が使用）である。

  これを残すと、`SwitchCurrentActionMap` を排除して消したはずの
  「シーンをまたぐ入力の暗黙的な有効状態」が別経路から復活する。
  本プロジェクトは uGUI による対話操作を使わない予定であるため（利用者判断、2026-08-17）、
  `UI` マップを残す理由もない。したがって以下を行う。

  - `Assets/InputSystem_Actions.inputactions` とその `.meta` を削除する。
  - `EditorBuildSettings.asset` の `com.unity.input.settings.actions` 登録を解除する。
  - Root シーンに EventSystem を置かない（ADR-006 の Q12 に関する記述を更新）。

### Alternatives Rejected

- **単一ファイルのまま `SwitchCurrentActionMap` で切り替える**: 却下。
  シーンをまたぐ可変状態を導入することになり、本講習会の趣旨に反する。
- **単一ファイルを `GameRoot` が所有し、各シーンへ渡す**: 却下。
  入力マッピングはシーン固有の関心事であり、シーンをまたぐ依存として扱う理由がない。

### トレーサビリティ

FR11、ADR-004、ADR-005、ADR-007、Q18、Q23

## ADR-011: `GameState` は変化する値のみを `ReactiveProperty` で保持し、設定値は ScriptableObject に置く

### Context

ADR-007 で `GameState` を `GameRoot` が保持し `Reset()` で初期化すること、
既定値を ScriptableObject で管理することは決めたが、
両者の具体的なフィールド構成は未確定だった（旧 Q21）。

要件（HP 100、制限時間 2 分、コイン 30 枚、被ダメージ 10、無敵 10 秒）から
おおむね導出できるが、4 点が要件から一意に定まらなかった。

### Decision

**1. 設定値はすべて `GameStateSettings`（ScriptableObject）で管理する**（旧 Q25）。

```csharp
public sealed class GameStateSettings : ScriptableObject
{
    public int   MaxHp              = 100;
    public float TimeLimitSeconds   = 120f;
    public int   CoinCount          = 30;
    public int   DamageOnEnemyHit   = 10;
    public float InvincibleDuration = 10f;
}
```

被ダメージ量や無敵時間の長さは変化しない値であるため `GameState` には置かない。
これを使うロジック（衝突処理）には `GameStateSettings` を直接渡す。
状態と設定の境界を明確にする。

**2. `GameState` は変化する値のみを保持し、各フィールドを `ReactiveProperty<T>` とする**（旧 Q28）。

```csharp
public sealed class GameState : IDisposable
{
    public ReactiveProperty<int>   Hp                         { get; } = new();
    public ReactiveProperty<float> RemainingTimeSeconds       { get; } = new();
    public ReactiveProperty<int>   CollectedCoinCount         { get; } = new();
    public ReactiveProperty<int>   TotalCoinCount             { get; } = new();
    public ReactiveProperty<float> InvincibleRemainingSeconds { get; } = new();

    public void Reset(GameStateSettings settings) { ... }
    public void Tick(float deltaTime) { ... }
}
```

HP や残り時間は「状態」であるため `ReactiveProperty` が適切である。
ADR-009 で `OnFinishScene` を「出来事」として `Subject` にしたのと同じ判断軸の裏返しであり、
講習では対比として提示できる。実務でも頻繁に用いるため教える価値がある（利用者判断、2026-08-17）。

**3. 時間経過の反映は `GameState.Tick(float deltaTime)` とし、Play シーンの Composition Root が呼ぶ**（旧 Q27）。

`GameState` は Pure C# であり `Update` を持たない。残り時間と無敵残り時間の減算は
`Tick` に集約し、`PlayCompositionRoot` が毎フレーム呼ぶ。
「時間が進むことも明示的な操作である」という形になり、`Reset()` を明示的に呼ばせる方針と一貫する。

**4. 時間切れによる終了は `GameFailure` に分類する**（旧 Q26）。
コインを集めきれずに時間切れになった状態であるため。

**5. 終了判定は純粋関数として切り出し、派生状態を `GameState` に持たせない**（利用者承認、2026-08-17。旧 Q29）。

```csharp
public enum GameOutcome { InProgress, Clear, Failure }

public static GameOutcome Evaluate(GameState state) =>
    state.CollectedCoinCount.CurrentValue >= state.TotalCoinCount.CurrentValue ? GameOutcome.Clear
  : state.Hp.CurrentValue <= 0                                                 ? GameOutcome.Failure
  : state.RemainingTimeSeconds.CurrentValue <= 0f                              ? GameOutcome.Failure
  : GameOutcome.InProgress;
```

Play シーンはこれを監視して `SceneResult` に変換して発火し、
Result シーンは同じ関数で表示内容を決める。終了理由を `GameState` のフィールドとして
別に持たないことで、状態の二重管理を避ける。

### Consequences

- 既定値がすべて Inspector で調整可能になり、講習中に値を変えて挙動の違いを見せられる。
- `ReactiveProperty` により、HP バーやスコア表示は購読するだけで済む。
  ビュー側が毎フレーム `GameState` を読みに行く必要がなくなる。
- **R3 本体は Unity 非依存の .NET ライブラリであるため、Core 層が参照しても
  NFR3 の層分離は崩れない。** `grep` チェックの対象は InputSystem と MonoBehaviour のみである。
- `ReactiveProperty<T>` は `IDisposable` であるため、`GameState` も `IDisposable` とし、
  `GameRoot` の `OnDestroy` で破棄する。
- `ReactiveProperty` を公開しているため、ビュー側からも値を書き換えられる。
  これは ADR-007 項目 3 で受け入れた制約と同じものであり、
  次回講習でインターフェース分離を導入する動機として残す。
- **`Tick` を手で呼ぶ形は、次回講習への 4 本目の橋になる。**
  VContainer の `ITickable` は、この「毎フレーム呼ぶ相手を登録する」作業を自動化する仕組みである。
  Zenject の `ITickable` を使った経験があるならそのまま接続できる（利用者の経験、2026-08-17）。
- 終了判定を純粋関数にしたことで、EditMode テストの題材が 1 つ増える（FR5 と同じ形）。

### Alternatives Rejected

- **設定値も `GameState` に持たせる**: 却下。変化しない値が状態に混ざり、
  「何が状態か」の定義が曖昧になる。本講習の主題に反する。
- **素のフィールド + ビュー側のポーリング**: 却下。R3 を採用済みであり、
  「状態は `ReactiveProperty`」という軸を示す機会を失う。
- **`GameState` に終了理由フィールドを持たせる**: 却下。
  HP・残り時間・コイン数から導出できる値を二重に持つことになり、整合性の管理が必要になる。
  「導出できる状態は持たない」という原則を示す機会でもある。
- **`Initialize` に `SceneResult` も渡せるよう `CompositionRoot` の署名を変える**: 却下。
  ADR-007 で確定した注入口の形を、この 1 点のために崩す必要はない。

### トレーサビリティ

FR8、FR12、FR13、ADR-007、ADR-009、Q21、Q25、Q26、Q27、Q28、Q29

## レビュー

- Status: `NOT-READY`
