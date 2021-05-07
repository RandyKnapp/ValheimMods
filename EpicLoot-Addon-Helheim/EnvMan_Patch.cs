using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Random = UnityEngine.Random;

namespace EpicLoot_Addon_Helheim
{
    [HarmonyPatch]
    public static class EnvMan_Patch
    {
        public static int PreviousHelheimLevel;
        public static Heightmap.Biome PreviousBiome;
        public static string PreviousEnvName;

        //Awake
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.Awake))]
        public static class EnvMan_Awake_Patch
        {
            public static void Prefix(EnvMan __instance)
            {
                var eikthyr = __instance.m_environments.Find(x => x.m_name == "Eikthyr");
                AddHelheimEnvironments(__instance, eikthyr, 1);

                var ashrain = __instance.m_environments.Find(x => x.m_name == "Ashrain");
                AddHelheimEnvironments(__instance, ashrain, 2);

                var moder = __instance.m_environments.Find(x => x.m_name == "Moder");
                AddHelheimEnvironments(__instance, moder, 3);
            }
        }

        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.UpdateEnvironment))]
        public static class EnvMan_UpdateEnvironment_Patch
        {
            public static bool Prefix(EnvMan __instance, long sec, Heightmap.Biome biome)
            {
                if (HasEnvChanged(Helheim.HelheimLevel, biome, __instance.GetCurrentEnvironment().m_name))
                {
                    Helheim.Log($"Helheim: {Helheim.HelheimLevel}, Biome: {biome}, Current: {__instance.GetCurrentEnvironment().m_name}, Wet: {__instance.IsWet()}, Freezing: {__instance.IsFreezing()}");
                }

                var allowBaseMethod = true;
                var forceSwitch = Helheim.HelheimLevel != PreviousHelheimLevel;
                __instance.m_firstEnv = forceSwitch;

                if (Helheim.HelheimLevel > 0)
                {
                    var num = sec / __instance.m_environmentDuration;
                    if (!forceSwitch && __instance.m_currentEnv.m_name.StartsWith("Helheim") && __instance.m_environmentPeriod == num && __instance.m_currentBiome == biome)
                    {
                        return false;
                    }

                    __instance.m_environmentPeriod = num;
                    __instance.m_currentBiome = biome;
                    var state = Random.state;
                    Random.InitState((int)num);
                    var availableEnvironments = __instance.GetAvailableEnvironments(biome);
                    if (availableEnvironments != null && availableEnvironments.Count > 0)
                    {
                        var biomeEnv = __instance.SelectWeightedEnvironment(availableEnvironments);
                        var helheimEnv = GetHelheimEnvironment(__instance, biomeEnv, Helheim.HelheimLevel);
                        __instance.QueueEnvironment(helheimEnv);
                        Helheim.LogWarning($"Changing Environment: {helheimEnv.m_name}");
                    }
                    Random.state = state;
                    allowBaseMethod = false;
                }
                else
                {
                    if (forceSwitch)
                    {
                        __instance.m_currentBiome = Heightmap.Biome.None;
                    }
                }

                PreviousHelheimLevel = Helheim.HelheimLevel;
                PreviousBiome = biome;
                PreviousEnvName = __instance.GetCurrentEnvironment().m_name;
                return allowBaseMethod;
            }
        }

        public static bool HasEnvChanged(int helheimLevel, Heightmap.Biome biome, string envName)
        {
            return PreviousHelheimLevel != helheimLevel ||
            PreviousBiome != biome ||
            PreviousEnvName != envName;
        }

        public static EnvSetup GetHelheimEnvironment(EnvMan envMan, EnvSetup biomeEnv, int level)
        {
            var name = GetHelheimEnvName(level, biomeEnv.m_isWet, biomeEnv.m_isFreezing);
            return envMan.GetEnv(name);
        }

        public static void AddHelheimEnvironments(EnvMan envMan, EnvSetup baseEnv, int level)
        {
            var helheimEnvBase = baseEnv.Clone();
            helheimEnvBase.m_name = GetHelheimEnvName(level, false, false);
            //FindAndCopyEnvObjectByName(envMan, "Thunder", helheimEnvBase);
            envMan.m_environments.Add(helheimEnvBase);

            var helheimEnvWet = helheimEnvBase.Clone();
            helheimEnvWet.m_name = GetHelheimEnvName(level, true, false);
            helheimEnvWet.m_isWet = true;
            FindAndCopyPsystemByName(envMan, level == 1 ? "LightRain" : "Rain", helheimEnvWet);
            envMan.m_environments.Add(helheimEnvWet);

            var helheimEnvFreezing = helheimEnvBase.Clone();
            helheimEnvFreezing.m_name = GetHelheimEnvName(level, false, true);
            helheimEnvFreezing.m_isFreezing = true;
            FindAndCopyPsystemByName(envMan, level == 1 ? "Snow" : "SnowStorm", helheimEnvFreezing);
            envMan.m_environments.Add(helheimEnvFreezing);
        }

        public static void FindAndCopyEnvObjectByName(EnvMan envMan, string name, EnvSetup targetEnv)
        {
            if (targetEnv.m_envObject.name == name)
            {
                return;
            }

            foreach (var env in envMan.m_environments)
            {
                if (env.m_envObject.name.Equals(name))
                {
                    targetEnv.m_envObject = env.m_envObject;
                    return;
                }
            }
        }

        public static void FindAndCopyPsystemByName(EnvMan envMan, string name, EnvSetup targetEnv)
        {
            if (targetEnv.m_psystems.ToList().Exists(x => x.name == name))
            {
                return;
            }

            foreach (var env in envMan.m_environments)
            {
                foreach (var psystem in env.m_psystems)
                {
                    if (psystem.name.Equals(name))
                    {
                        targetEnv.m_psystems = targetEnv.m_psystems.AddToArray(psystem);
                        return;
                    }
                }
            }
        }

        public static string GetHelheimEnvName(int level, bool wet, bool freezing)
        {
            return $"Helheim{level}{(wet ? "_Wet" : (freezing ? "_Freezing" : ""))}";
        }
    }
}
