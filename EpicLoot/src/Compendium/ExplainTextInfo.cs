using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.Compendium;

public class ExplainTextInfo(string topic) : MagicTextInfo(topic)
{
    public override void Build(MagicPages instance)
    {
        IOrderedEnumerable<KeyValuePair<string, string>> sortedMagicEffects = MagicItemEffectDefinitions.AllDefinitions
            .Where(x => !x.Value.Requirements.NoRoll && x.Value.CanBeAugmented)
            .Select(x => new KeyValuePair<string, string>(string.Format(Localization.instance.Localize(x.Value.DisplayText),
                    "<b><color=yellow>X</color></b>"),
                Localization.instance.Localize(x.Value.Description)))
            .OrderBy(x => x.Key);

        foreach (KeyValuePair<string, string> kvp in sortedMagicEffects)
        {
            instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>{kvp.Key}</size>",
                $"<color=#c0c0c0ff>{kvp.Value}</color>", "");
        }
    }
}