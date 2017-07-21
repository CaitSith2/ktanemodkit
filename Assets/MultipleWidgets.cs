using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Random = UnityEngine.Random;

public class MultipleWidgets : MonoBehaviour
{
    private class PortSet
    {
        public GameObject port;
        public PortType type;
    }

    private List<List<PortSet>> _portGroups;

    #region public variables
    public GameObject[] Batteries;
    public GameObject[] BatteryHolders;
    public Transform[] AACells;
    public Transform NineVoltCell;

    public GameObject TwoFactor;
    public TextMesh[] TwoFactorDigits;
    public GameObject Ports;

    public GameObject ParallelPort;
    public GameObject SerialPort;

    public GameObject PS2Port;
    public GameObject DVIPort;
    public GameObject RJ45Port;
    public GameObject StereoRCAPort;

    public GameObject HDMIPort;
    public GameObject USBPort;
    public GameObject ComponentVideoPort;
    public GameObject CompositeVideoPort;

    public GameObject ACPort;
    public GameObject PCMCIAPort;
    public GameObject VGAPort;

    public GameObject Indicator;
    public TextMesh IndicatorText;
    public GameObject[] IndicatorLights;

    public Transform IndicatorTransform;
    public Transform BatteryTransform;
    public Transform TwoFactorTransform;

    public KMBombInfo Info;
    public AudioClip Notify;
    #endregion

    private bool _indicator = false;
    private string _indicatorLabel;
    private bool _indicatorLight;
    private int _indicatorLightColor;

    private bool _ports = false;
    private PortType _presentPorts = (PortType) 0;

    private bool _batteries = false;
    private BatteryType _batteryType = BatteryType.NineVolt;

    private bool _twofactor = false;
    private int _key;
    private float _timeElapsed;

    private const float TimerLength = 60.0f;

    private Type _indicatorWidgetType = null;
    private FieldInfo _indicatorLabelsField = null;
    private List<string> _knownIndicators = null;

    void Awake()
    {
        #region Port Sets
        _portGroups = new List<List<PortSet>>
        {
            new List<PortSet> //Vanilla set 1
            {
                new PortSet {port = ParallelPort, type = PortType.Parallel },
                new PortSet {port = SerialPort, type = PortType.Serial },
            },
            new List<PortSet> //Vanilla set 2
            {
                new PortSet {port = PS2Port, type = PortType.PS2 },
                new PortSet {port = DVIPort, type = PortType.DVI },
                new PortSet {port = RJ45Port, type = PortType.RJ45 },
                new PortSet {port = StereoRCAPort, type = PortType.StereoRCA },
            },
            new List<PortSet> //New Port
            {
                new PortSet {port = HDMIPort, type = PortType.HDMI },
                new PortSet {port = USBPort, type = PortType.USB },
                new PortSet {port = ComponentVideoPort, type = PortType.Component },
                new PortSet {port = ACPort, type = PortType.AC },
                new PortSet {port = PCMCIAPort, type = PortType.PCMCIA },
                new PortSet {port = VGAPort, type = PortType.VGA },
                new PortSet {port = CompositeVideoPort, type = PortType.Composite },
            },
            new List<PortSet> //Monitor
            {
                new PortSet {port = DVIPort, type = PortType.DVI },
                new PortSet {port = StereoRCAPort, type = PortType.StereoRCA },
                new PortSet {port = HDMIPort, type = PortType.HDMI },
                new PortSet {port = ComponentVideoPort, type = PortType.Component },
                new PortSet {port = VGAPort, type = PortType.VGA },
                new PortSet {port = CompositeVideoPort, type = PortType.Composite },
            },
            new List<PortSet> //Computer related
            {
                new PortSet {port = ParallelPort, type = PortType.Parallel },
                new PortSet {port = SerialPort, type = PortType.Serial },
                new PortSet {port = PCMCIAPort, type = PortType.PCMCIA },
                new PortSet {port = VGAPort, type = PortType.VGA },
                new PortSet {port = PS2Port, type = PortType.PS2 },
                new PortSet {port = RJ45Port, type = PortType.RJ45 },
                new PortSet {port = USBPort, type = PortType.USB },
                new PortSet {port = ACPort, type = PortType.AC },
            }
        };
        #endregion

        Ports.SetActive(false);
        Indicator.SetActive(false);
        IndicatorText.text = string.Empty;
        TwoFactor.SetActive(false);

        foreach (GameObject battery in Batteries)
        {
            battery.SetActive(false);
        }

        foreach (GameObject holder in BatteryHolders)
        {
            holder.SetActive(false);
        }

        foreach (GameObject light in IndicatorLights)
        {
            light.SetActive(false);
        }

        foreach (var portGroup in _portGroups)
        {
            foreach (var set in portGroup)
            {
                set.port.SetActive(false);
            }
        }

        GetComponent<KMWidget>().OnQueryRequest += GetQueryResponse;
        GetComponent<KMWidget>().OnWidgetActivate += Activate;

        _indicatorWidgetType = ReflectionHelper.FindType("IndicatorWidget");
        if (_indicatorWidgetType != null)
        {
            _indicatorLabelsField = _indicatorWidgetType.GetField("Labels", BindingFlags.Public | BindingFlags.Static);
            _knownIndicators = (List<string>) _indicatorLabelsField.GetValue(null);
        }
        else
        {
            _knownIndicators = new List<string>
            {
                "CLR",
                "IND",
                "TRN",
                "FRK",
                "CAR",
                "FRQ",
                "NSA",
                "SIG",
                "MSA",
                "SND",
                "BOB"
            };
        }

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

        if (_indicator && _batteries || (Random.value < 0.5f && !_twofactor))
        {
            IndicatorTransform.localPosition = TwoFactorTransform.localPosition;
        }
    }

