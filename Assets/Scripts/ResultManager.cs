using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class ResultManager : MonoBehaviour
{
    public TextMeshProUGUI resultText;

    void Start()
    {
        if (resultText != null)
        {
            if (GameManager.IsClear)
            {
                resultText.text = "GAME CLEAR";
                resultText.color = new Color(1f, 0.8f, 0.2f);
            }
            else
            {
                resultText.text = "GAME FAILED";
                resultText.color = new Color(1f, 0.3f, 0.3f);
            }
        }
    }

    void Update()
    {
        // スペースでタイトルに戻る
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("Title");
        }
    }
}
