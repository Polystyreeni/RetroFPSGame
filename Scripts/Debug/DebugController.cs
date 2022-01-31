/*
    Base code from Matt Gambell
    https://www.youtube.com/c/GameDevGuide/
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugController : MonoBehaviour
{
    public static DebugController Instance = null;
    
    bool showConsole = false;
    bool showHelp = false;
    string consoleInput = string.Empty;

    public bool ConsoleOpen { get { return showConsole; } }

    // Commands
    public static DebugCommand FULL_ABILITY;
    public static DebugCommand NOCLIP;
    public static DebugCommand<int> GIVE_WEAPON;
    public static DebugCommand HELP;
    public List<object> commandList;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        else if (Instance != this)
        {
            Destroy(this);
        }

        // TODO: Move away from Awake-function
        FULL_ABILITY = new DebugCommand("full_ability", "Fills the players ability to 100 %", "full_ability", () =>
        {
            GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInventory>().ChargeAbility(100f);
        });

        NOCLIP = new DebugCommand("noclip", "Enable player flying through objects", "noclip", () =>
        {
            GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().NoClipEnabled();
        });

        GIVE_WEAPON = new DebugCommand<int>("give_weapon", "Gives a weapon with given index", "<give_weapon> <index (between 1-10)>", (x) =>
        {
            GameObject weaponPrefab = InventoryInfo.Instance.GetWeaponByIndex(x);
            GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInventory>().AddWeapon(weaponPrefab);
        });

        HELP = new DebugCommand("help", "Show a list of available commands", "help", () => { showHelp = true; });

        commandList = new List<object>
        {
            FULL_ABILITY, NOCLIP, GIVE_WEAPON, HELP
        };
    }

    private void Update()
    {
        // TODO: Move to new input system when project gets transferred to it
        if(Input.GetKeyDown(KeyCode.F1))
        {
            OnToggleDebug(!showConsole);
        }

        if(Input.GetKeyDown(KeyCode.Return))
        {
            OnReturn();
        }
    }

    void OnToggleDebug(bool value)
    {
        showConsole = value;

        if (showConsole)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }   

        else if (!showConsole && UIManager.Instance.BGamePaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }    
    }

    void OnReturn()
    {
        if(showConsole)
        {
            HandleInput();

            if(consoleInput != HELP.CommandID)
                OnToggleDebug(false);

            consoleInput = "";
        }
    }

    void HandleInput()
    {
        string[] properties = consoleInput.Split(' ');
        for(int i = 0; i < commandList.Count; i++)
        {
            DebugCommandBase commandBase = commandList[i] as DebugCommandBase;
            if(consoleInput.Contains(commandBase.CommandID))
            {
                if(commandList[i] as DebugCommand != null)
                {
                    (commandList[i] as DebugCommand).Invoke();
                }

                else if(commandList[i] as DebugCommand<int> != null)
                {
                    (commandList[i] as DebugCommand<int>).Invoke(int.Parse(properties[1]));
                }
            }
        }
    }

    Vector2 scroll;
    private void OnGUI()
    {
        if (!showConsole)
            return;

        float y = 0f;

        if (showHelp)
        {
            GUI.Box(new Rect(0, y, Screen.width, 100), "");

            Rect viewport = new Rect(0, 0, Screen.width - 30, 20 * commandList.Count);

            scroll = GUI.BeginScrollView(new Rect(0, y + 5f, Screen.width, 90), scroll, viewport);

            for(int i = 0; i < commandList.Count; i++)
            {
                DebugCommandBase command = commandList[i] as DebugCommandBase;
                string label = $"{command.CommandFormat} - {command.CommandDescription}";

                Rect labelRect = new Rect(5, 20 * i, viewport.width - 100, 20);
                GUI.Label(labelRect, label);
            }

            GUI.EndScrollView();

            y += 100;
        }
            
        
        GUI.Box(new Rect(0, y, Screen.width, 30), "");
        GUI.backgroundColor = new Color(0, 0, 0, 0);
        consoleInput = GUI.TextField(new Rect(10f, y + 5f, Screen.width - 20f, 20f), consoleInput);
    }
}