    #region Indicators
    void SetIndicators()
    {
        Indicator.SetActive(true);
        var KnownIndicators = new List<string>(_knownIndicators);
        _indicatorLight = Random.value > 0.4f;
        if (_indicatorLight)
        {
            _indicatorLightColor = Random.Range(1, IndicatorLights.Length);
        }
        foreach (var exsting in Info.GetIndicators())
        {
            if (KnownIndicators.Contains(exsting))
                KnownIndicators.Remove(exsting);
        }
        _indicatorLabel = KnownIndicators.Count > 0 ? KnownIndicators[Random.Range(0, KnownIndicators.Count)] : "NLL";
        Debug.LogFormat("[IndicatorWidget] Randomizing Indicator Widget: {0} {1}", (!_indicatorLight) ? "unlit" : "lit", _indicatorLabel);
        Debug.LogFormat("[MultipleWidgets] Indicator Light Color is {0}", IndicatorLights[_indicatorLightColor].name);
        IndicatorText.text = _indicatorLabel;
        IndicatorLights[_indicatorLightColor].SetActive(true);
    }
    #endregion

    #region Ports
    void SetPorts()
    {
        Ports.SetActive(true);
        var portset = _portGroups[Random.Range(0, _portGroups.Count)];
        foreach (var set in portset)
        {
            if (Random.value > 0.5f)
            {
                _presentPorts |= set.type;
                set.port.SetActive(true);
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
        
    }

    public bool IsPortPresent(PortType port)
    {
        return (_presentPorts & port) == port;
    }
    #endregion

    #region Batteries
    void SetBatteries()
    {
        _batteryType = (BatteryType) Random.Range(0, Batteries.Length);
        var holder = (int)_batteryType - 1;
        if (holder < 0) holder = Random.Range(0, BatteryHolders.Length);
        Debug.LogFormat("[BatteryWidget] Randomizing Battery Widget: {0}", GetNumberOfBatteries());
        Batteries[(int) _batteryType].SetActive(true);
        BatteryHolders[holder].SetActive(true);

        foreach (var cell in AACells)
        {
            cell.Rotate(new Vector3(0, 0, Random.Range(0, 360.0f)));
        }
        NineVoltCell.Rotate(new Vector3(0, 0, Random.value < 0.5f ? 0 : 180));
    }

    int GetNumberOfBatteries()
    {
        return (int) _batteryType;
    }
    #endregion

    #region TwoFactor
    void SetTwoFactor()
    {
        GenerateKey();
        TwoFactor.SetActive(true);
    }

    private void GenerateKey()
    {
        _key = Random.Range(0, 1000000);
    }

    private void DisplayKey()
    {
        var text = _key.ToString("000000");
        var zero = true;
        for (var i = 0; i < text.Length; i++)
        {
            zero &= text.Substring(i, 1) == "0";
            TwoFactorDigits[i].text = zero ? "" : text.Substring(i, 1);
        }
    }

    void UpdateKey()
    {
        GetComponent<KMAudio>().HandlePlaySoundAtTransform(Notify.name, transform);
        GenerateKey();
        DisplayKey();
    }
    #endregion


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

        if (queryKey == (KMBombInfo.QUERYKEY_GET_INDICATOR + "Color") && _indicator)
        {
            return JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                {
                    "label",
                    _indicatorLabel
                },
                {
                    "color",
                    IndicatorLights[_indicatorLightColor].name
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
            if (IsPortPresent(PortType.PCMCIA))
            {
                list.Add("PCMCIA");
            }
            if (IsPortPresent(PortType.VGA))
            {
                list.Add("PCA");
            }
            if (IsPortPresent(PortType.AC))
            {
                list.Add("AC");
            }
            if (IsPortPresent(PortType.Composite))
            {
                list.Add("CompositeVideo");
            }
            dictionary.Add("presentPorts", list);
            return JsonConvert.SerializeObject(dictionary);
        }

        if (queryKey == KMBombInfoExtensions.WidgetQueryTwofactor && _twofactor)
        {
            var response = new Dictionary<string, int> { {KMBombInfoExtensions.WidgetTwofactorKey, _key } };
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

        AC = 512,
        PCMCIA = 1024,
        VGA = 2048,
        Composite = 4096,
    }
}

static class Ext
{
    public static Color WithAlpha(this Color color, float alpha) { return new Color(color.r, color.g, color.b, alpha); }

    public static T[] NewArray<T>(params T[] array) { return array; }
}
