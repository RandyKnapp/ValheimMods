using EpicLoot.Adventure;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace EpicLoot;

public static partial class API
{
    [PublicAPI]
    public static string AddBountyTarget(string json)
    {
        try
        {
            var bounty = JsonConvert.DeserializeObject<BountyTargetConfig>(json);

            if (bounty == null)
            {
                return null;
            }

            ExternalBountyTargets.Add(bounty);
            AdventureDataManager.Config.Bounties.Targets.Add(bounty);
            return RuntimeRegistry.Register(bounty);
        }
        catch
        {
            OnError?.Invoke("Failed to parse bounty target passed in through external plugin.");
            return null;
        }
    }

    [PublicAPI]
    public static bool UpdateBountyTarget(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out BountyTargetConfig bountyTarget))
        {
            return false;
        }

        BountyTargetConfig config = JsonConvert.DeserializeObject<BountyTargetConfig>(json);
        bountyTarget.CopyFieldsFrom(config);
        return true;
    }
}