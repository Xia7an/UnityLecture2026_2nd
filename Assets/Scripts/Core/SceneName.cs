namespace Game.Core
{
    /// <summary>
    /// シーンの識別子。文字列を直接扱わずに済むよう enum で表す。
    /// </summary>
    public enum SceneName
    {
        Root,
        Title,
        Play,
        Result,
    }

    public static class SceneNameExtensions
    {
        /// <summary>
        /// SceneManager に渡すアセット名へ変換する。
        /// </summary>
        public static string ToAssetName(this SceneName sceneName) => sceneName.ToString();
    }
}
