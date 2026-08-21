namespace Game.Core
{
    /// <summary>
    /// シーンが終わった理由。各シーンの CompositionRoot が発火し、
    /// GameRoot が次のシーンを決めるために使う。
    /// </summary>
    public enum SceneResult
    {
        /// <summary>通常の遷移。Title → Play、Result → Title で使う。</summary>
        Normal,

        /// <summary>ゲームクリア。Play → Result で使う。</summary>
        GameClear,

        /// <summary>ゲーム失敗。Play → Result で使う。</summary>
        GameFailure,
    }
}
