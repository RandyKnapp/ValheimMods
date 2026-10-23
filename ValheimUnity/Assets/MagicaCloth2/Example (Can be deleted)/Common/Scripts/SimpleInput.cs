// Magica Cloth 2.
// Copyright (c) 2025 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;
#if MC2_INPUTSYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace MagicaCloth2
{
    /// <summary>
    /// InputManager/InputSystem入力切り替えラッパー
    /// </summary>
    public class SimpleInput
    {
#if MC2_INPUTSYSTEM
        // (New) Imput System
        public static void Init()
        {
            EnhancedTouchSupport.Enable();
        }

        public static int touchCount
        {
            get
            {
                return UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count;
            }
        }

        public static UnityEngine.Touch GetTouch(int index)
        {
            var touchData = new UnityEngine.Touch();

            int count = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count;
            if (index < count)
            {
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[index];

                // convert
                touchData.fingerId = touch.finger.index;
                touchData.position = touch.screenPosition;
                touchData.deltaPosition = touch.delta;
                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        touchData.phase = UnityEngine.TouchPhase.Canceled;
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                        touchData.phase = UnityEngine.TouchPhase.Ended;
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        touchData.phase = UnityEngine.TouchPhase.Moved;
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        touchData.phase = UnityEngine.TouchPhase.Began;
                        break;
                }
            }

            return touchData;
        }

        public static bool GetKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Escape:
                    return Keyboard.current.escapeKey.isPressed;
                default:
                    return false;
            }
        }

        public static bool GetKeyDown(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Backspace:
                    return Keyboard.current.backspaceKey.wasPressedThisFrame;
                default:
                    return false;
            }
        }

        public static bool GetMouseButtonDown(int button)
        {
            switch (button)
            {
                case 0:
                    return Mouse.current.leftButton.wasPressedThisFrame;
                case 1:
                    return Mouse.current.rightButton.wasPressedThisFrame;
                case 2:
                    return Mouse.current.middleButton.wasPressedThisFrame;
                default:
                    return false;
            }
        }

        public static bool GetMouseButtonUp(int button)
        {
            switch (button)
            {
                case 0:
                    return Mouse.current.leftButton.wasReleasedThisFrame;
                case 1:
                    return Mouse.current.rightButton.wasReleasedThisFrame;
                case 2:
                    return Mouse.current.middleButton.wasReleasedThisFrame;
                default:
                    return false;
            }
        }

        public static Vector3 mousePosition
        {
            get
            {
                return Mouse.current.position.ReadValue();
            }
        }

        public static float GetMouseScrollWheel()
        {
            // 古いInputSystemに不具合あり
            // ホイールデルタがデフォルトでx120されて返ってくる
            // これはWindowsのみの挙動でMac/Linuxでは発生しない
            // そしてUnity 2023.2以降は修正された

            float value = Mouse.current.scroll.ReadValue().y * 0.12f; // base
#if UNITY_2023_2_OR_NEWER
            return value;
#else
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return value / 120.0f; // ホイールデルタの不具合を吸収
#else
            return value;
#endif
#endif
        }
#else
        // (Old) Imput Manager
        public static void Init()
        {
        }

        public static int touchCount
        {
            get
            {
                return Input.touchCount;
            }
        }

        public static Touch GetTouch(int index)
        {
            return Input.GetTouch(index);
        }

        public static bool GetKey(KeyCode key)
        {
            return Input.GetKey(key);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        public static bool GetMouseButtonDown(int button)
        {
            return Input.GetMouseButtonDown(button);
        }

        public static bool GetMouseButtonUp(int button)
        {
            return Input.GetMouseButtonUp(button);
        }

        public static Vector3 mousePosition
        {
            get
            {
                return Input.mousePosition;
            }
        }

        public static float GetMouseScrollWheel()
        {
            return Input.GetAxis("Mouse ScrollWheel");
        }
#endif
    }
}
