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
  - 検証: `IPlayerInput` のスタブを用いて `PlayerMoveLogic` の EditMode テストを 1 件以上追加し、
    シーンを開かずに合否が出ることを確認する。（未実行）
  - 情報源: 推論。ADR-002 の層分離を検証可能にするため。

- **FR6**: 依存オブジェクトの生成と結線は、シーン内に 1 つ置く Composition Root
  （`PlaySceneRoot`）でのみ行う。`static` なシングルトンおよびグローバル可変状態を用いない。
  - 検証: `grep` により、プロジェクト固有スクリプトに `static` フィールド（定数・
    `Animator.StringToHash` 結果等の不変値を除く）と `Instance` プロパティが
    存在しないことを確認する。（未実行）
  - 情報源: ADR-001

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

- **NFR3**（層の強制）: Core 層のアセンブリは Input System および MonoBehaviour 派生型に
  依存しない。層の逸脱はコンパイルエラーとして検出される。
  - 閾値: `Game.Play.Core` の asmdef の `references` に `Unity.InputSystem` および
    `Game.Play` を含めない。
  - 検証: asmdef の参照定義を確認し、Core 層から `CharacterView` を参照するコードが
    コンパイルエラーになることを確認する。（未実行）

## 制約

- Unity Editor `6000.3.21f1`、URP `17.3.0`、Input System `1.20.0`。
  根拠: `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json`。
- DIContainer（VContainer、Zenject 等）、UniTask、R3 / UniRx はいずれも未導入。
  非同期・通知は素の C# の `event` と `Update()` で表現する。
  根拠: `Packages/manifest.json`。
- `Assets/UnityChan/` は同梱パッケージ由来として変更しない。
  根拠: `aidlc/spaces/default/memory/project.md`。
- `Assets/Prefabs/Character.prefab` は `SD_unitychan_humanoid`
  （GUID `13a16f60da4245c45a865b2136ba272c`）の Prefab Variant である。
  Variant 側へのコンポーネント追加で対応し、元 Prefab は変更しない。
- `Assets/` 配下のファイル追加・移動時は `.meta` の GUID を維持する。

## 対象外

- HP、制限時間、コイン取得、無敵状態、ゲーム終了判定の実装本体。
  本 intent では、それらを収める層構造を決めるところまでを対象とする。
- 敵キャラクターの実装、およびランダムウォーク以外の移動ロジック。
  ただし `ICharacterMoveLogic` による差し替え可能性は FR4 で担保する。
- UI、シーン遷移の実装。
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
| Q5 | 未確認 | SD Unity-chan の Animator Controller が持つパラメータ名（歩行・走行のブレンドに使う float 名）を確認していない。`CharacterView` のアニメーション反映はこれに依存する。 | 未解決 |
| Q6 | 未決 | 歩行速度・走行速度の具体値。ゲーム仕様書に既定値の記載がない（制限時間 2 分、HP 100 等は既定値が示されている）。 | 未解決 |
| Q7 | 未決 | Title / Play / Result のシーン跨ぎ状態の受け渡し方式。ADR-006 に Root シーン + Additive ロード案を記録したが未承認。 | 未解決 |
| Q8 | 事実 | `ProjectSettings/EditorBuildSettings.asset` が実在しない `Assets/Scenes/SampleScene.unity` を参照している（既知課題、`memory/project.md`）。本 intent の PlayMode 検証前に是正が必要になる可能性がある。 | 未解決 |
| Q9 | 仮定 | フィールドは平面であり、移動は XZ 平面上の 2 自由度で足りる（ジャンプ・段差なし）。 | 要合意 |

## レビュー

- Status: `NOT-READY`
