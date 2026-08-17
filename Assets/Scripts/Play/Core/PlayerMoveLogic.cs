using System;
using UnityEngine;

namespace Game.Play.Core
{
    /// <summary>
    /// 入力に従ってキャラクターを動かす移動ロジック。
    ///
    /// MonoBehaviour ではない普通の C# クラスであり、依存はすべてコンストラクタで受け取る。
    /// 何に依存しているかがコンストラクタの引数に現れているため、
    /// このクラスを読むだけで必要なものが分かる。
    /// </summary>
    public sealed class PlayerMoveLogic : ICharacterMoveLogic
    {
        private readonly IPlayerInput input;
        private readonly float walkSpeed;
        private readonly float dashSpeed;

        public PlayerMoveLogic(IPlayerInput input, float walkSpeed, float dashSpeed)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.walkSpeed = walkSpeed;
            this.dashSpeed = dashSpeed;
        }

        public Vector3 EvaluateVelocity(Vector3 currentPosition, float deltaTime)
        {
            var direction = input.MoveDirection;
            var speed = input.IsDashing ? dashSpeed : walkSpeed;

            // 入力の x / y をフィールド平面の x / z に割り当てる。
            return new Vector3(direction.x, 0f, direction.y) * speed;
        }
    }
}
