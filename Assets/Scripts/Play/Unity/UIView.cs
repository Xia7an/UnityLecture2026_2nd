using Game.Core;
using UnityEngine;

namespace Game.Play
{
    public abstract class UIView: MonoBehaviour
    {
        public abstract void Initialize(GameState gameState);
    }
}