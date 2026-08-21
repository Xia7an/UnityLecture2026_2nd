using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// Play シーンの調整値。移動速度やフィールドの広さなど、シーン内で完結する設定を持つ。
    /// シーンをまたぐゲーム状態の既定値は GameStateSettings 側にある。
    /// </summary>
    [CreateAssetMenu(fileName = "PlaySettings", menuName = "Game/Play Settings")]
    public sealed class PlaySettings : ScriptableObject
    {
        [Header("プレイヤー")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float dashSpeed = 6f;

        [Header("敵")]
        [Tooltip("ステージ上に生成する敵の数。")]
        [SerializeField] private int enemyCount = 5;

        [SerializeField] private float enemySpeed = 2f;
        [SerializeField] private float enemyDirectionChangeInterval = 1.5f;

        [Header("フィールド")]
        [Tooltip("Play シーンの Ground に合わせる。既定値は 20x20 の Quad（原点中心）を想定。")]
        [SerializeField] private Vector3 fieldCenter = Vector3.zero;
        [SerializeField] private Vector3 fieldSize = new(20f, 0f, 20f);

        [Header("コイン")]
        [Tooltip("コインを浮かせる高さ。プレハブの見た目に合わせる。")]
        [SerializeField] private float coinHeight = 0.5f;

        [Tooltip("コイン同士を離したい最小距離。枚数に対して狭すぎると重なりを許容する。")]
        [SerializeField] private float coinMinDistance = 0.8f;

        [Tooltip("コインの配置に使う乱数のシード。0 なら毎回変わる。")]
        [SerializeField] private int coinRandomSeed;

        /// <summary>歩行速度。</summary>
        public float WalkSpeed => walkSpeed;

        /// <summary>ダッシュ中の速度。</summary>
        public float DashSpeed => dashSpeed;

        /// <summary>ステージ上に生成する敵の数。</summary>
        public int EnemyCount => enemyCount;

        /// <summary>敵の移動速度。</summary>
        public float EnemySpeed => enemySpeed;

        /// <summary>敵が進行方向を変える間隔（秒）。</summary>
        public float EnemyDirectionChangeInterval => enemyDirectionChangeInterval;

        /// <summary>キャラクターが動ける範囲。</summary>
        public Bounds FieldBounds => new(fieldCenter, fieldSize);

        /// <summary>コインを浮かせる高さ。</summary>
        public float CoinHeight => coinHeight;

        /// <summary>コイン同士を離したい最小距離。</summary>
        public float CoinMinDistance => coinMinDistance;

        /// <summary>
        /// コイン配置に使う乱数を作る。
        /// シードが 0 なら毎回異なる配置、0 以外なら毎回同じ配置になる。
        /// 講習中に配置を固定したいときはシードを設定する。
        /// </summary>
        public System.Random CreateCoinRandom()
            => coinRandomSeed == 0 ? new System.Random() : new System.Random(coinRandomSeed);
    }
}
