using System.Collections;
using UnityEngine;
using Photon.Pun;
using System.IO;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;
    public const string LevelDataKey = "levelData";
    void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.buildIndex != 0) // We're in the game scene
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LevelDataKey, out object levelDataValue))
                {
                    string levelData = (string)levelDataValue;
                    LevelManager.Instance.SaveProvidedLevelData(levelData);
                }
            }
            StartCoroutine(PlayerInitializer());
        }
    }
    IEnumerator PlayerInitializer()
    {
        for (int i = 0; i < LevelPrep.Instance.numberOfPlayers; i++)
        {
            GameObject playerManager = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerManager"), transform.position + new Vector3(i, 0, 0), Quaternion.identity);
            playerManager.GetComponent<PhotonView>().RPC("Initialize", RpcTarget.AllBuffered, i);
            yield return new WaitForSeconds(2);
            GameStateManager.Instance.InitializeGameState();
        }
    }
}
