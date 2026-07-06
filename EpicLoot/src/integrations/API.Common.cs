using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    public enum RequirementWireState
    {
        Absent,
        Have,
        DontHave
    }

    public class PlayerPanelTabData
    {
        public int Index;
        public Text TabTitle;
        public GameObject TabButtonGO;
        public GameObject ContentGO;
    }

    public class WorkbenchTabData
    {
        public int Index;
        public Text TabTitle;
        public GameObject TabButtonGO;
        public GameObject RequirementsPanelGO;
        public GameObject ItemInfoGO;
    }
}
