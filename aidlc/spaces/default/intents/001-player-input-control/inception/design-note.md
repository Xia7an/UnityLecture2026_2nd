# 設計ノート: 入力によるキャラクター操作

対象 Intent: `INT-001`
関連: `requirements.md`、`decisions.md`（ADR-001 〜 ADR-010）

本ノートのコードは設計意図を伝えるためのスケッチであり、コンパイル検証は行っていない。

## 1. 層構造

```
Assets/Scripts/
  Core/                          ← Pure C#、namespace Game.Core
    GameState.cs                 ← シーンをまたぐゲーム状態（ADR-007、ADR-011）
    GameOutcome.cs               ← 終了判定の enum と純粋関数 Evaluate（ADR-011）
    SceneResult.cs               ← シーン終了理由の enum（ADR-009）
    SceneName.cs                 ← シーン識別の enum
  GameRoot.cs                    ← Root シーンに常駐。シーン管理と注入（ADR-007）
  GameStateSettings.cs           ← ScriptableObject。設定値（ADR-011）
  CompositionRoot.cs             ← abstract。各シーンの注入口 + OnFinishScene
  Title/
    Title.inputactions           → TitleInput（ADR-010）
    TitleCompositionRoot.cs
  Play/
    Play.inputactions            → PlayInput（ADR-010）
    Core/                        ← Pure C#、namespace Game.Play.Core
      IPlayerInput.cs
      ICharacterMoveLogic.cs
      ICharacterView.cs
      PlayerMoveLogic.cs
      RandomWalkMoveLogic.cs
    Unity/                       ← MonoBehaviour / InputSystem、namespace Game.Play
      PlayerInputAdapter.cs
      CharacterView.cs
      PlayCompositionRoot.cs     ← CompositionRoot の実装
  Result/
    Result.inputactions          → ResultInput（ADR-010）
    ResultCompositionRoot.cs
Assets/Editor/
  PlayerMoveLogicTest.cs         ← EditMode テスト（FR5）
```

`Assets/Scripts/Main.inputactions` と生成物 `Assets/Scripts/Main.cs` は削除する（ADR-010）。

依存の向きは `Unity → Core` の一方向のみ。
**asmdef は用いない（ADR-002）。** したがってこの一方向性はコンパイラでは強制されず、
ディレクトリ・名前空間の規約と、以下の `grep` チェックで担保する（NFR3）。

```sh
# 出力が空であること
grep -rnE "UnityEngine\.InputSystem|MonoBehaviour|ScriptableObject|GameObject|Transform" \
  Assets/Scripts/Play/Core/
```

受講者が Core から `CharacterView` を参照してもビルドは通ってしまう。
これは asmdef を入れないことのコストとして受け入れ、講師のレビューで補う。

```
[Play.inputactions]
      │ 生成
      ▼
 PlayInput（生成クラス）
      │
      ▼
PlayerInputAdapter ───implements──▶ IPlayerInput          ┐
                                          │                │ Core
                                          ▼                │
                                    PlayerMoveLogic ──▶ ICharacterMoveLogic
                                                             │
CharacterView ───────────────────────────────────────────────┘ 参照して毎フレーム呼ぶ
      ▲
      │ Initialize()
PlayCompositionRoot ◀── Initialize(GameState) ── GameRoot（Root シーン）

  ※ new を書いてよいのは GameRoot と 各 CompositionRoot だけ
  ※ 矢印はすべて GameRoot → シーンの一方向。逆向きの参照は存在しない（ADR-007）
```

## 2. Core 層

### 2.1 入力の抽象

```csharp
// InputSystem に依存しない。差し替えとテストの境界（ADR-004）
public interface IPlayerInput
{
    Vector2 MoveDirection { get; }  // 正規化済み
    bool IsDashing { get; }
}
```

`Move`（Value）と `Dash`（押下継続）はポーリング。
「押した瞬間」が必要な `Title/Start`、`Result/Back` は別インターフェースで `event Action` として公開する。

### 2.2 移動ロジック

