using System;
using System.Diagnostics;
using System.Reflection;
using EpicLoot.LegendarySystem;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpicLoot.MagicItemEffects
{
    // Does at world load, behind the loading screen, what the set effects would otherwise do the first time they show
    // (hitch profiles put each of those at 50 ms to a second): the wings' texture, mesh and template rig, the snow
    // emitter and ice floe templates, the eitr infusion's auras, the sets' weapon and hand glows, drawn arrows and body
    // auras, Sniper's charge, arrow and impact, the local fx copies, the spirit animal's materials. It
    // also compiles every set effect's code up front: a method's first call is when Mono compiles it, and a set
    // effect's first activation runs dozens of methods for the first time at once.
    internal static class SetEffectWarmup
    {
        // The set effects' own types; their nested types (patches, lambdas, coroutines) come along.
        private static readonly Type[] CompiledTypes =
        {
            typeof(Earthshaker), typeof(FreyjaWings), typeof(KineticQuake), typeof(KineticQuakeRocks),
            typeof(KineticQuakeHud), typeof(Firebrand), typeof(Frostwalker), typeof(SE_Frostwalker),
            typeof(FrostwalkerIceField), typeof(FrostwalkerSnow), typeof(FloeTemplate), typeof(SpiritAnimal),
            typeof(SpiritAnimalVisual), typeof(EitrBarrier), typeof(SE_EitrBarrier), typeof(EitrInfusion),
            typeof(SE_EitrInfusion), typeof(EitrInfusionAura), typeof(AuraFade), typeof(HelsHarvest), typeof(Vanish), typeof(LifeSiphon), typeof(LifeSiphonAura),
            typeof(QuarryMark), typeof(QuarryMarker), typeof(ConjuredArrows), typeof(ElementalArchery),
            typeof(PiercingBolts), typeof(Interconnected), typeof(DeepWell), typeof(ConvertHealthToEitr), typeof(Sniper),
            typeof(SniperVisuals), typeof(SniperCharge), typeof(SniperArrowFx), typeof(SniperArrowMarker), typeof(LightPulse),
            typeof(OverwhelmingLaunch), typeof(LaunchedWeaponVisual), typeof(Artillery), typeof(Assassin),
            typeof(AssassinChargeHud), typeof(LocalFx), typeof(SetAbilityKeyHint), typeof(WeaponAuraKit),
            typeof(SetVisuals), typeof(FallingLeaves), typeof(SetDrawArrow), typeof(ShieldThrow),
            typeof(ThrownShieldVisual), typeof(SetGaugeBar), typeof(ReduceStaggerTaken),
        };

        private static bool _compiled;

        private static bool Drawn => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        // PrefabManager.OnPrefabsRegistered, subscribed after the effects' own RegisterPrefabs, so the copies they
        // register (the barrier's hit fx) exist by now.
        internal static void OnPrefabsRegistered()
        {
            var timer = Stopwatch.StartNew();
            bool drawn = Drawn;
            Run(nameof(Earthshaker), () => Earthshaker.WarmupAtLoad(drawn));
            Run(nameof(Frostwalker), () => Frostwalker.WarmupAtLoad(drawn));
            Run(nameof(KineticQuake), () =>
            {
                if (drawn)
                {
                    KineticQuake.WarmupFx();
                }
            });
            Run(nameof(QuarryMark), () => QuarryMark.WarmupAtLoad(drawn));
            Run(nameof(EitrBarrier), () => EitrBarrier.WarmupAtLoad(drawn));
            Run(nameof(EitrInfusion), () => EitrInfusion.WarmupAtLoad(drawn));
            Run(nameof(SpiritAnimal), () => SpiritAnimal.WarmupAtLoad(drawn));
            Run(nameof(SniperVisuals), () => SniperVisuals.WarmupAtLoad(drawn));
            Run(nameof(SetVisuals), () => SetVisuals.WarmupAtLoad(drawn));
            long assets = timer.ElapsedMilliseconds;

            int compiled = 0;
            if (!_compiled)
            {
                _compiled = true;
                Run("compile", () =>
                {
                    foreach (Type type in CompiledTypes)
                    {
                        compiled += Compile(type);
                    }
                });
            }

            EpicLoot.Log($"[SetEffectWarmup] set effect assets in {assets} ms, {compiled} methods compiled in " +
                $"{timer.ElapsedMilliseconds - assets} ms.");
        }

        private static void Run(string name, Action step)
        {
            try
            {
                step();
            }
            catch (Exception e)
            {
                EpicLoot.LogWarning($"[SetEffectWarmup] {name}: {e}");
            }
        }

        // RuntimeMethodHandle.GetFunctionPointer is where Mono compiles a method that has not run yet; nothing runs.
        private static int Compile(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.Instance | BindingFlags.DeclaredOnly;
            if (type.ContainsGenericParameters)
            {
                return 0;
            }

            int count = 0;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (TryCompile(method))
                {
                    count++;
                }
            }
            foreach (ConstructorInfo constructor in type.GetConstructors(flags & ~BindingFlags.Static))
            {
                if (TryCompile(constructor))
                {
                    count++;
                }
            }
            foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                count += Compile(nested);
            }
            return count;
        }

        private static bool TryCompile(MethodBase method)
        {
            if (method.IsAbstract || method.ContainsGenericParameters || method.GetMethodBody() == null)
            {
                return false;
            }

            try
            {
                method.MethodHandle.GetFunctionPointer();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // The local player's spawn: what depends on the character (the bound spirit animal) is made before the first
        // fight rather than in it.
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static class Player_OnSpawned_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    Run(nameof(SpiritAnimal), () => SpiritAnimal.WarmupForPlayer(__instance, Drawn));
                }
            }
        }
    }
}
