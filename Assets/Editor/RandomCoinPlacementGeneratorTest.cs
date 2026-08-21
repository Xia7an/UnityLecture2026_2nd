using Game.Play.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// コイン配置のテスト。
    ///
    /// 「どこに置くか」を GameObject の生成から切り離したので、
    /// シーンを開かずに配置ルールだけを検証できる。
    /// </summary>
    public sealed class RandomCoinPlacementGeneratorTest
    {
        private static readonly Bounds Field = new(Vector3.zero, new Vector3(20f, 0f, 20f));

        private const float Height = 0.5f;
        private const float MinDistance = 0.8f;

        private static RandomCoinPlacementGenerator CreateGenerator(int seed = 12345)
            => new(new System.Random(seed), Field, Height, MinDistance);

        [Test]
        public void 指定した枚数だけ配置される()
        {
            var positions = CreateGenerator().Generate(30);

            Assert.That(positions.Count, Is.EqualTo(30));
        }

        [Test]
        public void すべてフィールドの内側に配置される()
        {
            var positions = CreateGenerator().Generate(30);

            foreach (var position in positions)
            {
                Assert.That(position.x, Is.InRange(Field.min.x, Field.max.x), $"x が範囲外: {position}");
                Assert.That(position.z, Is.InRange(Field.min.z, Field.max.z), $"z が範囲外: {position}");
            }
        }

        [Test]
        public void 指定した高さに配置される()
        {
            var positions = CreateGenerator().Generate(10);

            foreach (var position in positions)
            {
                Assert.That(position.y, Is.EqualTo(Height).Within(0.0001f));
            }
        }

        [Test]
        public void 同じシードなら同じ配置になる()
        {
            var first = CreateGenerator(seed: 999).Generate(20);
            var second = CreateGenerator(seed: 999).Generate(20);

            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(second[i], Is.EqualTo(first[i]));
            }
        }

        [Test]
        public void 十分な広さがあればコイン同士が最小距離以上離れる()
        {
            var positions = CreateGenerator().Generate(30);

            for (var i = 0; i < positions.Count; i++)
            {
                for (var j = i + 1; j < positions.Count; j++)
                {
                    var distance = Vector3.Distance(positions[i], positions[j]);

                    // 浮動小数点の誤差を許容するため、しきい値をわずかに緩める。
                    Assert.That(distance, Is.GreaterThanOrEqualTo(MinDistance - 0.0001f),
                        $"{i} と {j} が近すぎる: {distance}");
                }
            }
        }

        [Test]
        public void 狭すぎるフィールドでも枚数どおり返り無限ループしない()
        {
            // 1x1 のフィールドに最小距離 0.8 で 30 枚は物理的に置けない。
            // 諦めて重なりを許容し、必ず指定枚数を返すことを確認する。
            var narrow = new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));
            var generator = new RandomCoinPlacementGenerator(
                new System.Random(1), narrow, Height, MinDistance);

            var positions = generator.Generate(30);

            Assert.That(positions.Count, Is.EqualTo(30));
        }

        [Test]
        public void 枚数がゼロなら空になる()
        {
            var positions = CreateGenerator().Generate(0);

            Assert.That(positions, Is.Empty);
        }
    }
}
