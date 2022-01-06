using Gismo.Networking.Client;
using Gismo.Networking.Core;
using Gismo.Networking.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Dev
{
    public enum DebugLogType { normal, error, blue, italics }
    public static bool noClipEnabled;
    public static string devFileLocation = Application.dataPath + "/Dev";
    public enum DebugLevel { Off, Low, Medium, High };

    public static DebugLevel debugLevel = DebugLevel.High;

    public static bool autoLogUnityLogger = false;

    public static bool CheckDebugLevel(int min)
    {
        if ((int)debugLevel >= min)
            return true;
        else
            return false;
    }
}

public class DL : MonoBehaviour
{
    public static DL instance;
    [Header("Main Console Stuffs")]
    bool consoleUp = false;

    public TMP_InputField commandInput;
    public TextMeshProUGUI outputLog;

    public GameObject mainConsole;

    public ScrollRect rect;

    public CommandStack commandStack;
    public int maxCommandsInStack = 20;
    public int index;

    int screenCaptureSS = 1;

    public delegate void EditorAction(string[] s);

    Dictionary<List<string>, EditorAction> allEditorActions;
    Dictionary<List<string>, HelpListInformation> allEditorActionHelpDescriptions;

    public ContentSizeFitter fitter;

    // Game specific

    // Networking
    Server selfServer;
    Client selfClient;

    void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(this);

