using Game.Play.Core;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// シーン上のキャラクターの見た目を制御する。
    ///
    /// このクラスは「速度を受け取って Transform と Animator に反映する」しか知らない。
    /// 渡された移動ロジックがプレイヤーの入力によるものか、敵のランダムウォークかも知らない。
    /// そのためプレイヤーにも敵にも、同じこのコンポーネントを使える。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterView : MonoBehaviour, ICharacterView
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] private CharacterController characterController;

        [Tooltip("未設定でも動作します。設定した場合は Speed という float パラメータを更新します。")]
        [SerializeField] private Animator animator;

        private ICharacterMoveLogic moveLogic;

        public Vector3 Position => transform.position;

        /// <summary>
        /// Initialize が呼ばれるまで動かない。
        ///
        /// Additive でロードしたシーンのオブジェクトは、GameRoot が Initialize を呼ぶより
        /// 先に Awake が走る。enabled で制御しておくことで、Update の中で毎フレーム
        /// null を確かめる必要がなくなり、「初期化されていない状態」を持たずに済む。
        /// </summary>
        private void Awake()
        {
            enabled = false;

            if (characterController == null) characterController = GetComponent<CharacterController>();
        }

        public void Initialize(ICharacterMoveLogic moveLogic)
        {
            this.moveLogic = moveLogic;
            enabled = true;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var velocity = moveLogic.EvaluateVelocity(Position, deltaTime);

            characterController.Move(velocity * deltaTime);

            if (velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(velocity);
            }

            if (animator != null) animator.SetFloat(SpeedHash, velocity.magnitude);
        }
    }
}