```csharp
public interface ICharacterMoveLogic
{
    // 現在位置と経過時間から、このフレームの速度を返す
    Vector3 EvaluateVelocity(Vector3 currentPosition, float deltaTime);
}

public sealed class PlayerMoveLogic : ICharacterMoveLogic
{
    private readonly IPlayerInput input;
    private readonly float walkSpeed;
    private readonly float dashSpeed;

    public PlayerMoveLogic(IPlayerInput input, float walkSpeed, float dashSpeed)
    {
        this.input = input;
        this.walkSpeed = walkSpeed;
        this.dashSpeed = dashSpeed;
    }

    public Vector3 EvaluateVelocity(Vector3 currentPosition, float deltaTime)
    {
        var d = input.MoveDirection;
        var speed = input.IsDashing ? dashSpeed : walkSpeed;
        return new Vector3(d.x, 0f, d.y) * speed;
    }
}
```

`RandomWalkMoveLogic` も同じインターフェースを実装する（ADR-003）。
講習後半の「移動ロジック差し替え」は、実装クラスを 1 つ追加して
`PlaySceneRoot` の 1 行を差し替えるだけの作業になる。

### 2.3 教材上の要点: テスト可能性

`IPlayerInput` をスタブに差し替えれば、`PlayerMoveLogic` はシーンを開かずに
EditMode テストできる。これが「なぜ Pure C# に切り出すのか」に対する
最も説得力のある回答であり、講習で 1 本書いて見せる価値がある（FR5）。

asmdef を作らないため（ADR-002）、テストは `Assets/Editor/` 配下に置き、
predefined assembly `Assembly-CSharp-Editor` でコンパイルさせる。
Unity Test Framework がこの経路に対応していることは、パッケージの
`UnityEngine.TestRunner/AssemblyInfo.cs` にある
`InternalsVisibleTo("Assembly-CSharp-Editor-testable")` から確認済み。
Editor 上での実行確認は未実施（Q10）。

### 2.4 GameState と GameStateSettings（ADR-011）

設定値（変化しない値）は ScriptableObject に、状態（変化する値）は `GameState` に置く。

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

```csharp
public sealed class GameState : IDisposable
{
    public ReactiveProperty<int>   Hp                         { get; } = new();
    public ReactiveProperty<float> RemainingTimeSeconds       { get; } = new();
    public ReactiveProperty<int>   CollectedCoinCount         { get; } = new();
    public ReactiveProperty<int>   TotalCoinCount             { get; } = new();
    public ReactiveProperty<float> InvincibleRemainingSeconds { get; } = new();

    public void Reset(GameStateSettings settings) { ... }

    // 残り時間と無敵残り時間を進める。PlayCompositionRoot が毎フレーム呼ぶ
    public void Tick(float deltaTime) { ... }
}
```

被ダメージ量や無敵時間の長さは `GameState` に置かない。
これらを使うロジック（衝突処理）には `GameStateSettings` を直接渡す。

**HP や残り時間は「状態」であるため `ReactiveProperty` が適切である。**
ADR-009 で `OnFinishScene` を「出来事」として `Subject` にしたのと同じ判断軸の裏返しであり、
講習では対比として提示できる。

R3 本体は Unity 非依存の .NET ライブラリであるため、Core 層が参照しても層分離は崩れない
（NFR3 の `grep` チェック対象は InputSystem と MonoBehaviour のみ）。

終了判定は純粋関数として切り出し、終了理由を `GameState` のフィールドとして持たない
（ADR-011 項目 5、FR13）。

```csharp
public enum GameOutcome { InProgress, Clear, Failure }

public static GameOutcome Evaluate(GameState state) =>
    state.CollectedCoinCount.CurrentValue >= state.TotalCoinCount.CurrentValue ? GameOutcome.Clear
  : state.Hp.CurrentValue <= 0                                                 ? GameOutcome.Failure
  : state.RemainingTimeSeconds.CurrentValue <= 0f                              ? GameOutcome.Failure
  : GameOutcome.InProgress;
```

時間切れは `Failure` に分類する。コインを集めきれなかった状態であるため（Q26）。
Play シーンはこれを監視して `SceneResult` に変換して発火し、
Result シーンは同じ関数で表示内容を決める。

**導出できる状態は持たない。** 終了理由を `GameState` のフィールドに置くと、
HP・残り時間・コイン数から導出できる値を二重に管理することになる。
この原則自体が状態管理の教材になる（FR13）。

`Evaluate` は Pure C# の純粋関数であるため、EditMode テストの題材になる（FR5 と同じ形）。

