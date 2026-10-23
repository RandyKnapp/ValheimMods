using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // One credit per kill for an on-kill effect. Every hit that lands on a dying victim reads as lethal: an
    // owned victim's health is already zero until CheckDeath destroys it on the next fixed step, and a remote
    // victim's synced health does not move until its owner applies the hit, so a volley or a cleave would pay
    // once per hit. The same victim is credited once per burst window. A lethal read on it after the window
    // pays again, which is what credits the real kill when an earlier estimate was wrong (the owner blocked
    // or absorbed the hit): no victim is ever killed twice, so there is nothing else a repeat can be.
    public sealed class KillCredit
    {
        // Longer than a volley's spread (the arrows leave the same frame at the same speed and land within a
        // frame or two), shorter than any weapon's next swing.
        private const float BurstWindow = 0.25f;

        private ZDOID _victim = ZDOID.None;
        private float _time;

        public bool TryClaim(Character target, HitData hit)
        {
            if (!MagicEffectsHelper.IsLethalHit(target, hit))
            {
                return false;
            }

            // IsLethalHit has checked that the view is valid.
            ZDOID victim = target.m_nview.GetZDO().m_uid;
            if (victim == _victim && Time.time - _time < BurstWindow)
            {
                return false;
            }

            _victim = victim;
            _time = Time.time;
            return true;
        }
    }
}
