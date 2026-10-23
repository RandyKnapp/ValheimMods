using System.Linq;

namespace EpicLoot.Compendium;

public class MagicTextGroup
{
    public MagicTextGroup(IMagicTextPart title, params IMagicTextPart[] content)
    {
        Title = title;
        Content = content;
    }

    public readonly IMagicTextPart Title;
    public readonly IMagicTextPart[] Content;

    // Lowers the query once here rather than in every element, since a group fans it out across its
    // title plus every content line on each keystroke.
    public bool IsMatch(string query)
    {
        string lowered = query.ToLowerInvariant();
        return Title.IsMatch(lowered) || Content.Any(x => x.IsMatch(lowered));
    }

    public virtual void Filter(string query) => Enable(IsMatch(query));

    public void Enable(bool enable)
    {
        Title.Enable(enable);
        foreach (IMagicTextPart element in Content)
        {
            element.Enable(enable);
        }
    }
}

public class MagicTableGroup : MagicTextGroup
{
    private readonly MagicTextTable _table;

    public MagicTableGroup(MagicTextTable table) : base(table)
    {
        _table = table;
    }

    public MagicTextTable Table => _table;

    public override void Filter(string query) => _table.Filter(query.ToLowerInvariant());
}
