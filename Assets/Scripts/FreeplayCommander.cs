using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class FreeplayCommander
{
    public static void DebugLog(string message, params object[] args)
    {
        var debugstring = string.Format("[Freeplay Helper] {0}", message);
        Debug.LogFormat(debugstring, args);
    }

    public enum freeplaySelection
    {
        Timer = 0,
        Bombs,
        Modules,
        Needy,
        Hardcore,
        ModsOnly
    };

    #region Constructors
    static FreeplayCommander()
    {
        DebugLog("Initializing Static Constructor");
        _freeplayDeviceType = CommonReflectedTypeInfo.FreeplayDeviceType;
        _moduleCountIncrementField = _freeplayDeviceType.GetField("ModuleCountIncrement", BindingFlags.Public | BindingFlags.Instance);
        _moduleCountDecrementField = _freeplayDeviceType.GetField("ModuleCountDecrement", BindingFlags.Public | BindingFlags.Instance);
        _timeIncrementField = _freeplayDeviceType.GetField("TimeIncrement", BindingFlags.Public | BindingFlags.Instance);
        _timeDecrementField = _freeplayDeviceType.GetField("TimeDecrement", BindingFlags.Public | BindingFlags.Instance);
        _needyToggleField = _freeplayDeviceType.GetField("NeedyToggle", BindingFlags.Public | BindingFlags.Instance);
        _hardcoreToggleField = _freeplayDeviceType.GetField("HardcoreToggle", BindingFlags.Public | BindingFlags.Instance);
        _modsOnlyToggleField = _freeplayDeviceType.GetField("ModsOnly", BindingFlags.Public | BindingFlags.Instance);
        _startButtonField = _freeplayDeviceType.GetField("StartButton", BindingFlags.Public | BindingFlags.Instance);
        _currentSettingsField = _freeplayDeviceType.GetField("currentSettings", BindingFlags.NonPublic | BindingFlags.Instance);

        _freeplaySettingsType = ReflectionHelper.FindType("Assets.Scripts.Settings.FreeplaySettings");
        _moduleCountField = _freeplaySettingsType.GetField("ModuleCount", BindingFlags.Public | BindingFlags.Instance);
        _timeField = _freeplaySettingsType.GetField("Time", BindingFlags.Public | BindingFlags.Instance);
        _isHardCoreField = _freeplaySettingsType.GetField("IsHardCore", BindingFlags.Public | BindingFlags.Instance);
        _hasNeedyField = _freeplaySettingsType.GetField("HasNeedy", BindingFlags.Public | BindingFlags.Instance);
        _onlyModsField = _freeplaySettingsType.GetField("OnlyMods", BindingFlags.Public | BindingFlags.Instance);

        _floatingHoldableType = ReflectionHelper.FindType("FloatingHoldable");
        if (_floatingHoldableType == null)
        {
            DebugLog("Could not find _floatingHoldableType");
            return;
        }
        _pickupTimeField = _floatingHoldableType.GetField("PickupTime", BindingFlags.Public | BindingFlags.Instance);
        _holdStateProperty = _floatingHoldableType.GetProperty("HoldState", BindingFlags.Public | BindingFlags.Instance);

        _selectableType = ReflectionHelper.FindType("Selectable");
        _handleSelectMethod = _selectableType.GetMethod("HandleSelect", BindingFlags.Public | BindingFlags.Instance);
        _onInteractEndedMethod = _selectableType.GetMethod("OnInteractEnded", BindingFlags.Public | BindingFlags.Instance);
        _childrenField = _selectableType.GetField("Children", BindingFlags.Public | BindingFlags.Instance);
        

        _selectableManagerType = ReflectionHelper.FindType("SelectableManager");
        if (_selectableManagerType == null)
        {
            DebugLog("Could not find _selectableManagerType");
            return;
        }
        _selectMethod = _selectableManagerType.GetMethod("Select", BindingFlags.Public | BindingFlags.Instance);
        _handleInteractMethod = _selectableManagerType.GetMethod("HandleInteract", BindingFlags.Public | BindingFlags.Instance);
        _handleCancelMethod = _selectableManagerType.GetMethod("HandleCancel", BindingFlags.Public | BindingFlags.Instance);
        _setZSpinMethod = _selectableManagerType.GetMethod("SetZSpin", BindingFlags.Public | BindingFlags.Instance);
        _setControlsRotationMethod = _selectableManagerType.GetMethod("SetControlsRotation", BindingFlags.Public | BindingFlags.Instance);
        _getBaseHeldObjectTransformMethod = _selectableManagerType.GetMethod("GetBaseHeldObjectTransform", BindingFlags.Public | BindingFlags.Instance);
        _handleFaceSelectionMethod = _selectableManagerType.GetMethod("HandleFaceSelection", BindingFlags.Public | BindingFlags.Instance);

        _inputManagerType = ReflectionHelper.FindType("KTInputManager");
        if (_inputManagerType == null)
        {
            DebugLog("Could not find _inputManagerType");
            return;
        }
        _instanceProperty = _inputManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _selectableManagerProperty = _inputManagerType.GetProperty("SelectableManager", BindingFlags.Public | BindingFlags.Instance);

        _inputManager = (MonoBehaviour)_instanceProperty.GetValue(null, null);
        DebugLog("Static Constructor Initialization complete");
    }

    public FreeplayCommander(MonoBehaviour freeplayDevice)
    {
        FreeplayDevice = freeplayDevice;
        Selectable = (MonoBehaviour)FreeplayDevice.GetComponent(_selectableType);
        DebugLog("Freeplay device: Attempting to get the Selectable list.");
        SelectableChildren = (MonoBehaviour[]) _childrenField.GetValue(Selectable);
        FloatingHoldable = (MonoBehaviour)FreeplayDevice.GetComponent(_floatingHoldableType);
        SelectableManager = (MonoBehaviour)_selectableManagerProperty.GetValue(_inputManager, null);
    }
    #endregion


    #region Helper Methods

    private freeplaySelection _index = freeplaySelection.Timer;
    private static int _bombCount = 1;

    public IEnumerator HandleInput()
    {
        if (!Input.GetKeyDown(KeyCode.LeftArrow) && !Input.GetKeyDown(KeyCode.RightArrow) &&
            !Input.GetKeyDown(KeyCode.UpArrow) && !Input.GetKeyDown(KeyCode.DownArrow) &&
            !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            yield break;
        }
        int holdState = (int)_holdStateProperty.GetValue(FloatingHoldable, null);
        if (holdState != 0)
        {
            _index = 0;
            IEnumerator hold = HoldFreeplayDevice();
            while (hold.MoveNext())
                yield return hold.Current;
            ToggleIndex();
            yield break;
        }
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            StartBomb();
            yield break;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _index--;
            if (_index == freeplaySelection.Bombs && !IsDualBombInstalled())
                _index = freeplaySelection.Timer;
            if (_index < freeplaySelection.Timer)
                _index = freeplaySelection.ModsOnly;
            ToggleIndex();
            yield break;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _index++;
            if (_index == freeplaySelection.Bombs && !IsDualBombInstalled())
                _index = freeplaySelection.Modules;
            if (_index > freeplaySelection.ModsOnly)
                _index = freeplaySelection.Timer;
            ToggleIndex();
            yield break;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            IEnumerator handler = null;
            switch (_index)
            {
                case freeplaySelection.Timer:
                    handler = Input.GetKeyDown(KeyCode.LeftArrow) ? DecrementBombTimer() : IncrementBombTimer();
                    break;
                case freeplaySelection.Bombs:
                    handler = SetBombCount();
                    break;
                case freeplaySelection.Modules:
                    handler = Input.GetKeyDown(KeyCode.LeftArrow) ? DecrementModuleCount() : IncrementModuleCount();
                    break; 
                case freeplaySelection.Needy:
                    handler = SetNeedy();
                    break;
                case freeplaySelection.Hardcore:
                    handler = SetHardcore();
                    break;
                case freeplaySelection.ModsOnly:
                    handler = SetModsOnly();
                    break;
            }
            if (handler == null)
                yield break;
            while (handler.MoveNext())
            {
                yield return handler.Current;
            }
        }
    }

    public void ToggleIndex()
    {
        object currentSettings = _currentSettingsField.GetValue(FreeplayDevice);
        int currentModuleCount = (int)_moduleCountField.GetValue(currentSettings);
        float currentTime = (float) _timeField.GetValue(currentSettings);
        bool onlyMods = (bool) _onlyModsField.GetValue(currentSettings);
        switch (_index)
        {
            case freeplaySelection.Timer:
                MonoBehaviour timerUp = (MonoBehaviour)_timeIncrementField.GetValue(FreeplayDevice);
                MonoBehaviour timerDown = (MonoBehaviour) _timeDecrementField.GetValue(FreeplayDevice);
                SelectObject((MonoBehaviour)timerUp.GetComponent(_selectableType));
                if (Mathf.FloorToInt(currentTime) == Mathf.FloorToInt((float) _timeField.GetValue(currentSettings)))
                    break;
                SelectObject((MonoBehaviour)timerDown.GetComponent(_selectableType));
                break;
            case freeplaySelection.Bombs:
                SelectObject(SelectableChildren[_bombCount + 1]);
                break;
            case freeplaySelection.Modules:
                MonoBehaviour moduleUp = (MonoBehaviour) _moduleCountIncrementField.GetValue(FreeplayDevice);
                MonoBehaviour moduleDown = (MonoBehaviour) _moduleCountDecrementField.GetValue(FreeplayDevice);
                SelectObject((MonoBehaviour)moduleUp.GetComponent(_selectableType));
                if (currentModuleCount == (int) _moduleCountField.GetValue(currentSettings))
                    break;
                SelectObject((MonoBehaviour)moduleDown.GetComponent(_selectableType));
                break;
            case freeplaySelection.Needy:
                MonoBehaviour needyToggle = (MonoBehaviour)_needyToggleField.GetValue(FreeplayDevice);
                SelectObject((MonoBehaviour)needyToggle.GetComponent(_selectableType));
                SelectObject((MonoBehaviour)needyToggle.GetComponent(_selectableType));
                break;
            case freeplaySelection.Hardcore:
                MonoBehaviour hardcoreToggle = (MonoBehaviour)_hardcoreToggleField.GetValue(FreeplayDevice);
                SelectObject((MonoBehaviour)hardcoreToggle.GetComponent(_selectableType));
                SelectObject((MonoBehaviour)hardcoreToggle.GetComponent(_selectableType));
                break;
            case freeplaySelection.ModsOnly:
                MonoBehaviour modsToggle = (MonoBehaviour)_modsOnlyToggleField.GetValue(FreeplayDevice);
                SelectObject((MonoBehaviour)modsToggle.GetComponent(_selectableType));
                bool onlyModsCurrent = (bool) _onlyModsField.GetValue(currentSettings);
                SelectObject((MonoBehaviour)modsToggle.GetComponent(_selectableType));
                if (onlyMods == onlyModsCurrent )
                {
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        _index = freeplaySelection.Timer;
                        goto case freeplaySelection.Timer;
                    }
                    else
                    {
                        _index = freeplaySelection.Hardcore;
                        goto case freeplaySelection.Hardcore;
                    }
                }
                break;
        }
    }

    private static float startDelay = 0.2f;
    private static float Acceleration = 0.005f;
    private static float minDelay = 0.01f;

    public IEnumerator IncrementBombTimer()
    {
        MonoBehaviour button = (MonoBehaviour) _timeIncrementField.GetValue(FreeplayDevice);
        MonoBehaviour buttonSelectable = (MonoBehaviour)button.GetComponent(_selectableType);
        float delay = startDelay;
        while (Input.GetKey(KeyCode.RightArrow))
        {
            SelectObject(buttonSelectable);
            yield return new WaitForSeconds(Mathf.Max(delay, minDelay));
            delay -= Acceleration;
        }
    }

    public IEnumerator DecrementBombTimer()
    {
        MonoBehaviour button = (MonoBehaviour)_timeDecrementField.GetValue(FreeplayDevice);
        MonoBehaviour buttonSelectable = (MonoBehaviour)button.GetComponent(_selectableType);
        float delay = startDelay;
        while (Input.GetKey(KeyCode.LeftArrow))
        {
            SelectObject(buttonSelectable);
            yield return new WaitForSeconds(Mathf.Max(delay, minDelay));
            delay -= Acceleration;
        }
    }

    public IEnumerator IncrementModuleCount()
    {
        MonoBehaviour button = (MonoBehaviour)_moduleCountIncrementField.GetValue(FreeplayDevice);
        MonoBehaviour buttonSelectable = (MonoBehaviour)button.GetComponent(_selectableType);
        float delay = startDelay;
        while (Input.GetKey(KeyCode.RightArrow))
        {
            SelectObject(buttonSelectable);
            yield return new WaitForSeconds(Mathf.Max(delay, minDelay));
            delay -= Acceleration;
        }
    }

    public IEnumerator DecrementModuleCount()
    {
        MonoBehaviour button = (MonoBehaviour)_moduleCountDecrementField.GetValue(FreeplayDevice);
        MonoBehaviour buttonSelectable = (MonoBehaviour)button.GetComponent(_selectableType);
        float delay = startDelay;
        while (Input.GetKey(KeyCode.LeftArrow))
        {
            SelectObject(buttonSelectable);
            yield return new WaitForSeconds(Mathf.Max(delay, minDelay));
            delay -= Acceleration;
        }
    }

    public bool IsDualBombInstalled()
    {
        return SelectableChildren.Length == 11;
    }

    public IEnumerator SetBombCount()
    {
        _bombCount = Input.GetKeyDown(KeyCode.LeftArrow) ? 1 : 2;
        SelectObject(Input.GetKeyDown(KeyCode.LeftArrow) ? SelectableChildren[2] : SelectableChildren[3]);
        yield return null;
    }

    public IEnumerator SetNeedy()
    {
        bool on = !Input.GetKeyDown(KeyCode.LeftArrow);
        object currentSettings = _currentSettingsField.GetValue(FreeplayDevice);
        bool hasNeedy = (bool)_hasNeedyField.GetValue(currentSettings);
        if (hasNeedy != on)
        {
            MonoBehaviour needyToggle = (MonoBehaviour)_needyToggleField.GetValue(FreeplayDevice);
            SelectObject( (MonoBehaviour)needyToggle.GetComponent(_selectableType) );
            
        }
        yield return null;
    }

    public IEnumerator SetHardcore()
    {
        bool on = !Input.GetKeyDown(KeyCode.LeftArrow);
        object currentSettings = _currentSettingsField.GetValue(FreeplayDevice);
        bool isHardcore = (bool)_isHardCoreField.GetValue(currentSettings);
        if (isHardcore != on)
        {
            MonoBehaviour hardcoreToggle = (MonoBehaviour)_hardcoreToggleField.GetValue(FreeplayDevice);
            SelectObject( (MonoBehaviour)hardcoreToggle.GetComponent(_selectableType) );
        }
        yield return null;
    }

    public IEnumerator SetModsOnly()
    {
        bool on = !Input.GetKeyDown(KeyCode.LeftArrow);
        object currentSettings = _currentSettingsField.GetValue(FreeplayDevice);
        bool onlyMods = (bool)_onlyModsField.GetValue(currentSettings);
        if (onlyMods != on)
        {
            MonoBehaviour modsToggle = (MonoBehaviour)_modsOnlyToggleField.GetValue(FreeplayDevice);
            SelectObject( (MonoBehaviour)modsToggle.GetComponent(_selectableType) );
        }
        yield return null;
    }

    public void StartBomb()
    {
        MonoBehaviour startButton = (MonoBehaviour)_startButtonField.GetValue(FreeplayDevice);
        SelectObject( (MonoBehaviour)startButton.GetComponent(_selectableType) );
    }

    public IEnumerator HoldFreeplayDevice()
    {
        int holdState = (int)_holdStateProperty.GetValue(FloatingHoldable, null);

        if (holdState != 0)
        {
            SelectObject(Selectable);

            float holdTime = (float)_pickupTimeField.GetValue(FloatingHoldable);
            IEnumerator forceRotationCoroutine = ForceHeldRotation(holdTime);
            while (forceRotationCoroutine.MoveNext())
            {
                yield return forceRotationCoroutine.Current;
            }
        }
    }

    public void LetGoFreeplayDevice()
    {
        int holdState = (int)_holdStateProperty.GetValue(FloatingHoldable, null);
        if (holdState == 0)
        {
            DeselectObject(Selectable);
        }
    }

    private void SelectObject(MonoBehaviour selectable)
    {
        _handleSelectMethod.Invoke(selectable, new object[] { true });
        _selectMethod.Invoke(SelectableManager, new object[] { selectable, true });
        _handleInteractMethod.Invoke(SelectableManager, null);
        _onInteractEndedMethod.Invoke(selectable, null);
    }

    private void DeselectObject(MonoBehaviour selectable)
    {
        _handleCancelMethod.Invoke(SelectableManager, null);
    }

    private IEnumerator ForceHeldRotation(float duration)
    {
        Transform baseTransform = (Transform)_getBaseHeldObjectTransformMethod.Invoke(SelectableManager, null);

        float initialTime = Time.time;
        while (Time.time - initialTime < duration)
        {
            Quaternion currentRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

            _setZSpinMethod.Invoke(SelectableManager, new object[] { 0.0f });
            _setControlsRotationMethod.Invoke(SelectableManager, new object[] { baseTransform.rotation * currentRotation });
            _handleFaceSelectionMethod.Invoke(SelectableManager, null);
            yield return null;
        }

        _setZSpinMethod.Invoke(SelectableManager, new object[] { 0.0f });
        _setControlsRotationMethod.Invoke(SelectableManager, new object[] { baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, 0.0f) });
        _handleFaceSelectionMethod.Invoke(SelectableManager, null);
    }
    #endregion

    #region Readonly Fields
    public readonly MonoBehaviour FreeplayDevice = null;
    public readonly MonoBehaviour Selectable = null;
    public readonly MonoBehaviour[] SelectableChildren = null;
    public readonly MonoBehaviour FloatingHoldable = null;
    private readonly MonoBehaviour SelectableManager = null;
    #endregion

    #region Private Static Fields
    private static Type _freeplayDeviceType = null;
    private static FieldInfo _moduleCountIncrementField = null;
    private static FieldInfo _moduleCountDecrementField = null;
    private static FieldInfo _timeIncrementField = null;
    private static FieldInfo _timeDecrementField = null;
    private static FieldInfo _needyToggleField = null;
    private static FieldInfo _hardcoreToggleField = null;
    private static FieldInfo _modsOnlyToggleField = null;
    private static FieldInfo _startButtonField = null;
    private static FieldInfo _currentSettingsField = null;

    private static Type _freeplaySettingsType = null;
    private static FieldInfo _moduleCountField = null;
    private static FieldInfo _timeField = null;
    private static FieldInfo _isHardCoreField = null;
    private static FieldInfo _hasNeedyField = null;
    private static FieldInfo _onlyModsField = null;

    private static Type _floatingHoldableType = null;
    private static FieldInfo _pickupTimeField = null;
    private static PropertyInfo _holdStateProperty = null;

    private static Type _selectableType = null;
    private static MethodInfo _handleSelectMethod = null;
    private static MethodInfo _onInteractEndedMethod = null;
    private static FieldInfo _childrenField = null;

    private static Type _selectableManagerType = null;
    private static MethodInfo _selectMethod = null;
    private static MethodInfo _handleInteractMethod = null;
    private static MethodInfo _handleCancelMethod = null;
    private static MethodInfo _setZSpinMethod = null;
    private static MethodInfo _setControlsRotationMethod = null;
    private static MethodInfo _getBaseHeldObjectTransformMethod = null;
    private static MethodInfo _handleFaceSelectionMethod = null;

    private static Type _inputManagerType = null;
    private static PropertyInfo _instanceProperty = null;
    private static PropertyInfo _selectableManagerProperty = null;

    private static MonoBehaviour _inputManager = null;
    #endregion
}

