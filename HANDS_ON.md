# Unity 講習会 2 回目 ハンズオン

このリポジトリは **同じゲームを 2 通りの設計で作ったもの** です。
ブランチを移動しながら、要件が増えていくとコードがどう変わるかを見ていきます。

## いま開いているブランチ

| | |
|---|---|
| ブランチ | `feat/pure-1` |
| 設計 | **ピュア（模範）** — 状態と純粋関数を分ける |
| 開くシーン | `Assets/Scenes/Root.unity` |

### このブランチのゲーム要件

- フィールドのランダムな位置に現れる 30 枚のコインを全て集めたらクリア

### 注目してほしいコード

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/Core/GameState.cs` | **状態**。値を持つフィールドと、それを書き換えるメソッドしかない |
| `Assets/Scripts/Core/GameOutcome.cs` | **純粋関数**。`Evaluate` が状態から決着状況を導出する |
| `Assets/Scripts/Core/IGameStateSettings.cs` | **設定**。変化しない値はこちら |
| `Assets/Scripts/Play/Unity/PlayCompositionRoot.cs` | **組み立て役**。誰と誰をつなぐかだけを決める |
| `Assets/Scripts/Play/Unity/CoinView.cs` | **View**。触れたら渡された処理を呼ぶだけ。コイン枚数を知らない |

`GameState` に「クリアしたかどうか」というフィールドが **無い** ことを確かめてください。
決着状況はコイン枚数から毎回導出しています。

## ブランチ一覧

要件は 4 段階で増えていきます。同じ番号どうしが同じ要件です。

| 要件 | 地獄（反面教師） | ピュア（模範） |
|---|---|---|
| ① 30 枚のコインを全て集めたらクリア | `feat/hell-1` | `feat/pure-1` |
| ② ＋ 1 分の制限時間。間に合わなければ失敗 | `feat/hell-2` | `feat/pure-2` |
| ③ ＋ 敵と HP。HP 0 で失敗 | `feat/hell-3` | `feat/pure-3` |
| ④ ＋ 無敵になるスペシャルコイン 3 枚 | `feat/hell-4` | `feat/pure-4` |

```sh
git switch feat/pure-2      # 次の要件へ
git diff feat/pure-1 feat/pure-2   # 要件が増えたぶんの差分だけが見える
```

## 動かし方

1. Unity Hub からこのプロジェクトを開く（Unity 6000.3.21f1）
2. `Assets/Scenes/Root.unity` を開いて再生
3. WASD で移動、左 Shift でダッシュ

Play シーンを単独で開いたまま `Root.unity` を Additive で開いて再生すると、
Title を経由せずにそのシーンだけを確かめられます。
