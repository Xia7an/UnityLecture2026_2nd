# INT-001: Main.inputactions による Character.prefab の操作

## 状態

| 項目 | 値 |
|---|---|
| Intent ID | `INT-001` |
| 実行済み stage | `ideation`（設計方針の検討）、`inception`（要求・アーキテクチャ決定）、`construction`（Step 1 〜 4 まで実施、Step 5 以降は未着手） |
| 未実行 stage | `construction` の Step 5 以降、`operation` |
| 承認状況 | 未承認。すべての成果物は `NOT-READY`。 |
| 記録日 | 2026-08-17 |

### 改訂履歴

| 日付 | 内容 |
|---|---|
| 2026-08-17 | 初版。ADR-001 〜 ADR-006 を記録。 |
| 2026-08-17 | 利用者判断により 2 件を更新。ADR-002: asmdef を不採用とし、層の分離はディレクトリ・名前空間と `grep` チェックで担保する方式に改訂（便益より受講者の混乱コストが上回ると判断）。ADR-006: Root シーン + Additive ロードを提案から正式採用に変更。 |
| 2026-08-17 | ADR-007（シーンをまたぐ依存の注入方式）と ADR-008（UniTask 導入）を追加。利用者提示のサンプルコードに基づき、`GameRoot` がロード直後に `CompositionRoot`（abstract）を取得して `Initialize(GameState)` を呼ぶ方式を確定。あわせて FR7 〜 FR9 を追加し、Q11・Q12・Q14 を解決済みとした。当初「DIP を遵守」としていた本方式の利点は「依存方向の単方向化」に訂正（利用者確認済み）。 |
| 2026-08-17 | ADR-009（R3 によるイベント駆動のシーン遷移）と ADR-010（InputActions のシーン別分割）を追加。あわせて ADR-004（実装クラス名と「瞬間はイベント」の表現）、ADR-005（生成クラスが 3 つに分割）、ADR-007（`GameStateSettings` による既定値管理と `OnFinishScene` の追加）を改訂。FR10・FR11 を追加し、Q16・Q17・Q18 を解決済みとした。`OnFinishScene` は `ReactiveProperty` ではなく `Subject` 由来の `Observable` とする（購読時に初期値が流れるため）。 |
| 2026-08-17 | ADR-011（`GameState` の構成）を追加。設定値は `GameStateSettings`（ScriptableObject）に集約、`GameState` は変化する値のみを `ReactiveProperty<T>` で保持、時間経過は `GameState.Tick` を `PlayCompositionRoot` が呼ぶ、時間切れは `GameFailure`、と確定（Q25 〜 Q28 を解決）。あわせて ADR-010 に追記し、`Assets/InputSystem_Actions.inputactions` の削除と Project-wide Actions 登録解除、EventSystem 非配置を決定（Q23 を解決、Q12 を改訂）。`SceneResult` の 3 値と購読解除位置も反映（Q21・Q22 を解決）。終了判定の純粋関数化は提案として Q29 に残した。 |
| 2026-08-17 | Q29 を利用者が承認。終了判定を純粋関数 `Evaluate(GameState) → GameOutcome` に集約し、終了理由を `GameState` のフィールドとして持たない方針を確定（ADR-011 項目 5、FR13 として追加）。これにより設計上の未解決事項はすべて解消し、残りは Unity Editor 上の作業と数値決めのみとなった。 |

## 背景

本リポジトリは、Unity 入門を修了した受講者に対して「ゲーム開発における状態管理の重要性」を教える
講習会のハンズオン教材である。次回講習で DIContainer を扱うための布石という位置付けも持つ。

題材となるゲームは、キャラクターを操作してフィールド上のコインを集めるゲームであり、
状態管理の複雑さを教えるために以下の要素を持つ（情報源: 利用者からの説明、2026-08-17）。

- 制限時間あり。既定値 2 分。時間切れでゲーム終了。
- プレイヤーキャラクターは HP を持つ。既定値 100。
- フィールド上の敵と衝突すると HP が減少する。既定減少量 10。
- HP が 0 になるとゲーム終了。
- 敵の移動は既定でランダムウォーク。講習後半で別の移動ロジックに差し替える。
  そのため移動ロジックと、シーン上の見た目制御を行う MonoBehaviour は分離する。
- コインはランダム位置に配置される。既定配置枚数 30。
- 全コインを取得するとゲーム終了。
- コインには通常コインと特殊コインがあり、特殊コイン取得後 10 秒間は無敵状態となり、
  敵と衝突しても HP が減少しない。

