using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class AAAAAScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;
    public TextMesh[] letters;

    private List<string> displays = new List<string>();
    private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private int stageCt;
    private int stage;
    private int dispIndex;
    private int pressCt;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleActive;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleActive = false;
        GetComponent<KMNeedyModule>().OnNeedyActivation += OnNeedyActivation;
        GetComponent<KMNeedyModule>().OnTimerExpired += OnTimerExpired;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        bomb.OnBombExploded += delegate () { OnEnd(false); };
        bomb.OnBombSolved += delegate () { OnEnd(true); };
        GetComponent<KMNeedyModule>().OnActivate += OnActivate;
    }

    void Start () {
        for (int i = 0; i < 5; i++)
            letters[i].text = "";
        Debug.LogFormat("[AAAAA #{0}] Needy AAAAA has loaded! Waiting for first activation...", moduleId);
    }

    void OnActivate()
    {
        /**if (TwitchPlaysActive)
            stageCt = 2;
        else*/
            stageCt = 10;
    }

    void OnEnd(bool n)
    {
        //bombSolved = true;
        if (n)
        {
            StopAllCoroutines();
            for (int i = 0; i < 5; i++)
                letters[i].text = "";
            moduleActive = false;
        }
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleActive == true)
        {
            if (letters[Array.IndexOf(buttons, pressed)].text == "A")
            {
                if (pressCt < 1)
                {
                    pressCt++;
                    stage++;
                    pressed.AddInteractionPunch(0.5f);
                    audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                    if (stage == stageCt)
                    {
                        pressCt = 0;
                        stage = 0;
                        dispIndex = 0;
                        StopAllCoroutines();
                        for (int i = 0; i < 5; i++)
                            letters[i].text = "";
                        GetComponent<KMNeedyModule>().HandlePass();
                        Debug.LogFormat("[AAAAA #{0}] Successfully pressed “A” ten times! Module temporarily neutralized! Waiting for next activation...", moduleId);
                        moduleActive = false;
                    }
                }
            }
            else
            {
                pressed.AddInteractionPunch(0.5f);
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                pressCt = 0;
                stage = 0;
                dispIndex = 0;
                StopAllCoroutines();
                Debug.LogFormat("[AAAAA #{0}] Pressed “{1}” instead of “A”! Strike! Waiting for next activation...", moduleId, letters[Array.IndexOf(buttons, pressed)].text);
                for (int i = 0; i < 5; i++)
                    letters[i].text = "";
                GetComponent<KMNeedyModule>().HandleStrike();
                GetComponent<KMNeedyModule>().HandlePass();
                moduleActive = false;
            }
        }
    }

    protected void OnNeedyActivation()
    {
        redo:
        displays.Clear();
        for (int i = 0; i < (stageCt == 10 ? 40 : 5); i++)
        {
            List<char> usedChars = new List<char>();
            while (usedChars.Count < 5)
            {
                char choice = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
                while (usedChars.Contains(choice))
                    choice = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
                usedChars.Add(choice);
            }
            displays.Add(usedChars.Join(""));
        }
        if (!ValidDisplays())
            goto redo;
        StartCoroutine(CycleText());
        Debug.LogFormat("[AAAAA #{0}] The module has activated!", moduleId);
        moduleActive = true;
    }

    protected void OnTimerExpired()
    {
        GetComponent<KMNeedyModule>().HandleStrike();
        Debug.LogFormat("[AAAAA #{0}] Not enough “A”s were not pressed in time! Strike! Waiting for next activation...", moduleId);
        pressCt = 0;
        stage = 0;
        dispIndex = 0;
        for (int i = 0; i < 5; i++)
            letters[i].text = "";
        moduleActive = false;
    }

    private bool ValidDisplays()
    {
        int ct = 0;
        for (int i = stageCt == 10 ? 5 : 1; i < (stageCt == 10 ? 40 : 5); i++)
        {
            if (displays[i].Contains("A"))
                ct++;
        }
        if (ct >= stageCt)
            return true;
        else
            return false;
    }

    private IEnumerator CycleText()
    {
        for (int i = 0; i < (stageCt == 10 ? 40 : 5); i++)
        {
            for (int j = 0; j < 5; j++)
                letters[j].text = displays[dispIndex][j].ToString();
            yield return new WaitForSeconds(stageCt == 10 ? 1f : 8f);
            dispIndex++;
            pressCt = 0;
        }
    }

    //twitch plays
    /**private bool bombSolved = false;
    private bool TwitchPlaysActive;
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <#> [Presses the letter in the specified position] | Valid positions are 1-5 from left to right | On Twitch Plays instead of the letters changing every second and requiring 10 presses the letters will change every 8 seconds and require 2 presses";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 2)
            {
                int temp = 0;
                if (int.TryParse(parameters[1], out temp))
                {
                    if (temp < 1 || temp > 5)
                    {
                        yield return "sendtochaterror The specified position '" + parameters[1] + "' is out of range 1-5!";
                        yield break;
                    }
                    buttons[temp - 1].OnInteract();
                }
                else
                {
                    yield return "sendtochaterror!f The specified position '" + parameters[1] + "' is invalid!";
                }
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify which action to press!";
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (moduleActive)
        {
            int ct = stage;
            int start = dispIndex;
            for (int i = start; i < 5; i++)
            {
                if (displays[i].Contains("A"))
                    ct++;
            }
            if (ct < 2)
            {
                pressCt = 0;
                stage = 0;
                dispIndex = 0;
                StopAllCoroutines();
                for (int i = 0; i < 5; i++)
                    letters[i].text = "";
                GetComponent<KMNeedyModule>().HandlePass();
                moduleActive = false;
            }
        }
        while (!bombSolved)
        {
            while (!moduleActive) { yield return true; }
            int start = stage;
            for (int i = start; i < 2; i++)
            {
                while (!displays[dispIndex].Contains("A") || pressCt == 1) { yield return null; }
                buttons[displays[dispIndex].IndexOf("A")].OnInteract();
            }
        }
    }*/
}
