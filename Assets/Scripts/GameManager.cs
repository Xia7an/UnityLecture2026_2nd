using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static bool isClear;

    public GameObject coinPrefab;
    public GameObject playerObject;
    public TextMeshProUGUI coinText;

    // 取ったコインの枚数。Coin.cs から直接足される
    public int coinCount = 0;

    // 出したコインの枚数
    public int CoinNum = 0;

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
    }

    void Update()
    {
        // 念のため毎フレーム表示を更新しておく
        if (coinText != null)
        {
            coinText.text = "COIN " + coinCount + " / 30";
        }
    }
}