        PopulateEditorActions();
    }

    void PopulateEditorActions(string[] tags, EditorAction action, string displayName, string description, HelpListDetail detailLevelType = HelpListDetail.min)
    {
        allEditorActions.Add(tags.ToList(), action);

        HelpListInformation info = new HelpListInformation
        {
            detailLevel = detailLevelType,
            displayName = displayName,
            description = description
        };

        allEditorActionHelpDescriptions.Add(tags.ToList(), info);
    }
    void PopulateEditorActions(string tag, EditorAction action, string displayName, string description, HelpListDetail detailLevelType = HelpListDetail.min)
    {
        string[] s = { tag };
        PopulateEditorActions(s, action, displayName, description, detailLevelType);
    }

    void PopulateEditorActions()
    {
        allEditorActions = new Dictionary<List<string>, EditorAction>();
        allEditorActionHelpDescriptions = new Dictionary<List<string>, HelpListInformation>();

        PopulateEditorActions("print", f_print, "Print", "Prints out a piece of text to the console", HelpListDetail.max);

        PopulateEditorActions("clear", f_clear, "Clear", "Clears the console", HelpListDetail.max);

        PopulateEditorActions("version", f_version, "Version", "Prints out the current game version to the console", HelpListDetail.max);

        PopulateEditorActions(new string[] { "setscreencapscale", "sscs" }, f_setScreenCapScale, "Screen Capture Scalar", "Changes the scale at which screenshots are taken");

        PopulateEditorActions("setdebuglevel", f_setDebugLevel, "Debug Level", "Sets the game wide debug level");

        PopulateEditorActions("showstack", f_showStack, "Console Stack", "Shows the current stack of commands in the console");

        PopulateEditorActions(new string[] { "exit", "quit" }, f_quit, "Quit", "Exits the game, what else were you expecting?", HelpListDetail.max);

        PopulateEditorActions("help", f_help, "Help function", "Help function, use -i for insider only information, -f for all help information, and -d to add descriptions... (Thats what your using now)", HelpListDetail.max);
        PopulateEditorActions("psr", f_palleteSwap, "Random Pallete Swap", "Randomize pallete on object", HelpListDetail.max);
        PopulateEditorActions("netconnect", f_netConnect, "Networking Connection", "Connects to network as either client or server", HelpListDetail.min);
    
    }

    public void Log(string msg, Dev.DebugLogType flagType)
    {
        Log(msg, (int)flagType);
    }

    public void Log(string msg, int flag = 0)
    {
        if (flag == 1)
        {
            if (Dev.autoLogUnityLogger)
                Debug.Log("<b><i>GConsole:</i></b>\t<i><color=red>" + msg + "</color></i>");
            outputLog.text += "<i> <color=red>" + msg + "</i> </color>\n";
        }
        else if (flag == 2)
        {
            if (Dev.autoLogUnityLogger)
                Debug.Log("<b><i>GConsole:</i></b>\t<color=blue> " + msg + "</color>");
            outputLog.text += "<color=blue> " + msg + "</color>\n";
        }
        else if (flag == 3)
        {
            if (Dev.autoLogUnityLogger)
                Debug.Log("<b><i>GConsole:</i></b>\t<i>" + msg + "</i>");
            outputLog.text += "<i>" + msg + "</i>\n";
        }
        else
        {
            if (Dev.autoLogUnityLogger)
                Debug.Log("<b><i>GConsole:</i></b>\t" + msg);
            outputLog.text += msg + "\n";
        }

        fitter.SetLayoutVertical();
        rect.verticalNormalizedPosition = 0f;
    }

    public void Log(string[] msgs, int flag = 0)
    {
        foreach (string s in msgs)
            Log(s, flag);
    }

    public void Log(string[] msgs, Dev.DebugLogType flagType)
    {
        Log(msgs, (int)flagType);
    }

    private void Start()
    {
        commandStack = new CommandStack(maxCommandsInStack);
        f_clear(null);
        updateMenu();
    }

    private void StringSentToServer(Packet packet, int playerID)
    {
        Log($"Got {packet.ReadString()} from player {playerID}");
    }

    private void StringSentToClient(Packet packet)
    {
        Log($"Got {packet.ReadString()} from server");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.Slash))
        {
            consoleUp = !consoleUp;
            commandInput.Select();
            updateMenu();

            commandInput.Select();
        }

        if (consoleUp && (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)))
        {
            enterConsole(commandInput.text);
            commandStack.add(commandInput.text);
            commandInput.text = "";
            index = commandStack.getCount();

            commandInput.ActivateInputField();

            commandInput.Select();
        }

        if (consoleUp && Input.GetKeyDown(KeyCode.DownArrow))
        {
            index++;
            index = Mathf.Clamp(index, 0, commandStack.getCount());
            commandInput.text = commandStack.getItem(index);
            commandInput.caretPosition = commandInput.text.Length;

            commandInput.ActivateInputField();

            commandInput.Select();
        }
        if (consoleUp && Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;
            index = Mathf.Clamp(index, 0, commandStack.getCount());
            commandInput.text = commandStack.getItem(index);
            commandInput.caretPosition = commandInput.text.Length;

            commandInput.ActivateInputField();

            commandInput.Select();
        }
        if (Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Tab))
        {
            if (!Directory.Exists(Dev.devFileLocation + "/DevPhotos/"))
            {
                Directory.CreateDirectory(Dev.devFileLocation + "/DevPhotos/");
            }
            string fileLocation = Dev.devFileLocation + "/DevPhotos/" + System.DateTime.Now.Millisecond + ".png";
            ScreenCapture.CaptureScreenshot(fileLocation, screenCaptureSS);
            Log($"Took picture: {fileLocation}", Dev.DebugLogType.italics);
        }
    }

    void updateMenu()
    {
        if (consoleUp)
        {
            mainConsole.SetActive(true);

            commandInput.ActivateInputField();
        }
        else
        {
            mainConsole.SetActive(false);
        }
    }

    public void enterConsole(string p)
    {
        string[] allSectors = p.Trim().Split(' ');

        if (allSectors.Length == 1 && allSectors[0] == "")
        {
            Log("Please input something!", 1);
            return;
        }

        allSectors[0] = allSectors[0].ToLower().Trim();

        foreach (List<string> s in allEditorActions.Keys)
        {
            if (s.Contains(allSectors[0]))
            {
                allEditorActions[s].Invoke(allSectors);
                return;
            }
        }

        Log("Command " + allSectors[0].Trim() + " not found", 1);
    }

    #region Commands
    void f_help(string[] s)
    {
        string[] u = { };
        f_clear(u);
        List<HelpListDetail> detailsNeeded = new List<HelpListDetail>
        {
            HelpListDetail.max
        };

        bool addDescriptions = false;

        string additionalArgument = "";
        if (s.Length >= 1)
        {
            for (int z = 1; z < s.Length; z++)
            {
                bool foundAugment = false;
                if (s[z] == "-i")
                {
                    detailsNeeded.Add(HelpListDetail.insider);
                    foundAugment = true;
                }
                if (s[z] == "-f")
                {
                    detailsNeeded.Add(HelpListDetail.min);
                    foundAugment = true;
                }

                if (s[z] == "-d")
                {
                    addDescriptions = true;
                    foundAugment = true;
                }

                if (!foundAugment)
                {
                    if (additionalArgument == "")
                    {
                        additionalArgument = s[z];
                    }
                    else
                    {
                        additionalArgument += " " + s[z];
                    }
                    foreach (List<string> p in allEditorActionHelpDescriptions.Keys)
                    {
                        if (p.Contains(additionalArgument))
                        {
                            string allTags = "";
                            foreach (string q in p)
                            {
                                allTags += $"{q}, ";
                            }
                            HelpListInformation i = allEditorActionHelpDescriptions[p];
                            Log($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i> \n<color=#00ffffff>{i.description}</color>");
                            return;
                        }

                        if (allEditorActionHelpDescriptions[p].displayName.ToLower() == additionalArgument)
                        {
                            string allTags = "";
                            foreach (string q in p)
                            {
                                allTags += $"{q}, ";
                            }
                            HelpListInformation i = allEditorActionHelpDescriptions[p];
                            Log($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i> \n<color=#00ffffff>{i.description}</color>");
                            return;
                        }
                    }
                }
            }

            foreach (List<string> p in allEditorActionHelpDescriptions.Keys)
            {
                HelpListInformation i = allEditorActionHelpDescriptions[p];

                if (p.Contains("help"))
                {
                    string allTags = "";
                    foreach (string q in p)
                    {
                        allTags += $"{q}, ";
                    }
                    Log($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i> \n<color=#00ffffff>{i.description}</color>");
                    continue;
                }

                if (detailsNeeded.Contains(i.detailLevel))
                {

                    string allTags = "";
                    foreach (string q in p)
                    {
                        allTags += $"{q}, ";
                    }
                    if (addDescriptions)
                    {
                        Log($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i> \n<color=#00ffffff>{i.description}</color>");
                    }
                    else
                    {
                        Log($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i>");
                    }
                }
            }
        }
    }

    void f_showStack(string[] s)
    {
        if (commandStack.ToString().Equals("{[<ERROR>]}"))
        {
            Log("Nothing to show!", 3);
        }
        else
        {
            Log(commandStack.ToString(), 3);
        }
    }
  
    void f_setDebugLevel(string[] s)
    {
        try
        {
            switch (s[1].ToLower())
            {
                case "off":
                    Dev.debugLevel = Dev.DebugLevel.Off;
                    Log("Debug level is now set to \"Off\"");
                    break;
                case "low":
                    Dev.debugLevel = Dev.DebugLevel.Low;
                    Log("Debug level is now set to \"Low\"");
                    break;
                case "medium":
                    Dev.debugLevel = Dev.DebugLevel.Medium;
                    Log("Debug level is now set to \"Medium\"");
                    break;
                case "high":
                    Dev.debugLevel = Dev.DebugLevel.High;
                    Log("Debug level is now set to \"High\"");
                    break;
                case "o":
                    Dev.debugLevel = Dev.DebugLevel.Off;
                    Log("Debug level is now set to \"Off\"");
                    break;
                case "l":
                    Dev.debugLevel = Dev.DebugLevel.Low;
                    Log("Debug level is now set to \"Low\"");
                    break;
                case "m":
                    Dev.debugLevel = Dev.DebugLevel.Medium;
                    Log("Debug level is now set to \"Medium\"");
                    break;
                case "h":
                    Dev.debugLevel = Dev.DebugLevel.High;
                    Log("Debug level is now set to \"High\"");
                    break;
                default:
                    Log("Please input proper debug level name", 1);
                    break;
            }
        }
        catch
        {
            Log("Please properly use the command", 1);
        }
    }
    
    void f_setScreenCapScale(string[] s)
    {
        try
        {
            screenCaptureSS = int.Parse(s[2]);
            Log("Seting screen capture scaling to " + screenCaptureSS);
        }
        catch
        {
            Log("Invalid integer for screen cap scale", 1);
        }
    }
    
    void f_print(string[] s)
    {
        if (s.Length >= 2)
        {
            string final = "";
            for (int i = 1; i < s.Length; i++)
            {
                final += s[i] + " ";
            }
            Log(final);
        }
    }
    
    void f_version(string[] s)
    {
        Log("<i>Version: </i>" + Application.version);
    }
  
    void f_clear(string[] s)
    {
        outputLog.text = "";
        rect.verticalNormalizedPosition = 1f;
    }
  
    void f_quit(string[] s)
    {
        Application.Quit();
    }
    
    void f_palleteSwap(string[] s)
    {
        if (s.Length >= 2)
        {
            if (FindObject(s, out GameObject found))
            {
                if (found.TryGetComponent(out Gismo.PalletSwap.PalleteSwap swap))
                {
                    swap.RandomizeColorSet();
                    swap.UpdateSpriteWithPallete();
                    Log($"Randomization Complete On {found.name}");
                }
                else
                {
                    Log($"Pallete Swap Script not found on {found.name}");
                }
            }
        }
    }

    void f_netConnect(string[] s)
    {
        if (s.Length >= 2)
        {
            if(s[1].ToLower() == "server")
            {
                selfServer = new Server(10);
                selfServer.StartListening();

                Gismo.Networking.NetworkPackets.ServerFunctions.Add(Gismo.Networking.NetworkPackets.ClientSentPackets.StringSend, StringSentToServer);
            }
            else if(s[1].ToLower() == "client")
            {
                selfClient = new Client();
                selfClient.Connect("localHost");

                Gismo.Networking.NetworkPackets.ClientFunctions.Add(Gismo.Networking.NetworkPackets.ServerSentPackets.StringSend, StringSentToClient);
            }
            else if(s[1].ToLower() == "msg")
            {
                if(selfServer != null)
                {
                    Packet packet = new Packet(Gismo.Networking.NetworkPackets.ServerSentPackets.StringSend);

                    string resultingString = "PING!";

                    if (s.Length >= 2)
                    {
                        resultingString = GetString(s,2);
                    }

                    packet.WriteString(resultingString);

                    selfServer.SendDataToAll(packet);

                    Log($"\"{resultingString}\"Ping sent to clients");
                }
                else if(selfClient != null)
                {
                    Packet packet = new Packet(Gismo.Networking.NetworkPackets.ClientSentPackets.StringSend, selfClient.clientID);

                    string resultingString = "PING!";

                    if (s.Length >= 2)
                    {
                        resultingString = GetString(s, 2);
                    }

                    packet.WriteString(resultingString);

                    selfClient.SendData(packet);
                    Log($"\"{resultingString}\"Ping sent to server");
                }
                else
                {
                    Log("Please connect to server or act as client before pinging");
                }
            }
            else
            {
                Log($"Command {s[1]} not found");
            }
        }
    }

    #endregion
    public static void SelfPrint(string msg, int flag = 0)
    {
        instance.Log(msg, flag);
    }
    public static void SelfPrint(string[] msg, int flag = 0)
    {
        instance.Log(msg, flag);
    }

    public static void SelfPrint(string[] msg, Dev.DebugLevel level)
    {
        instance.Log(msg, (int)level);
    }

    public static void SelfPrint(string msg, Dev.DebugLevel level)
    {
        instance.Log(msg, (int)level);
    }

    public string GetString(string [] s, int startIndex = 1)
    {
        string result = "";
        for (int i = startIndex; i < s.Length; i++)
        {
            result += s[i] + " ";
        }
        return result;
    }

    public bool FindObject(string[] s, out GameObject foundObject)
    {
        string resultingName = GetString(s).Trim();

        if (GameObject.Find(resultingName) != null)
        {
            foundObject = GameObject.Find(resultingName);
            return true;
        }
        else if(GameObject.Find(resultingName.ToLower()) != null)
        {
            foundObject = GameObject.Find(resultingName.ToLower());
            return true;
        }
        else if (GameObject.Find(resultingName.ToUpper()) != null)
        {
            foundObject = GameObject.Find(resultingName.ToUpper());
            return true;
        }
        else
        {
            Log($"Cannot find object with name of {resultingName}");
            foundObject = null;
            return false;
        }
    }

    bool convertToBool(string i)
    {
        if (i.ToLower() == "true" || i.ToLower() == "t")
        {
            return true;
        }
        else if (i.ToLower() == "false" || i.ToLower() == "f")
        {
            return false;
        }
        else
        {
            Log("Please insert a boolean compatatble input", 1);
            Log("Options are \"true\"/\"t\" or \"false\"/\"f\"", 1);
            Log(i,Dev.DebugLogType.error);

            return false;
        }
    }
}

public enum HelpListDetail { min, insider, max }
public struct HelpListInformation
{
    public HelpListDetail detailLevel;
    public string displayName;
    public string description;
}

[Serializable]
public class CommandStack
{
    public List<string> commands;
    public int maxStack;
    public CommandStack(int mStack)
    {
        commands = new List<string>();
        maxStack = mStack;
    }
    public int getCount()
    {
        return commands.Count;
    }
    public string lastIn()
    {
        return commands[0];
    }
    public string firstIn()
    {
        return commands[commands.Count - 1];
    }
    public void add(string s)
    {
        if (!commands.Contains(s))
        {
            commands.Add(s);
            if (commands.Count > maxStack)
            {
                removeAt(0);
            }
        }
    }
    public bool remove(string s)
    {
        return commands.Remove(s);
    }
    public void removeAt(int s)
    {
        commands.RemoveAt(s);
        commands.TrimExcess();
    }
    public string getItem(int s)
    {
        if (s > commands.Count - 1)
        {
            return "";
        }
        else
        {
            return commands[s];
        }
    }
    public override string ToString()
    {
        string p = "";
        foreach (string s in commands)
        {
            p += s + "\n";
        }

        if (p.Equals(""))
        {
            return "{[<ERROR>]}";
        }

        return p;
    }
}