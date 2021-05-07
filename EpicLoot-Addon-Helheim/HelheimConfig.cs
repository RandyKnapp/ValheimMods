using System;
using System.Collections.Generic;

namespace EpicLoot_Addon_Helheim
{
    [Serializable]
    public class HelheimLevelConfig
    {
        public int Level;
    }

    [Serializable]
    public class HelheimConfig
    {
        public List<HelheimLevelConfig> HelheimLevels = new List<HelheimLevelConfig>();
    }
}
