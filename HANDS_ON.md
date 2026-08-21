# Unity 講習会 2 回目 ハンズオン

このリポジトリは **同じゲームを 2 通りの設計で作ったもの** です。
ブランチを移動しながら、要件が増えていくとコードがどう変わるかを見ていきます。

## いま開いているブランチ

| | |
|---|---|
| ブランチ | `feat/hell-1` |
| 設計 | **地獄（反面教師）** — 処理と状態が分かれていない |
| 開くシーン | `Assets/Scenes/Title.unity` |

### このブランチのゲーム要件

- フィールドのランダムな位置に現れる 30 枚のコインを全て集めたらクリア

### コードを追ってみよう

- `Assets/Scripts/TitleManager.cs`
- `Assets/Scripts/GameManager.cs`
- `Assets/Scripts/PlayerController.cs`
- `Assets/Scripts/Coin.cs`
- `Assets/Scripts/ResultManager.cs`

答えてみてください。

- このゲームは、どこでクリア判定をしていますか？

## ブランチ一覧

要件は 4 段階で増えていきます。同じ番号どうしが同じ要件です。

| 要件 | 地獄（反面教師） | ピュア（模範） |
|---|---|---|
| ① 30 枚のコインを全て集めたらクリア | `feat/hell-1` | `feat/pure-1` |
| ② ＋ 1 分の制限時間。間に合わなければ失敗 | `feat/hell-2` | `feat/pure-2` |
| ③ ＋ 敵と HP。HP 0 で失敗 | `feat/hell-3` | `feat/pure-3` |
| ④ ＋ 無敵になるスペシャルコイン 3 枚 | `feat/hell-4` | `feat/pure-4` |

```sh
git switch feat/hell-2      # 次の要件へ
git diff feat/hell-1 feat/hell-2   # 要件が増えたぶんの差分
```

## 動かし方

1. Unity Hub からこのプロジェクトを開く（Unity 6000.3.21f1）
2. `Assets/Scenes/Title.unity` を開いて再生
3. スペースキーでスタート、WASD で移動、左 Shift でダッシュ

シーンは Title → Play → Result と `SceneManager.LoadScene` で直接切り替わります。
シーンをまたぐデータは `static` 変数で持ち回しています。

