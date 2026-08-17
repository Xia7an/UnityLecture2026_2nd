using System;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// シーン上のコイン 1 枚。プレイヤーが触れたら通知して自分を消す。
    ///
    /// このクラスは自分が通常コインなのか特殊コインなのかを知らない。
    /// 「触れられたら渡されたコールバックを呼ぶ」だけを担当し、
    /// 取得したときに何が起きるか（枚数を増やす／無敵にする）は
    /// PlayCompositionRoot が組み立てるときに決める。
    ///
    /// CharacterView と同じく、Initialize されるまで動かない。
    /// </summary>
    public sealed class CoinView : MonoBehaviour
    {
        [Tooltip("プレイヤーとの接触を検出するトリガー。isTrigger を有効にしておくこと。")]
        [SerializeField] private Collider triggerCollider;

        private Collider playerCollider;
        private Action onCollected;
        private bool collected;

        private void Awake()
        {
            enabled = false;

            if (triggerCollider == null) triggerCollider = GetComponent<Collider>();
        }

        /// <param name="playerCollider">
        /// プレイヤーの当たり判定。タグや名前で判定せず、実物の参照を渡して比較する。
        /// どのオブジェクトを「プレイヤー」とみなすかを、組み立て役が明示的に決められる。
        /// </param>
        /// <param name="onCollected">取得されたときに呼ぶ処理。</param>
        public void Initialize(Collider playerCollider, Action onCollected)
        {
            this.playerCollider = playerCollider
                ?? throw new ArgumentNullException(nameof(playerCollider));
            this.onCollected = onCollected
                ?? throw new ArgumentNullException(nameof(onCollected));

            enabled = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 同じフレームに二重で取得されないようにする。
            if (collected) return;
            if (other != playerCollider) return;

            collected = true;
            onCollected();

            Destroy(gameObject);
        }
    }
}
