using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Random = UnityEngine.Random;

public class MultipleWidgets : MonoBehaviour
{

    public TextMesh WidgetText;
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
    private BattryType _batteryType = BattryType.NineVolt;

    private bool _twofactor = false;
    private int _key;
    private float _timeElapsed;

    private const float TimerLength = 60.0f;

    public static string WidgetQueryTwofactor = "twofactor";
    public static string WidgetTwofactorKey = "twofactor_key";

    private Type _indicatorWidgetType = null;
    private FieldInfo _indicatorLabelsField = null;
    private List<string> _knownIndicators = null;

    void Start ()
	{
	    GetComponent<KMWidget>().OnQueryRequest += GetQueryResponse;
	    GetComponent<KMWidget>().OnWidgetActivate += Activate;

	    _indicatorWidgetType = ReflectionHelper.FindType("IndicatorWidget");
	    _indicatorLabelsField = _indicatorWidgetType.GetField("Labels", BindingFlags.Public | BindingFlags.Static);
	    _knownIndicators = (List<string>)_indicatorLabelsField.GetValue(null);

        List<int> WidgetSet = new List<int> {0,1,2,3};
	    for (var i = 0; i < 2; i++)
	    {
	        var widget = WidgetSet[Random.Range(0, WidgetSet.Count)];
	        WidgetSet.Remove(widget);
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
        Debug.LogFormat("Randomizing Indicator Widget: {0} {1}", (!_indicatorLight) ? "unlit" : "lit", _indicatorLabel);
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
        Debug.LogFormat("Randomizing Port Widget: {0}", _presentPorts);
        PortText.text = "Ports" + Environment.NewLine + _presentPorts.ToString();
    }

    public bool IsPortPresent(PortType port)
    {
        return (_presentPorts & port) == port;
    }

    void SetBatteries()
    {
        _batteryType = (BattryType) Random.Range(0, 4);
        Debug.LogFormat("Randomizing Battery Widget: {0}", GetNumberOfBatteries());
        switch (_batteryType)
        {
            case BattryType.CR2032:
                WidgetText.text = "2 x CR2032 Batteries";
                break;
            case BattryType.NineVolt:
                WidgetText.text = "1 x 9-Volt Battery";
                break;
            case BattryType.TripleA:
                WidgetText.text = "3 x AAA Batteries";
                break;
            default:
                WidgetText.text = "Empty Battery Holder";
                break;
        }
    }

    int GetNumberOfBatteries()
    {
        switch (_batteryType)
        {
            case BattryType.NineVolt:
                return 1;
            case BattryType.CR2032:
                return 2;
            case BattryType.TripleA:
                return 3;
            default:
                return 0;
        }
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
	    _timeElapsed += Time.deltaTime;

	    // ReSharper disable once InvertIf
	    if (_timeElapsed >= TimerLength)
	    {
	        _timeElapsed = 0f;
	        UpdateKey();
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

    public enum BattryType
    {
        Empty,
        NineVolt,
        CR2032,
        TripleA,
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
