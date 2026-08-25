using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Enemy : MonoBehaviour
{
    private const float DirectionChangeInterval = 1.5f;

    [SerializeField] private float speed = 2f;
    [SerializeField] private Animator animator;

    private CharacterController characterController;
    private Vector3 direction;
    private float directionChangeTimer;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        ChooseRandomDirection();
    }

    private void Update()
    {
        directionChangeTimer += Time.deltaTime;
        if (directionChangeTimer >= DirectionChangeInterval)
        {
            ChooseRandomDirection();
            directionChangeTimer = 0f;
        }

        var nextPosition = transform.position + direction * speed * Time.deltaTime;
        if (nextPosition.x < -10f || nextPosition.x > 10f) direction.x = -direction.x;
        if (nextPosition.z < -10f || nextPosition.z > 10f) direction.z = -direction.z;

        var velocity = direction * speed;
        if (characterController != null)
        {
            var gravity = Physics.gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime + gravity);
        }
        else
        {
            transform.position += velocity * Time.deltaTime;
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

    private void ChooseRandomDirection()
    {
        var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.IsInvincible) return;

        // HP は Enemy から直接書き換えるが、減算と下限処理は 1 回の代入で行う。
        gameManager.hp = Mathf.Max(0, gameManager.hp - GameManager.DamagePerHit);
        gameManager.RefreshHpDisplay();

        if (gameManager.hp > 0) return;

        GameManager.IsClear = false;
        SceneManager.LoadScene("Result");
    }
}