## 3. Unity 層

### 3.1 InputSystem の実装

```csharp
public sealed class PlayerInputAdapter : IPlayerInput, IDisposable
{
    private readonly PlayInput input;   // Play.inputactions から自動生成されたラッパー

    public PlayerInputAdapter(PlayInput input)
    {
        this.input = input;
        this.input.Play.Enable();
    }

    public Vector2 MoveDirection
    {
        get
        {
            var raw = input.Play.Move.ReadValue<Vector2>();
            return raw.sqrMagnitude > 1f ? raw.normalized : raw;
        }
    }

    public bool IsDashing => input.Play.Dash.IsPressed();

    public void Dispose()
    {
        input.Play.Disable();
        input.Dispose();   // 生成クラス自体が IDisposable
    }
}
```

生成クラスの形は確認済みである。`Assets/Scripts/Main.cs` として生成された
`public partial class @Main`（グローバル名前空間、`IInputActionCollection2`、`IDisposable`）に
`Play.Move` / `Play.Dash` のアクセサが存在した。
ADR-010 により分割するため、`Main.inputactions` と `Main.cs` は削除し、
`Play.inputactions` から `PlayInput` を生成し直す。
`wrapperClassName` を明示指定してクラス名を固定する。

MonoBehaviour である必要はない。Input System は Pure C# から普通に使える。
これは「Unity の機能＝MonoBehaviour」という思い込みを崩す教材になる。

**`SwitchCurrentActionMap` は使わない。** 単一ファイル設計では
「今どのマップが有効か」というシーンをまたぐ可変状態が必要になるが、
シーンごとに分割することでこの状態自体が消滅する（ADR-010）。

### 3.2 見た目制御

```csharp
public sealed class CharacterView : MonoBehaviour, ICharacterView
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;

    private ICharacterMoveLogic moveLogic;

    public Vector3 Position => transform.position;

    // Initialize は Awake より後に呼ばれるため、それまで動かさない（FR9）
    private void Awake() => enabled = false;

    public void Initialize(ICharacterMoveLogic moveLogic)
    {
        this.moveLogic = moveLogic;
        enabled = true;
    }

    private void Update()
    {
        var velocity = moveLogic.EvaluateVelocity(Position, Time.deltaTime);
        controller.Move(velocity * Time.deltaTime);

        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity);

        animator.SetFloat(SpeedHash, velocity.magnitude);
    }
}
```

このクラスは「速度を受け取って Transform と Animator に反映する」しか知らない。
プレイヤーか敵かも知らないため、両方に使える。

**`Awake()` で `enabled = false` としているのは重要な点である。**
Additive ロードしたシーン内オブジェクトの `Awake()` は、`GameRoot` が
`Initialize` を呼ぶより前に実行される（ADR-007）。
`enabled` で制御することで、毎フレームの `if (moveLogic == null) return;` が不要になり、
「未初期化という状態を持たない」形になる。
講習の主題が状態管理であることを踏まえると、ここは説明の材料としても使える。

`SpeedHash` に対応する Animator パラメータ名は未確認（未解決事項 Q5）。実装前に要確認。

### 3.3 シーン構成（ADR-006）

```
Root.unity（常駐・起動シーン、Build Settings 先頭）
  ├ GameRoot            シーン管理と注入。GameState を保持する
  └ Main Camera         Camera / AudioListener は Root に集約（Q12）

  ＋ Additive ロード
      Title.unity   → TitleCompositionRoot
      Play.unity    → PlayCompositionRoot
      Result.unity  → ResultCompositionRoot
```

Root シーンは常駐し、Title / Play / Result を Additive でロード・アンロードする。
`static` を使わずにシーンをまたぐ状態を保持できる。

Camera / AudioListener は Root シーンに集約する。
各シーンは単独で完結しないため、シーンごとに置く利点がなく、
双方に存在すると重複警告が出る（Q12）。

**EventSystem は配置しない。** uGUI による対話操作を使わないため不要である。
あわせて `Assets/InputSystem_Actions.inputactions` を削除し、
`EditorBuildSettings.asset` の Project-wide Actions 登録も解除する（ADR-010 追記、Q23）。

各シーンを単独で開いて Play しても `GameRoot` が無いため `Initialize` が呼ばれず動作しない。
**Root シーンから起動する**運用を講習資料に明記する必要がある。

