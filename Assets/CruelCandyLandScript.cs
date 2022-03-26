using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;

public class CruelCandyLandScript : MonoBehaviour {

    public KMBombModule Module;
    public KMAudio Audio;
    public KMBombInfo Info;
    public KMColorblindMode Colorblind;
    public KMSelectable upArrow, downArrow, screen;
    public Material[] candyLandMats, unoMats, cahMats, baseballMats, cardiMats;
    public Material codenames, credit;
    public MeshRenderer cardRenderer;
    public MeshRenderer[] deckRenderer;
    public TextMesh screenText, unoText, cahText, baseballText, codenamesText, cbText;
    public TextMesh[] creditTexts;

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
    private static readonly string[] cardNames = {
        "1 red square", "1 orange square", "1 yellow square", "1 green square", "1 blue square", "1 purple square", "2 red squares", "2 orange squares", "2 yellow squares", "2 green squares", "2 blue squares", "2 purple squares" };
    private static readonly string[] normalColors = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple" };
    private int cardDrawn = 0;
    private static readonly string[] caseNames = { "Candy Land card", "Uno card", "Cards Against Humanity card", "Codenames card", "baseball card", "credit card", "picture of Cardi B" };

    private int screenNumber = 0;

    void Awake()
    {
        _moduleId = _moduleIdCounter++;
        upArrow.OnInteract += delegate () { ArrowPress(1); return false; };
        downArrow.OnInteract += delegate () { ArrowPress(-1); return false; };
        screen.OnInteract += delegate () { Submit(); return false; };
    }

    private void Start()
    {
        for (int i = 0; i < 3; i++)
            deckRenderer[i].material = candyLandMats[Random.Range(0, 12)];
        GenerateCard();
        if (Colorblind.ColorblindModeActive)
            ToggleCB();
    }

