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
    public static bool DLUp = false;

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


        if (!Directory.Exists(Dev.devFileLocation))
        {
            Directory.CreateDirectory(Dev.devFileLocation);
        }

        //if (!Directory.Exists(Path.Combine(Dev.devFileLocation, "/DevPhotos/")))
        //{
        //    Directory.CreateDirectory(Path.Combine(Dev.devFileLocation, "/DevPhotos/"));
        //}
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
        PopulateEditorActions("name", f_netUsername, "Username", "Setting and logging of username", HelpListDetail.max);
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

        username = NetGameController.Instance.GetRandomUserName();
        updateMenu();
    }

    private void StringSentToServer(Packet packet, byte playerID)
    {
        LogMsg($"Got {packet.ReadString()} from player {playerID}");
    }

    private void StringSentToClient(Packet packet)
    {
        LogMsg($"Got {packet.ReadString()} from server");
    }

    void ClearCommandInput()
    {
        commandInput.text = "";

        index = commandStack.GetCount();

        commandInput.ActivateInputField();

        commandInput.Select();
    }

    void Update()
    {
        if (Keyboard.current.f1Key.wasReleasedThisFrame || Keyboard.current.slashKey.wasReleasedThisFrame)
        {
            DLUp = !DLUp;
            commandInput.Select();
            updateMenu();

            commandInput.Select();
        }

        if (DLUp && Keyboard.current.enterKey.wasReleasedThisFrame)
        {
            commandStack.Add(commandInput.text);

            enterConsole(commandInput.text);
            ClearCommandInput();
        }

        if (DLUp && Keyboard.current.downArrowKey.wasReleasedThisFrame)
        {
            index++;
            index = Mathf.Clamp(index, 0, commandStack.GetCount());
            commandInput.text = commandStack.GetItem(index);
            commandInput.caretPosition = commandInput.text.Length;

            commandInput.ActivateInputField();

            commandInput.Select();
        }
        if (DLUp && Keyboard.current.upArrowKey.IsPressed())
        {
            index--;
            index = Mathf.Clamp(index, 0, commandStack.GetCount());
            commandInput.text = commandStack.GetItem(index);
            commandInput.caretPosition = commandInput.text.Length;

            commandInput.ActivateInputField();

            commandInput.Select();
        }

        if(Keyboard.current.backspaceKey.wasReleasedThisFrame && Keyboard.current.leftCtrlKey.isPressed)
        {
            ClearCommandInput();
        }

        //if (Keyboard.current.leftShiftKey.IsPressed() && Keyboard.current.tabKey.IsPressed())
        //{
        //    if (!Directory.Exists(Dev.devFileLocation + "/DevPhotos/"))
        //    {
        //        Directory.CreateDirectory(Dev.devFileLocation + "/DevPhotos/");
        //    }
        //    string fileLocation = Dev.devFileLocation + "/DevPhotos/" + DateTime.Now.Millisecond + ".png";
        //    ScreenCapture.CaptureScreenshot(fileLocation, screenCaptureSS);
        //    LogMsg($"Took picture: {fileLocation}", Dev.DebugLogType.italics);
        //}
    }

    void updateMenu()
    {
        if (DLUp)
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
        if (s.Length >= 2 && !NetGameController.Instance.IsConnected())
        {
            if (s[1].ToLower() == "server" || s[1].ToLower() == "s")
            {
                NetGameController.Instance.onControllerIsReady += () =>
                {
                    NetGameController.Instance.GetLocalPlayer().SetUserName(username);
                };

                NetGameController.Instance.StartServer();

                NetworkPackets.ServerFunctions.Add(NetworkPackets.ClientSentPackets.MSGSend, StringSentToServer);
            }
            else if (s[1].ToLower() == "client" || s[1].ToLower() == "c")
            {
                NetGameController.Instance.onAssignedID += () =>
                {
                    NetGameController.Instance.GetLocalPlayer().SetUserName(username);
                    Packet usernamePacket = new Packet(NetworkPackets.ClientSentPackets.PlayerInformationSend, NetGameController.Instance.GetUserID());

                    usernamePacket.WriteString(username);

                    NetGameController.Instance.SendData_C(usernamePacket);
                };

                NetGameController.Instance.StartClient(GetString("localhost", s, 2));
                NetworkPackets.ClientFunctions.Add(NetworkPackets.ServerSentPackets.MSGSend, StringSentToClient);
            }
            else
            {
                LogMsg($"Command {s[1]} not found");
            }
        }
    }

    string username;
    void f_netUsername(string[] s)
    {
        if (s.Length >= 2)
        {
            switch (s[1].ToLower())
            {
                case "set":
                    username = GetString(username, s, 2);
                    Log($"Your username is now \"{username}\"");

                    NetGameController.Instance.GetLocalPlayer().SetUserName(username);

                    if (NetGameController.Instance.IsConnectedAs(ConnectionType.Client))
                    {
                        Packet usernamePacket = new Packet(NetworkPackets.ClientSentPackets.PlayerInformationSend, NetGameController.Instance.GetUserID());

                        usernamePacket.WriteString(username);

                        NetGameController.Instance.SendData_C(usernamePacket);
                    }
                    else
                    {
                        NetGameController.Instance.SendDataToAll_S(NetGameController.Instance.GetClientArrayPacket());
                    }
                    break;
                case "show":
                    Log(username);
                    break;
            }
        }
    }

    void f_net(string[] s)
    {
        if (NetGameController.Instance.IsConnected())
        {
            if (s.Length >= 2)
            {
                switch (s[1].ToLower())
                {
                    case "msg":
                        {
                            if (NetGameController.Instance.GetConnectionType() == ConnectionType.Server)
                            {
                                Packet packet = new Packet(NetworkPackets.ServerSentPackets.MSGSend);

                                string resultingString = GetString("PING!", s, 2);

                                packet.WriteString(resultingString);

                                NetGameController.Instance.SendDataToAll_S(packet);

                                LogMsg($"\"{resultingString}\"Ping sent to clients");
                            }
                            else if (NetGameController.Instance.GetConnectionType() == ConnectionType.Client)
                            {
                                Packet packet = new Packet(NetworkPackets.ClientSentPackets.MSGSend, NetGameController.Instance.GetUserID());

                                string resultingString = GetString("PING!", s, 2);
                                packet.WriteString(resultingString);

                                NetGameController.Instance.SendData_C(packet);
                                LogMsg($"\"{resultingString}\"Ping sent to server");
                            }

                            break;
                        }

                    case "id":
                        Log($"Your player ID is {NetGameController.Instance.GetUserID()}");
                        break;
                    case "debug":
                        NetGameController.Instance.DoDebug = !NetGameController.Instance.DoDebug;

                        LogMsg($"NetGameController -> {NetGameController.Instance.DoDebug}");
                        break;
                    case "cidd":
                        Log(NetGameController.Instance.f_gnetHelper());
                        break;
                    case "disconnect":
                        NetGameController.Instance.Disconnect();
                        break;
                    case "connecttype":
                        Log(NetGameController.Instance.GetConnectionType().ToString());
                        break;
                    case "start":
                        NetGameController.Instance.BeginPlaySession();
                        break;
                    case "role":
                        if (s.Length >= 3)
                        {
                            NetGameController.Instance.SetUserRole((Role)int.Parse(s[2]));

                            Log($"Player has gotten new role: {PlayerCentralization.Instance.playerRole}");
                        }
                        break;
                    case "where":
                        if (s.Length == 3)
                        {
                            if (uint.TryParse(s[2], out uint id))
                            {
                                if (NetGameController.Instance.GetTrackedScript(id) != null)
                                {
                                    Log($"{id} is at {NetGameController.Instance.GetTrackedScript(id).transform.position}");
                                }
                                else
                                {
                                    Log($"There is no script with {id} as it's ID");
                                }
                            }
                            else
                            {
                                Log($"Error reading id");
                            }
                        }
                        else
                        {
                            Log($"Please insert and ID to search for");
                        }
                        break;
                    case "tostring":
                        if (s.Length == 3)
                        {
                            if (uint.TryParse(s[2], out uint id))
                            {
                                if (NetGameController.Instance.GetTrackedScript(id) != null)
                                {
                                    Log($"{NetGameController.Instance.GetTrackedScript(id)}");
                                }
                                else
                                {
                                    Log($"There is no script with {id} as it's ID");
                                }
                            }
                            else
                            {
                                Log($"Error reading id");
                            }
                        }
                        else
                        {
                            Log($"Please insert and ID to search for");
                        }
                        break;
                    case "tracked":
                        NetGameController.Instance.GetTrackedIDs();
                        break;
                    case "ip":
                        if(NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
                        {
                            LogMsg($"Your ip, for others to connect to is: {NetGameController.Instance.GetIps()}");
                        }
                        break;
                    default:
                        LogMsg($"Command {s[1]} not found");
                        break;
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
        if (s.Length < 1+startIndex)
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
    public int GetCount()
    {
        return commands.Count;
    }
    public string LastIn()
    {
        return commands[0];
    }
    public string FirstIn()
    {
        return commands[commands.Count - 1];
    }
    public void Add(string s)
    {
        if (!commands.Contains(s))
        {
            commands.Add(s);
            if (commands.Count > maxStack)
            {
                RemoveAt(0);
            }
        }
    }
    public bool Remove(string s)
    {
        return commands.Remove(s);
    }
    public void RemoveAt(int s)
    {
        commands.RemoveAt(s);
        commands.TrimExcess();
    }
    public string GetItem(int s)
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