### 3.4 CompositionRoot（ADR-007、ADR-009）

`CompositionRoot` は abstract class とし、シーンごとに実装する。
`GameRoot` は基底型で取得するため、シーンが増えても `GameRoot` は変更不要である。

```csharp
public abstract class CompositionRoot : MonoBehaviour
{
    // DI の配線
    public abstract void Initialize(GameState gameState);

    // このシーンの役目が終わったことを通知する
    public abstract Observable<SceneResult> OnFinishScene { get; }
}
```

```csharp
public sealed class PlayCompositionRoot : CompositionRoot
{
    [SerializeField] private CharacterView playerView;
    [SerializeField] private CharacterView[] enemyViews;
    [SerializeField] private PlaySettings settings;   // ScriptableObject 推奨

    private readonly Subject<SceneResult> onFinishScene = new();
    private PlayerInputAdapter playerInput;

    public override Observable<SceneResult> OnFinishScene => onFinishScene;

    public override void Initialize(GameState gameState)
    {
        // 入力アセットはこのシーンが所有する（ADR-010）
        playerInput = new PlayerInputAdapter(new PlayInput());

        playerView.Initialize(
            new PlayerMoveLogic(playerInput, settings.WalkSpeed, settings.DashSpeed));

        foreach (var enemy in enemyViews)
            enemy.Initialize(new RandomWalkMoveLogic(settings.FieldBounds, settings.EnemySpeed));
    }

    private void Update() => gameState.Tick(Time.deltaTime);   // ADR-011 項目 3

    private void OnDestroy()
    {
        playerInput?.Dispose();
        onFinishScene.Dispose();
    }
}
```

終了条件は `Evaluate(gameState)` で判定し、`Clear` / `Failure` に応じて
`onFinishScene.OnNext(SceneResult.GameClear)` のように発火する。
判定が純粋関数 1 箇所に集まるため、**終了条件の判断が散らばらない**。

`Update` から `gameState.Tick(Time.deltaTime)` を呼ぶ形は、
次回講習で VContainer の `ITickable` に置き換わる（ADR-011）。

`ReactiveProperty<T>` は用いない。購読時に現在値を即座に流すため、
`GameRoot` が購読した瞬間に初期値で遷移が発火してしまう。
「シーンが終わった」は状態ではなく出来事であるため `Subject` が適切である（ADR-009）。

`Initialize` の引数は `GameState` のみとし、参照・変更範囲に応じたインターフェースの
切り出しは行わない。本講習で扱う範囲はゲームにおける状態管理であるため、
インターフェース分離は次回の VContainer 講習へ持ち越す（ADR-007）。

### 3.5 GameRoot（ADR-007、ADR-009）

```csharp
public sealed class GameRoot : MonoBehaviour
{
    [SerializeField] private GameStateSettings settings;   // 既定値の所在（Q16）

    private readonly GameState gameState = new();
    private readonly CompositeDisposable sceneSubscription = new();

    private void Start() => LoadSceneAsync(SceneName.Title).Forget();

    private async UniTask LoadSceneAsync(SceneName sceneName)
    {
        await SceneManager.LoadSceneAsync(sceneName.ToAssetName(), LoadSceneMode.Additive);

        var scene = SceneManager.GetSceneByName(sceneName.ToAssetName());
        SceneManager.SetActiveScene(scene);   // ライティング・スカイボックスの反映用

        var compositionRoot = FindCompositionRoot(scene);

        // Initialize より先に購読する（発火の取りこぼしを防ぐ）
        compositionRoot.OnFinishScene
            .Subscribe(result => TransitionAsync(scene, sceneName, result).Forget())
            .AddTo(sceneSubscription);

        // 明示的な状態初期化（FR8）。既定値は ScriptableObject から与える
        if (sceneName == SceneName.Play) gameState.Reset(settings);

        compositionRoot.Initialize(gameState);
    }

    private async UniTask TransitionAsync(Scene current, SceneName from, SceneResult result)
    {
        var next = DecideNextScene(from, result);   // 純粋関数

        sceneSubscription.Clear();   // アンロード前に解除する（Q22）

        await SceneManager.UnloadSceneAsync(current);
        await LoadSceneAsync(next);
    }

    // 遷移のコアロジック。UnityEngine に依存しない純粋関数
    private static SceneName DecideNextScene(SceneName from, SceneResult result) => (from, result) switch
    {
        (SceneName.Title,  SceneResult.Normal)      => SceneName.Play,
        (SceneName.Play,   SceneResult.GameClear)   => SceneName.Result,
        (SceneName.Play,   SceneResult.GameFailure) => SceneName.Result,
        (SceneName.Result, SceneResult.Normal)      => SceneName.Title,
        _ => throw new ArgumentOutOfRangeException(...),
    };
}
```

