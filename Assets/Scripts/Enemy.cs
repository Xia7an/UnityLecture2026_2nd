using UnityEngine;
using UnityEngine.SceneManagement;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;
    public Animator animator;

    private CharacterController cc;
    private Vector3 dir;
    private float timer = 0f;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        // 適当な方向を向かせる
        float ang = Random.Range(0f, 360f);
        dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0f, Mathf.Sin(ang * Mathf.Deg2Rad));
    }

    void Update()
    {
        // 1.5秒たったら向きを変える
        timer = timer + Time.deltaTime;
        if (timer > 1.5f)
        {
            float ang = Random.Range(0f, 360f);
            dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0f, Mathf.Sin(ang * Mathf.Deg2Rad));
            timer = 0f;
        }

        // フィールドの外に出そうだったら跳ね返す
        Vector3 next = transform.position + dir * speed * Time.deltaTime;
        if (next.x < -10f || next.x > 10f) dir.x = -dir.x;
        if (next.z < -10f || next.z > 10f) dir.z = -dir.z;

        Vector3 v = dir * speed;

        if (cc != null)
        {
            cc.Move(v * Time.deltaTime + new Vector3(0f, -9.8f, 0f) * Time.deltaTime);
        }
        else
        {
            transform.position = transform.position + v * Time.deltaTime;
        }

        if (v.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(v);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", v.magnitude);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.instance == null) return;

        // ぶつかったら10ダメージ
        GameManager.instance.hp = GameManager.instance.hp - 10;
        if (GameManager.instance.hp < 0)
        {
            GameManager.instance.hp = 0;
        }

        // ゲージも更新する
        if (GameManager.instance.hpGauge != null)
        {
            GameManager.instance.hpGauge.fillAmount = GameManager.instance.hp / 100f;
        }

        // HPが0になったらゲームオーバー
        if (GameManager.instance.hp <= 0)
        {
            GameManager.isClear = false;
            SceneManager.LoadScene("Result");
        }
    }
}
