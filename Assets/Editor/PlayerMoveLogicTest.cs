using Game.Play.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// PlayerMoveLogic のテスト。
    ///
    /// 注目してほしいのは、このテストがシーンも GameObject も使っていないことである。
    /// 移動ロジックを MonoBehaviour から切り離し、入力を IPlayerInput という
    /// インターフェース越しに受け取る形にしたので、スタブを渡すだけで検証できる。
    ///
    /// これが「なぜコアロジックを Pure C# に置くのか」に対する一番はっきりした答えになる。
    /// </summary>
    public sealed class PlayerMoveLogicTest
    {
        private const float WalkSpeed = 3f;
        private const float DashSpeed = 6f;

        /// <summary>テスト用の入力。実物の代わりにこれを渡す。</summary>
        private sealed class StubPlayerInput : IPlayerInput
        {
            public Vector2 MoveDirection { get; set; }
            public bool IsDashing { get; set; }
        }

        private static PlayerMoveLogic CreateLogic(StubPlayerInput input)
            => new(input, WalkSpeed, DashSpeed);

        [Test]
        public void 入力がなければ速度はゼロ()
        {
            var input = new StubPlayerInput { MoveDirection = Vector2.zero };
            var logic = CreateLogic(input);

            var velocity = logic.EvaluateVelocity(Vector3.zero, 0.016f);

            Assert.That(velocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void 前入力はZ軸プラス方向へ歩行速度で進む()
        {
            var input = new StubPlayerInput { MoveDirection = Vector2.up };
            var logic = CreateLogic(input);

            var velocity = logic.EvaluateVelocity(Vector3.zero, 0.016f);

            Assert.That(velocity.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(velocity.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(velocity.z, Is.EqualTo(WalkSpeed).Within(0.0001f));
        }

        [Test]
        public void 右入力はX軸プラス方向へ進む()
        {
            var input = new StubPlayerInput { MoveDirection = Vector2.right };
            var logic = CreateLogic(input);

            var velocity = logic.EvaluateVelocity(Vector3.zero, 0.016f);

            Assert.That(velocity.x, Is.EqualTo(WalkSpeed).Within(0.0001f));
            Assert.That(velocity.z, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ダッシュ中は走行速度になる()
        {
            var input = new StubPlayerInput { MoveDirection = Vector2.up, IsDashing = true };
            var logic = CreateLogic(input);

            var velocity = logic.EvaluateVelocity(Vector3.zero, 0.016f);

            Assert.That(velocity.magnitude, Is.EqualTo(DashSpeed).Within(0.0001f));
        }

        [Test]
        public void 移動は常に水平面上でY成分を持たない()
        {
            var input = new StubPlayerInput { MoveDirection = new Vector2(0.5f, -0.5f) };
            var logic = CreateLogic(input);

            var velocity = logic.EvaluateVelocity(new Vector3(1f, 5f, 2f), 0.016f);

            Assert.That(velocity.y, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
