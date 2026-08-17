# UnityLecture2026_2nd プロジェクトコンテキスト

## プロダクト

- Unity で作成されている講義用プロジェクトであることは、リポジトリ名と Unity プロジェクト構成から読み取れる。
- 以下の授業目標とゲームルールは、利用者からの説明（2026-08-17、`INT-001` の会話）に基づく。リポジトリ内に対応する一次文書は存在しない。
  - 授業目標: Unity 入門を修了した受講者に対し、ゲーム開発における状態管理の重要性を教える。
  - 位置付け: 次回講習で DIContainer を習得してもらうための布石。
- ゲームルール（既定値は講習用の初期値であり、変更可能な設計とする想定）。
  - キャラクターを操作してフィールド上のコインを集めるゲームである。
  - 制限時間あり。既定値 2 分。時間切れでゲーム終了。
  - プレイヤーキャラクターは HP を持つ。既定値 100。
  - フィールドに敵キャラクターがおり、衝突すると HP が減少する。既定減少量 10。
  - HP が 0 になるとゲーム終了。
  - 敵の移動は既定でランダムウォーク。講習後半で別の移動ロジックへ差し替える。そのため移動ロジックと、シーン上のキャラクターの見た目制御を行う MonoBehaviour は分離する。
  - コインはランダム位置に配置される。既定配置枚数 30。
  - フィールド上の全コインを取得するとゲーム終了。
  - コインには通常コインと特殊コインがある。特殊コイン取得後 10 秒間は無敵状態となり、敵と衝突しても HP が減少しない。
- 完成条件、対象プラットフォーム、講習の所要時間は未確認である。

## 主な利用者

- 受講者: Unity 入門を修了済み。DIContainer は未習であり、`[Inject]` 等の記法を前提にできない。ハンズオンの実装者兼プレイヤーである。
- 講師: 本リポジトリを教材として保守する。リポジトリの Git ユーザーは `Inoyu`。
- 上記は利用者からの説明（2026-08-17）に基づく。受講者数、受講環境（OS、Unity 導入形態）は未確認。

## 主な実行フロー

- `Assets/Scenes/Title.unity`、`Play.unity`、`Result.unity` が存在する。名前から画面フローを推測できるが、実装済みの遷移としては未確認である。
- `ProjectSettings/EditorBuildSettings.asset` の有効シーンは、存在を確認できない `Assets/Scenes/SampleScene.unity` を参照している。ビルド前に是正または意図の確認が必要である。

## 構成上の目印

- Unity バージョン: `ProjectSettings/ProjectVersion.txt`。
- パッケージ宣言・ロック: `Packages/manifest.json`、`Packages/packages-lock.json`。
- プロジェクトシーン: `Assets/Scenes/`。
- 入力定義: `Assets/Scripts/Main.inputactions` と `Assets/InputSystem_Actions.inputactions`。
- URP 設定: `Assets/Settings/`。
- 同梱 Unity-Chan コード・アセット: `Assets/UnityChan/`。

## 外部境界

- Unity Package Registry 経由のパッケージ依存がある。
- 入力デバイスとの境界は Unity Input System が担う。
- AI Assistant、AI Inference、Multiplayer Center が依存関係に含まれるが、プロジェクト固有コードからの利用有無は未確認である。

## 既知の文書

- `Assets/Readme.asset` と `Assets/TutorialInfo/` は Unity テンプレート由来の案内資産であり、プロダクト要求文書としては扱わない。
- リポジトリ直下の README、設計文書、CI 設定は今回の調査では確認できていない。

## 設計方針（`INT-001` で決定、承認は未通過）

- ゲームのコアロジックの状態管理は Pure C# に置き、MonoBehaviour は見た目制御に限定する。
- 依存の解決は DIContainer を用いず、シーンごとの Composition Root による手動 DI で行う。`static` シングルトンは用いない。次回講習で DIContainer へ移行する際、コア層のコードが変わらないことを条件とする。
- 詳細は `aidlc/spaces/default/intents/001-player-input-control/inception/decisions.md`（ADR-001 〜 ADR-006）を参照。

## 既知のギャップ

- 対象プラットフォーム、配布経路、性能・アクセシビリティ目標は未確認。
- プロジェクト固有 C# 実装と自動テストは確認できていない。`Assets/Scripts/Play/`、`Result/`、`Title/` はいずれも空である。
- `Title`、`Play`、`Result` の遷移条件と、シーンを跨ぐ状態の受け渡し方式は未決である。
- `Assets/Prefabs/Character.prefab` は `SD_unitychan_humanoid` の Prefab Variant であり、追加コンポーネントを持たない。移動・接触判定に必要なコンポーネントは未追加である。
- DIContainer、UniTask、R3 / UniRx はいずれも未導入である。根拠: `Packages/manifest.json`。
- この文書はワークスペース初期化時の浅い構成調査と、`INT-001` における利用者からの説明に基づく。完全なリバースエンジニアリング結果ではない。
