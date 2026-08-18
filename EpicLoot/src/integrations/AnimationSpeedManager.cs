using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EpicLoot
{
    internal class AnimationSpeedManager
    {
        private static readonly Harmony harmony = new("AnimationSpeedManager");
        private static bool hasMarkerPatch = false;

        public delegate double Handler(Character character, double speed);

        private static readonly MethodInfo method = AccessTools.DeclaredMethod(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.CustomFixedUpdate));
        private static int index = 0;
        private static bool changed = false;
        private static Handler[][] handlers = Array.Empty<Handler[]>();
        private static readonly Dictionary<int, List<Handler>> handlersPriorities = new();

        // When another mod already provides an AnimationSpeedManager (its own ILRepack-embedded
        // copy, in the global namespace), we forward our handlers into that shared instance rather
        // than running a second, possibly version-mismatched manager that would fight over
        // animator.speed. Resolved once, lazily, on the first Add call.
        private static bool? _useExternal;      // null = unresolved
        private static MethodInfo _externalAdd; // external Add(Handler, int)
        private static Type _externalHandler;   // external nested Handler delegate type

        public static void Add(Handler handler, int priority = Priority.Normal)
        {
            _useExternal ??= ResolveExternal();

            if (_useExternal == true)
            {
                try
                {
                    // Handler is double(Character, double) - only shared types, so the delegate can
                    // be rebound to the external manager's Handler type across assemblies.
                    Delegate external = Delegate.CreateDelegate(_externalHandler, handler.Target, handler.Method);
                    _externalAdd.Invoke(null, new object[] { external, priority });
                    return;
                }
                catch (Exception e)
                {
                    EpicLoot.LogWarning($"Failed to forward attack-speed handler to external AnimationSpeedManager, falling back to bundled copy: {e.Message}");
                    _useExternal = false;
                }
            }

            if (!hasMarkerPatch)
            {
                harmony.Patch(method, finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(AnimationSpeedManager), nameof(markerPatch))));
                hasMarkerPatch = true;
            }

            if (!handlersPriorities.TryGetValue(priority, out List<Handler> priorityHandlers))
            {
                handlersPriorities.Add(priority, priorityHandlers = new List<Handler>());
                harmony.Patch(method, postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(AnimationSpeedManager), nameof(wrapper))));
            }
            priorityHandlers.Add(handler);

            handlers = handlersPriorities.OrderBy(kv => kv.Key).Select(kv => kv.Value.ToArray()).ToArray();
        }

        // Look for another mod's AnimationSpeedManager (a global-namespace type named
        // "AnimationSpeedManager", typically an ILRepack-embedded internal copy) and, if found,
        // cache the reflection handles needed to forward our handlers into it.
        private static bool ResolveExternal()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type externalType = asm.GetType("AnimationSpeedManager", false);
                    if (externalType == null || externalType.Assembly == typeof(AnimationSpeedManager).Assembly)
                    {
                        continue;
                    }

                    Type handlerType = externalType.GetNestedType("Handler", BindingFlags.Public | BindingFlags.NonPublic);
                    MethodInfo addMethod = externalType.GetMethod("Add", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (handlerType == null || addMethod == null)
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = addMethod.GetParameters();
                    if (parameters.Length < 1 || parameters[0].ParameterType != handlerType)
                    {
                        continue;
                    }

                    _externalHandler = handlerType;
                    _externalAdd = addMethod;
                    EpicLoot.Log($"Routing attack-speed handlers into external AnimationSpeedManager from {asm.GetName().Name}");
                    return true;
                }
                catch
                {
                    // Ignore assemblies we can't reflect over (dynamic assemblies, forks with
                    // ambiguous overloads, etc.) and keep scanning.
                }
            }

            return false;
        }

        private static void wrapper(Character ___m_character, Animator ___m_animator)
        {
            double currentSpeedMarker = ___m_animator.speed * 1e7 % 100;
            if (currentSpeedMarker is > 10 and < 30 || ___m_animator.speed <= 0.001f)
            {
                return;
            }

            double speed = ___m_animator.speed;
            double newSpeed = handlers[index++].Aggregate(speed, (current, handler) => handler(___m_character, current));
            if (newSpeed != speed)
            {
                ___m_animator.speed = (float)(newSpeed - newSpeed % 1e-5);
                changed = true;
            }
        }

        private static void markerPatch(Animator ___m_animator)
        {
            if (changed)
            {
                float speed = ___m_animator.speed;
                double currentSpeedMarker = speed * 1e7 % 100;
                if (currentSpeedMarker is < 10 or > 30)
                {
                    ___m_animator.speed += 19e-7f;
                }
                changed = false;
            }
            index = 0;
        }
    }
}
