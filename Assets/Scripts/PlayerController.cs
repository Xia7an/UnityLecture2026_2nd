using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float dashSpeed = 6f;
    public Animator animator;

    private CharacterController cc;

    // GameManager が持っているのと同じコインの枚数。表示以外にも使うのでこっちにも持っておく
    private int myCoinCount = 0;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 時間切れになったらもう動かさない
        if (GameManager.instance != null && GameManager.instance.timeUp == true)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        var kb = Keyboard.current;

        float x = 0f;
        float z = 0f;
        if (kb != null)
        {
            if (kb.wKey.isPressed) z = z + 1f;
            if (kb.sKey.isPressed) z = z - 1f;
            if (kb.dKey.isPressed) x = x + 1f;
            if (kb.aKey.isPressed) x = x - 1f;
        }

        Vector3 dir = new Vector3(x, 0f, z);
        if (dir.sqrMagnitude > 1f) dir = dir.normalized;

        // シフトでダッシュ
        float speed = walkSpeed;
        if (kb != null && kb.leftShiftKey.isPressed)
        {
            speed = dashSpeed;
        }

        Vector3 velocity = dir * speed;

        if (cc != null)
        {
            cc.Move(velocity * Time.deltaTime + new Vector3(0f, -9.8f, 0f) * Time.deltaTime);
        }

        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", velocity.magnitude);
        }

        // GameManager 側の枚数をこっちにも写しておく
        if (GameManager.instance != null)
        {
            myCoinCount = GameManager.instance.coinCount;
        }
    }

    // Coin.cs から呼ばれる
    public void AddCoin()
    {
        myCoinCount = myCoinCount + 1;
    }
}
