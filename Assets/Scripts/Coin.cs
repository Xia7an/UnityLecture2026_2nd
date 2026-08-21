using UnityEngine;
using UnityEngine.SceneManagement;

public class Coin : MonoBehaviour
{
    public bool isSpecial;
    public float rotateSpeed = 90f;

    private bool got = false;

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (got) return;
        if (!other.CompareTag("Player")) return;
        got = true;

        GameManager.instance.coinCount = GameManager.instance.coinCount + 1;

        // 表示も更新する
        if (GameManager.instance.coinText != null)
        {
            GameManager.instance.coinText.text = "COIN " + GameManager.instance.coinCount + " / 30";
        }

        // プレイヤー側の枚数も合わせておく
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.AddCoin();
        }

        // スペシャルコインだったら10秒間無敵になる
        if (isSpecial == true)
        {
            GameManager.instance.isMuteki = true;
            GameManager.instance.mutekiZanri = 10f;

            // 敵にも無敵になったことを教えておく
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].SetMuteki(true);
            }

            // ゲージの色も変えておく
            if (GameManager.instance.hpGauge != null)
            {
                GameManager.instance.hpGauge.color = new Color(0.2f, 0.4f, 1f);
            }
        }

        Destroy(gameObject);

        // 30枚集めたらクリア
        if (GameManager.instance.coinCount >= 30)
        {
            GameManager.isClear = true;
            SceneManager.LoadScene("Result");
        }
    }
}
