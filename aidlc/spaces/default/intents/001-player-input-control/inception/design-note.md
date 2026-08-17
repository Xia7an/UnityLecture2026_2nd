# 設計ノート: 入力によるキャラクター操作

対象 Intent: `INT-001`
関連: `requirements.md`、`decisions.md`（ADR-001 〜 ADR-006）

本ノートのコードは設計意図を伝えるためのスケッチであり、コンパイル検証は行っていない。

## 1. 層構造

```
Assets/Scripts/Play/
  Core/                          ← Pure C#（asmdef: Game.Play.Core）
    IPlayerInput.cs
    ICharacterMoveLogic.cs
    ICharacterView.cs
    PlayerMoveLogic.cs
    RandomWalkMoveLogic.cs
  Unity/                         ← MonoBehaviour / InputSystem（asmdef: Game.Play）
    MainInputPlayerInput.cs
    CharacterView.cs
    PlaySceneRoot.cs             ← Composition Root
```

依存の向きは `Unity → Core` の一方向のみ。asmdef により逆方向はコンパイルエラーになる（ADR-002）。

```
[Main.inputactions]
      │ 生成
      ▼
   Main（生成クラス）
      │
      ▼
MainInputPlayerInput ──implements──▶ IPlayerInput          ┐
                                          │                │ Core
                                          ▼                │
                                    PlayerMoveLogic ──▶ ICharacterMoveLogic
                                                             │
CharacterView ───────────────────────────────────────────────┘ 参照して毎フレーム呼ぶ
      ▲
      │ Initialize()
PlaySceneRoot（唯一 new を書いてよい場所）
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

## 3. Unity 層

### 3.1 InputSystem の実装

```csharp
public sealed class MainInputPlayerInput : IPlayerInput, IDisposable
{
    private readonly Main main;   // .inputactions から自動生成されたラッパー

    public MainInputPlayerInput(Main main)
    {
        this.main = main;
        this.main.Play.Enable();
    }

    public Vector2 MoveDirection
    {
        get
        {
            var raw = main.Play.Move.ReadValue<Vector2>();
            return raw.sqrMagnitude > 1f ? raw.normalized : raw;
        }
    }

    public bool IsDashing => main.Play.Dash.IsPressed();

    public void Dispose()
    {
        main.Play.Disable();
        main.Dispose();   // 生成クラス @Main 自体が IDisposable
    }
}
```

生成物は `Assets/Scripts/Main.cs`、型はグローバル名前空間の `public partial class @Main`。
`main.Play.Move` / `main.Play.Dash` のアクセサが存在することを確認済み。

MonoBehaviour である必要はない。Input System は Pure C# から普通に使える。
これは「Unity の機能＝MonoBehaviour」という思い込みを崩す教材になる。

### 3.2 見た目制御

```csharp
public sealed class CharacterView : MonoBehaviour, ICharacterView
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;

    private ICharacterMoveLogic moveLogic;

    public Vector3 Position => transform.position;

    public void Initialize(ICharacterMoveLogic moveLogic) => this.moveLogic = moveLogic;

    private void Update()
    {
        if (moveLogic == null) return;

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

`SpeedHash` に対応する Animator パラメータ名は未確認（未解決事項 Q5）。実装前に要確認。

### 3.3 Composition Root

```csharp
public sealed class PlaySceneRoot : MonoBehaviour
{
    [SerializeField] private CharacterView playerView;
    [SerializeField] private CharacterView[] enemyViews;
    [SerializeField] private PlaySettings settings;   // ScriptableObject 推奨

    private MainInputPlayerInput playerInput;

    private void Awake()
    {
        // ここだけが new を書いてよい場所
        playerInput = new MainInputPlayerInput(new Main());

        playerView.Initialize(
            new PlayerMoveLogic(playerInput, settings.WalkSpeed, settings.DashSpeed));

        foreach (var enemy in enemyViews)
            enemy.Initialize(new RandomWalkMoveLogic(settings.FieldBounds, settings.EnemySpeed));
    }

    private void OnDestroy() => playerInput.Dispose();
}
```

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

## 5. 実装着手前に確定・是正が必要な項目

| 項目 | 状態 | 参照 |
|---|---|---|
| `Move` の `right` バインディングの `path` | 是正済み（`<Keyboard>/d`） | Q1 |
| `.inputactions` の C# クラス生成 | 完了。`Assets/Scripts/Main.cs`（`@Main`、グローバル名前空間）を生成済み | Q2、ADR-005 |
| `Character.prefab` へのコンポーネント追加（`CharacterController`、`Collider`） | 未着手 | Q3、ADR-005 |
| 移動方式（`CharacterController` / `Rigidbody`） | `CharacterController` を暫定採用 | Q4、ADR-005 |
| Animator パラメータ名の確認 | 未確認 | Q5 |
| 歩行速度・走行速度の値 | 未決 | Q6 |
| シーン跨ぎ状態の方式 | 提案のみ（Root シーン + Additive） | Q7、ADR-006 |
| `EditorBuildSettings.asset` のシーン参照切れ | 未是正 | Q8 |

## レビュー

- Status: `NOT-READY`
