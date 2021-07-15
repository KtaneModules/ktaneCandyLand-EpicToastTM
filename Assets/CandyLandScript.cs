using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;

public class CandyLandScript : MonoBehaviour {

    public KMBombModule Module;
    public KMAudio Audio;
    public KMColorblindMode Colorblind;
    public KMSelectable upArrow, downArrow, screen;
    public Material[] cardMats;
    public MeshRenderer cardRenderer;
    public MeshRenderer[] deckRenderer;
    public TextMesh screenText, cbText;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool solved;
    private bool cbON;

    private readonly int[] map = { 99, 1, 2, 3, 2, 0, 1, 5, 4, 3, 0, 2, 0, 3, 1, 5, 2, 4, 5, 1, 3, 4, 3, 5, 0, 2, 4, 0, 5, 4, 2 }; // ROYGBP
    private int[] spacesAhead;
    private int playerPosition = 0;
    private int correctAnswer = -1;
    private int correctAnswerPosition = 0;

    private bool[] canBeDrawn = { true, true, true, true, true, true, true, true, true, true, true, true }; // 0-5 are the 1-cards, 6-11 are the 2-cards
    private static readonly string[] cardNames = { "1 red square", "1 orange square", "1 yellow square", "1 green square", "1 blue square", "1 purple square", "2 red squares", "2 orange squares", "2 yellow squares", "2 green squares", "2 blue squares", "2 purple squares" };
    private static readonly string[] colorNames = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple" };
    private int cardDrawn = 0;

    private int screenNumber = 0;
    
    void Awake () {
        _moduleId = _moduleIdCounter++;
        upArrow.OnInteract += delegate () { ArrowPress(1); return false; };
        downArrow.OnInteract += delegate () { ArrowPress(-1); return false; };
        screen.OnInteract += delegate () { Submit(); return false; };
    }

    private void Start()
    {
        for (int i = 0; i < 3; i++)
            deckRenderer[i].material = cardMats[Random.Range(0, 12)];
        GenerateCard();
        if (Colorblind.ColorblindModeActive)
            ToggleCB();
    }

    void GenerateCard()
    {
        Audio.PlaySoundAtTransform("card" + Random.Range(1, 11), Module.transform);
        screenText.text = "00";
        screenNumber = 0;
        if (playerPosition != map.Length - 1)
            spacesAhead = new int[map.Length - playerPosition - 1];
        for (int i = 0; i < spacesAhead.Length; i++)
            spacesAhead[i] = map[playerPosition + i + 1];

        for (int i = 0; i < 6; i++)
        {
            if (spacesAhead.ToList().Count(x => x == i) >= 1)
                canBeDrawn[i] = true;
            else
                canBeDrawn[i] = false;
            if (spacesAhead.ToList().Count(x => x == i) >= 2 && map[playerPosition] != i) // obligatory sloppy workaround :zany_face:
                canBeDrawn[i + 6] = true;
            else
                canBeDrawn[i + 6] = false;
        }

        cardDrawn = Random.Range(0, 12);
        while (!canBeDrawn[cardDrawn])
            cardDrawn = Random.Range(0, 12);

        DebugMsg("The card is " + cardNames[cardDrawn] + ".");

        cardRenderer.material = cardMats[cardDrawn];
        cbText.text = colorNames[cardDrawn % 6];

        correctAnswer = 1;
        correctAnswerPosition = playerPosition + 1;
        while (map[correctAnswerPosition] != cardDrawn % 6)
        {
            correctAnswer += 1;
            correctAnswerPosition += 1;
        }
        if (cardDrawn >= 6)
        {
            correctAnswer += 1;
            correctAnswerPosition += 1;
            while (map[correctAnswerPosition] != cardDrawn % 6)
            {
                correctAnswer += 1;
                correctAnswerPosition += 1;
            }
        }

        DebugMsg("The correct answer is " + correctAnswer.ToString() + ".");
    }

    void ArrowPress(int up)
    {
        if (up == 1)
            upArrow.AddInteractionPunch();
        else
            downArrow.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Module.transform);
        if (screenNumber + up >= 0 && screenNumber + up <= map.Length)
        {
            screenNumber = screenNumber + up;
            if (screenNumber > 9)
                screenText.text = screenNumber.ToString();
            else
                screenText.text = "0" + screenNumber.ToString();
        }
    }

    void Submit()
    {
        screen.AddInteractionPunch(2);
        if (solved)
            return;
        if (screenNumber == correctAnswer)
        {
            playerPosition += correctAnswer;
            if (playerPosition == map.Length - 1)
            {
                Module.HandlePass();
                cbText.text = "ok then";
                solved = true;
                DebugMsg("You won the game! Module solved.");
            }

            else
                GenerateCard();
        }

        else
        {
            Module.HandleStrike();
            DebugMsg("how did you strike");
            playerPosition = 0;
            GenerateCard();
        }
    }
	
    void DebugMsg(string message)
    {
        Debug.LogFormat("[Candy Land #{0}] {1}", _moduleId, message);
    }
    void ToggleCB()
    {
        cbON = !cbON;
        cbText.gameObject.SetActive(cbON);
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} submit 23> to enter that number into the module. Use <!{0} colorblind> to toggle colorblind mode.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpperInvariant();
        Match m = Regex.Match(command, @"^SUBMIT\s+([1-9]?[0-9])$");
        if (m.Success)
        {
            yield return null;
            int target = int.Parse(m.Groups[1].Value);
            KMSelectable which = screenNumber < target ? upArrow : downArrow;
            while (screenNumber != target)
            {
                which.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            screen.OnInteract();
        }
        else if (command.EqualsAny("COLORBLIND", "COLOURBLIND", "COLOR-BLIND", "COLOUR-BLIND", "CB"))
        {
            yield return null;
            ToggleCB();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!solved)
        {
            KMSelectable which = screenNumber < correctAnswer ? upArrow : downArrow;
            while (screenNumber != correctAnswer)
            {
                which.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            screen.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
