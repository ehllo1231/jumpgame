#if ENABLE_LEGACY_INPUT_MANAGER
using System;
using System.Collections.Generic;
#endif
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class KeyboardTouchJumpInput : MonoBehaviour, IJumpInput
{
    [SerializeField] private bool allowMouseClickInEditor = true;

#if ENABLE_LEGACY_INPUT_MANAGER
    private static readonly KeyCode[] KeyboardKeys = BuildKeyboardKeyList();
    private bool warnedLegacyInputUnavailable;
    private bool legacyInputUnavailable;
#endif
    private bool fallbackJumpQueued;
    private bool fallbackJumpHeld;
    private bool fallbackJumpReleasedQueued;

    public bool JumpPressedThisFrame
    {
        get
        {
            if (ConsumeFallbackJump())
            {
                return true;
            }

#if ENABLE_INPUT_SYSTEM
            if (IsNewInputSystemJumpPressedThisFrame())
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (legacyInputUnavailable)
            {
                return false;
            }

            try
            {
                return IsKeyboardPressedThisFrame() || IsTouchStartedThisFrame() || IsEditorMousePressedThisFrame();
            }
            catch (InvalidOperationException)
            {
                legacyInputUnavailable = true;

                if (!warnedLegacyInputUnavailable)
                {
                    warnedLegacyInputUnavailable = true;
                    Debug.LogWarning("KeyboardTouchJumpInput could not read legacy Input. Install Input System or set Active Input Handling to Both.");
                }

                return false;
            }
#endif

            return false;
        }
    }

    public bool JumpHeld
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if (IsNewInputSystemJumpHeld())
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!legacyInputUnavailable)
            {
                try
                {
                    return IsKeyboardHeld() || IsTouchHeld() || IsEditorMouseHeld();
                }
                catch (InvalidOperationException)
                {
                    legacyInputUnavailable = true;
                }
            }
#endif

            return fallbackJumpHeld;
        }
    }

    public bool JumpReleasedThisFrame
    {
        get
        {
            if (ConsumeFallbackJumpRelease())
            {
                return true;
            }

#if ENABLE_INPUT_SYSTEM
            if (IsNewInputSystemJumpReleasedThisFrame())
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!legacyInputUnavailable)
            {
                try
                {
                    return IsKeyboardReleasedThisFrame() || IsTouchEndedThisFrame() || IsEditorMouseReleasedThisFrame();
                }
                catch (InvalidOperationException)
                {
                    legacyInputUnavailable = true;
                }
            }
#endif

            return false;
        }
    }

    private void OnGUI()
    {
        var currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode != KeyCode.None && !fallbackJumpHeld)
        {
            fallbackJumpQueued = true;
            fallbackJumpHeld = true;
        }

        if (currentEvent.type == EventType.KeyUp && currentEvent.keyCode != KeyCode.None)
        {
            fallbackJumpHeld = false;
            fallbackJumpReleasedQueued = true;
        }

#if UNITY_EDITOR
        if (allowMouseClickInEditor && currentEvent.type == EventType.MouseDown && !fallbackJumpHeld)
        {
            fallbackJumpQueued = true;
            fallbackJumpHeld = true;
        }

        if (allowMouseClickInEditor && currentEvent.type == EventType.MouseUp)
        {
            fallbackJumpHeld = false;
            fallbackJumpReleasedQueued = true;
        }
#endif
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    private static KeyCode[] BuildKeyboardKeyList()
    {
        var keys = new List<KeyCode>();
        var values = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        foreach (var keyCode in values)
        {
            if (keyCode == KeyCode.None)
            {
                continue;
            }

            if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6)
            {
                continue;
            }

            keys.Add(keyCode);
        }

        return keys.ToArray();
    }

    private static bool IsKeyboardPressedThisFrame()
    {
        if (Input.anyKeyDown)
        {
            return true;
        }

        foreach (var keyCode in KeyboardKeys)
        {
            if (Input.GetKeyDown(keyCode))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsKeyboardHeld()
    {
        if (Input.anyKey)
        {
            return true;
        }

        foreach (var keyCode in KeyboardKeys)
        {
            if (Input.GetKey(keyCode))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsKeyboardReleasedThisFrame()
    {
        foreach (var keyCode in KeyboardKeys)
        {
            if (Input.GetKeyUp(keyCode))
            {
                return true;
            }
        }

        return false;
    }
#endif

#if ENABLE_INPUT_SYSTEM
    private bool IsNewInputSystemJumpPressedThisFrame()
    {
        if (IsAnyKeyboardKeyPressedThisFrame())
        {
            return true;
        }

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

#if UNITY_EDITOR
        if (allowMouseClickInEditor && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

        return false;
    }

    private bool IsNewInputSystemJumpHeld()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
        {
            return true;
        }

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed)
                {
                    return true;
                }
            }
        }

#if UNITY_EDITOR
        if (allowMouseClickInEditor && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }
#endif

        return false;
    }

    private bool IsNewInputSystemJumpReleasedThisFrame()
    {
        if (IsAnyKeyboardKeyReleasedThisFrame())
        {
            return true;
        }

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasReleasedThisFrame)
                {
                    return true;
                }
            }
        }

#if UNITY_EDITOR
        if (allowMouseClickInEditor && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            return true;
        }
#endif

        return false;
    }

    private static bool IsAnyKeyboardKeyPressedThisFrame()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        foreach (var key in Keyboard.current.allKeys)
        {
            if (key.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAnyKeyboardKeyReleasedThisFrame()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        foreach (var key in Keyboard.current.allKeys)
        {
            if (key.wasReleasedThisFrame)
            {
                return true;
            }
        }

        return false;
    }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    private static bool IsTouchStartedThisFrame()
    {
        for (var i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTouchHeld()
    {
        return Input.touchCount > 0;
    }

    private static bool IsTouchEndedThisFrame()
    {
        for (var i = 0; i < Input.touchCount; i++)
        {
            var phase = Input.GetTouch(i).phase;
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsEditorMousePressedThisFrame()
    {
#if UNITY_EDITOR
        return allowMouseClickInEditor && Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private bool IsEditorMouseHeld()
    {
#if UNITY_EDITOR
        return allowMouseClickInEditor && Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    private bool IsEditorMouseReleasedThisFrame()
    {
#if UNITY_EDITOR
        return allowMouseClickInEditor && Input.GetMouseButtonUp(0);
#else
        return false;
#endif
    }
#endif

    private bool ConsumeFallbackJump()
    {
        if (!fallbackJumpQueued)
        {
            return false;
        }

        fallbackJumpQueued = false;
        return true;
    }

    private bool ConsumeFallbackJumpRelease()
    {
        if (!fallbackJumpReleasedQueued)
        {
            return false;
        }

        fallbackJumpReleasedQueued = false;
        return true;
    }
}
