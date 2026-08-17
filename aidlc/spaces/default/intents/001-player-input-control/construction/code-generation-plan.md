# コード生成計画

対象 Intent: `INT-001`（`Main.inputactions` による `Character.prefab` の操作）

## Unit

入力によるキャラクター操作と、以降の機能が乗る層構造・依存の流れの構築。

HP・制限時間・コイン・無敵状態・敵 AI の「中身」は本 Unit の対象外だが、
それらを収める `GameState` と Composition Root の骨格までを含む。

## 承認済み入力

- `inception/requirements.md`（FR1 〜 FR13、NFR1 〜 NFR3）
- `inception/decisions.md`（ADR-001 〜 ADR-011）
- `inception/design-note.md`
- 実装計画（利用者承認、2026-08-17）

## 計画ステップ

- [x] **Step 1**: 入力アセットをシーンごとに分割し、Project-wide Actions を解除する
  - トレーサビリティ: FR11、ADR-010、Q18、Q23
  - 対象ファイル:
    - 新規 `Assets/Scripts/{Title,Play,Result}/{Title,Play,Result}.inputactions`（+ `.meta`）
    - 削除 `Assets/Scripts/Main.inputactions`、`Main.cs`（+ 各 `.meta`）
    - 削除 `Assets/InputSystem_Actions.inputactions`（+ `.meta`）
    - 変更 `ProjectSettings/EditorBuildSettings.asset`（`m_configObjects` から
      `com.unity.input.settings.actions` を削除）
  - 検証: 生成クラス名の確認、Project-wide Actions の解除確認 → **完了**

- [x] **Step 2**: Core 層（Pure C#）を実装する
  - トレーサビリティ: FR4、FR5、FR8、FR12、FR13、NFR3、ADR-003、ADR-004、ADR-011
  - 対象ファイル:
    - `Assets/Scripts/Core/`: `SceneName.cs`、`SceneResult.cs`、`GameOutcome.cs`、
      `GameState.cs`、`IGameStateSettings.cs`
    - `Assets/Scripts/Play/Core/`: `IPlayerInput.cs`、`ICharacterMoveLogic.cs`、
      `ICharacterView.cs`、`PlayerMoveLogic.cs`、`RandomWalkMoveLogic.cs`
  - 検証: `grep` による層分離チェック → **完了**

- [x] **Step 3**: Unity 層を実装する
  - トレーサビリティ: FR1 〜 FR3、FR6、FR7、FR9、FR10、ADR-007、ADR-009、ADR-010
  - 対象ファイル:
    - `Assets/Scripts/`: `CompositionRoot.cs`、`GameRoot.cs`、`GameStateSettings.cs`
    - `Assets/Scripts/Play/Unity/`: `PlaySettings.cs`、`PlayerInputAdapter.cs`、
      `CharacterView.cs`、`PlayCompositionRoot.cs`
    - `Assets/Scripts/Title/TitleCompositionRoot.cs`
    - `Assets/Scripts/Result/ResultCompositionRoot.cs`
  - 検証: コンパイル、`static` 可変状態の不在確認 → **完了**

- [x] **Step 4**: EditMode テストを実装する
  - トレーサビリティ: FR5、FR13、Q10、ADR-002
  - 対象ファイル: `Assets/Editor/PlayerMoveLogicTest.cs`、`GameOutcomeTest.cs`
  - 検証: Test Runner での実行 → **未実行**（`construction/test-results.md` 参照）

- [x] **Step 5**: Prefab・シーン・Animator・Build Settings を構成する（5-1 〜 5-6 完了）
  - トレーサビリティ: FR1 〜 FR3、FR7、Q3、Q4、Q5、Q8、Q12、Q13、Q16
  - 実施方法: Unity MCP 経由。利用者の承認を得て着手した
  - 実施結果:
    - 5-1 `Character.prefab` に `CharacterController`（Height 1.2 / Radius 0.25 /
      Center (0, 0.6, 0) / StepOffset 0.15 / SlopeLimit 45）と `CharacterView` を追加。
      両 SerializeField を結線。`m_ApplyRootMotion` を `0` に上書き → **完了**
    - 5-2 `Assets/Animations/Character.controller` を新規作成。`Speed`（float）、
      単一ステート `Locomotion`、1D Blend Tree、しきい値 0 / 3 / 6。
      クリップは humanoid FBX（guid `f320efa63ce12874a9ad50add869c0b5`）由来 → **完了**
    - 5-3 `Assets/Scenes/Root.unity` を新規作成。`GameRoot` と `Main Camera` の 2 ルート。
      EventSystem なし → **完了**
    - 5-4 各シーンに `CompositionRoot` を配置。Play に `Character` を原点配置。
      3 シーンとも Camera / AudioListener を削除 → **完了**
    - 5-5 Build Settings を Root → Title → Play → Result に置き換え。
      実在しない `SampleScene.unity` を削除 → **完了**
    - 5-6 `Assets/Settings/` に `GameStateSettings` と `PlaySettings` を作成 → **完了**
  - 検証: コンソール 0 件。Step 6 の PlayMode 手動確認は未実施

