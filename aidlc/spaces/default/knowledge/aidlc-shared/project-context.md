# UnityLecture2026_2nd プロジェクトコンテキスト

## プロダクト

- Unity で作成されている講義用プロジェクトであることは、リポジトリ名と Unity プロジェクト構成から読み取れる。
- 具体的な授業目標、ゲームルール、完成条件を説明する一次文書は、今回の浅いリポジトリ調査では確認できていない。

## 主な利用者

- 未確認。受講者、講師、プレイヤー等の役割を intent-capture で確認する。

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

## 既知のギャップ

- 対象プラットフォーム、配布経路、性能・アクセシビリティ目標は未確認。
- プロジェクト固有 C# 実装と自動テストは今回の調査範囲では確認できていない。
- `Title`、`Play`、`Result` の期待動作と遷移条件は未確認。
- この文書はワークスペース初期化時の浅い構成調査に基づき、完全なリバースエンジニアリング結果ではない。
