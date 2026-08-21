# Unity 講習会 2 回目 ハンズオン

このリポジトリは **同じゲームを 2 通りの設計で作ったもの** です。
ブランチを移動しながら、要件が増えていくとコードがどう変わるかを見ていきます。

## いま開いているブランチ

| | |
|---|---|
| ブランチ | `feat/pure-4` |
| 設計 | **ピュア（模範）** — 状態と純粋関数を分ける |
| 開くシーン | `Assets/Scenes/Root.unity` |

### このブランチのゲーム要件

- フィールドのランダムな位置に現れる 30 枚のコインを、1 分以内に全て集めたらクリア
- 集められなかったら失敗
- 敵（デカい猫）が現れる。自機の HP は初期値 100。敵に衝突したら HP が 10 減る。HP が 0 になったら失敗
- **拾うと一定時間無敵になる「スペシャルコイン」が、全 30 枚のうち 3 枚出現する**

### 注目してほしいコード

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/Core/GameState.cs` | **状態**。値を持つフィールドと、それを書き換えるメソッドしかない |
| `Assets/Scripts/Core/GameOutcome.cs` | **純粋関数**。`Evaluate` が状態から決着状況を導出する |
| `Assets/Scripts/Core/IGameStateSettings.cs` | **設定**。変化しない値はこちら |
| `Assets/Scripts/Play/Unity/PlayCompositionRoot.cs` | **組み立て役**。誰と誰をつなぐかだけを決める |
| `Assets/Scripts/Play/Unity/CoinView.cs` | **View**。触れたら渡された処理を呼ぶだけ。コイン枚数を知らない |
| `Assets/Scripts/Play/Unity/EnemyContactView.cs` | **View**。触れたら渡された処理を呼ぶだけ。HP もダメージ量も知らない |
| `Assets/Scripts/Core/GameStateQueries.cs` | **純粋関数**。状態と設定から表示用の値（HP 割合）を導出する |

`GameState` に「クリアしたかどうか」「なぜ終わったか」というフィールドが **無い** ことを
確かめてください。決着状況は HP・残り時間・コイン枚数から毎回導出しています。

同じく `IsInvincible` もフィールドではありません。
`InvincibleRemainingSeconds > 0` から導出するプロパティです。

> **導出できる値は、状態として持たない。**
> 二重に持つと、片方だけ更新し忘れたときに矛盾するからです。

### 前のブランチからの差分

```sh
git diff feat/pure-3 feat/pure-4
```

- `GameState` に `InvincibleRemainingSeconds` と `CollectSpecialCoin` が増えた
- `Tick` が無敵時間も減らすようになった
- `ApplyEnemyHit` の先頭に `if (IsInvincible) return;` が 1 行増えた
- `CoinView` は **変わっていません**。自分が通常コインか特殊コインかを知らないまま、
  `PlayCompositionRoot` が渡す処理が変わっただけです

`GameOutcomeEvaluator.Evaluate` は **1 行も変わっていません**。
無敵はゲームの終了条件ではないからです。要件の差分と、コードの差分が一致します。

### 変化する値と、変化しない値

| | どこに置くか | 例 |
|---|---|---|
| 変化する値 = **状態** | `GameState` | 残り HP、残り時間、取得済みコイン枚数、無敵の残り時間 |
| 変化しない値 = **設定** | `IGameStateSettings` | 最大 HP、制限時間、コイン枚数、1 ヒットの被ダメージ量、無敵時間の長さ |

「最大 HP」は変化しないので `GameState` にはありません。
だから HP バーの割合は状態だけでは求められず、`GameStateQueries.HpRatio` が
状態と設定の **両方** を引数に取ります。

### 純粋関数はテストできる

`Assets/Editor/GameOutcomeTest.cs` を開いてください。
`Evaluate` は副作用のない純粋関数なので、シーンを 1 つも開かずに終了条件を検証できます。
Unity のメニュー `Window > General > Test Runner` の EditMode から実行できます。

## ブランチ一覧

要件は 4 段階で増えていきます。同じ番号どうしが同じ要件です。

| 要件 | 地獄（反面教師） | ピュア（模範） |
|---|---|---|
| ① 30 枚のコインを全て集めたらクリア | `feat/hell-1` | `feat/pure-1` |
| ② ＋ 1 分の制限時間。間に合わなければ失敗 | `feat/hell-2` | `feat/pure-2` |
| ③ ＋ 敵と HP。HP 0 で失敗 | `feat/hell-3` | `feat/pure-3` |
| ④ ＋ 無敵になるスペシャルコイン 3 枚 | `feat/hell-4` | `feat/pure-4` |

```sh
git switch feat/hell-3      # 同じ要件を「地獄」側で見る
git diff feat/pure-3 feat/pure-4   # 要件が増えたぶんの差分だけが見える
```

## 動かし方

1. Unity Hub からこのプロジェクトを開く（Unity 6000.3.21f1）
2. `Assets/Scenes/Root.unity` を開いて再生
3. WASD で移動、左 Shift でダッシュ

Play シーンを単独で開いたまま `Root.unity` を Additive で開いて再生すると、
Title を経由せずにそのシーンだけを確かめられます。
