# テスト結果

対象 Intent: `INT-001`
記録日: 2026-08-17

## 環境

| 項目 | 値 |
|---|---|
| Unity Editor | `6000.3.21f1`（`ProjectSettings/ProjectVersion.txt`） |
| レンダリング | URP `17.3.0` |
| Input System | `1.20.0` |
| Test Framework | `1.6.0` |
| UniTask | `2.5.11`（git、`Library/PackageCache/com.cysharp.unitask@d648f5692cf2/`） |
| R3.Unity | `1.3.1`（git） |
| NuGetForUnity | `4.5.0`（git） |
| R3 本体 | `1.3.1`（`Assets/Packages/R3.1.3.1/lib/netstandard2.1/R3.dll`） |
| R3 の依存 | `Microsoft.Bcl.TimeProvider 8.0.0`、`Microsoft.Bcl.AsyncInterfaces 6.0.0` |
| プラットフォーム | macOS（Darwin 25.5.0） |

## コマンドと手順

| 種別 | コマンド / 手順 | 結果 | 証拠 |
|---|---|---|---|
| 静的 | `grep -rnE "UnityEngine\.InputSystem\|MonoBehaviour\|ScriptableObject\|GameObject\|Transform" Assets/Scripts/Core/ Assets/Scripts/Play/Core/`（コメント行を除外） | `PASS` | 出力 0 件。NFR3 を満たす |
| 静的 | `grep -rn "SwitchCurrentActionMap" Assets/Scripts/`（コメント行を除外） | `PASS` | 出力 0 件。FR11 を満たす |
| 静的 | `Assets/Scripts` 配下の `static` 宣言の目視確認 | `PASS` | 該当 8 件はすべて純粋関数・拡張メソッド・`Animator.StringToHash` の `readonly` 定数。可変のグローバル状態と `Instance` プロパティは存在しない。FR6 を満たす |
| 静的 | 生成クラス名の確認（`grep "public partial class" Assets/Scripts/*/{Title,Play,Result}.cs`） | `PASS` | `@TitleInput` / `@PlayInput` / `@ResultInput`。グローバル名前空間 |
| 静的 | Project-wide Actions の解除確認（`ProjectSettings/EditorBuildSettings.asset`） | `PASS` | `m_configObjects` から `com.unity.input.settings.actions` が消えている |
| コンパイル | Unity Editor による `Assembly-CSharp` / `Assembly-CSharp-Editor` のビルド | `PASS` | 両アセンブリ生成。プロジェクト固有コード由来の error / warning は 0 件。`CS0121`（`GetAwaiter` の曖昧呼び出し）も発生せず |
| ロード | Editor 内リフレクションによるアセンブリ読み込み確認 | `PASS` | `R3` / `R3.Unity` / `Assembly-CSharp` / `Assembly-CSharp-Editor` がいずれも `loaded=True`。型 `Game.Core.GameState` も解決可能 |
| 自動 | `TestRunnerApi` で EditMode 実行 `Game.Tests.PlayerMoveLogicTest`（5 ケース） | `PASS` | 5/5 通過 |
| 自動 | `TestRunnerApi` で EditMode 実行 `Game.Tests.GameOutcomeTest`（8 ケース） | `PASS` | 8/8 通過 |
| 自動 | EditMode 全体 | `PASS` | passed=13 / failed=0 / skipped=0 / inconclusive=0、所要 0.99 秒 |
| 静的 | humanoid FBX からの `AnimationClip` ロード確認 | `PASS` | `Standing@loop`(2.0s) / `Walking@loop`(1.2s) / `Running@loop`(0.8s) がいずれも `isLooping=True`、`humanMotion=True` |
| 手動 / 実機 | Root シーンから PlayMode 起動しての通し確認 | `NOT-RUN` | Step 5 未実施のため実行不可 |

## トレーサビリティ

