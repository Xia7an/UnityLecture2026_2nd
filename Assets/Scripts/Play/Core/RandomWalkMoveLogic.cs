using System;
using UnityEngine;

// UnityEngine.Random と紛れるので、このファイルでは Random = System.Random とする。
using Random = System.Random;

namespace Game.Play.Core
{
    /// <summary>
    /// 一定間隔で向きを変えながら歩き回る移動ロジック。敵キャラクターの既定の動きにあたる。
    ///
    /// PlayerMoveLogic と同じ ICharacterMoveLogic を実装しているので、
    /// CompositionRoot で渡す実装を差し替えるだけで、プレイヤーをこの動きにもできる。
    /// 講習後半の「移動ロジックの差し替え」は、このクラスの隣に実装を 1 つ足す作業になる。
    ///
    /// UnityEngine.Random ではなく System.Random を使っているのは、
    /// シードを外から与えればテストで結果を再現できるようにするためである。
    /// </summary>
    public sealed class RandomWalkMoveLogic : ICharacterMoveLogic
    {
        private readonly Random random;
        private readonly float speed;
        private readonly float directionChangeInterval;
        private readonly Bounds fieldBounds;

        private Vector3 direction;
        private float elapsedSinceDirectionChange;

        public RandomWalkMoveLogic(
            Random random,
            float speed,
            float directionChangeInterval,
            Bounds fieldBounds)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.speed = speed;
            this.directionChangeInterval = Mathf.Max(0.01f, directionChangeInterval);
            this.fieldBounds = fieldBounds;

            direction = PickRandomDirection();
            elapsedSinceDirectionChange = 0f;
        }

        public Vector3 EvaluateVelocity(Vector3 currentPosition, float deltaTime)
        {
            elapsedSinceDirectionChange += deltaTime;

            if (elapsedSinceDirectionChange >= directionChangeInterval)
            {
                direction = PickRandomDirection();
                elapsedSinceDirectionChange = 0f;
            }

            // フィールドの外へ出ようとしていたら向きを内側へ折り返す。
            var next = currentPosition + direction * (speed * deltaTime);
            if (next.x < fieldBounds.min.x || next.x > fieldBounds.max.x) direction.x = -direction.x;
            if (next.z < fieldBounds.min.z || next.z > fieldBounds.max.z) direction.z = -direction.z;

            return direction * speed;
        }

        private Vector3 PickRandomDirection()
        {
            var angle = (float)(random.NextDouble() * Math.PI * 2.0);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }
}
