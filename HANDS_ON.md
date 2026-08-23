# Unity 講習会 2 回目 ハンズオン

このリポジトリは **同じゲームを 2 通りの設計で作ったもの** です。
ブランチを移動しながら、要件が増えていくとコードがどう変わるかを見ていきます。

## いま開いているブランチ

| | |
|---|---|
| ブランチ | `feat/hell-4` |
| 設計 | **素朴な実装** — 各 `MonoBehaviour` が状態・判定・表示を直接参照する |
| 開くシーン | `Assets/Scenes/Title.unity` |

### このブランチのゲーム要件

- フィールドのランダムな位置に現れる 30 枚のコインを、1 分以内に全て集めたらクリア
- 集められなかったら失敗
- 敵（デカい猫）が現れる。自機の HP は初期値 100。敵に衝突したら HP が 10 減る。HP が 0 になったら失敗
- **拾うと一定時間無敵になる「スペシャルコイン」が、全 30 枚のうち 3 枚出現する**

### コードを追ってみよう

- `Assets/Scripts/TitleManager.cs`
- `Assets/Scripts/GameManager.cs`
- `Assets/Scripts/PlayerController.cs`
- `Assets/Scripts/Coin.cs`
- `Assets/Scripts/Enemy.cs`
- `Assets/Scripts/ResultManager.cs`

答えてみてください。

- **いま無敵ですか？** どの状態から判断していますか？
- その状態を読み書きするクラスと、HP ゲージの色を更新するクラスを挙げてください。
- **このゲームは、どういうときに終わりますか？**

## ブランチ一覧

要件は 4 段階で増えていきます。同じ番号どうしが同じ要件です。

| 要件 | 素朴な実装 | 責務を分けた実装 |
|---|---|---|
| ① 30 枚のコインを全て集めたらクリア | `feat/hell-1` | `feat/pure-1` |
| ② ＋ 1 分の制限時間。間に合わなければ失敗 | `feat/hell-2` | `feat/pure-2` |
| ③ ＋ 敵と HP。HP 0 で失敗 | `feat/hell-3` | `feat/pure-3` |
| ④ ＋ 無敵になるスペシャルコイン 3 枚 | `feat/hell-4` | `feat/pure-4` |

```sh
git diff feat/hell-3 feat/hell-4   # 要件④で増えたぶんの差分
```

## 動かし方

1. Unity Hub からこのプロジェクトを開く（Unity 6000.3.21f1）
2. `Assets/Scenes/Title.unity` を開いて再生
3. スペースキーでスタート、WASD で移動、左 Shift でダッシュ

シーンは Title → Play → Result と `SceneManager.LoadScene` で直接切り替わります。
シーンをまたぐデータは `static` 変数で持ち回しています。

