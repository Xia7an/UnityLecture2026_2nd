using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.Result
{
    /// <summary>Result シーンで、直前のゲームの決着状況を表示する。</summary>
    public sealed class ResultOutcomeView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI outcomeText;

        [Header("表示内容")]
        [SerializeField] private string clearMessage = "GAME CLEAR";

        [Header("文字色")]
        [SerializeField] private Color clearColor = new(1f, 0.8f, 0.2f);

        /// <summary>決着状況に対応するメッセージと色を反映する。</summary>
        public void Initialize(GameOutcome outcome)
        {
            if (outcomeText == null)
            {
                Debug.LogError(
                    $"{nameof(ResultOutcomeView)}: {nameof(outcomeText)} が設定されていません。",
                    this);
                return;
            }

            switch (outcome)
            {
                case GameOutcome.Clear:
                    outcomeText.text = clearMessage;
                    outcomeText.color = clearColor;
                    break;

                default:
                    outcomeText.text = string.Empty;
                    Debug.LogError(
                        $"{nameof(ResultOutcomeView)}: Result シーンでは表示できない決着状況です: {outcome}",
                        this);
                    break;
            }
        }
    }
}
