using System.Reflection;
using Game.Core;
using Game.Result;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Game.Tests
{
    public sealed class ResultOutcomeViewTest
    {
        private GameObject gameObject;
        private TextMeshProUGUI outcomeText;
        private ResultOutcomeView view;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject(
                "ResultOutcomeView",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(ResultOutcomeView));

            outcomeText = gameObject.GetComponent<TextMeshProUGUI>();
            view = gameObject.GetComponent<ResultOutcomeView>();

            typeof(ResultOutcomeView)
                .GetField("outcomeText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(view, outcomeText);
        }

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null) Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void クリアならクリア表示になる()
        {
            view.Initialize(GameOutcome.Clear);

            Assert.That(outcomeText.text, Is.EqualTo("GAME CLEAR"));
            Assert.That(outcomeText.color, Is.EqualTo(new Color(1f, 0.8f, 0.2f)));
        }

        [Test]
        public void 失敗なら失敗表示になる()
        {
            view.Initialize(GameOutcome.Failure);

            Assert.That(outcomeText.text, Is.EqualTo("GAME FAILED"));
            Assert.That(outcomeText.color, Is.EqualTo(new Color(1f, 0.3f, 0.3f)));
        }
    }
}