`SceneResult` は 3 値とする（ADR-009）。

```csharp
public enum SceneResult
{
    Normal,        // 通常の遷移（Title → Play、Result → Title）
    GameClear,     // Play → Result
    GameFailure,   // Play → Result
}
```

購読解除は `UnloadSceneAsync` の**前**に行う。アンロード中に発火が届いて
二重に遷移が走ることを防ぐため（Q22）。

**依存の向きは `GameRoot` → 各シーンの一方向で、逆向きの参照は存在しない。**
シーンは「終わった」という事実と理由を発するだけで、次に何が起きるかを知らない。
これが本方式の要点であり、次回講習の親スコープ → 子スコープの関係にそのまま対応する。

「次はどこか」を決める `DecideNextScene` は純粋関数として切り出せる。
ただしこれは R3 固有の利点ではなく、「次はどこか」と「どう遷移するか」を
分離したことによる利点である（ADR-009）。

実装時の注意:

- `GetRootGameObjects()` に対する `GetComponent` は子階層を探索しない。
  **`CompositionRoot` はシーンのルート GameObject 直下に置く**規約とする。
- `FindCompositionRoot` は該当なしのとき、シーン名を含む例外を投げるようにする。
  `.First(x => x != null)` の既定メッセージ（`Sequence contains no matching element`）では
  教材として原因が読み取れない。
- **購読を解除しないと累積し、多重遷移を引き起こす。** `CompositeDisposable.Clear()` は
  `UnloadSceneAsync` の前に呼ぶ（Q22、決定済み）。
- `OnFinishScene` の発火はシーン内オブジェクトのコールスタック上で起きるため、
  そのハンドラ内で同じシーンをアンロードすることになる。
  `UnloadSceneAsync` は非同期であるため実害は出ない見込みだが未検証である。
  実装後に実機で確認する方針とした（Q24、保留）。
- `SceneManager.SetActiveScene` を呼ばないと、ライティングとスカイボックスが
  Root シーンのものになる。忘れやすい箇所である。
- `UniTask` を用いる（ADR-008）。Unity 標準の `Awaitable` との `GetAwaiter` 競合は
  発生しない（UniTask 側が `#if !UNITY_2023_1_OR_NEWER` で除外されるため。Q15 で確認済み）。
- Build Settings は Root を先頭に、Title / Play / Result を続けて登録する（Q8、Q13）。

シーン内で完結する依存だけを `PlaySceneRoot` が生成し、
シーンを跨ぐ依存（`IPlayerInput`）は `GameRoot` から受け取る。
この親子関係が、次回講習の親子 `LifetimeScope` にそのまま対応する。

## 4. 次回講習への接続

以下の対応関係を講習の最後に並べて提示すると、そのまま次回の導入になる（ADR-001、NFR2）。

今回（手動 DI）:

```csharp
playerInput = new MainInputPlayerInput(new Main());
playerView.Initialize(new PlayerMoveLogic(playerInput, walkSpeed, dashSpeed));
```

