using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class LevelPrep : MonoBehaviourPunCallbacks
{
    [HideInInspector]
    public static LevelPrep Instance;
    public string settlementName;
    public string currentLevel;
    public int numberOfPlayers;
    public bool offline;
    [HideInInspector]
    public bool receivedLevelFiles;
    public bool firstPlayerGamePad;
    public bool isFirstLoad; //Tells weather the portals should be on or off based on weather this is the first load or not
    public SettingsConfig settingsConfig;

    void Awake()
    {
        isFirstLoad = true;
        Instance = this;
        firstPlayerGamePad = settingsConfig.firstPlayerGamePad;
        DontDestroyOnLoad(this);
    }
}
