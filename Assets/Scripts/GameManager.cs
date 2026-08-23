using TMPro;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public const int TotalCoinCount = 30;

    public static GameManager Instance { get; private set; }
    public static bool IsClear;

    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private TextMeshProUGUI coinText;

    // Coin から直接更新される。状態の持ち主を分けていない素朴な実装。
    public int collectedCoinCount;

    private void Awake()
    {
        Instance = this;
        IsClear = false;
    }

    private void Start()
    {
        SpawnCoins();
        RefreshCoinDisplay();
    }

    private void SpawnCoins()
    {
        var positions = new Vector3[TotalCoinCount];

        for (var i = 0; i < TotalCoinCount; i++)
        {
            var position = FindCoinPosition(positions, i);
            positions[i] = position;

            if (coinPrefab != null)
            {
                Instantiate(coinPrefab, position, coinPrefab.transform.rotation);
            }
        }
    }

    private static Vector3 FindCoinPosition(Vector3[] positions, int placedCount)
    {
        var position = new Vector3(0f, 0.5f, 0f);

        // 近すぎたら置き直す。50 回で見つからなければ最後の候補を使う。
        for (var attempt = 0; attempt < 50; attempt++)
        {
            position = new Vector3(Random.Range(-9f, 9f), 0.5f, Random.Range(-9f, 9f));

            var isFarEnough = true;
            for (var i = 0; i < placedCount; i++)
            {
                if (Vector3.Distance(positions[i], position) >= 1.5f) continue;

                isFarEnough = false;
                break;
            }

            if (isFarEnough) break;
        }

        return position;
    }

    public void RefreshCoinDisplay()
    {
        if (coinText != null)
        {
            coinText.text = $"COIN {collectedCoinCount} / {TotalCoinCount}";
        }
    }
}
