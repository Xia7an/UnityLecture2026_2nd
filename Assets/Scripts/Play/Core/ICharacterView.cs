using UnityEngine;

namespace Game.Play.Core
{
    /// <summary>
    /// シーン上のキャラクターの見た目。Core 層が MonoBehaviour を知らずに
    /// 現在位置を問い合わせられるようにするための境界。
    /// </summary>
    public interface ICharacterView
    {
        /// <summary>ワールド座標での現在位置。</summary>
        Vector3 Position { get; }
    }
}
