using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Random = UnityEngine.Random;

public class MultipleWidgets : MonoBehaviour
{

    public GameObject[] Batteries;
    public TextMesh KeyText;
    public TextMesh PortText;
    public TextMesh IndicatorText;

    public KMBombInfo Info;
    public AudioClip Notify;

    private bool _indicator = false;
    private string _indicatorLabel;
    private bool _indicatorLight;

    private bool _ports = false;
    private PortType _presentPorts = (PortType) 0;

    private bool _batteries = false;
    private BatteryType _batteryType = BatteryType.NineVolt;

    private bool _twofactor = false;
    private int _key;
    private float _timeElapsed;

    private const float TimerLength = 60.0f;

    public static string WidgetQueryTwofactor = "twofactor";
    public static string WidgetTwofactorKey = "twofactor_key";

    private Type _indicatorWidgetType = null;
    private FieldInfo _indicatorLabelsField = null;
    private List<string> _knownIndicators = null;

    void Awake()
    {
        PortText.text = string.Empty;
        IndicatorText.text = string.Empty;
        KeyText.text = string.Empty;

        foreach (GameObject battery in Batteries)
        {
            battery.SetActive(false);
        }

        GetComponent<KMWidget>().OnQueryRequest += GetQueryResponse;
        GetComponent<KMWidget>().OnWidgetActivate += Activate;

        _indicatorWidgetType = ReflectionHelper.FindType("IndicatorWidget");
        _indicatorLabelsField = _indicatorWidgetType.GetField("Labels", BindingFlags.Public | BindingFlags.Static);
        _knownIndicators = (List<string>)_indicatorLabelsField.GetValue(null);

        string[] widgetTypes =
        {
            "Indicator", "Ports", "Batteries", "TwoFactor"
        };
        List<int> WidgetSet = new List<int> { 0, 1, 2, 3 };
        for (var i = 0; i < 2; i++)
        {
            var widget = WidgetSet[Random.Range(0, WidgetSet.Count)];
            WidgetSet.Remove(widget);
            Debug.LogFormat("[MultipleWidgets] Widget #{0} = {1}", i + 1, widgetTypes[widget]);
            if (widget == 0)
            {
                _indicator = true;
                SetIndicators();
            }
            else if (widget == 1)
            {
                _ports = true;
                SetPorts();
            }
            else if (widget == 2)
            {
                _batteries = true;
                SetBatteries();
            }
            else
            {
                _twofactor = true;
                SetTwoFactor();
            }
        }
    }

    void Start ()
    {
        
	}

    void SetIndicators()
    {
        var KnownIndicators = new List<string>(_knownIndicators);
        _indicatorLight = Random.value > 0.4f;
        foreach (var exsting in Info.GetIndicators())
        {
            if (KnownIndicators.Contains(exsting))
                KnownIndicators.Remove(exsting);
        }
        _indicatorLabel = KnownIndicators.Count > 0 ? KnownIndicators[Random.Range(0, KnownIndicators.Count)] : "NLL";
        Debug.LogFormat("[IndicatorWidget] Randomizing Indicator Widget: {0} {1}", (!_indicatorLight) ? "unlit" : "lit", _indicatorLabel);
        IndicatorText.text = string.Format("{0} {1}", (!_indicatorLight) ? "unlit" : "lit", _indicatorLabel);
    }

    void SetPorts()
    {
        List<List<PortType>> portGroups = new List<List<PortType>>
        {
            new List<PortType> {PortType.Parallel, PortType.Serial},
            new List<PortType> {PortType.DVI, PortType.PS2, PortType.RJ45, PortType.StereoRCA},
            new List<PortType> {PortType.HDMI, PortType.USB, PortType.Component}
        };
        List<PortType> ports = portGroups[Random.Range(0, portGroups.Count)];
        foreach (PortType port in ports)
        {
            if (Random.value > 0.5f)
            {
                _presentPorts |= port;
            }
        }

        string presentPorts = string.Empty;
        for (var i = (int) PortType.Component; i > 0; i >>= 1)
        {
            if (((int) _presentPorts & i) == i)
            {
                if (presentPorts != string.Empty)
                    presentPorts += ", ";
                presentPorts += ((PortType) i).ToString();
            }
        }

        Debug.LogFormat("[PortWidget] Randomizing Port Widget: {0}", presentPorts);
        PortText.text = "Ports" + Environment.NewLine + (presentPorts == string.Empty ? "Empty Port Plate" : presentPorts);
    }

