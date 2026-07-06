namespace EpicLoot.Compendium;

public class MagicTextInfo(string topic, bool showSearchBar = true) : TextsDialog.TextInfo(topic, "")
{
    public readonly bool ShowSearchBar = showSearchBar;
    public virtual void Build(MagicPages instance) { }
}
