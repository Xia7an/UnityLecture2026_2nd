using System;
using System.Collections.Generic;
using UnityEngine;

// UnityEngine.Random と紛れるので、このファイルでは Random = System.Random とする。
using Random = System.Random;

namespace Game.Play.Core
{
    /// <summary>
    /// フィールド内のランダムな位置にコインを配置する。
    ///
    /// できるだけコイン同士が重ならないよう、既に置いた位置から一定距離を離す。
    /// ただし枚数に対してフィールドが狭いと離しきれないので、
    /// 一定回数試して駄目なら諦めてその位置に置く。無限ループにはならない。
    ///
    /// UnityEngine.Random ではなく System.Random を使っているのは、
    /// シードを外から与えればテストで結果を再現できるようにするためである。
    /// </summary>
    public sealed class RandomCoinPlacementGenerator : ICoinPlacementGenerator
    {
        /// <summary>1 枚あたりの位置決めの試行上限。これを超えたら重なりを許容する。</summary>
        private const int MaxAttemptsPerCoin = 50;

        private readonly Random random;
        private readonly Bounds fieldBounds;
        private readonly float height;
        private readonly float minDistance;

        /// <param name="random">乱数。テストではシード固定のものを渡す。</param>
        /// <param name="fieldBounds">コインを置ける範囲。</param>
        /// <param name="height">コインを浮かせる高さ（ワールド Y）。</param>
        /// <param name="minDistance">コイン同士を離したい最小距離。</param>
        public RandomCoinPlacementGenerator(
            Random random,
            Bounds fieldBounds,
            float height,
            float minDistance)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.fieldBounds = fieldBounds;
            this.height = height;
            this.minDistance = Mathf.Max(0f, minDistance);
        }

        public IReadOnlyList<CoinPlacement> Generate(int totalCount, int specialCount)
        {
            if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount));
            if (specialCount < 0) throw new ArgumentOutOfRangeException(nameof(specialCount));

            // 特殊コインが総枚数を超えないよう丸める。
            specialCount = Mathf.Min(specialCount, totalCount);

            var kinds = CreateShuffledKinds(totalCount, specialCount);
            var placements = new List<CoinPlacement>(totalCount);
            var positions = new List<Vector3>(totalCount);

            for (var i = 0; i < totalCount; i++)
            {
                var position = PickPosition(positions);
                positions.Add(position);
                placements.Add(new CoinPlacement(kinds[i], position));
            }

            return placements;
        }

        /// <summary>種類の配列を作ってシャッフルする。特殊コインが偏らないようにするため。</summary>
        private CoinKind[] CreateShuffledKinds(int totalCount, int specialCount)
        {
            var kinds = new CoinKind[totalCount];
            for (var i = 0; i < totalCount; i++)
            {
                kinds[i] = i < specialCount ? CoinKind.Special : CoinKind.Normal;
            }

            // Fisher-Yates
            for (var i = totalCount - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (kinds[i], kinds[j]) = (kinds[j], kinds[i]);
            }

            return kinds;
        }

        private Vector3 PickPosition(List<Vector3> existing)
        {
            var candidate = Vector3.zero;

            for (var attempt = 0; attempt < MaxAttemptsPerCoin; attempt++)
            {
                candidate = RandomPointInField();
                if (IsFarEnough(candidate, existing)) return candidate;
            }

            // 置ききれなかった場合は最後の候補をそのまま使う。
            // 枚数に対してフィールドが狭いときに起こる。
            return candidate;
        }

        private Vector3 RandomPointInField()
        {
            var x = Lerp(fieldBounds.min.x, fieldBounds.max.x, (float)random.NextDouble());
            var z = Lerp(fieldBounds.min.z, fieldBounds.max.z, (float)random.NextDouble());
            return new Vector3(x, height, z);
        }

        private bool IsFarEnough(Vector3 candidate, List<Vector3> existing)
        {
            if (minDistance <= 0f) return true;

            var sqrMinDistance = minDistance * minDistance;
            foreach (var position in existing)
            {
                if ((position - candidate).sqrMagnitude < sqrMinDistance) return false;
            }

            return true;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