| ID | 検証 | Status | 注記 |
|---|---|---|---|
| FR1 | WASD で 4 方向へ移動 | `NOT-RUN` | Step 5 完了後に PlayMode で確認 |
| FR2 | LeftShift で走行速度へ切り替わる | 一部 `PASS` | `PlayerMoveLogicTest`「ダッシュ中は走行速度になる」でロジックは検証済み。実機での入力確認は `NOT-RUN` |
| FR3 | 進行方向を向く | `NOT-RUN` | |
| FR4 | `ICharacterMoveLogic` の差し替えで移動が変わる | `NOT-RUN` | `PlayCompositionRoot` の 1 行差し替えで確認 |
| FR5 | `IPlayerInput` スタブによる EditMode テスト | `PASS` | `PlayerMoveLogicTest` 5/5 通過。シーンを開かずに合否が出ることを確認 |
| FR6 | `static` 可変状態と `Instance` の不在 | `PASS` | 静的確認済み |
| FR7 | `CompositionRoot` への明示的な注入 | `NOT-RUN` | |
| FR8 | 2 周しても `GameState` が初期化される | 一部 `PASS` | `GameOutcomeTest`「Resetで前回の状態が残らない」で `GameState.Reset` 単体は検証済み。シーン遷移を伴う 2 周の確認は `NOT-RUN` |
| FR9 | `Initialize` 前にイベント関数が動かない | `PASS`（レビュー） | `CharacterView` / `TitleCompositionRoot` / `ResultCompositionRoot` が `Awake` で `enabled = false`。依存フィールドへの毎フレーム null チェックは存在しない |
| FR10 | `OnFinishScene` による遷移が 1 回だけ起きる | `NOT-RUN` | |
| FR11 | InputActions のシーン別分割、`SwitchCurrentActionMap` 不使用 | `PASS` | 静的確認済み |
| FR12 | `GameStateSettings` の値変更が挙動に追随 | `NOT-RUN` | |
| FR13 | 終了判定の純粋関数化 | `PASS` | `GameOutcomeTest` 8/8 通過。全コイン取得＝`Clear`、HP 0＝`Failure`、時間切れ＝`Failure`、それ以外＝`InProgress` を確認 |
| NFR1 | 未説明の属性・コンテナ API を含まない | `PASS`（レビュー） | `[Inject]` 等は不使用。新概念は interface とコンストラクタ引数のみ |
| NFR2 | Core 層が DIContainer 導入時に無変更で済む | `NOT-RUN` | 次回講習時に diff で確認 |
| NFR3 | Core 層が InputSystem / MonoBehaviour に依存しない | `PASS` | 静的確認済み |

## 解決した未解決事項

| ID | 内容 | 結果 |
|---|---|---|
| Q10 | asmdef なしの predefined assembly で Test Runner がテストを認識するか | **解決**。`Assembly-CSharp-Editor` を走査して `Game.Tests.GameOutcomeTest cases=8` / `Game.Tests.PlayerMoveLogicTest cases=5` が検出され、13 ケースすべてが実行・通過した。ADR-002 の asmdef 不採用は成立する |
| Q15 | UniTask と Unity 標準の `GetAwaiter` が `CS0121` にならないか | **解決**。`GameRoot` の `await SceneManager.LoadSceneAsync(...)` を含めコンパイルエラーなし |
| Q19 | UniTask の Editor による解決 | **解決** |
| Q20 | R3 + NuGetForUnity の導入と復元 | **解決**。依存は `R3 1.3.1` → `Microsoft.Bcl.TimeProvider 8.0.0` → `Microsoft.Bcl.AsyncInterfaces 6.0.0` / `System.Threading.Channels 8.0.0` / `System.Runtime.CompilerServices.Unsafe 6.0.0` の計 5 つ。`System.ComponentModel.Annotations` は最後まで要求されず不要 |
| Q5 | Animator のパラメータ名 | **解決**。同梱コントローラは `Next` / `Back` の Trigger のみで `Speed` を持たないため新規作成が必要。humanoid FBX（guid `f320efa63ce12874a9ad50add869c0b5`）から `Standing@loop` / `Walking@loop` / `Running@loop` を `AnimationClip` としてロードでき、3 つとも `isLooping=True` / `humanMotion=True` |

## 実装中に判明した重要な事実

**コンパイルの成否とアセンブリのロード可否は別である。**
`System.Threading.Channels` と `System.Runtime.CompilerServices.Unsafe` を欠いた状態でも、
Unity の netstandard2.1 参照アセンブリにより**コンパイルは通った**。
しかし実行時のロードで `Unable to resolve reference` となり、`R3.dll` が読み込めず、
それを参照する `Assembly-CSharp` まで芋づるでロード不能になった。
依存の過不足を判定する材料は、コンパイルエラーだけでは足りずコンソールのロードエラーまで必要である。

## 未検証領域

- **PlayMode での通し確認**。Step 5（Prefab・シーン・Animator・Build Settings）が
  未実施のため、シーン遷移も移動も実機で動かせていない。
  FR1・FR3・FR4・FR7・FR10・FR12 と、FR2・FR8 の実機部分が該当する。
- **Q24**。`OnFinishScene` のハンドラ内で同じシーンをアンロードすることの安全性は
  PlayMode 確認でしか判定できない。
- **NFR2**（Core 層が DIContainer 導入時に無変更で済むこと）は次回講習時に diff で確認する。

## 判定

- Status: `NOT-READY`

Step 1 〜 4 の実装は完了。静的検証・コンパイル・アセンブリロード・EditMode テスト（13/13）は
すべて `PASS`。ただし Step 5 が未実施で PlayMode による通し確認ができていないため、
Unit としては未完了である。