    void GenerateCard()
    {
        unoText.text = "";
        cahText.text = "";
        baseballText.text = "";
        codenamesText.text = "";
        creditTexts[0].text = "";
        creditTexts[1].text = "";
        creditTexts[2].text = "";

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

        int randomCase = Random.Range(0, 7);
        DebugMsg("You drew a " + caseNames[randomCase]);
        switch (randomCase)
        {
            case 0: // Candy Land
                cardRenderer.material = candyLandMats[cardDrawn];
                cbText.text = normalColors[cardDrawn % 6];
                break;
            case 1: // Uno
                string[] unoColorNames = { "Red", "Yellow", "Green", "Blue" };
                int unoColor = Random.Range(0, 4); // RYGB
                if (playerPosition == 0)
                    unoColor = Random.Range(0, 3); // obligatory sloppy workaround #2 :zany_face:
                int[] unoCards = { 8, 1, 7, 6, 11, 5, 10, 3, 4, 2, 9, 0 };
                int[] offsets = { Info.GetBatteryCount(), Info.GetIndicators().Count(), Info.GetPortCount(), Info.GetModuleIDs().Count() };
                string[] unoCardText = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "+2", "Ø" };
                int unoIndex = Array.FindIndex(unoCards, x => x == cardDrawn);

                DebugMsg(unoIndex.ToString());

                for (int i = 0; i < offsets[unoColor]; i++)
                {
                    unoIndex -= 1;
                    if (unoIndex < 0)
                        unoIndex = unoCards.Length - 1;
                }
                
                cardRenderer.material = unoMats[unoColor];
                cbText.text = unoColorNames[unoColor];
                unoText.text = unoCardText[unoIndex];

                DebugMsg("It was " + unoColorNames[unoColor]);
                DebugMsg("The card had " + unoText.text + " on it.");

                break;
            case 2: // Cards Against Humanity
                int[] cahTables = { 1, 0, 8, 5, 6, 4, 10, 3, 2, 7, 9, 11 };
                int[] ughhghhhghhhghhhLazyWorkaround = { 3, 0, 3, 0, 9, 6, 9, 6, 15, 12, 15, 12 }; // obligatory sloppy workaround #3 :zany_face:
                int cahIndex = Array.FindIndex(cahTables, x => x == cardDrawn);
                string[] cahCardText =
                {
                    "Just cut the\nwires! It's not that\ncomplicated.", // vanilla, no POOP
                    "You'd better do\nwhat your expert,\nSimon, says.",
                    "If you think this is\nhard, try it with\nMorse code.",
                    "Maybe if you\npress the button\nenough times,\nyou'll get past\nthis module.", // vanilla, POOP
                    "You'll need more\nthan a password\nto pass this\nbomb.",
                    "*slaps module*\nThis keypad can\nfit so many\nsymbols.",
                    "Don't want to put\nin effort? Just\nfilibuster until\nthe timer runs\nout.", // needy, no POOP
                    "If you're stressed\nout, just refill\nthat beer!",
                    "You're gonna\nneed all eight\npages of that\nmanual.",
                    "You're already\ndoomed; you may\nas well press F\nto pay respects\nnow.", // needy, POOP
                    "I'm running out of\nideas for cards.\nCommand\nprompt!",
                    "If you don't trust\nyour experts, you\ncan always flip\nthe coin.",
                    "Video poker? I\nhardly know 'er!", // game, no POOP
                    "Thankfully, you\ndon't need to\ndeal with snakes\nor climb ladders\nto get out of this.",
                    "If you're having a\nhard time, use\nSpecial Expert\nTechnology\n(SET).",
                    "Apparently this\nbomb is lousy at\nplaying chess.", // game, POOP
                    "It's technically\nnot murder if the\nbomb kills those\npeople.",
                    "Call a point of\norder if your\nexperts are\ncheating."
                };
                
                cahText.text = cahCardText[Random.Range(0, 3) + ughhghhhghhhghhhLazyWorkaround[cahIndex]];
                if (cahIndex % 4 < 2)
                {
                    cardRenderer.material = cahMats[0];
                    cahText.color = Color.black;
                    DebugMsg("The card was white.");
                }
                else
                {
                    cardRenderer.material = cahMats[1];
                    cahText.color = Color.white;
                    DebugMsg("The card was black.");
                }
                
                DebugMsg("The card's text was " + cahText.text);

                break;
            case 3: // Codenames
                string[] codenamesCards =
                {
                    "LABYRINTH", "LOST", "WALLS", "MINOTAUR", "PATHFINDER",
                    "REPETITION", "FRACTAL", "RECURSION", "AGAIN", "RECURRENT",
                    "FORCE", "GRAVITY", "MOMENTUM", "NEWTON", "ACCELERATION",
                    "EUROPE", "TAKEOVER", "SETTLING", "ASSIMILATE", "TERRITORY",
                    "EXOTIC", "WONKY", "PECULIAR", "ANOMALOUS", "STRANGE",
                    "DICE", "MONOPOLY", "SCRABBLE", "TABLETOP", "CHESS",
                    "PAST", "EIGHTIES", "NOSTALGIA", "AESTHETIC", "ANTIQUE",
                    "PROGRAMMING", "ROBOT", "COMPUTER", "ELECTRONIC", "MACHINERY",
                    "ILLUSION", "ALCHEMY", "WIZARD", "SPELL", "WAND",
                    "HAMBURGER", "DINNER", "STEAK", "EDIBLE", "DESSERT",
                    "KABOOM", "MODULE", "DETONATE", "DEFUSING", "DYNAMITE",
                    "SPREAD", "STUCK", "CRAM", "JELLY", "CLOG"
                };
                string[] codenamesOuter = // sloppy workaround #4 :zany_face:
                {
                    "LH", "LT", "WS", "MR", "PR",
                    "RN", "FL", "RN", "AN", "RT",
                    "FE", "GY", "MM", "NN", "AN",
                    "EE", "TR", "SG", "AE", "TY",
                    "EC", "WY", "PR", "AS", "SE",
                    "DE", "MY", "SE", "TP", "CS",
                    "PT", "ES", "NA", "AC", "AE",
                    "PG", "RT", "CR", "EC", "MY",
                    "IN", "AY", "WD", "SL", "WD",
                    "HR", "DR", "SK", "EE", "DT",
                    "KM", "ME", "DE", "DG", "DE",
                    "SD", "SK", "CM", "PE", "CG"
                };
                string[] codenamesInner =
                {
                    "ABYRINT", "OS", "ALL", "INOTAU", "ATHFINDE",
                    "EPETITIO", "RACTA", "ECURSIO", "GAI", "ECURREN",
                    "ORC", "RAVIT", "OMENTU", "EWTO", "CCELERATIO",
                    "UROP", "AKEOVE", "ETTLIN", "SSIMILAT", "ERRITOR",
                    "XOTI", "ONK", "ECULIA", "NOMALOU", "TRANG",
                    "IC", "ONOPOL", "CRABBL", "ABLETO", "HES",
                    "AS", "IGHTIE", "OSTALGI", "ESTHETI", "NTIQU",
                    "ROGRAMMIN", "OBO", "OMPUTE", "LECTRONI", "ACHINER",
                    "LLUSIO", "LCHEM", "IZAR", "PEL", "AN",
                    "AMBURGE", "INNE", "TEA", "DIBL", "ESSER",
                    "ABOO", "ODUL", "ETONAT", "EFUSIN", "YNAMIT",
                    "PREA", "TUC", "RA", "RESERV", "LO"
                };

                int codenamesRandom = Random.Range(0, 5);
                string codenamesDrawn = codenamesCards[cardDrawn * 5 + codenamesRandom];
                string codenamesChosenInner = codenamesInner[cardDrawn * 5 + codenamesRandom];
                string codenamesShuffledString = "";
                char[] codenamesShuffled = codenamesChosenInner.ToCharArray().Shuffle();
                for (int i = 0; i < codenamesShuffled.Length; i++)
                    codenamesShuffledString += codenamesShuffled[i];
                while (codenamesShuffledString == codenamesChosenInner)
                {
                    codenamesShuffledString = "";
                    codenamesShuffled = codenamesChosenInner.ToCharArray().Shuffle();
                    for (int i = 0; i < codenamesShuffled.Length; i++)
                        codenamesShuffledString += codenamesShuffled[i];
                }

                codenamesText.text = codenamesOuter[cardDrawn * 5 + codenamesRandom].First().ToString() + codenamesShuffledString + codenamesOuter[cardDrawn * 5 + codenamesRandom].Last().ToString();

                cardRenderer.material = codenames;

                DebugMsg("The scrambled word was " + codenamesText.text);
                DebugMsg("The unscrambled word was " + codenamesDrawn);
                break;
            case 4: // Baseball
                int baseballModifier = Random.Range(0, 6);
                int baseballInitial = (cardDrawn + baseballModifier) % 6;
                string[] firstNames = { "Joan", "Parker", "Sean", "Aaron", "Jon", "Wyatt", "Dawn", "Arin", "PolkaDot", "Shaun", "Erin", "Chorby", "Tillman", "Shawn", "Don", "Jaylen", "Dante", "John" };
                string[] lastNames = { "McMillan", "Wallace", "Farris", "Hotdogfingers", "Chamberlain", "Cosby", "Henderson", "Grey", "Ferris", "Soul", "Wallis", "Sandford", "Patterson", "Sanford", "Crosby", "Mason", "Gray", "Chamberland" };
                string fullName = "";
                
                fullName += firstNames[baseballInitial * 3 + Random.Range(0, 3)];
                fullName += "\n";
                fullName += lastNames[baseballModifier * 3 + Random.Range(0, 3)];

                if (cardDrawn < 6)
                {
                    cardRenderer.material = baseballMats[0];
                    DebugMsg("The card had a batter on it.");
                }
                else
                {
                    cardRenderer.material = baseballMats[1];
                    DebugMsg("The card had a pitcher on it.");
                }
                baseballText.text = fullName;

                DebugMsg("The name on the card was " + fullName.Replace('\n', ' '));


                break;
            case 5: // Credit
                cardRenderer.material = credit;
                string[] creditBankNames = { "Bank of KTaNE", "Bomb Corp.", "KaboomCard", "Simon, Bob & Co." };
                int[] creditNumber = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                int[] creditMidNumbers = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                int creditChosenBank = 0;
                int creditFinalResult = 99;
                string creditNumberText = "";
                string creditMidNumberText = "";
                int currentYear = DateTime.Today.Year;

                while (creditFinalResult != cardDrawn % 6)
                {
                    creditFinalResult = 0;
                    creditChosenBank = Random.Range(0, 4);
                    for (int i = 0; i < 12; i++)
                    {
                        creditNumber[i] = Random.Range(0, 10);
                        switch (creditChosenBank)
                        {
                            case 0:
                                if (creditNumber[i] % 2 == 0)
                                    creditMidNumbers[i] = creditNumber[i] + 1;
                                else
                                    creditMidNumbers[i] = creditNumber[i];
                                break;
                            case 1:
                                creditMidNumbers[i] = 9 - creditNumber[i];
                                break;
                            case 2:
                                if (creditNumber[i] % 2 == 1)
                                    creditMidNumbers[i] = creditNumber[i] - 1;
                                else
                                    creditMidNumbers[i] = creditNumber[i];
                                break;
                            case 3:
                                if ((creditNumber[i] == 0 || creditNumber[i] == 6) && i == 0)
                                    creditMidNumbers[i] = 1;
                                else if (creditNumber[i] == 0 || creditNumber[i] == 6)
                                    creditMidNumbers[i] = creditNumber[i - 1];
                                else
                                    creditMidNumbers[i] = creditNumber[i];
                                break;
                        }

                        creditFinalResult += creditMidNumbers[i];
                    }

                    creditFinalResult %= 6;
                }

                for (int i = 0; i < 12; i++)
                {
                    creditNumberText += creditNumber[i].ToString();
                    if (i % 3 == 2)
                        creditNumberText += " ";
                    
                    creditMidNumberText += creditMidNumbers[i].ToString();
                    if (i % 3 == 2)
                        creditMidNumberText += " ";
                }

                creditTexts[0].text = creditNumberText;
                creditTexts[2].text = creditBankNames[creditChosenBank];

                string randomMonth = Random.Range(1, 13).ToString();
                if (randomMonth.TryParseInt() < 10)
                    randomMonth = "0" + randomMonth;
                
                if (cardDrawn < 6)
                    creditTexts[1].text = randomMonth + "/" + (currentYear - Random.Range(1, 10)).ToString();
                else
                    creditTexts[1].text = randomMonth + "/" + (currentYear + Random.Range(1, 10)).ToString();

                DebugMsg("The card number was " + creditTexts[0].text);
                DebugMsg("The bank name was " + creditTexts[2].text);
                DebugMsg("This means the modified card number was " + creditMidNumberText);
                DebugMsg("The final number was " + creditFinalResult);
                DebugMsg("The expiration date was " + creditTexts[1].text);
                break;
            case 6: // Cardi B
                cardRenderer.material = cardiMats[cardDrawn];
                cbText.text = "Cardi B";
                DebugMsg("Cardi B");
                break;
            default:
                DebugMsg("Oh Fuck! Module broke. Solving...");
                Module.HandlePass();
                break;
        };
        if (randomCase >= 2 && randomCase != 6)
            cbText.text = "idk";
        DebugMsg("The card was actually " + cardNames[cardDrawn] + ".");
        DebugMsg("The correct answer was " + correctAnswer.ToString() + ".");

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
                solved = true;
                cbText.text = "BLAN";
                DebugMsg("You won the game! Module solved.");
            }

            else
                GenerateCard();
        }

        else
        {
            Module.HandleStrike();
            DebugMsg("You struck! Resetting module...");
            playerPosition = 0;
            GenerateCard();
        }
    }

    void DebugMsg(string message)
    {
        Debug.LogFormat("[Cruel Candy Land #{0}] {1}", _moduleId, message.Replace('\n', ' '));
    }
    void ToggleCB()
    {
        cbON = !cbON;
        cbText.gameObject.SetActive(cbON);
    }
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
