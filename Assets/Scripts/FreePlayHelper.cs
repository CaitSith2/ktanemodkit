using UnityEngine;
using System.Collections;

public class FreePlayHelper : MonoBehaviour
{

    private FreeplayCommander _freeplayCommander = null;
    private KMGameInfo _gameInfo = null;
    private KMGameInfo.State _state;

    public static void DebugLog(string message, params object[] args)
    {
        var debugstring = string.Format("[Freeplay Helper] {0}", message);
        Debug.LogFormat(debugstring, args);
    }

    // Use this for initialization
    void Start ()
	{
        DebugLog("Starting service");
	    _gameInfo = GetComponent<KMGameInfo>();
	    _gameInfo.OnStateChange += OnStateChange;
	}
	
	// Update is called once per frame
    private IEnumerator _handler = null;
	void Update ()
	{
	    if (_freeplayCommander == null) return;
	    if (Input.GetKeyDown(KeyCode.LeftArrow) ||
	        Input.GetKeyDown(KeyCode.RightArrow) ||
	        Input.GetKeyDown(KeyCode.UpArrow) ||
	        Input.GetKeyDown(KeyCode.DownArrow) ||
	        Input.GetKeyDown(KeyCode.KeypadEnter) || 
            Input.GetKeyDown(KeyCode.Return))
	    {
	        if (_handler != null)
	            StopCoroutine(_handler);
	        _handler = _freeplayCommander.HandleInput();
	        StartCoroutine(_handler);
        }
	}

    void OnStateChange(KMGameInfo.State state)
    {
        DebugLog("Current state = {0}", state.ToString());
        StopAllCoroutines();
        if (state == KMGameInfo.State.Setup)
        {
            StartCoroutine(CheckForFreeplayDevice());
        }
        else
        {
            _freeplayCommander = null;
            _handler = null;
        }
    }

    private IEnumerator CheckForFreeplayDevice()
    {
        yield return null;
        DebugLog("Attempting to finde Freeplay device");
        while (true)
        {
            UnityEngine.Object[] freeplayDevices = FindObjectsOfType(CommonReflectedTypeInfo.FreeplayDeviceType);
            if (freeplayDevices != null && freeplayDevices.Length > 0)
            {
                DebugLog("Freeplay Device found - Hooking into it.");
                _freeplayCommander = new FreeplayCommander((MonoBehaviour)freeplayDevices[0]);
                break;
            }

            yield return null;
        }
    }
}
