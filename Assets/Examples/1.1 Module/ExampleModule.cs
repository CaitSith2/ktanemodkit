using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExampleModule : MonoBehaviour
{
    public KMSelectable[] buttons;
    public KMBombInfo bombInfo;
    public KMBombModule bombModule;

    List<int> list = new List<int>() { 0, 1, 2, 3 };
    bool isActivated = false;
    bool isExploded = false;
    int twitchPlayStrikes = 0;

    void Awake()
    {
        //Since we have a button that will cause the bomb to explode no matter
        //how many strikes are remaining, we need some kind of notification that
        //the bomb has exploded, so that we can tell the button handler to
        //stop sending strikes.
        bombInfo.OnBombExploded += OnBombExploded;

        //We need to somehow keep track of the strikes awarded, in the case
        //of multiple strikes, for twitch plays purposes.
        bombModule.OnStrike += OnStrike;


        bombModule.OnActivate += ActivateModule;
    }

    void Start()
    {
        //Initialization that depends on the bomb Edgework should go here.

        Init();
    }

    void OnBombExploded()
    {
        //Notify the button handler that the bomb has exploded.
        isExploded = true;
    }

    bool OnStrike()
    {
        Debug.Log("Example Module OnStrike()");
        twitchPlayStrikes++;
        return false;
    }

    void Init()
    {
        list.Shuffle();

        for(int i = 0; i < buttons.Length; i++)
        {
            string label = "X";
            if (i == list[0])
            {
                label = "O";
            }
            else if (i == list[1])
            {
                label = "XX";
            }
            else if (i == list[2])
            {
                label = "EX";
            }

            TextMesh buttonText = buttons[i].GetComponentInChildren<TextMesh>();
            buttonText.text = label;
            int j = i;
            buttons[i].OnInteract += delegate () { OnPress(j); return false; };
        }
    }

    void ActivateModule()
    {
        isActivated = true;
    }

    void OnPress(int index)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        if (!isActivated)
        {
            Debug.Log("Pressed button before module has been activated!");
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            
            if (index == list[0])
            {
                Debug.LogFormat("Pressed button {0} which {1}", index, "was correct");
                GetComponent<KMBombModule>().HandlePass();
            }
            else if (index == list[1])
            {
                Debug.LogFormat("Pressed button {0} which {1}", index, "caused 2 strikes");
                GetComponent<KMBombModule>().HandleStrike();
                GetComponent<KMBombModule>().HandleStrike();
                Debug.Log(twitchPlayStrikes);
            }
            else if (index == list[2])
            {
                Debug.LogFormat("Pressed button {0} which {1}", index, "caused the bomb to explode");
                while(!isExploded)
                    GetComponent<KMBombModule>().HandleStrike();
                Debug.Log(twitchPlayStrikes);
            }
            else
            {
                Debug.LogFormat("Pressed button {0} which {1}", index, "caused a strike");
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }

    IEnumerator ProcessTwitchCommand(string command)
    {
        twitchPlayStrikes = 0;

        if (command == "strikes")
        {
            yield return null;
            for (var i = 0; i < 5; i++)
            {
                GetComponent<KMBombModule>().HandleStrike();
                yield return null;
            }
            yield break;
        }

        if (command == "long command")
        {
            yield return new WaitForSeconds(0.1f);
            for (var i = 200; i >= 0; i--)
            {
                yield return "trycancel";
                Debug.LogFormat("{0} button presses left", i);
                yield return new WaitForSeconds(0.1f);
            }
        }

        Dictionary<string, int> buttonsLookup = new Dictionary<string, int>()
        {
            {"press tl",0 }, {"press tr",1 },
            {"press bl", 2}, {"press br", 3 },

            {"press o",list[0] }, {"press xx",list[1] },
            {"press ex",list[2] }, {"press x", list[3] }
        };
        int index;
        if (!buttonsLookup.TryGetValue(command.ToLowerInvariant(), out index))
            yield break;
        
        if (index == list[1] || index == list[2])
        {
            yield return index == list[1]
                ? "strikemessage Pressing the button labeled XX"
                : "strikemessage Pressing the button labeled EX";

            //When you know the pressingg of the button is going to cause more than one strike,
            //first, yield return "multiple strikes";
            yield return "multiple strikes";

            //Next, Press the button.  Make sure you have some code somewhere that is keeping track of how many
            //strikes this is going to cause.
            yield return new KMSelectable[] {buttons[index]};
            yield return new WaitForSeconds(0.1f);

            //Finally, award all of the strikes to the author of the command.
            yield return "award strikes " + twitchPlayStrikes;
        }
        else
        {
            if (index == list[3])
                yield return "strikemessage Pressing the button labeled X";
            yield return new KMSelectable[] { buttons[index] };
        }
    }

}

static class MyExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            int k = Random.Range(0, n);
            n--;
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
