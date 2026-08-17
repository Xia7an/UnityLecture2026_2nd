# 要求

対象 Intent: `INT-001`（`Main.inputactions` による `Character.prefab` の操作）

## Intent 分析

`Assets/Scripts/Main.inputactions` の `Play` アクションマップ（`Move`、`Dash`）を用いて
`Assets/Prefabs/Character.prefab` を操作可能にする。

本 intent は単なる操作機能の追加ではなく、**講習会全体のクラス設計の型を決める位置付け**を持つ。
以降に追加する HP、制限時間、コイン、無敵状態、敵移動ロジックの差し替えは、
ここで決めた層構造と依存の渡し方に従う。したがって要求には、動作要求に加えて
教材としての制約（受講者の前提知識、次回講習への接続）を含める。

## 機能要求

- **FR1**: `Play` アクションマップの `Move`（WASD による 2D Vector 合成）入力に応じて、
  `Character.prefab` のインスタンスがフィールド平面上を移動する。
  - 検証: PlayMode で W / A / S / D を個別に入力し、それぞれ前 / 左 / 後 / 右へ移動することを目視確認する。（未実行）
  - 情報源: 利用者からの依頼（2026-08-17）、`Assets/Scripts/Main.inputactions`

- **FR2**: `Dash` アクション（`<Keyboard>/leftShift`）を押下している間、移動速度が歩行速度から
  走行速度へ切り替わる。離すと歩行速度へ戻る。
  - 検証: PlayMode で LeftShift の押下中・非押下中の移動距離を比較する。（未実行）
  - 情報源: `Assets/Scripts/Main.inputactions` の `Play/Dash`

- **FR3**: 移動速度がゼロでないとき、キャラクターは進行方向を向く。
  - 検証: PlayMode で 4 方向へ移動し、モデルの向きが進行方向と一致することを目視確認する。（未実行）
  - 情報源: 推論。SD Unity-chan モデルを用いる以上、後ろ向き移動が不自然に見えるため。要合意。

- **FR4**: 移動ロジックは MonoBehaviour から分離され、共通インターフェース
  `ICharacterMoveLogic` の実装を差し替えることで変更できる。プレイヤーの入力駆動移動と
  敵のランダムウォークは、同一インターフェースの別実装として表現する。
  - 検証: `Composition Root` の 1 行を差し替えるだけでプレイヤーの移動がランダムウォークに
    変わることを PlayMode で確認する。（未実行）
  - 情報源: 利用者からの説明（2026-08-17）「移動ロジックとシーン上のキャラクターの見た目制御を
    行う MonoBehavior スクリプトは分離する」

- **FR5**: 入力の取得は抽象 `IPlayerInput` を介して行い、`UnityEngine.InputSystem` に依存しない
  スタブ実装に差し替えられる。
  - 検証: `IPlayerInput` のスタブを用いて `PlayerMoveLogic` の EditMode テストを 1 件以上、
    `Assets/Editor/` 配下に追加し、シーンを開かずに合否が出ることを確認する。（未実行）
  - 情報源: 推論。ADR-002 の層分離を検証可能にするため。
  - 備考: asmdef 不採用（ADR-002）のため、テストは predefined assembly
    `Assembly-CSharp-Editor` に置く。Editor 上で 13 ケースが認識・実行され、
    この経路が成立することを確認済み（Q10、解決）。

- **FR6**: 依存オブジェクトの生成と結線は、シーン内に 1 つ置く Composition Root
  （`PlaySceneRoot`）でのみ行う。`static` なシングルトンおよびグローバル可変状態を用いない。
  - 検証: `grep` により、プロジェクト固有スクリプトに `static` フィールド（定数・
    `Animator.StringToHash` 結果等の不変値を除く）と `Instance` プロパティが
    存在しないことを確認する。（未実行）
  - 情報源: ADR-001

- **FR7**: 各シーンは `CompositionRoot` を継承したクラスをルート GameObject 直下に 1 つ持ち、
  `GameRoot` がシーンの Additive ロード直後にこれを取得して `Initialize(GameState)` を呼ぶ。
  シーンから `GameRoot` への参照は持たない。
  - 検証: Root シーンから起動し、Play シーンの `PlayCompositionRoot.Initialize` が
    シーン内オブジェクトの `Awake` の後、`Update` の前に 1 度だけ呼ばれることを
    ログで確認する。（未実行）
  - 情報源: 利用者提示のサンプルコードおよび判断（2026-08-17）、ADR-007

