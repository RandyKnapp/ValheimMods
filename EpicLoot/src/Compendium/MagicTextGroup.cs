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

    public bool IsMatch(string query) => Title.IsMatch(query) || Content.Any(x => x.IsMatch(query));
    public void Enable(bool enable)
    {
        Title.Enable(enable);
        foreach (MagicTextElement element in Content)
        {
            element.Enable(enable);
        }
    }
}