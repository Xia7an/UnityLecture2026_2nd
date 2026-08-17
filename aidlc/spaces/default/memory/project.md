# プロジェクトレベルルール

## プロジェクト

- 名前: UnityLecture2026_2nd
- 会話・レビュー言語: 日本語
- 種別: Unity プロジェクト

## 技術スタック

- Unity Editor `6000.3.21f1` を使用する。根拠: `ProjectSettings/ProjectVersion.txt`。
- レンダリングは Universal Render Pipeline `17.3.0` を使用する。根拠: `Packages/manifest.json` と `Assets/Settings/`。
- 入力は Input System `1.20.0` を使用し、`Assets/InputSystem_Actions.inputactions` がプロジェクト設定から参照されている。根拠: `Packages/manifest.json`、`ProjectSettings/EditorBuildSettings.asset`、対応 GUID を持つ `Assets/InputSystem_Actions.inputactions.meta`。
- Unity Test Framework `1.6.0` は導入済みである。根拠: `Packages/manifest.json`。
- AI Assistant `2.17.0-pre.1`、AI Inference `2.6.1`、Unity Toon Shader `0.13.0-preview` を含む。pre-release/preview 依存関係の更新は互換性確認を伴う。根拠: `Packages/manifest.json`。

## 構成と境界

- プロジェクト固有アセットは `Assets/Scenes/`、`Assets/Scripts/`、`Assets/Prefabs/`、`Assets/Settings/` に置かれている。
- `Assets/UnityChan/` は同梱パッケージ由来のコードとアセットとして扱い、明示的に対象化されない限りプロジェクト固有変更から除外する。
- Unity Package Manager の直接依存は `Packages/manifest.json`、解決結果は `Packages/packages-lock.json` を正とする。
- `Assets/` 内のファイルを追加・移動・削除するときは対応する `.meta` の GUID を維持し、参照切れを防ぐ。
- Unity が生成する `.csproj` と `.sln` は手編集しない。

## テスト状況

- Unity Test Framework は導入済みだが、現時点のリポジトリ調査ではプロジェクト固有の EditMode/PlayMode テストディレクトリやテスト asmdef を確認できていない。
- 変更ごとに、可能なら EditMode/PlayMode テストを追加する。自動化できないシーン、Prefab、入力、描画の確認は Unity Editor または対象ビルドで手順と結果を記録する。
- CI 構成は現時点のリポジトリ調査で確認できていない。CI 実行済みとは記録しない。

## 対象範囲の上書き

- `.gitignore` に列挙された `Library/`、`Temp/`、`Obj/`、`Build/`、`Builds/`、`Logs/`、`UserSettings/` 等の生成物を変更対象やレビュー対象に含めない。
- `Assets/UnityChan/` は明示的に要求された場合を除き変更しない。
- CodeKB は、9 成果物すべてを証拠付きで生成するか、部分調査の範囲を明記できるまで空のままにする。

## 決定済み

- DECIDED: AI-DLC V2 の space/intent 記録モデルを使用する。
- DECIDED: 既定 space 名を `default` とする。

## 禁止

- シーン名だけから `Title → Play → Result` の遷移を実装済みと断定しない。
- `ProjectSettings/EditorBuildSettings.asset` が参照する `Assets/Scenes/SampleScene.unity` は現在確認できないため、そのままビルド可能と断定しない。

## 必須

- シーンまたは Prefab の変更後は、欠落スクリプト、壊れた参照、意図しないシリアライズ差分を確認する。
- ビルドに関わる変更では、有効シーンと実在するシーンの整合を確認する。
- 対象プラットフォーム、配布方法、性能目標は未確認事項として扱い、intent ごとに必要なら合意する。

## 訂正
