using System;
using Game.Play.Core;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// Play.inputactions から生成された PlayInput を包み、Core 層の IPlayerInput として見せる。
    ///
    /// MonoBehaviour ではない点に注目してほしい。Input System は Pure C# から普通に使える。
    /// 「Unity の機能を使う＝MonoBehaviour を継承する」ではない。
    ///
    /// 入力アセットはシーンごとに分かれており、この実体は Play シーンの CompositionRoot が
    /// 生成して破棄する。生存期間がシーンの生存期間と一致するため、
    /// 「今どのアクションマップが有効か」というシーンをまたぐ状態を持たずに済む。
    /// </summary>
    public sealed class PlayerInputAdapter : IPlayerInput, IDisposable
    {
        private readonly PlayInput input;

        public PlayerInputAdapter(PlayInput input)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.input.Play.Enable();
        }

        public Vector2 MoveDirection
        {
            get
            {
                var raw = input.Play.Move.ReadValue<Vector2>();

                // 斜め入力で速くならないよう、長さが 1 を超えるときだけ正規化する。
                return raw.sqrMagnitude > 1f ? raw.normalized : raw;
            }
        }

        public bool IsDashing => input.Play.Dash.IsPressed();

        public void Dispose()
        {
            input.Play.Disable();
            input.Dispose();
        }
    }
}
