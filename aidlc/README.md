# AI-DLC Workflow V2 ドキュメントワークスペース

このリポジトリの AI-DLC V2 文書は `aidlc/spaces/<space>/` に置く。既定の
space は `default` とし、アプリケーションコードは `aidlc/` の外に置く。

## 構成

- `memory/`: 組織、チーム、プロジェクト、フェーズの順で加算されるルール
- `knowledge/`: 複数 intent から再利用する共有知識
- `codekb/`: リポジトリ単位の、証拠に基づくリバースエンジニアリング成果物
- `intents/`: 一つの一貫した変更につき一つのライフサイクル記録

人がレビューする成果物は日本語で記述する。ワークフローが要求するファイル名、
安定 ID、YAML キー、`[Answer]:`、`READY`、`NOT-READY` は変更しない。
stage ディレクトリは、その stage を実行したときだけ作成する。