- **FR8**: `GameState` は `GameRoot` がアプリ生存期間を通じて 1 インスタンス保持し、
  ゲーム開始時に `Reset()` を明示的に呼んで初期化する。
  - 検証: Title → Play → Result → Title → Play と 2 周し、2 周目の開始時に
    HP・スコア・残り時間が既定値に戻っていることを確認する。（未実行）
  - 情報源: 利用者判断（2026-08-17）、ADR-007

- **FR9**: 注入された依存を必要とする MonoBehaviour は、`Initialize` が呼ばれるまで
  `Update` 等のイベント関数を実行しない。
  - 検証: `Awake` で `enabled = false`、`Initialize` 末尾で `enabled = true` としている
    ことをレビューで確認する。あわせて、依存フィールドに対する毎フレームの
    null チェックが存在しないことを確認する。（未実行）
  - 情報源: 推論。Additive ロードしたシーンの `Awake` は `Initialize` より先に走るため（ADR-007）。

- **FR10**: 各シーンの Composition Root は終了条件を満たしたとき
  `Observable<SceneResult>` 型の `OnFinishScene` を発火する。`GameRoot` はこれを購読し、
  受信した `SceneResult` から次のシーンを純粋関数で決定して遷移する。
  シーンから `GameRoot` を参照しない。
  - 検証: Play シーンで時間切れ・HP 0・全コイン取得のそれぞれを発生させ、
    `SceneResult` に応じた遷移が 1 回だけ起きることを確認する。
    連続で 2 周し、購読の累積による多重遷移が起きないことも確認する。（未実行）
  - 情報源: 利用者判断（2026-08-17）、ADR-009

- **FR11**: InputActions はシーンごとのファイルに分割し、
  各シーンの Composition Root が生成・破棄する。`SwitchCurrentActionMap` を用いない。
  - 検証: `grep -rn "SwitchCurrentActionMap" Assets/Scripts/` の結果が空であること、
    および Play シーンのアンロード後に `Play` アクションマップが有効でないことを確認する。（未実行）
  - 情報源: 利用者判断（2026-08-17）、ADR-010

- **FR12**: `GameState` は変化する値のみを `ReactiveProperty<T>` で保持し、
  変化しない設定値は `GameStateSettings`（ScriptableObject）に置く。
  残り時間と無敵残り時間の減算は `GameState.Tick(float deltaTime)` に集約し、
  `PlayCompositionRoot` が毎フレーム呼ぶ。
  - 検証: `GameStateSettings` の値を Inspector で変更し、
    HP・制限時間・コイン枚数・被ダメージ量・無敵時間の挙動が追随することを確認する。
    あわせて `GameState` に設定値のフィールドが存在しないことをレビューで確認する。（未実行）
  - 情報源: 利用者判断（2026-08-17）、ADR-011

- **FR13**: ゲームの終了判定は純粋関数 `Evaluate(GameState) → GameOutcome` に集約し、
  終了理由を `GameState` のフィールドとして保持しない。
  時間切れは `Failure` に分類する。
  - 検証: `Evaluate` の EditMode テストを追加し、全コイン取得＝`Clear`、HP 0＝`Failure`、
    残り時間 0＝`Failure`、それ以外＝`InProgress` となることを確認する。
    あわせて `GameState` に終了理由フィールドが存在しないことをレビューで確認する。（未実行）
  - 情報源: 利用者承認（2026-08-17）、ADR-011 項目 5

## 非機能要求

- **NFR1**（教材適合性）: 受講者は DIContainer を未習である。前半の教材コードに、
  未説明の属性（`[Inject]` 等）およびコンテナ由来の API を含めない。
  - 閾値: 前半で新規に説明を要する C# 言語機能は「interface」「コンストラクタ引数による依存の受け渡し」の
    2 つに限定する。
  - 検証: 教材スクリプト一式のレビューで、上記 2 つ以外の新概念が出現しないことを確認する。（未実行）

- **NFR2**（次回講習への移行容易性）: DIContainer 導入時に、Core 層のクラス
  （`IPlayerInput`、`ICharacterMoveLogic`、`PlayerMoveLogic`、`RandomWalkMoveLogic` 等）の
  ソースコードを変更しない。
  - 閾値: Core 層の差分行数 0。変更は `PlaySceneRoot` → `LifetimeScope` の置き換えに限定する。
  - 検証: 次回講習教材の作成時に Core 層の diff を取得し、0 行であることを確認する。（未実行）

