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
        [SerializeField] private float enemySpeed = 2f;
        [SerializeField] private float enemyDirectionChangeInterval = 1.5f;

        [Header("フィールド")]
        [Tooltip("Play シーンの Ground に合わせる。既定値は 5x5 の Quad（原点中心）を想定。")]
        [SerializeField] private Vector3 fieldCenter = Vector3.zero;
        [SerializeField] private Vector3 fieldSize = new(5f, 0f, 5f);

        /// <summary>歩行速度。</summary>
        public float WalkSpeed => walkSpeed;

        /// <summary>ダッシュ中の速度。</summary>
        public float DashSpeed => dashSpeed;

        /// <summary>敵の移動速度。</summary>
        public float EnemySpeed => enemySpeed;

        /// <summary>敵が進行方向を変える間隔（秒）。</summary>
        public float EnemyDirectionChangeInterval => enemyDirectionChangeInterval;

        /// <summary>キャラクターが動ける範囲。</summary>
        public Bounds FieldBounds => new(fieldCenter, fieldSize);
    }
}
