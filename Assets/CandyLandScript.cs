using UnityEngine;
using System.Linq;

public class CandyLandScript : MonoBehaviour {

    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable upArrow, downArrow, screen;
    public Material[] cardMats;
    public MeshRenderer cardRenderer;
    public MeshRenderer[] deckRenderer;
    public TextMesh screenText;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool solved;

    private readonly int[] map = { 99, 1, 2, 3, 2, 0, 1, 5, 4, 3, 0, 2, 0, 3, 1, 5, 2, 4, 5, 1, 3, 4, 3, 5, 0, 2, 4, 0, 5, 4, 2 }; // ROYGBP
    private int[] spacesAhead;
    private int playerPosition = 0;
    private int correctAnswer = -1;
    private int correctAnswerPosition = 0;

    private bool[] canBeDrawn = { true, true, true, true, true, true, true, true, true, true, true, true }; // 0-5 are the 1-cards, 6-11 are the 2-cards
    private readonly string[] cardNames = { "1 red square", "1 orange square", "1 yellow square", "1 green square", "1 blue square", "1 purple square", "2 red squares", "2 orange squares", "2 yellow squares", "2 green squares", "2 blue squares", "2 purple squares" };
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
    }

    void GenerateCard()
    {
        Audio.PlaySoundAtTransform("card" + Random.Range(1, 11), Module.transform);
        screenText.text = "00";
        screenNumber = 0;
        
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
        if (screenNumber == correctAnswer)
        {
            playerPosition += correctAnswer;
            if (playerPosition == map.Length - 1)
            {
                Module.HandlePass();
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
}
