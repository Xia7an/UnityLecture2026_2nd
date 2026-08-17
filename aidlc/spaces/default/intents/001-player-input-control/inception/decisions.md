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
- 弱点はシーンを跨ぐ状態の保持である。ADR-006 で別途扱う。
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

## ADR-002: Core（Pure C#）層と Unity 層を分離し、asmdef で強制する

### Context

「状態管理を Pure C# で行う」方針を口約束ではなく構造として担保したい。
Unity では、参照可能である限り受講者は容易に MonoBehaviour へ依存を戻してしまう。

### Decision

`Assets/Scripts/Play/` を `Core/` と `Unity/` に分け、それぞれに asmdef を置く。

- `Game.Play.Core`: `IPlayerInput`、`ICharacterMoveLogic`、`ICharacterView`、
  `PlayerMoveLogic`、`RandomWalkMoveLogic` 等。`Unity.InputSystem` を参照しない。
- `Game.Play`: `MainInputPlayerInput`、`CharacterView`、`PlaySceneRoot`。
  `Game.Play.Core` と `Unity.InputSystem` を参照する。

asmdef は 2 枚に留め、それ以上細分化しない。

### Consequences

- Core から InputSystem や `CharacterView` を参照するとコンパイルエラーになる。
  設計方針がコンパイラに守られる。「参照できないから書けない」は講習で規約を守らせる強力な手段である。
- Core 層は `UnityEngine.Vector3` / `Vector2` に依存する。これは
  `UnityEngine.CoreModule` の struct であり、Editor もシーンも不要なため
  EditMode テストの妨げにならない。完全な UnityEngine 非依存は目的ではないと整理する。
- asmdef は初学者にとって摩擦になりうる（「なぜ参照できないのか」の説明が必要）。
  2 枚に留めることで説明コストを抑える。
- EditMode テスト用の asmdef を追加する場合は `Game.Play.Core` のみを参照させる。

### Alternatives Rejected

- **asmdef を使わず名前空間だけで分ける**: 却下。強制力がなく、受講者が層を越えても気付けない。
- **asmdef をより細かく分ける（Input / Domain / View / Composition）**: 却下。
  初学者向けの説明コストに見合わない。

### トレーサビリティ

NFR1、NFR3

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
`Title/Start`、`Result/Back` は瞬間の入力であるため、別インターフェースで
`event Action` として公開する。

実装 `MainInputPlayerInput` は Unity 層に置き、MonoBehaviour ではない
Pure C# クラス（`IDisposable`）とする。

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

- `.inputactions` の "Generate C# Class" を有効にし、生成された `Main` クラスを
  `new Main()` して `main.Play.Move` の形で参照する（本 intent 記録中に
  `generateWrapperCode: 1` へ変更済み）。
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

## ADR-006: シーン跨ぎ状態は Root シーン + Additive ロードで保持する（提案・未承認）

### Context

Title → Play → Result の 3 シーン構成では、スコアや終了理由を Result へ渡す必要がある。
ここが手動 DI（ADR-001）の唯一の弱点であり、多くの実装が `static` シングルトンに流れる箇所である。

### Decision

Root シーンを 1 枚作り、Title / Play / Result を Additive でロードする構成を提案する。
Composition Root を Root シーンに置くことで、シーンを跨ぐ状態を `static` 無しで保持できる。

本 ADR は提案段階であり、未承認（未解決事項 Q7）。

### Consequences

- `static` シングルトンを避けるという ADR-001 の方針を、シーン遷移を含めて貫ける。
- Root シーンが親スコープとして振る舞う構成は、VContainer の親 `LifetimeScope` が
  行っていることと同じであるため、次回講習への接続が良い。
- Additive ロードとシーンのアンロード手順を受講者に説明する必要があり、
  単純な `SceneManager.LoadScene` より手数が増える。
- `ProjectSettings/EditorBuildSettings.asset` の有効シーン設定の見直しが必要になる（Q8 と関連）。

### Alternatives Rejected

- **`static` フィールドで結果を受け渡す**: 却下。ADR-001 と矛盾する。
- **`DontDestroyOnLoad` の MonoBehaviour に結果を持たせる**: 保留。
  Root シーン案より手数は少ないが、実体としてはグローバル可変状態に近く、
  参照の取得方法が `FindObjectOfType` 等に流れやすい。
- **今回から VContainer を導入し、親 `LifetimeScope` で解決する**: 保留。
  この論点だけを見れば最も素直だが、ADR-001 の NFR1 と衝突する。

### トレーサビリティ

FR6、NFR2、Q7、Q8

## レビュー

- Status: `NOT-READY`
