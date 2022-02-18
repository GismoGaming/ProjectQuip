using Gismo.Networking.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Gismo.Quip;
using Gismo.Networking;

using UnityEngine.InputSystem;


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

    [SerializeField] private TMP_InputField commandInput;
    [SerializeField] private TextMeshProUGUI outputLog;

    [SerializeField] private GameObject mainConsole;

    [SerializeField] private ScrollRect rect;

    [SerializeField] private CommandStack commandStack;
    [SerializeField] private int maxCommandsInStack = 20;
    [SerializeField] private int index;

    int screenCaptureSS = 1;

    [SerializeField] private delegate void EditorAction(string[] s);

    Dictionary<List<string>, EditorAction> allEditorActions;
    Dictionary<List<string>, HelpListInformation> allEditorActionHelpDescriptions;

    [SerializeField] private ContentSizeFitter fitter;

    // Game specific

    void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(this);

        PopulateEditorActions();

        Application.logMessageReceived += (string condition, string stackTrace, LogType type) =>
        {
            string msg = $"{condition} | {stackTrace}";
            switch (type)
            {
                case LogType.Assert:
                    Log(msg, Dev.DebugLogType.italics);
                    break;
                case LogType.Error:
                case LogType.Exception:
                    Log(msg, Dev.DebugLogType.error);
                    break;
                case LogType.Log:
                default:
                    Log(msg);
                    break;
            }
        };

        Application.targetFrameRate = 60;
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
        PopulateEditorActions("net", f_net, "Networking", "Useful networking commands", HelpListDetail.max);
    }

    public void LogMsg(string msg, Dev.DebugLogType flagType)
    {
        LogMsg(msg, (int)flagType);
    }

    public void LogMsg(string msg, int flag = 0)
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

    public void LogMsg(string[] msgs, int flag = 0)
    {
        foreach (string s in msgs)
            LogMsg(s, flag);
    }

    public void LogMsg(string[] msgs, Dev.DebugLogType flagType)
    {
        LogMsg(msgs, (int)flagType);
    }

    public static void Log(string msg, Dev.DebugLogType flagType)
    {
        instance.LogMsg(msg, (int)flagType);
    }

    public static void Log(string msg, int flag = 0)
    {
        instance.LogMsg(msg, flag);
    }

    public static void Log(string[] msgs, int flag = 0)
    {
        foreach (string s in msgs)
            instance.LogMsg(s, flag);
    }

    public static void Log(string[] msgs, Dev.DebugLogType flagType)
    {
        instance.LogMsg(msgs, (int)flagType);
    }

    private void Start()
    {
        commandStack = new CommandStack(maxCommandsInStack);
        f_clear(null);
        updateMenu();
    }

    private void StringSentToServer(Packet packet, int playerID)
    {
        LogMsg($"Got {packet.ReadString()} from player {playerID}");
    }

    private void StringSentToClient(Packet packet)
    {
        LogMsg($"Got {packet.ReadString()} from server");
    }

    void Update()
    {
        if (Keyboard.current.f1Key.wasReleasedThisFrame || Keyboard.current.slashKey.wasReleasedThisFrame)
        {
            consoleUp = !consoleUp;
            commandInput.Select();
            updateMenu();

            commandInput.Select();
        }

        if (consoleUp && (Keyboard.current.numpadEnterKey.wasReleasedThisFrame || Keyboard.current.backquoteKey.wasReleasedThisFrame))
        {
            enterConsole(commandInput.text);
            commandStack.add(commandInput.text);
            commandInput.text = "";
            index = commandStack.getCount();

            commandInput.ActivateInputField();

            commandInput.Select();
        }

        if (consoleUp && Keyboard.current.downArrowKey.wasReleasedThisFrame)
        {
            index++;
            index = Mathf.Clamp(index, 0, commandStack.getCount());
            commandInput.text = commandStack.getItem(index);
            commandInput.caretPosition = commandInput.text.Length;

            commandInput.ActivateInputField();

            commandInput.Select();
        }
        if (consoleUp && Keyboard.current.upArrowKey.IsPressed())
        {
            index--;
            index = Mathf.Clamp(index, 0, commandStack.getCount());
            commandInput.text = commandStack.getItem(index);
            commandInput.caretPosition = commandInput.text.Length;

            commandInput.ActivateInputField();

            commandInput.Select();
        }
        if (Keyboard.current.leftShiftKey.IsPressed() && Keyboard.current.tabKey.IsPressed())
        {
            if (!Directory.Exists(Dev.devFileLocation + "/DevPhotos/"))
            {
                Directory.CreateDirectory(Dev.devFileLocation + "/DevPhotos/");
            }
            string fileLocation = Dev.devFileLocation + "/DevPhotos/" + DateTime.Now.Millisecond + ".png";
            ScreenCapture.CaptureScreenshot(fileLocation, screenCaptureSS);
            LogMsg($"Took picture: {fileLocation}", Dev.DebugLogType.italics);
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
            LogMsg("Please input something!", 1);
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

        LogMsg("Command " + allSectors[0].Trim() + " not found", 1);
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
                            LogMsg($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i> \n<color=#00ffffff>{i.description}</color>");
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
                            LogMsg($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i> \n<color=#00ffffff>{i.description}</color>");
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
                    LogMsg($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i> \n<color=#00ffffff>{i.description}</color>");
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
                        LogMsg($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i> \n<color=#00ffffff>{i.description}</color>");
                    }
                    else
                    {
                        LogMsg($"<sprite=0><color=#add8e6ff><b>{i.displayName}</b></color>: <i>{allTags}</i>");
                    }
                }
            }
        }
    }

    void f_showStack(string[] s)
    {
        if (commandStack.ToString().Equals("{[<ERROR>]}"))
        {
            LogMsg("Nothing to show!", 3);
        }
        else
        {
            LogMsg(commandStack.ToString(), 3);
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
                    LogMsg("Debug level is now set to \"Off\"");
                    break;
                case "low":
                    Dev.debugLevel = Dev.DebugLevel.Low;
                    LogMsg("Debug level is now set to \"Low\"");
                    break;
                case "medium":
                    Dev.debugLevel = Dev.DebugLevel.Medium;
                    LogMsg("Debug level is now set to \"Medium\"");
                    break;
                case "high":
                    Dev.debugLevel = Dev.DebugLevel.High;
                    LogMsg("Debug level is now set to \"High\"");
                    break;
                case "o":
                    Dev.debugLevel = Dev.DebugLevel.Off;
                    LogMsg("Debug level is now set to \"Off\"");
                    break;
                case "l":
                    Dev.debugLevel = Dev.DebugLevel.Low;
                    LogMsg("Debug level is now set to \"Low\"");
                    break;
                case "m":
                    Dev.debugLevel = Dev.DebugLevel.Medium;
                    LogMsg("Debug level is now set to \"Medium\"");
                    break;
                case "h":
                    Dev.debugLevel = Dev.DebugLevel.High;
                    LogMsg("Debug level is now set to \"High\"");
                    break;
                default:
                    LogMsg("Please input proper debug level name", 1);
                    break;
            }
        }
        catch
        {
            LogMsg("Please properly use the command", 1);
        }
    }

    void f_setScreenCapScale(string[] s)
    {
        try
        {
            screenCaptureSS = int.Parse(s[2]);
            LogMsg("Seting screen capture scaling to " + screenCaptureSS);
        }
        catch
        {
            LogMsg("Invalid integer for screen cap scale", 1);
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
            LogMsg(final);
        }
    }

    void f_version(string[] s)
    {
        LogMsg("<i>Version: </i>" + Application.version);
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
                    LogMsg($"Randomization Complete On {found.name}");
                }
                else
                {
                    LogMsg($"Pallete Swap Script not found on {found.name}");
                }
            }
        }
    }

    void f_netConnect(string[] s)
    {
        if (s.Length >= 2 && !NetGameController.instance.IsConnected())
        {
            if (s[1].ToLower() == "server")
            {
                NetGameController.instance.StartServer();

                NetworkPackets.ServerFunctions.Add(NetworkPackets.ClientSentPackets.MSGSend, StringSentToServer);
            }
            else if (s[1].ToLower() == "client")
            {
                string ip = "localHost";
                if (s.Length >= 3)
                {
                    ip = GetString("localHost", s, 2);
                }

                Log(ip);

                NetGameController.instance.StartClient(ip);

                NetworkPackets.ClientFunctions.Add(NetworkPackets.ServerSentPackets.MSGSend, StringSentToClient);
            }
            else
            {
                LogMsg($"Command {s[1]} not found");
            }
        }
    }

    void f_net(string[] s)
    {
        if (NetGameController.instance.IsConnected())
        {
            if (s.Length >= 2)
            {
                if (s[1].ToLower() == "msg")
                {
                    if (NetGameController.instance.GetConnectionType() == ConnectionType.Server)
                    {
                        Packet packet = new Packet(NetworkPackets.ServerSentPackets.MSGSend);

                        string resultingString = GetString("PING!", s, 2);

                        packet.WriteString(resultingString);

                        NetGameController.instance.SendDataToAll_S(packet);

                        LogMsg($"\"{resultingString}\"Ping sent to clients");
                    }
                    else if (NetGameController.instance.GetConnectionType() == ConnectionType.Client)
                    {
                        Packet packet = new Packet(NetworkPackets.ClientSentPackets.MSGSend, NetGameController.instance.GetUserID());

                        string resultingString = GetString("PING!", s, 2);
                        packet.WriteString(resultingString);

                        NetGameController.instance.SendData_C(packet);
                        LogMsg($"\"{resultingString}\"Ping sent to server");
                    }
                }
                else if (s[1].ToLower() == "id")
                {
                    Log($"Your player ID is {NetGameController.instance.GetUserID()}");
                }
                else if(s[1].ToLower() == "debug")
                {
                    NetGameController.instance.DoDebug = !NetGameController.instance.DoDebug;

                    LogMsg($"NetGameController -> {NetGameController.instance.DoDebug}");
                }
                else if(s[1].ToLower() == "cidd")
                {
                    Log(NetGameController.instance.f_gnetHelper());
                }
                else if(s[1].ToLower() == "disconnect")
                {
                    NetGameController.instance.Disconnect();
                }
                else if(s[1].ToLower() == "connecttype")
                {
                    Log(NetGameController.instance.GetConnectionType().ToString());
                }
                else
                {
                    LogMsg($"Command {s[1]} not found");
                }
            }
        }
        else
        {
            LogMsg("Please connect to server or act as client before doing net commands");
        }
    }

    #endregion
    public static void SelfPrint(string msg, int flag = 0)
    {
        instance.LogMsg(msg, flag);
    }

    public static void SelfPrint(string[] msg, int flag = 0)
    {
        instance.LogMsg(msg, flag);
    }

    public static void SelfPrint(string[] msg, Dev.DebugLevel level)
    {
        instance.LogMsg(msg, (int)level);
    }

    public static void SelfPrint(string msg, Dev.DebugLevel level)
    {
        instance.LogMsg(msg, (int)level);
    }

    public string GetString(string startingValue, string[] s, int startIndex = 1)
    {
        if (s.Length < 2)
        {
            return startingValue;
        }
        string result = "";
        for (int i = startIndex; i < s.Length; i++)
        {
            result += s[i] + " ";
        }
        return result;
    }

    public bool FindObject(string[] s, out GameObject foundObject)
    {
        string resultingName = GetString(null, s).Trim();

        if (GameObject.Find(resultingName) != null)
        {
            foundObject = GameObject.Find(resultingName);
            return true;
        }
        else if (GameObject.Find(resultingName.ToLower()) != null)
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
            LogMsg($"Cannot find object with name of {resultingName}");
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
            LogMsg("Please insert a boolean compatatble input", 1);
            LogMsg("Options are \"true\"/\"t\" or \"false\"/\"f\"", 1);
            LogMsg(i, Dev.DebugLogType.error);

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