- [ ] **Step 5-7**: PlayMode での動作確認
  - 実施方法: `GameStateSettings.TimeLimitSeconds` を一時的に 5 秒へ下げ、
    Root シーンから Play して Title → Play → Result → Title を 1 周する。確認後 120 秒へ戻す
  - **キーボード入力（Space / WASD / LeftShift）を伴うため、利用者の実操作が必要になる見込み**

- [ ] **Step 6**: 検証（自動・静的・手動）
  - トレーサビリティ: 全 FR / NFR
  - 検証: `construction/test-results.md` に記録

## 実装中に生じた設計上の変更

| 項目 | 内容 | 影響 |
|---|---|---|
| `GameState.Reset` の引数 | ADR-011 の `Reset(GameStateSettings)` では Core 層が Unity 層に依存するため、Core に `IGameStateSettings` を新設し `Reset(IGameStateSettings)` とした | ADR-011 に改訂として記録済み。NFR3 を満たすための変更であり、設計意図は不変 |
| `PlaySettings.fieldSize` の既定値 | Play シーンの `Ground` が原点中心 5×5 の Quad（X 軸 90° 回転、scale (5,5,1)）であることを実測し、(20,0,20) から (5,0,5) へ変更 | 既定値のみ。Inspector で調整可能 |
| `RandomWalkMoveLogic` の `using` | `using System;` と `using UnityEngine;` の併存で `Random` が CS0104 となったため `using Random = System.Random;` を追加 | 実装詳細。`System.Random` を選ぶ設計意図は不変 |
| `Character.prefab` から `FaceAnimationPreviewer` を削除 | 新規 `Character.controller` を単一レイヤー構成にしたところ、ベース Prefab の `FaceAnimationPreviewer`（`FACE_LAYER_INDEX = 1`）が `OnValidate` のたびに `AssertionException` を投げるようになった。Variant の `m_RemovedComponents` で除去する | 下記の判断根拠を参照 |

## 実装中に判明した事実

| 事項 | 内容 |
|---|---|
| NuGet 依存チェーン | NuGetForUnity の Restore は推移的依存を解決しない。`R3 1.3.1` → `Microsoft.Bcl.TimeProvider 8.0.0` → `Microsoft.Bcl.AsyncInterfaces 6.0.0` を 1 つずつ `Assets/packages.config` に追記して解決した。R3 が要求する残り 3 パッケージ（`System.ComponentModel.Annotations`、`System.Runtime.CompilerServices.Unsafe`、`System.Threading.Channels`）は Unity の netstandard2.1 プロファイルが提供しており追加不要 |
| `CS0433` の懸念 | `Microsoft.Bcl.AsyncInterfaces` と netstandard2.1 の `IAsyncDisposable` は型フォワーディングで解決され、重複エラーは発生しなかった |
| 生成クラスのファイル名 | `wrapperCodePath` が空のため、生成ファイル名はアセット名（`Title.cs` 等）になる。クラス名のみ `wrapperClassName` に従う |
| Q5（Animator パラメータ） | 同梱の `SD_unitychan_motion_humanoid.controller` は `Next` / `Back` の Trigger のみで `Speed` float を持たない。FBX に `Standing@loop` / `Walking@loop` / `Running@loop` があるため新規コントローラを作成する（Step 5-2） |
| FBX の取り違えリスク | `Standing@loop` 等の同名クリップを持つ FBX が 2 つある。`SD_unitychan_motion_humanoid.fbx`（guid `f320efa63ce12874a9ad50add869c0b5`、`animationType: 3` = Humanoid）を使う。Generic 版（guid `b6475d894388e0f4bbe9ba904c073a2e`、`animationType: 2`）を割り当てるとリグが合わない |
| Root Motion の二重適用 | ベース Prefab が `m_ApplyRootMotion: 1`。`CharacterController.Move` と併用すると移動が二重にかかるため、Variant で `0` に上書きする |

## Animator Controller のレイヤー構成に関する判断

