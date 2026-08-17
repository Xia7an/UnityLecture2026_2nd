# INT-001: Main.inputactions による Character.prefab の操作

## 状態

| 項目 | 値 |
|---|---|
| Intent ID | `INT-001` |
| 実行済み stage | `ideation`（設計方針の検討）、`inception`（要求・アーキテクチャ決定） |
| 未実行 stage | `construction`、`operation` |
| 承認状況 | 未承認。すべての成果物は `NOT-READY`。 |
| 記録日 | 2026-08-17 |

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
| `inception/decisions.md` | ADR-001 〜 ADR-006 |
| `inception/design-note.md` | クラス構成とコードスケッチ、実装前の是正項目 |

## 実施した調査

| 対象 | 手段 | 結果 |
|---|---|---|
| `Assets/Scripts/` 配下 | `ls -R Assets` | `Play/`、`Result/`、`Title/` はいずれも空。プロジェクト固有 C# は未実装。 |
| `Assets/Scripts/Main.inputactions` | ファイル読み取り | `Title`（`Start`）、`Play`（`Move`、`Dash`）、`Result`（`Back`）の 3 マップを確認。当初 `Move` の `right` バインディングの `path` が空文字列だったが、本 intent 記録中に `<Keyboard>/d` へ是正済み。 |
| `Assets/Scripts/Main.inputactions.meta` | ファイル読み取り | 当初 `generateWrapperCode: 0` だったが、本 intent 記録中に `1` へ変更済み。 |
| `Assets/Scripts/Main.cs` | `grep` によるクラス定義・アクセサ抽出 | 生成済み。グローバル名前空間の `public partial class @Main`（`IInputActionCollection2`、`IDisposable`）。`Play.Move`、`Play.Dash` のアクセサを確認。 |
| `Assets/Prefabs/Character.prefab` | ファイル読み取り | `SD_unitychan_humanoid`（GUID `13a16f60da4245c45a865b2136ba272c`）の Prefab Variant。`m_AddedComponents: []` であり、追加コンポーネントなし。 |
| `Packages/manifest.json` | ファイル読み取り | DIContainer（VContainer、Zenject 等）、UniTask、R3/UniRx はいずれも未導入。Input System `1.20.0`、Test Framework `1.6.0` は導入済み。 |
