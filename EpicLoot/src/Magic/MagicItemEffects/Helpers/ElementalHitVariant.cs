namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // HitData.m_variant picks which colour of an elemental status effect the target shows: Burning's start
    // effects are vfx_Burning (0), vfx_Burning_blue (1) and vfx_Burning_green (2); Lightning's are fx_Lightning
    // (0) and fx_Lightning_red (1). EffectList.Create spawns every entry when the variant is -1, and -1 is what
    // an ordinary weapon carries (SharedData.m_hitVariant defaults to it). So fire or lightning added to such a
    // weapon -- AddFireDamage, NecroticFire, the lightning conversions -- lit the target in every colour at once.
    //
    // An unset variant on a player's elemental hit is defaulted to 0, the ordinary colour. A weapon that sets
    // its own variant (the FrostFire / BloodLightning golds, Fader's green fire) keeps it.
    public static class ElementalHitVariant {
        public static void ModifyOutgoingHit(HitData hit, Character attacker) {
            if (hit.m_variant >= 0 || attacker is not Player) {
                return;
            }

            if (hit.m_damage.m_fire > 0f || hit.m_damage.m_lightning > 0f) {
                hit.m_variant = 0;
            }
        }
    }
}
