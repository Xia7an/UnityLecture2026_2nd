using System;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// 敵の接触判定を Unity の物理イベントからゲーム上の出来事へ変換する。
    ///
    /// このクラスは HP やダメージ量を知らない。登録されたプレイヤーに触れたら、
    /// 組み立て時に渡された処理を 1 回呼ぶだけである。
    /// 接触相手はタグや名前ではなく Collider の実物で判定するため、
    /// 何をプレイヤーとみなすかは PlayCompositionRoot が明示的に決められる。
    /// </summary>
    public sealed class EnemyContactView : MonoBehaviour
    {
        [Tooltip("プレイヤーとの接触を検出するトリガー。isTrigger を有効にしておくこと。")]
        [SerializeField] private Collider triggerCollider;

        private Collider playerCollider;
        private Action onPlayerContact;

        private void Awake()
        {
            FindTriggerColliderIfNeeded();

            // GameRoot から依存が渡されるより先に物理イベントを受けないようにする。
            if (triggerCollider != null) triggerCollider.enabled = false;
            enabled = false;
        }

        /// <param name="playerCollider">
        /// プレイヤーの当たり判定。タグや名前ではなく実物の参照を比較する。
        /// </param>
        /// <param name="onPlayerContact">プレイヤーとの接触開始時に呼ぶ処理。</param>
        public void Initialize(Collider playerCollider, Action onPlayerContact)
        {
            this.playerCollider = playerCollider
                ?? throw new ArgumentNullException(nameof(playerCollider));
            this.onPlayerContact = onPlayerContact
                ?? throw new ArgumentNullException(nameof(onPlayerContact));

            FindTriggerColliderIfNeeded();

            if (triggerCollider == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(EnemyContactView)} の {nameof(triggerCollider)} が設定されていません。");
            }

            if (!triggerCollider.isTrigger)
            {
                throw new InvalidOperationException(
                    $"{nameof(EnemyContactView)} の {nameof(triggerCollider)} は isTrigger を有効にしてください。");
            }

            triggerCollider.enabled = true;
            enabled = true;
        }

        private void FindTriggerColliderIfNeeded()
        {
            if (triggerCollider != null) return;

            foreach (var candidate in GetComponents<Collider>())
            {
                if (!candidate.isTrigger) continue;

                triggerCollider = candidate;
                return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != playerCollider) return;

            onPlayerContact();
        }
    }
}