    public bool IsPortPresent(PortType port)
    {
        return (_presentPorts & port) == port;
    }

    void SetBatteries()
    {
        _batteryType = (BatteryType) Random.Range(0, Batteries.Length);
        Debug.LogFormat("[BatteryWidget] Randomizing Battery Widget: {0}", GetNumberOfBatteries());
        Batteries[(int) _batteryType].SetActive(true);
    }

    int GetNumberOfBatteries()
    {
        return (int) _batteryType;
    }

    void SetTwoFactor()
    {
        GenerateKey();
    }

    private void GenerateKey()
    {
        _key = Random.Range(0, 1000000);
    }

    private void DisplayKey()
    {
        KeyText.text = _key.ToString() + ".";
    }

    void UpdateKey()
    {
        GetComponent<KMAudio>().HandlePlaySoundAtTransform(Notify.name, transform);
        GenerateKey();
        DisplayKey();
    }


    void Update () {
        if (_twofactor)
        {
            _timeElapsed += Time.deltaTime;

            if (_timeElapsed >= TimerLength)
            {
                _timeElapsed = 0f;
                UpdateKey();
            }
        }
    }

    public void Activate()
    {
        if (_twofactor)
        {
            _timeElapsed = 0f;
            DisplayKey();
        }
        if (_batteries)
        {
            
        }
        if (_ports)
        {
            
        }
        if (_indicator)
        {
            
        }
    }

    public string GetQueryResponse(string queryKey, string queryInfo)
    {
        if (queryKey == KMBombInfo.QUERYKEY_GET_BATTERIES && _batteries)
        {
            return JsonConvert.SerializeObject(new Dictionary<string, int>
            {
                {
                    "numbatteries",
                    GetNumberOfBatteries()
                }
            });
        }

        if (queryKey == KMBombInfo.QUERYKEY_GET_INDICATOR && _indicator)
        {
            return JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                {
                    "label",
                    _indicatorLabel
                },
                {
                    "on",
                    string.Empty + _indicatorLight
                }
            });
        }

        if (queryKey == KMBombInfo.QUERYKEY_GET_PORTS && _ports)
        {
            Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
            List<string> list = new List<string>();
            if (IsPortPresent(PortType.DVI))
            {
                list.Add("DVI");
            }
            if (IsPortPresent(PortType.Parallel))
            {
                list.Add("Parallel");
            }
            if (IsPortPresent(PortType.PS2))
            {
                list.Add("PS2");
            }
            if (IsPortPresent(PortType.RJ45))
            {
                list.Add("RJ45");
            }
            if (IsPortPresent(PortType.Serial))
            {
                list.Add("Serial");
            }
            if (IsPortPresent(PortType.StereoRCA))
            {
                list.Add("StereoRCA");
            }
            if (IsPortPresent(PortType.HDMI))
            {
                list.Add("HDMI");
            }
            if (IsPortPresent(PortType.USB))
            {
                list.Add("USB");
            }
            if (IsPortPresent(PortType.Component))
            {
                list.Add("ComponentVideo");
            }
            dictionary.Add("presentPorts", list);
            return JsonConvert.SerializeObject(dictionary);
        }

        if (queryKey == WidgetQueryTwofactor && _twofactor)
        {
            var response = new Dictionary<string, int> { { WidgetTwofactorKey, _key } };
            var serializedResponse = JsonConvert.SerializeObject(response);

            return serializedResponse;
        }
        return "";
    }

    public enum BatteryType
    {
        Empty,
        NineVolt,
        AAx2,
        _9Vx1_AAx2,
        AAx4
    }

    public enum PortType
    {
        Serial = 1,
        Parallel = 2,

        DVI = 4,
        PS2 = 8,
        RJ45 = 16,
        StereoRCA = 32,

        HDMI = 64,
        USB = 128,
        Component = 256,
    }
}
