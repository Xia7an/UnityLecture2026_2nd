using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static bool isClear;

    public GameObject coinPrefab;
    public GameObject enemyPrefab;
    public GameObject playerObject;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI timerText;
    public Image hpGauge;

    // 取ったコインの枚数。Coin.cs から直接足される
    public int coinCount = 0;

    // 出したコインの枚数
    public int CoinNum = 0;

    // プレイヤーのHP。Enemy.cs から直接減らされる
    public int hp = 100;

    // タイマー
    private float keikaJikan = 0f;   // 経過時間
    public int nokoriByou = 60;      // 残り秒数
    public bool timeUp = false;      // 時間切れかどうか

    void Awake()
    {
        instance = this;
        isClear = false;
    }

    void Start()
    {
        if (playerObject == null)
        {
            playerObject = GameObject.Find("Player");
        }

        // コインを30個出す
        Vector3[] okiba = new Vector3[30];
        for (int i = 0; i < 30; i++)
        {
            Vector3 p = new Vector3(0f, 0.5f, 0f);

            // 近すぎたら置き直す。50回やって駄目だったらあきらめる
            for (int t = 0; t < 50; t++)
            {
                p = new Vector3(Random.Range(-9f, 9f), 0.5f, Random.Range(-9f, 9f));

                bool ok = true;
                for (int j = 0; j < i; j++)
                {
                    if (Vector3.Distance(okiba[j], p) < 1.5f)
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok) break;
            }

            okiba[i] = p;

            if (coinPrefab != null)
            {
                Instantiate(coinPrefab, p, coinPrefab.transform.rotation);
                CoinNum = CoinNum + 1;
            }
        }

        coinCount = 0;
        if (coinText != null)
        {
            coinText.text = "COIN " + coinCount + " / 30";
        }

        // 敵を5体出す
        for (int i = 0; i < 5; i++)
        {
            Vector3 ep = new Vector3(Random.Range(-9f, 9f), 0f, Random.Range(-9f, 9f));
            if (enemyPrefab != null)
            {
                Instantiate(enemyPrefab, ep, Quaternion.identity);
            }
        }

        // HPの初期化
        hp = 100;
        if (hpGauge != null)
        {
            hpGauge.fillAmount = 1f;
            hpGauge.color = Color.green;
        }

        // タイマーの初期化
        keikaJikan = 0f;
        nokoriByou = 60;
        timeUp = false;
        if (timerText != null)
        {
            timerText.text = "01:00";
        }
    }

    void Update()
    {
        // 念のため毎フレーム表示を更新しておく
        if (coinText != null)
        {
            coinText.text = "COIN " + coinCount + " / 30";
        }

        // HPゲージ
        if (hpGauge != null)
        {
            hpGauge.fillAmount = hp / 100f;
        }

        // ここからタイマー
        keikaJikan = keikaJikan + Time.deltaTime;
        nokoriByou = 60 - (int)keikaJikan;
        if (nokoriByou < 0)
        {
            nokoriByou = 0;
        }
        if (nokoriByou <= 0)
        {
            timeUp = true;
        }

        // mm:ss の形にする
        int m = (int)(nokoriByou / 60);
        int s = (int)nokoriByou - m * 60;
        string mm = "" + m;
        if (m < 10) mm = "0" + m;
        string ss = "" + s;
        if (s < 10) ss = "0" + s;
        if (timerText != null)
        {
            timerText.text = mm + ":" + ss;
        }

        // 時間切れになったら失敗
        if (timeUp == true && isClear == false)
        {
            GameManager.isClear = false;
            SceneManager.LoadScene("Result");
        }
    }
}
