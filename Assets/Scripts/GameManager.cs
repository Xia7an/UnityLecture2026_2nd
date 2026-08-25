using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    public const int TotalCoinCount = 30;
    public const int TimeLimitSeconds = 60;

    public static GameManager Instance { get; private set; }
    public static bool IsClear;

    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI timerText;

    // Coin から直接更新される。状態の持ち主を分けていない素朴な実装。
    public int collectedCoinCount;
    public int remainingTimeSeconds = TimeLimitSeconds;

    private float elapsedSeconds;

    private void Awake()
    {
        Instance = this;
        IsClear = false;
    }

    private void Start()
    {
        SpawnCoins();
        RefreshCoinDisplay();
        RefreshTimerDisplay();
    }

    private void Update()
    {
        elapsedSeconds += Time.deltaTime;

        var nextRemainingSeconds = Mathf.Max(
            0,
            Mathf.CeilToInt(TimeLimitSeconds - elapsedSeconds));

        if (nextRemainingSeconds != remainingTimeSeconds)
        {
            remainingTimeSeconds = nextRemainingSeconds;
            RefreshTimerDisplay();
        }

        if (remainingTimeSeconds > 0 || IsClear) return;

        IsClear = false;
        enabled = false;
        SceneManager.LoadScene("Result");
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

    private void RefreshTimerDisplay()
    {
        if (timerText == null) return;

        var minutes = remainingTimeSeconds / 60;
        var seconds = remainingTimeSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