- **NFR3**（層の分離）: Core 層のコードは Input System および MonoBehaviour 派生型に依存しない。
  asmdef を用いないため（ADR-002）、強制はコンパイラではなく規約とレビューで行う。
  - 閾値: `Assets/Scripts/Core/` と `Assets/Scripts/Play/Core/` 配下のファイルに
    `UnityEngine.InputSystem` の `using`、`MonoBehaviour`、`ScriptableObject`、
    `GameObject`、`Transform` が出現しない。出現件数 0（コメント行を除く）。
  - 検証: `grep -rnE "UnityEngine\.InputSystem|MonoBehaviour|ScriptableObject|GameObject|Transform" Assets/Scripts/Core/ Assets/Scripts/Play/Core/`
    の結果が空であることを確認する。→ **`PASS`（2026-08-17）**
  - 備考: 設定値の受け渡しでは Core 層に `IGameStateSettings` インターフェースを置き、
    ScriptableObject である `GameStateSettings` がこれを実装する。
    `IPlayerInput` と同じ、依存の向きを一方向に保つための境界である。

## 制約

- Unity Editor `6000.3.21f1`、URP `17.3.0`、Input System `1.20.0`。
  根拠: `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json`。
- DIContainer（VContainer、Zenject 等）は導入しない。
- 非同期処理は UniTask `2.5.11` を用いる（ADR-008、利用者判断 2026-08-17）。
  `Packages/manifest.json` に Git URL で追記し、解決済み（Q19、解決）。
- イベント駆動には R3 `1.3.1` を用いる（ADR-009、利用者判断 2026-08-17）。UniRx は用いない。
  R3 本体は NuGet 配布のため NuGetForUnity `4.5.0` の導入が前提となる。
  `Assets/packages.config` に以下 5 つを明示列挙して復元済み（Q20、解決）。
  NuGetForUnity の Restore は推移的依存を解決しないため、列挙が必須である。
  - `R3 1.3.1`
  - `Microsoft.Bcl.TimeProvider 8.0.0`
  - `Microsoft.Bcl.AsyncInterfaces 6.0.0`
  - `System.Threading.Channels 8.0.0`
  - `System.Runtime.CompilerServices.Unsafe 6.0.0`
- `Assets/UnityChan/` は同梱パッケージ由来として変更しない。
  根拠: `aidlc/spaces/default/memory/project.md`。
- `Assets/Prefabs/Character.prefab` は `SD_unitychan_humanoid`
  （GUID `13a16f60da4245c45a865b2136ba272c`）の Prefab Variant である。
  Variant 側へのコンポーネント追加で対応し、元 Prefab は変更しない。
- `Assets/` 配下のファイル追加・移動時は `.meta` の GUID を維持する。
- asmdef を作成しない。プロジェクト固有スクリプトは predefined assembly に置く。
  根拠: ADR-002（利用者判断、2026-08-17）。
- シーン構成は Root シーン + Title / Play / Result の Additive ロードを前提とする。
  根拠: ADR-006（利用者判断、2026-08-17）。

## 対象外

- HP、制限時間、コイン取得、無敵状態、ゲーム終了判定の実装本体。
  本 intent では、それらを収める層構造を決めるところまでを対象とする。
- 敵キャラクターの実装、およびランダムウォーク以外の移動ロジック。
  ただし `ICharacterMoveLogic` による差し替え可能性は FR4 で担保する。
- UI の実装。
- シーン遷移処理そのものの実装（Additive ロード / アンロードの手続き、遷移条件）。
  ただし Root シーン + Additive ロードという構成前提は確定済みであり（ADR-006）、
  本 intent のクラス設計はその前提と矛盾しないこと。
- DIContainer の導入そのもの（次回講習の範囲）。
- ゲームパッド・タッチ等、キーボード以外の入力デバイス対応。

## 情報源

- 利用者からの説明および依頼（2026-08-17、会話）。
- `Assets/Scripts/Main.inputactions`、同 `.meta`
- `Assets/Prefabs/Character.prefab`
- `Packages/manifest.json`
- `aidlc/spaces/default/memory/project.md`

## 仮定と未解決事項

