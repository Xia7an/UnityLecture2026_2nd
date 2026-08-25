using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float dashSpeed = 6f;
    [SerializeField] private Animator animator;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;

        float x = 0f;
        float z = 0f;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) z += 1f;
            if (keyboard.sKey.isPressed) z -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.aKey.isPressed) x -= 1f;
        }

        var direction = new Vector3(x, 0f, z);
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        // シフトでダッシュ
        var isDashing = keyboard != null && keyboard.leftShiftKey.isPressed;
        var speed = isDashing ? dashSpeed : walkSpeed;
        var velocity = direction * speed;

        if (characterController != null)
        {
            var gravity = Physics.gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime + gravity);
        }

        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", velocity.magnitude);
        }
    }
}
