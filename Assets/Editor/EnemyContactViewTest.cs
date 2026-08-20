using System.Reflection;
using Game.Play;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class EnemyContactViewTest
    {
        private GameObject enemy;
        private GameObject player;
        private GameObject unrelated;

        [TearDown]
        public void TearDown()
        {
            if (enemy != null) Object.DestroyImmediate(enemy);
            if (player != null) Object.DestroyImmediate(player);
            if (unrelated != null) Object.DestroyImmediate(unrelated);
        }

        [Test]
        public void 登録したプレイヤーとの接触だけを通知する()
        {
            enemy = new GameObject("Enemy");
            var trigger = enemy.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            var view = enemy.AddComponent<EnemyContactView>();

            player = new GameObject("Player");
            var playerCollider = player.AddComponent<BoxCollider>();

            unrelated = new GameObject("Unrelated");
            var unrelatedCollider = unrelated.AddComponent<BoxCollider>();

            var contactCount = 0;
            view.Initialize(playerCollider, () => contactCount++);

            // EditMode では物理イベントが発火しないため、Unity が呼ぶ入口を直接実行する。
            var onTriggerEnter = typeof(EnemyContactView).GetMethod(
                "OnTriggerEnter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onTriggerEnter, Is.Not.Null);

            onTriggerEnter.Invoke(view, new object[] { unrelatedCollider });
            Assert.That(contactCount, Is.Zero);

            onTriggerEnter.Invoke(view, new object[] { playerCollider });
            Assert.That(contactCount, Is.EqualTo(1));
        }

        [Test]
        public void 敵プレハブは接触通知に必要な物理構成を持つ()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            Assert.That(prefab, Is.Not.Null);

            var trigger = prefab.GetComponent<CapsuleCollider>();
            Assert.That(trigger, Is.Not.Null);
            Assert.That(trigger.isTrigger, Is.True);
            Assert.That(prefab.GetComponent<EnemyContactView>(), Is.Not.Null);

            // CharacterController は skin 幅ぶん離れて止まるため、その外側までトリガーを広げる。
            var characterController = prefab.GetComponent<CharacterController>();
            Assert.That(characterController, Is.Not.Null);
            Assert.That(
                trigger.radius,
                Is.GreaterThan(characterController.radius + characterController.skinWidth * 2f));

            // Trigger イベントを発生させつつ CharacterController の移動を邪魔しない物理ボディ。
            var rigidbody = prefab.GetComponent<Rigidbody>();
            Assert.That(rigidbody, Is.Not.Null);
            Assert.That(rigidbody.isKinematic, Is.True);
            Assert.That(rigidbody.useGravity, Is.False);
        }
    }
}
