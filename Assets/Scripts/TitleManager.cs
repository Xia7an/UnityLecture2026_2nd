using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    void Update()
    {
        // スペースでゲーム開始
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("Play");
        }
    }
}
