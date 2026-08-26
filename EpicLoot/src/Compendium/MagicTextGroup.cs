using System.Linq;

namespace EpicLoot.Compendium;

public class MagicTextGroup
{
    public MagicTextGroup(MagicTextElement title, params MagicTextElement[] content)
    {
        Title = title;
        Content = content;
    }

    public readonly MagicTextElement Title;
    public readonly MagicTextElement[] Content;

    // Lowers the query once here rather than in every element, since a group fans it out across its
    // title plus every content line on each keystroke.
    public bool IsMatch(string query)
    {
        string lowered = query.ToLowerInvariant();
        return Title.IsMatch(lowered) || Content.Any(x => x.IsMatch(lowered));
    }
    public void Enable(bool enable)
    {
        Title.Enable(enable);
        foreach (MagicTextElement element in Content)
        {
            element.Enable(enable);
        }
    }
}