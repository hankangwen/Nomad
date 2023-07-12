using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections.Generic;
using Photon.Realtime;
using System.IO;
using Newtonsoft.Json;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher Instance;
    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] Transform roomListContent;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] GameObject playerListItemPrefab;
    [SerializeField] GameObject startGameButton;
    public const string LevelDataKey = "levelData";
    public string levelData;

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        if (LevelPrep.Instance.offline)
        {
            PhotonNetwork.OfflineMode = true; // Enable offline mode
            PhotonNetwork.CreateRoom("OfflineRoom"); // Create a local "offline" room
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings(); // Otherwise connect as normal
        }
    }
    public override void OnConnectedToMaster()
    {
        if (!PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.JoinLobby();
            PhotonNetwork.AutomaticallySyncScene = true;
        }
    }

    public override void OnJoinedLobby()
    {
        if (!PhotonNetwork.OfflineMode)
        {
            MenuManager.Instance.OpenMenu("title");
            PhotonNetwork.NickName = "Player" + Random.Range(0, 1000).ToString("0000");
        }
    }

    public void CreateRoom()
    {
        // if input field is empty, do not create room
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }
        RoomOptions ro = SetLevelData(true);
        PhotonNetwork.CreateRoom(roomNameInputField.text, ro);
        MenuManager.Instance.OpenMenu("loading");

    }

    public RoomOptions SetLevelData(bool createRoom)
    {
        string levelName = FindObjectOfType<LevelPrep>().worldName;
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, $"Levels/{levelName}/");
        Directory.CreateDirectory(saveDirectoryPath);
        string[] filePaths = Directory.GetFiles(saveDirectoryPath);

        // Read file contents and add to levelData
        List<string> levelDataList = new List<string>();
        foreach (string filePath in filePaths)
        {
            string fileContent = File.ReadAllText(filePath);
            levelDataList.Add(fileContent);
        }

        // Convert the list of strings to a single string
        string levelData = string.Join("|SEPARATOR|", levelDataList);

        if (createRoom)
        {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable() { { LevelDataKey, levelData } };
            return roomOptions;
        }
        else
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { LevelDataKey, levelData } });
            return null;
        }
    }
    public override void OnJoinedRoom()
    {
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        MenuManager.Instance.OpenMenu("room");
        Player[] players = PhotonNetwork.PlayerList;
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }
        if (!PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LevelDataKey, out object levelDataValue))
            {
                levelData = (string)levelDataValue;
                LevelManager.Instance.SaveProvidedLevelData(levelData);
            }
        }
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }
    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed: " + message;
        MenuManager.Instance.OpenMenu("error");
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("loading");
    }

    public void JoinRoom(RoomInfo roomInfo)
    {
        PhotonNetwork.JoinRoom(roomInfo.Name);
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnLeftRoom()
    {
        MenuManager.Instance.OpenMenu("title");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform trans in roomListContent)
        {
            Destroy(trans.gameObject);
        }
        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].RemovedFromList)
            {
                continue;
            }
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomList[i]);
        }

    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }
}