| ID | 種別 | 内容 | 状態 |
|---|---|---|---|
| Q1 | 事実 / 是正済み | `Main.inputactions` の `Move` 合成バインディング `right` の `path` が空だった。`<Keyboard>/d` へ是正済み。 | 解決 |
| Q2 | 事実 / 是正済み | `Main.inputactions.meta` の `generateWrapperCode` が `0` だった。`1` へ変更済み。`Assets/Scripts/Main.cs` が生成され、グローバル名前空間の `public partial class @Main`（`IInputActionCollection2`、`IDisposable`）に `Play.Move` / `Play.Dash` のアクセサが存在することを確認済み。 | 解決 |
| Q3 | 事実 | `Character.prefab` に追加コンポーネントが 1 つも無い（`m_AddedComponents: []`）。移動には `CharacterController` または `Rigidbody` + `Collider` が、敵・コインとの接触判定には `Collider` が必要。 | 未解決 |
| Q4 | 未決 | 移動方式を `CharacterController.Move` と `Rigidbody` のどちらにするか。ADR-005 で `CharacterController` を暫定採用しているが、敵との衝突表現次第で再検討の余地がある。 | 未解決 |
| Q5 | 事実 / 解決 | 同梱の `SD_unitychan_motion_humanoid.controller` は `Next` / `Back` の Trigger のみで `Speed` float を持たない。humanoid FBX（guid `f320efa63ce12874a9ad50add869c0b5`、`animationType: 3`）から `Standing@loop`(2.0s) / `Walking@loop`(1.2s) / `Running@loop`(0.8s) を `AnimationClip` としてロード可能で、3 つとも `isLooping=True` / `humanMotion=True` であることを確認（2026-08-17）。新規 Animator Controller を作成する（Step 5-2）。同名クリップを持つ Generic 版 FBX（guid `b6475d894388e0f4bbe9ba904c073a2e`）と取り違えないこと。 | 解決 |
| Q6 | 未決 | 歩行速度・走行速度の具体値。ゲーム仕様書に既定値の記載がない（制限時間 2 分、HP 100 等は既定値が示されている）。 | 未解決 |
| Q7 | 決定済み | Title / Play / Result のシーン跨ぎ状態の受け渡し方式。Root シーン + Additive ロードを正式採用（ADR-006、2026-08-17）。 | 解決 |
| Q8 | 事実 | `ProjectSettings/EditorBuildSettings.asset` が実在しない `Assets/Scenes/SampleScene.unity` を参照している（既知課題、`memory/project.md`）。ADR-006 の採用により、Root シーンの新規作成と有効シーンの全面的な登録し直しが必要になった。本 intent の PlayMode 検証前に是正が必要。 | 未解決 |
| Q9 | 仮定 | フィールドは平面であり、移動は XZ 平面上の 2 自由度で足りる（ジャンプ・段差なし）。 | 要合意 |
| Q10 | 事実 / 解決 | `Assets/Editor/` に置いた EditMode テストは predefined assembly `Assembly-CSharp-Editor` で認識され、`TestRunnerApi` により 13 ケースすべてが実行・通過した（2026-08-17）。ADR-002 の asmdef 不採用は成立する。 | 解決 |
| Q11 | 決定済み | Root シーンの `GameRoot` から各シーンの `CompositionRoot` へ依存を渡す手段。ロード直後に `Scene.GetRootGameObjects()` から `CompositionRoot`（abstract）を取得し、明示的に `Initialize(GameState)` を呼ぶ（ADR-007、2026-08-17）。 | 解決 |
| Q12 | 決定済み | Camera / AudioListener は Root シーンに集約する。各シーンは単独で完結しないため、シーンごとに置く利点がない（ADR-006、ADR-007）。EventSystem は uGUI の対話操作を使わないため配置しない（2026-08-17、ADR-010 追記）。 | 解決 |
| Q13 | 未決 | Root シーンのファイル名と配置先（`Assets/Scenes/Root.unity` 等）、および Build Settings への登録順。 | 未解決 |
| Q14 | 決定済み | シーン遷移（Additive ロードとアンロード）の責務は `GameRoot` が持つ。独立クラスへの切り出しは後から可能だが今回は行わない（ADR-007）。 | 解決 |
| Q15 | 事実 / 解決 | Unity 標準の `AsyncOperationAwaitableExtensions.GetAwaiter` と UniTask 側の同名拡張メソッドの競合（`CS0121`）は発生しない。UniTask 2.5.11 の `Runtime/UnityAsyncExtensions.cs` 18 〜 25 行目で `#if !UNITY_2023_1_OR_NEWER` により除外されるため。確認日 2026-08-17。 | 解決 |
| Q16 | 決定済み | `GameState` の既定値は ScriptableObject `GameStateSettings` で管理する。`GameRoot` が `SerializeField` で保持し、Play シーンのロード直前に `gameState.Reset(settings)` を呼ぶ（ADR-007 項目 5、2026-08-17）。フィールド構成そのものは Q21 へ引き継ぐ。 | 解決 |
| Q17 | 決定済み | シーンの終了通知は R3 の `Observable<SceneResult>`（`Subject` 由来）で行い、`GameRoot` が購読して遷移する（ADR-009、2026-08-17）。 | 解決 |
| Q18 | 決定済み | 入力アセットはシーンごとに分割し、各シーンの Composition Root が生成して `OnDestroy` で破棄する（ADR-010、2026-08-17）。 | 解決 |
| Q19 | 事実 / 解決 | UniTask 2.5.11 は Unity Editor により解決済み。`Packages/packages-lock.json` に `source: git`、`hash: 2e993ff1...` として記録され、`Library/PackageCache/com.cysharp.unitask@d648f5692cf2/` に展開されていることを確認（2026-08-17）。 | 解決 |
| Q20 | 事実 / 解決 | R3 の依存は `R3 1.3.1` → `Microsoft.Bcl.TimeProvider 8.0.0` → `Microsoft.Bcl.AsyncInterfaces 6.0.0` / `System.Threading.Channels 8.0.0` / `System.Runtime.CompilerServices.Unsafe 6.0.0` の計 5 つ。すべて `Assets/packages.config` に記載し復元済み。`R3` / `R3.Unity` / `Assembly-CSharp` のロード成功を確認（2026-08-17）。NuGetForUnity の Restore は推移的依存を解決しないため、必要なものを明示列挙する必要がある。 | 解決 |
| Q21 | 決定済み | `SceneResult` は `Normal` / `GameClear` / `GameFailure` の 3 値（2026-08-17）。`GameClear` と `GameFailure` は Play → Result の遷移、`Normal` はそれ以外の遷移で用いる。`GameState` / `GameStateSettings` のフィールド構成は ADR-011 に確定。 | 解決 |
| Q22 | 決定済み | `GameRoot` の購読解除は `UnloadSceneAsync` の前に行う（2026-08-17）。アンロード中の発火による二重遷移を防ぐため。 | 解決 |
| Q23 | 決定済み | `Assets/InputSystem_Actions.inputactions`（GUID `052faaac586de48259a63d0c4782560b`）は削除する。`ProjectSettings/EditorBuildSettings.asset` の `m_configObjects` に `com.unity.input.settings.actions` として登録されており Project-wide Actions として全シーンで暗黙に有効になるため、ADR-010 の趣旨と衝突する。uGUI の対話操作を使わないため `UI` マップを残す理由もない（利用者判断、2026-08-17）。登録解除と Root シーンへの EventSystem 非配置を伴う（ADR-010 追記）。 | 解決 |
| Q24 | 保留 | `OnFinishScene` ハンドラ内での同シーンアンロードの安全性。実装後に実機で確認する方針とした（利用者判断、2026-08-17）。 | 保留 |
| Q25 | 決定済み | 変化しない設定値（被ダメージ量、無敵時間の長さ等）はすべて `GameStateSettings`（ScriptableObject）で管理し、`GameState` には置かない（2026-08-17、ADR-011）。 | 解決 |
| Q26 | 決定済み | 時間切れによる終了は `GameFailure` に分類する。コインを集めきれなかった状態であるため（2026-08-17、ADR-011）。 | 解決 |
| Q27 | 決定済み | 残り時間と無敵残り時間の減算は `GameState.Tick(float deltaTime)` に集約し、`PlayCompositionRoot` が毎フレーム呼ぶ（2026-08-17、ADR-011）。この構造は VContainer の `ITickable` にそのまま対応する。 | 解決 |
| Q28 | 決定済み | `GameState` の各フィールドは R3 の `ReactiveProperty<T>` とする。実務でも頻繁に用いるため（2026-08-17、ADR-011）。R3 本体は Unity 非依存の .NET ライブラリであり、Core 層が参照しても NFR3 の層分離は崩れない。 | 解決 |
| Q29 | 決定済み | 終了判定は純粋関数 `Evaluate(GameState) → GameOutcome` として切り出し、終了理由を `GameState` のフィールドとして持たせない（利用者承認、2026-08-17、ADR-011 項目 5）。Play シーンはこれを監視して `SceneResult` に変換し、Result シーンは同じ関数で表示内容を決める。 | 解決 |

## レビュー

- Status: `NOT-READY`