詳細は `aidlc/spaces/default/knowledge/aidlc-shared/project-context.md` に反映済み。

## この Intent の対象

`Assets/Scripts/Main.inputactions` を用いて `Assets/Prefabs/Character.prefab` を操作する機能を
追加する。その際のクラス設計を、講習会の趣旨（状態管理／DIContainer への布石）に沿って確定する。

## 主要な論点

1. コアロジックの状態管理を Pure C# に置く場合、MonoBehaviour との境界をどこに引くか。
2. 受講者が DIContainer 未習である前提で、依存の解決方法をどうするか。
   `[Inject]` を「おまじない」として導入するか、しないか。

論点 2 に対する結論は ADR-001 を参照。

## 成果物

| ファイル | 内容 |
|---|---|
| `inception/requirements.md` | 機能要求・非機能要求・制約・未解決事項 |
| `inception/decisions.md` | ADR-001 〜 ADR-011 |
| `inception/design-note.md` | クラス構成とコードスケッチ、実装前の是正項目 |

## 実施した調査

| 対象 | 手段 | 結果 |
|---|---|---|
| `Assets/Scripts/` 配下 | `ls -R Assets` | `Play/`、`Result/`、`Title/` はいずれも空。プロジェクト固有 C# は未実装。 |
| `Assets/Scripts/Main.inputactions` | ファイル読み取り | `Title`（`Start`）、`Play`（`Move`、`Dash`）、`Result`（`Back`）の 3 マップを確認。当初 `Move` の `right` バインディングの `path` が空文字列だったが、本 intent 記録中に `<Keyboard>/d` へ是正済み。 |
| `Assets/Scripts/Main.inputactions.meta` | ファイル読み取り | 当初 `generateWrapperCode: 0` だったが、本 intent 記録中に `1` へ変更済み。 |
| `Assets/Scripts/Main.cs` | `grep` によるクラス定義・アクセサ抽出 | 生成済み。グローバル名前空間の `public partial class @Main`（`IInputActionCollection2`、`IDisposable`）。`Play.Move`、`Play.Dash` のアクセサを確認。 |
| `Assets/Prefabs/Character.prefab` | ファイル読み取り | `SD_unitychan_humanoid`（GUID `13a16f60da4245c45a865b2136ba272c`）の Prefab Variant。`m_AddedComponents: []` であり、追加コンポーネントなし。 |
| `Packages/manifest.json` | ファイル読み取り | 調査時点で DIContainer（VContainer、Zenject 等）、UniTask、R3/UniRx はいずれも未導入。Input System `1.20.0`、Test Framework `1.6.0` は導入済み。その後 UniTask・R3・NuGetForUnity を追記（ADR-008、ADR-009）。 |
| `ProjectSettings/EditorBuildSettings.asset` | `grep m_configObjects` | `com.unity.input.settings.actions` として `Assets/InputSystem_Actions.inputactions`（GUID `052faaac586de48259a63d0c4782560b`）が Project-wide Actions に登録されていることを確認。ADR-010 追記により登録解除・削除の対象とした。 |
| `Assets/InputSystem_Actions.inputactions` | JSON パース | Unity テンプレート由来。`Player` マップ（Move / Look / Attack / Jump 等、未使用）と `UI` マップ（`InputSystemUIInputModule` が使用）、および 5 つの control scheme を含む。 |
| `Library/PackageCache/com.unity.test-framework@bd7f943e9647/UnityEngine.TestRunner/AssemblyInfo.cs` | ファイル読み取り | `InternalsVisibleTo("Assembly-CSharp-testable")` と `InternalsVisibleTo("Assembly-CSharp-Editor-testable")` の存在を確認。asmdef 不採用でも EditMode テストが書けることの根拠（ADR-002）。 |
| Unity Editor `6000.3.21f1` の `UnityEngine.CoreModule.dll` | `strings` による型名抽出 | `AsyncOperationAwaitableExtensions`、`UnityEngine.Awaitable`、`Awaitable+AwaitableAsyncMethodBuilder` の存在を確認。UniTask なしでも `await AsyncOperation` が可能であることの根拠（ADR-008）。 |
| UniTask `2.5.11` の `Runtime/UnityAsyncExtensions.cs` | ダウンロードして `grep` | `GetAwaiter(this AsyncOperation)` が `#if !UNITY_2023_1_OR_NEWER` で囲まれており、Unity 6 では `CS0121` が発生しないことを確認（Q15）。 |
