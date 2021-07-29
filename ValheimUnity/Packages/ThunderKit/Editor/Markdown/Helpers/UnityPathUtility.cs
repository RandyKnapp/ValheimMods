namespace ThunderKit.Markdown.Helpers
{
    public static class UnityPathUtility
    {
        public static bool IsAssetDirectory(string path) => path.StartsWith("Packages") || path.StartsWith("/Packages") || path.StartsWith("Assets") || path.StartsWith("/Assets");
    }
}