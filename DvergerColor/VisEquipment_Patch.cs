using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DvergerColor
{
    [RequireComponent(typeof(Light))]
    public class LightChanger : MonoBehaviour
    {
        private int _maxSteps = 3;
        private float _minAngle = 30;
        private float _maxAngle = 110;
        private float _minIntensity = 2.0f;
        private float _maxIntensity = 1.4f;
        private float _minRange = 45;
        private float _maxRange = 15;
        private float _pointIntensity = 1.1f;
        private float _pointRange = 10;
        private Light _light;
        private Light[] _lights;

        public int Step;
        public bool On;

        private void Awake()
        {
            _maxSteps = Mathf.Clamp(DvergerColor.MaxSteps.Value, 2, 50);
            _minAngle = Mathf.Clamp(DvergerColor.MinAngle.Value, 1, 360);
            _maxAngle = Mathf.Clamp(DvergerColor.MaxAngle.Value, 1, 360);
            _minIntensity = Mathf.Clamp(DvergerColor.MaxIntensity.Value, 0, 10);
            _maxIntensity = Mathf.Clamp(DvergerColor.MinIntensity.Value, 0, 10);
            _minRange = Mathf.Clamp(DvergerColor.MinRange.Value, 0, 1000);
            _maxRange = Mathf.Clamp(DvergerColor.MaxRange.Value, 0, 1000);
            _pointIntensity = Mathf.Clamp(DvergerColor.PointIntensity.Value, 0, 10);
            _pointRange = Mathf.Clamp(DvergerColor.PointRange.Value, 0, 1000);

            Step = _maxSteps - 1;
            On = true;
        }

        private void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var zdo = player.GetZDO();
            bool changedStep = false;
            if (player != null && player.TakeInput())
            {
                if (On && Input.GetKeyDown(DvergerColor.NarrowBeamHotkey.Value))
                {
                    Step = Mathf.Clamp(Step - 1, 0, _maxSteps);
                    changedStep = true;
                }
                else if (On && Input.GetKeyDown(DvergerColor.WidenBeamHotkey.Value))
                {
                    Step = Mathf.Clamp(Step + 1, 0, _maxSteps);
                    changedStep = true;
                }
                else if (Input.GetKeyDown(DvergerColor.ToggleHotkey.Value))
                {
                    On = !On;
                    changedStep = true;
                }
            }

            if (changedStep)
            {
                ShowStatusMessage();
                zdo.Set(DvergerColor.StepDataKey, Step);
                zdo.Set(DvergerColor.OnDataKey, On);
            }

            if (Step == _maxSteps)
            {
                _light.type = LightType.Point;
                _light.intensity = _pointIntensity;
                _light.range = _pointRange;
            }
            else
            {
                var t = Step / (float) (_maxSteps - 1);

                _light.type = LightType.Spot;
                _light.spotAngle = Mathf.Lerp(_minAngle, _maxAngle,t );
                _light.intensity = Mathf.Lerp(_minIntensity, _maxIntensity, t);
                _light.range = Mathf.Lerp(_minRange, _maxRange, t);
            }

            var inBed = player.InBed();
            var turnOffInBed = DvergerColor.TurnOffInBed.Value;
            foreach (Light light in _lights) {
                if (turnOffInBed && inBed)
                {
                    light.enabled = false;
                }
                else
                {
                    light.enabled = On;
                }
            }
        }

        public void SetLights(Light[] lights)
        {
            _lights = lights;
            _light = lights[0];
            if (_light != null)
            {
                _light.color = DvergerColor.Color.Value;
            }

            ShowStatusMessage();

            if (Player.m_localPlayer != null)
            {
                var zdo = Player.m_localPlayer.GetZDO();
                Step = zdo.GetInt(DvergerColor.StepDataKey, DvergerColor.MaxSteps.Value - 1);
                On = zdo.GetBool(DvergerColor.OnDataKey, true);
            }
            else
            {
                Step = DvergerColor.MaxSteps.Value - 1;
                On = true;
            }
        }

        private void ShowStatusMessage()
        {
            if (MessageHud.instance != null)
            {
                if (!On)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Dverger Circlet: Off");
                }
                else
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, (Step == _maxSteps) ? "Dverger Circlet: Pool of Radiance" : $"Dverger Circlet: Luminous Beam {Step + 1}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "AttachItem")]
    public static class VisEquipment_AttachItem_Patch
    {
        private const int DvergerItemHash = 703889544;

        private static void Postfix(VisEquipment __instance, GameObject __result, int itemHash)
        {
            if (!__instance.m_isPlayer || __result == null || itemHash != DvergerItemHash)
            {
                return;
            }

            if (__result.GetComponent<LightChanger>() != null)
            {
                Object.Destroy(__result.GetComponent<LightChanger>());
            }

            var lights = __result.GetComponentsInChildren<Light>();

            if (lights.Length > 0)
            {
                __result.AddComponent<LightChanger>().SetLights(lights);
            }
        }
    }
}