同梱の `SD_unitychan_motion_humanoid.controller` は `Base Layer` と `face` の 2 レイヤー構成である。
仕様どおり単一レイヤーの `Character.controller` を作成したところ、
ベース Prefab に付属する `FaceAnimationPreviewer` が `OnValidate` のたびに
`AssertionException` を投げるようになった。

原因は `Assets/UnityChan/Common/Runtime/Scripts/AnimationEditorUtility.cs:27` の
`Assert.IsTrue(layerIndex >= 0 && layerIndex < controller.layers.Length)` であり、
`FaceAnimationPreviewer.cs:165` の `const int FACE_LAYER_INDEX = 1;` が
単一レイヤー構成では範囲外になるためである。

### 検討した選択肢

| 案 | 内容 | 判定 |
|---|---|---|
| A | `Character.controller` に空の `face` レイヤーを 1 枚足す | 却下 |
| B | `Character.prefab`（Variant）から `FaceAnimationPreviewer` を削除する | **採用** |
| C | 同梱の `SD_unitychan_motion_humanoid.controller` を使い続ける | 却下。`Speed` による Blend Tree が組めない |

### 採用理由

- **A は機能を保存しない。** 空のレイヤーでは表情アニメーションは再生されないため、
  A も B も「表情アニメーションがない」という結果は同じである。
  A は assert を黙らせるためだけに説明のつかないレイヤーを 1 枚残す案にすぎない。
- `FaceAnimationPreviewer` は Editor 上で表情を試すためのプレビュー用ユーティリティであり、
  ゲームロジックから呼ばれない。`grep -rn "FaceAnimationPreviewer" Assets --include="*.cs"` の結果、
  本体以外からの参照は 0 件。
- 削除しても瞬きは失われない。`AutoBlinkforSD` は `SkinnedMeshRenderer` のブレンドシェイプで
  動作しており、Animator のレイヤーに依存しない。
- 教材として、「なぜ空のレイヤーがあるのか」の答えが
  「使っていないデモ用スクリプトが assert するから」では筋が通らない。
  説明できない残留物を教材に持ち込まない。
- `m_RemovedComponents` は Prefab Variant の正規の仕組みであり、
  `Assets/UnityChan/` 自体には手を触れないため変更禁止の制約に抵触しない。

### 残すコンポーネント

`SpringBone` / `SpringManager` / `SpringCollider`（髪とスカートの揺れ）、
`SDRandomWind`（風）、`AutoBlinkforSD`（瞬き）、`IKLookAt`（視線）はキャラクターの
見た目に効くため残す。

`BodyAnimationPreviewer`（`BODY_LAYER_INDEX = 0`）は assert では落ちないが、
Controller を差し替えるたびに `OnValidate` が `m_bodyStateNames` を書き換えて
Prefab の差分ノイズになるため、あわせて削除した。

### 実施結果（2026-08-17）

`m_RemovedComponents` に 2 件が入り、コンソールは 0 件になった。
Prefab の再インポートを 2 周繰り返して `OnValidate` を意図的に走らせても再発しない。

```yaml
m_RemovedComponents:
- {fileID: 11400000, guid: 13a16f60da4245c45a865b2136ba272c, type: 3}  # FaceAnimationPreviewer
- {fileID: 11400002, guid: 13a16f60da4245c45a865b2136ba272c, type: 3}  # BodyAnimationPreviewer
```

残存コンポーネントは `AutoBlinkforSD` / `CharacterView` / `IKLookAt` / `SDRandomWind` /
`SpringBone` / `SpringCollider` / `SpringManager` の 7 つで、指定どおり。

`m_Modifications` も意図した override のみに整理された。
`BodyAnimationPreviewer` 削除後も残っていた `m_bodyStateNames` 宛の override
（参照先コンポーネントが存在しないのに残る形）は除去済み。
`Character.controller` は単一レイヤーのまま維持している。

## リスクとロールバック

| リスク | 対応 |
|---|---|
| Step 5 のシーン・Prefab 変更が壊れやすい | YAML を手編集せず MCP 経由で行う。Git で差分を確認してからコミット |
| 既存シーンからの Camera / AudioListener 削除は元に戻しにくい | 利用者の確認を経てから実施 |
| `Assembly-CSharp` 未ビルド時は Editor 作業も止まる | 依存パッケージの復元を先に完了させる（解決済み） |

ロールバックは Git で行う。Step 1 〜 4 の変更はすべてファイル単位で、コミット前であれば
`git checkout` と削除ファイルの復元で戻せる。

## 計画承認

- `[Answer]:` 承認済み（利用者、2026-08-17）。Step 1 〜 4 を実施、Step 5 以降は別途確認のうえ着手。