次回（VContainer）:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    builder.Register<IPlayerInput, MainInputPlayerInput>(Lifetime.Singleton);
    builder.Register<ICharacterMoveLogic, PlayerMoveLogic>(Lifetime.Singleton);
    builder.RegisterComponent(playerView);
}
```

`IPlayerInput`、`ICharacterMoveLogic`、`PlayerMoveLogic`、`CharacterView` の
ソースコードはいずれも変わらない。変わるのは組み立て役だけである。

橋渡しは 2 本ある。

1. **配線の自動化**: 手で書いた `new` の連鎖が `Configure` の登録に置き換わる。
2. **スコープの親子関係**: `GameRoot` → `CompositionRoot` の依存の受け渡し（ADR-007）が、
   親 `LifetimeScope` → 子 `LifetimeScope` にそのまま対応する。
   Root シーン + Additive 構成は、この対応を見せるための土台にもなっている。

3. **インターフェース分離**: 今回 `GameState` を丸ごと渡していることで、
   「ビューからも `ReactiveProperty` を書き換えられる」問題が実物として残る。
   次回、参照範囲に応じたインターフェースの切り出しを導入する動機として使える（ADR-007）。

4. **`ITickable`**: `PlayCompositionRoot.Update` から `gameState.Tick()` を手で呼ぶ形が、
   VContainer の `ITickable` に置き換わる（ADR-011）。
   「毎フレーム呼ぶ相手を登録する」作業の自動化として説明できる。

## 5. 実装着手前に確定・是正が必要な項目

| 項目 | 状態 | 参照 |
|---|---|---|
| `Move` の `right` バインディングの `path` | 是正済み（`<Keyboard>/d`） | Q1 |
| `.inputactions` の C# クラス生成 | 完了。`Assets/Scripts/Main.cs`（`@Main`、グローバル名前空間）を生成済み | Q2、ADR-005 |
| シーン跨ぎ状態の方式 | 決定済み（Root シーン + Additive ロード） | Q7、ADR-006 |
| asmdef の採否 | 決定済み（不採用。ディレクトリ・名前空間 + `grep` で担保） | ADR-002 |
| `GameRoot` → 各シーンへの依存の渡し方 | 決定済み（ロード直後に `CompositionRoot` を取得して `Initialize`） | Q11、ADR-007 |
| Camera / AudioListener / EventSystem の配置先 | 決定済み（Root シーンに集約） | Q12 |
| シーン遷移の実行責務の所在 | 決定済み（`GameRoot` が持つ） | Q14、ADR-007 |
| `GameState` の寿命 | 決定済み（アプリ生存期間で 1 インスタンス、`Reset()` で初期化） | FR8、ADR-007 |
| `GameState` 既定値の所在 | 決定済み（ScriptableObject `GameStateSettings`、`GameRoot` が保持） | Q16、ADR-007 |
| シーン終了の通知方式 | 決定済み（R3 の `Observable<SceneResult>`。`ReactiveProperty` は使わない） | Q17、ADR-009 |
| InputActions の分割 | 決定済み（シーンごとに分割し、各 Composition Root が所有） | Q18、ADR-010 |
| UniTask 2.5.11 の導入 | `manifest.json` 追記済み。Editor による解決は未実施 | ADR-008、Q19 |
| R3 1.3.1 + NuGetForUnity 4.5.0 の導入 | 未着手 | ADR-009、Q20 |
| `Main.inputactions` / `Main.cs` の削除と 3 ファイルへの分割 | 未着手 | ADR-010 |
| `Character.prefab` へのコンポーネント追加（`CharacterController`、`Collider`） | 未着手 | Q3、ADR-005 |
| 移動方式（`CharacterController` / `Rigidbody`） | `CharacterController` を暫定採用 | Q4、ADR-005 |
| Animator パラメータ名の確認 | 未確認 | Q5 |
| 歩行速度・走行速度の値 | 未決 | Q6 |
| `EditorBuildSettings.asset` のシーン参照切れ、Root シーンの新規作成と登録 | 未着手 | Q8、Q13 |
| predefined assembly での EditMode テスト実行 | 未検証 | Q10 |
| `SceneResult` の値 | 決定済み（`Normal` / `GameClear` / `GameFailure`） | Q21、ADR-009 |
| `GameRoot` の購読解除の呼び出し位置 | 決定済み（`UnloadSceneAsync` の前） | Q22 |
| `GameState` / `GameStateSettings` のフィールド構成 | 決定済み（ADR-011） | Q21、Q25 〜 Q28 |
| `Assets/InputSystem_Actions.inputactions` の削除と Project-wide Actions の登録解除 | 決定済み・未着手 | Q23、ADR-010 |
| 終了判定の純粋関数化（`Evaluate`）と終了理由フィールドの不保持 | 決定済み | Q29、FR13 |
| `OnFinishScene` ハンドラ内での同シーンアンロードの安全性 | 保留（実装後に実機確認） | Q24 |

## レビュー

- Status: `NOT-READY`
