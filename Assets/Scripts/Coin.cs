using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Coin : MonoBehaviour
{
    public bool isSpecial;

    [SerializeField] private float rotateSpeed = 90f;

    private bool collected;

    private void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;

        var gameManager = GameManager.Instance;
        if (gameManager == null) return;

        collected = true;
        gameManager.collectedCoinCount++;
        gameManager.RefreshCoinDisplay();

        if (isSpecial)
        {
            gameManager.invincibleRemainingSeconds = GameManager.InvincibleDurationSeconds;
            gameManager.RefreshHpDisplay();
        }

        Destroy(gameObject);

        if (gameManager.collectedCoinCount < GameManager.TotalCoinCount) return;

        GameManager.IsClear = true;
        SceneManager.LoadScene("Result");
    }
}
