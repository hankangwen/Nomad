using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using System.Threading;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    private static GameStateManager gameController;
    private static TextureData textureData;
    private static ObjectPool grassObjectPool;
    private static ObjectPool rockObjectPool;
    private static ObjectPool treeObjectPool;
    static ObjectPool spawnerObjectPool;
    static ObjectPool appleObjectPool;
    static ObjectPool stickObjectPool;
    static ObjectPool stoneObjectPool;
    private bool poolsAreReady = false;
    private int initFrameCounter = 0;
    public int seed;
    public static LevelManager Instance;
    public const string LevelDataKey = "levelData";
    public PhotonView pv;
    public bool initialized = false;
    public TerrainChunk currentTerrainChunk;

    void Awake()
    {
        Instance = this;
        pv = GetComponent<PhotonView>();
        DontDestroyOnLoad(gameObject);
    }

    public void InitializeLevelManager()
    {
        gameController = FindObjectOfType<GameStateManager>();
        seed = FindObjectOfType<TerrainGenerator>().biomeDataArray[0].heightMapSettings.noiseSettings.seed;
        UnityEngine.Random.InitState(seed);
        // Assumes that the grass object is at index 6, rock object at index 1, etc.
        grassObjectPool = new ObjectPool(ItemManager.Instance.environmentItemList[6].gameObject, 300);
        rockObjectPool = new ObjectPool(ItemManager.Instance.environmentItemList[1].gameObject, 100);
        treeObjectPool = new ObjectPool(ItemManager.Instance.environmentItemList[0].gameObject, 200);
        spawnerObjectPool = new ObjectPool(ItemManager.Instance.environmentItemList[8].gameObject, 50);
        // appleObjectPool = new ObjectPool(itemManager.itemList[8], 50);
        // stickObjectPool = new ObjectPool(itemManager.itemList[2], 50);
        // stoneObjectPool = new ObjectPool(itemManager.itemList[3], 50);
        poolsAreReady = true;
        initialized = true;

    }
    public List<string> GetAllChunkSaveData()
    {
        List<string> data = new List<string>();
        foreach (KeyValuePair<Vector2, TerrainChunk> kvp in TerrainGenerator.Instance.terrainChunkDictionary)
        {
            data.Add(LoadChunkJson(kvp.Value));
        }
        return data;
    }
    public void PopulateObjects(TerrainChunk terrainChunk, Mesh terrainMesh)
    {
        StartCoroutine(PopulateObjectsCoroutine(terrainChunk, terrainMesh));
    }
    IEnumerator PopulateObjectsCoroutine(TerrainChunk terrainChunk, Mesh terrainMesh)
    {
        if (gameController != null)
        {
            textureData = terrainChunk.biomeData.textureData;
        }

        TerrainChunkSaveData chunkSaveData = LevelManager.LoadChunk(terrainChunk);
        Transform parentTransform = terrainChunk.meshObject.transform;
        int c = 0;

        int objectDensity = 20;  // Higher values will place more objects
        float objectScale = 144f;
        bool hasSpawner = false;
        for (int x = 0; x < objectDensity; x++)
        {
            for (int z = 0; z < objectDensity; z++)
            {
                // Use Perlin noise to get a value between 0 and 1
                float noiseValue = terrainChunk.heightMap.values[x, objectDensity - z];

                float fractionalPart = noiseValue % 1;
                int randValue = fractionalPart > .1 && fractionalPart < .2 || fractionalPart > .3 && fractionalPart < .4 || fractionalPart > .5 && fractionalPart < .6 || fractionalPart > .7 && fractionalPart < .9 ? 1 : -1;
                // Calculate position based on noise value
                Vector3 position = new Vector3(x * objectScale / objectDensity, 0, z * objectScale / objectDensity) + new Vector3(terrainChunk.sampleCentre.x - 72, 0, terrainChunk.sampleCentre.y - 72);
                System.Random random = new System.Random(seed);
                position += new Vector3(fractionalPart * 10, 0f, fractionalPart * 10 * randValue);

                GameObject newObj;
                ObjectPool objPl;
                if ((x == objectDensity / 2 || x + 1 == objectDensity / 2) && (z == objectDensity / 2 || z + 1 == objectDensity / 2) && !hasSpawner)
                {
                    hasSpawner = true;
                    if (PhotonNetwork.IsMasterClient)
                    {
                        newObj = spawnerObjectPool.GetObject();
                        objPl = spawnerObjectPool;
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (noiseValue > 5 && randValue == 1)
                {
                    newObj = treeObjectPool.GetObject();
                    objPl = treeObjectPool;
                }
                else if (noiseValue > 4.4 && noiseValue < 4.5)
                {
                    newObj = rockObjectPool.GetObject();
                    objPl = rockObjectPool;

                }
                else if (noiseValue > 1)
                {
                    newObj = grassObjectPool.GetObject();
                    objPl = grassObjectPool;
                }
                else
                {
                    continue;
                }

                // If newObject does not have a sourceObject component, set the prefab index to match the actor spawner index. Apparently that is not saved anywhere on that object. 
                SourceObject sourceObj = newObj.GetComponent<SourceObject>();
                int prefabIndex = sourceObj ? sourceObj.itemIndex : 8;
                string _id = $"{(int)terrainChunk.coord.x},{(int)terrainChunk.coord.y}_{prefabIndex}_{(int)position.x}_{(int)position.z}_{(int)0}_{false}_{null}";

                bool isRemoved = false;

                // Check save data to see if this generated object  has been removed based on it's id
                // If so, skip this iteration of object/item spawning
                if (chunkSaveData != null && chunkSaveData.removedObjects != null)
                {
                    foreach (string obj in chunkSaveData.removedObjects)
                    {
                        if (obj == _id)
                        {
                            objPl.ReturnObject(newObj);
                            isRemoved = true;
                            continue;
                        }
                    }
                }
                // If this generated object has been removed, continue to the next object
                if (isRemoved)
                {
                    continue;
                }

                newObj.transform.position = position;
                newObj.transform.SetParent(terrainChunk.meshObject.transform);

                if (sourceObj)
                {
                    sourceObj.id = _id;
                }
                else
                {
                    newObj.GetComponent<ActorSpawner>().id = _id;
                }

                int objectPerFrame = initFrameCounter > 1 ? 30 : 100000;
                c++;
                if (c % objectPerFrame == 0)
                {
                    yield return null;
                }
            }
        }

        HashSet<string> instantiatedObjectIds = new HashSet<string>();

        if (chunkSaveData != null && chunkSaveData.objects != null && chunkSaveData.objects.Length > 0 && chunkSaveData.objects[0] != null)
        {
            foreach (string objId in chunkSaveData.objects)
            {
                // If this object has already been instantiated, skip to the next one
                if (instantiatedObjectIds.Contains(objId))
                {
                    continue;
                }
                string[] saveDataArr = objId.Split("_");
                GameObject _obj = saveDataArr[5] == "True" ? ItemManager.Instance.itemList[int.Parse(saveDataArr[1])] : ItemManager.Instance.environmentItemList[int.Parse(saveDataArr[1])];

                GameObject newObj = Instantiate(_obj, new Vector3(float.Parse(saveDataArr[2]), 0, float.Parse(saveDataArr[3])), Quaternion.Euler(0, float.Parse(saveDataArr[4]), 0));
                BuildingMaterial bm = newObj.GetComponent<BuildingMaterial>();
                if (saveDataArr[5] == "True")
                {
                    if (bm == null) newObj.GetComponent<SpawnMotionDriver>().hasSaved = true;
                    newObj.GetComponent<Item>().hasLanded = true;
                }
                newObj.GetComponent<Rigidbody>().isKinematic = true;
                newObj.transform.SetParent(terrainChunk.meshObject.transform);
                if (saveDataArr[5] == "True")
                {
                    if (bm != null)
                    {
                        bm.id = objId;
                        bm.parentChunk = terrainChunk;
                    }
                    else
                    {
                        Item itm = newObj.GetComponent<Item>();
                        itm.id = objId;
                        itm.parentChunk = terrainChunk;
                    }
                }
                else
                {
                    newObj.GetComponent<SourceObject>().id = objId;
                }
                if (saveDataArr[6] != "")
                {
                    string sateData = saveDataArr[6];
                    switch (int.Parse(saveDataArr[1]))
                    {
                        case 9:
                            if (saveDataArr[6] == "Packed")
                            {
                                newObj.GetComponent<PackableItem>().Pack(newObj);
                            }
                            break;
                    }
                }
                instantiatedObjectIds.Add(objId);
            }
        }
        //TODO: FIX
        //PopulateItems(terrainMesh, terrainChunk);
    }

    public void PopulateItems(Mesh terrainMesh, TerrainChunk terrainChunk)
    {
        StartCoroutine(PopulateItemsCoroutine(terrainMesh, terrainChunk));
    }

    IEnumerator PopulateItemsCoroutine(Mesh terrainMesh, TerrainChunk terrainChunk)
    {
        if (gameController != null)
        {
            textureData = terrainChunk.biomeData.textureData;
            ItemManager.Instance = FindObjectOfType<ItemManager>();
        }

        int width = terrainChunk.heightMap.values.GetLength(0);
        Transform parentTransform = terrainChunk.meshObject.transform;

        GameObject newItem;
        for (int i = 0; i < terrainMesh.vertices.Length; i += 6)
        {
            float randomNumber = UnityEngine.Random.value;

            //apples
            if (randomNumber > 0.9996f)
            {
                Quaternion itemRotation = Quaternion.FromToRotation(Vector3.up, terrainMesh.normals[i]);
                newItem = appleObjectPool.GetObject(); //Assumed itemObjectPool similar to object pools for tree, rock etc
                newItem.transform.position = terrainMesh.vertices[i] + new Vector3(terrainChunk.sampleCentre.x, 0, terrainChunk.sampleCentre.y);
                newItem.transform.Rotate(new Vector3(0, UnityEngine.Random.Range(-180, 180), 0));
                newItem.transform.SetParent(parentTransform);
                newItem.GetComponent<Rigidbody>().isKinematic = true;
                continue;
            }
            //Stones
            if (randomNumber > 0.9994f)
            {
                Quaternion itemRotation = Quaternion.FromToRotation(Vector3.up, terrainMesh.normals[i]);
                newItem = stoneObjectPool.GetObject(); //Assumed itemObjectPool similar to object pools for tree, rock etc
                newItem.transform.position = terrainMesh.vertices[i] + new Vector3(terrainChunk.sampleCentre.x, 0, terrainChunk.sampleCentre.y);
                newItem.transform.Rotate(new Vector3(0, UnityEngine.Random.Range(-180, 180), 0));
                newItem.transform.SetParent(parentTransform);
                newItem.GetComponent<Rigidbody>().isKinematic = true;
                continue;

            }
            //Sticks
            if (randomNumber > 0.9993f)
            {

                Quaternion itemRotation = Quaternion.FromToRotation(Vector3.up, terrainMesh.normals[i]);
                newItem = stickObjectPool.GetObject(); //Assumed itemObjectPool similar to object pools for tree, rock etc
                newItem.transform.position = terrainMesh.vertices[i] + new Vector3(terrainChunk.sampleCentre.x, 0, terrainChunk.sampleCentre.y);
                newItem.transform.Rotate(new Vector3(0, UnityEngine.Random.Range(-180, 180), 0));
                newItem.transform.SetParent(parentTransform);
                newItem.GetComponent<Rigidbody>().isKinematic = true;
                continue;
            }

            int objectPerFrame = initFrameCounter > 1 ? 1 : 100000;

            if (i % objectPerFrame == 0)  // Choose the number that works best for you.
            {
                yield return null;
            }
        }
    }

    public static TerrainChunkSaveData LoadChunk(TerrainChunk terrainChunk)
    {
        string levelName = LevelPrep.Instance.worldName;
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, $"Levels/{levelName}/");
        Directory.CreateDirectory(saveDirectoryPath);

        string filePath = saveDirectoryPath + terrainChunk.id + ".json";
        string json;
        try
        {
            json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<TerrainChunkSaveData>(json);

        }
        catch
        {
            Debug.Log("~ New Chunk. No data to load");
            return null;
        }
    }
    public static string LoadChunkJson(TerrainChunk terrainChunk)
    {
        string levelName = LevelPrep.Instance.worldName;
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, $"Levels/{levelName}");
        Directory.CreateDirectory(saveDirectoryPath);
        string filePath = saveDirectoryPath + terrainChunk.id + ".json";

        string json;
        try
        {
            json = File.ReadAllText(filePath);
            return json;

        }
        catch
        {
            Debug.Log("~ New Chunk. No data to load");
            return null;
        }
    }

    public void UpdateSaveData(TerrainChunk terrainChunk, int itemIndex, string objectId, bool isDestroyed, Vector3 pos, Vector3 rot, bool isItem, string stateData = null)
    {
        if (terrainChunk == null)
        {
            Debug.Log($"! Missing terrain chunk, can not update save data");
            return;
        }
        TerrainChunkSaveData data = LoadChunk(terrainChunk);
        List<string> currentData = data?.objects?.ToList() ?? new List<string>();

        // Saving objects that have been removed from the world 
        if (isDestroyed)
        {
            // Remove the object from the currentData
            int count = currentData.Count;
            currentData.RemoveAll(_obj => _obj == objectId);
            if (count == currentData.Count)
            {
            }

            if (!isItem)
            {
                // Saving items being added to the world
                List<string> _removedObjects = data?.removedObjects?.ToList() ?? new List<string>();
                _removedObjects.Add(objectId);
                data.removedObjects = _removedObjects.ToArray();
            }
        }
        else
        {
            //TODO: Build ID and add here
            currentData.Add($"{terrainChunk.coord.x},{terrainChunk.coord.y}_{itemIndex}_{pos.x}_{pos.z}_{rot.y}_{isItem}_{stateData}");
        }

        string id = LevelPrep.Instance.worldName + terrainChunk.coord.x + '-' + terrainChunk.coord.y;
        terrainChunk.saveData = new TerrainChunkSaveData(terrainChunk.id, currentData.ToArray(), data?.removedObjects);
        LevelManager.SaveChunk(terrainChunk);
        LevelManager.Instance.UpdateLevelData();
    }

    public RoomOptions UpdateLevelData()
    {

        string levelName = FindObjectOfType<LevelPrep>().worldName;
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, $"Levels/{levelName}/");
        Directory.CreateDirectory(saveDirectoryPath);
        string[] filePaths = Directory.GetFiles(saveDirectoryPath);

        // Read file contents and add to levelData
        List<string> levelDataList = new List<string>();
        foreach (string filePath in filePaths)
        {
            int retries = 5;
            string fileContent = "";
            while (retries > 0)
            {
                try
                {
                    fileContent = File.ReadAllText(filePath);
                    retries = 0;
                }
                catch (IOException)
                {
                    if (retries <= 0)
                        throw; // If we've retried enough times, rethrow the exception.
                    retries--;
                    Thread.Sleep(1000); // Wait a second before retrying.
                }
            }
            if (fileContent != "") levelDataList.Add(fileContent);
        }

        // Convert the list of strings to a single string
        string levelData = string.Join("|-|", levelDataList);

        // Pass level data to network
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { LevelDataKey, levelData } });
        }
        return null;
    }
    public static LevelSaveData LoadLevel()
    {
        string levelName = LevelPrep.Instance.worldName;
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, $"Levels/{levelName}/");
        string filePath = saveDirectoryPath + levelName + ".json";

        string json;
        try
        {
            json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<LevelSaveData>(json);

        }
        catch
        {
            Debug.Log("~ New Level. No data to load");
            return null;
        }
    }
    public static void SaveLevel()
    {
        if (!FindObjectOfType<GameStateManager>().initialized)
        {
            return;
        }
        string levelName = LevelPrep.Instance.worldName;
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, $"Levels/{levelName}/");
        Directory.CreateDirectory(saveDirectoryPath);
        Vector3 playerPos = gameController.playersManager.playersCentralPosition;
        Debug.LogWarning("~ SavingLevel " + playerPos);
        GameStateManager.Instance.spawnPoint = playerPos;
        LevelSaveData data = new LevelSaveData(playerPos.x, playerPos.y, playerPos.z, gameController.currentRespawnPoint.x, gameController.currentRespawnPoint.y, gameController.currentRespawnPoint.z, GameStateManager.Instance.timeCounter, GameStateManager.Instance.sun.transform.rotation.eulerAngles.x);
        string json = JsonConvert.SerializeObject(data);
        string filePath = saveDirectoryPath + levelName + ".json";
        // Open the file for writing
        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        using (StreamWriter writer = new StreamWriter(stream))
        {
            // Write the JSON string to the file
            writer.Write(json);
        }
    }
    public static void SaveLevel(Vector3 SpawnPoint)
    {
        if (!FindObjectOfType<GameStateManager>().initialized)
        {
            return;
        }
        string levelName = LevelPrep.Instance.worldName;
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, $"Levels/{LevelPrep.Instance.worldName}/");
        Directory.CreateDirectory(saveDirectoryPath);
        Vector3 playerPos = gameController.playersManager.playersCentralPosition;
        Debug.LogWarning("~ SavingLevel " + playerPos);
        GameStateManager.Instance.spawnPoint = playerPos;
        LevelSaveData data = new LevelSaveData(SpawnPoint.x, SpawnPoint.y, SpawnPoint.z, SpawnPoint.x, SpawnPoint.y, SpawnPoint.z, GameStateManager.Instance.timeCounter, GameStateManager.Instance.sun.transform.rotation.x);
        string json = JsonConvert.SerializeObject(data);
        string filePath = saveDirectoryPath + levelName + ".json";
        // Open the file for writing
        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        using (StreamWriter writer = new StreamWriter(stream))
        {
            // Write the JSON string to the file
            writer.Write(json);
        }
    }
    public static void SaveChunk(TerrainChunk terrainChunk)
    {
        string levelName = LevelPrep.Instance.worldName;
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, $"Levels/{levelName}/");
        Directory.CreateDirectory(saveDirectoryPath);
        string filePath = saveDirectoryPath + terrainChunk.id + ".json";
        TerrainChunkSaveData data = terrainChunk.saveData;
        string json = JsonConvert.SerializeObject(data);
        // Open the file for writing
        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        using (StreamWriter writer = new StreamWriter(stream))
        {
            // Write the JSON string to the file
            writer.Write(json);
        }
        SaveLevel();
    }
    public void SaveProvidedLevelData(string levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("No level data to load " + PhotonNetwork.LocalPlayer.UserId);
            return;
        }
        string[] separateFileStrings = levelData.Split(new string[] { "|-|" }, StringSplitOptions.RemoveEmptyEntries);
        string levelName = LevelPrep.Instance.worldName;
        string saveDirectoryPath = Path.Combine(Application.persistentDataPath, $"Levels/{levelName}/");
        try
        {

            Directory.Delete(saveDirectoryPath, true);
        }
        catch
        {
            Debug.LogWarning("No existing directory to remove for level");
        }
        Directory.CreateDirectory(saveDirectoryPath);
        for (int i = 0; i < separateFileStrings.Length; i++)
        {
            TerrainObjectSaveData level = JsonConvert.DeserializeObject<TerrainObjectSaveData>(separateFileStrings[i]);
            string filePath;
            if (i < separateFileStrings.Length - 1)
            {
                filePath = saveDirectoryPath + level.id + ".json";
            }
            else
            {
                filePath = saveDirectoryPath + levelName + ".json";
            }
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                // Write the JSON string to the file
                writer.Write(separateFileStrings[i]);
            }
        }
        LevelPrep.Instance.receivedLevelFiles = true;
    }

    public void CallPackItem(string id)
    {
        pv.RPC("PackItemRPC", RpcTarget.AllBuffered, id);
    }
    [PunRPC]
    public void PackItemRPC(string id)
    {
        PackableItem[] packabels = FindObjectsOfType<PackableItem>();
        foreach (PackableItem item in packabels)
        {
            if (item.GetComponent<Item>().id == id)
            {
                item.Pack(item.gameObject);
            }
        }
    }
    public void CallPlaceObjectPRC(int activeChildIndex, Vector3 position, Vector3 rotation, string id, bool isPacked)
    {
        pv.RPC("PlaceObjectPRC", RpcTarget.AllBuffered, activeChildIndex, position, rotation, id, isPacked);
    }

    [PunRPC]
    void PlaceObjectPRC(int activeChildIndex, Vector3 _position, Vector3 _rotation, string id, bool isPacked)
    {
        GameObject newObject = ItemManager.Instance.environmentItemList[activeChildIndex];
        GameObject finalObject = Instantiate(newObject, _position, Quaternion.Euler(_rotation));
        //Check the final object for a source object script and set the ID
        SourceObject so = finalObject.GetComponent<SourceObject>();
        if (so != null)
        {
            so.id = id;
        }
        else// If no source object is found, we need to set the id on the item.
        {   // This is for crafting benches and fire pits.
            finalObject.GetComponent<Item>().id = id;
        }
        finalObject.GetComponent<BuildingObject>().isPlaced = true;
        if (isPacked)
        {
            finalObject.GetComponent<PackableItem>().Pack(finalObject);
        }
    }

    public void CallUpdateObjectsPRC(string objectId, int damage, ToolType toolType, Vector3 hitPos, PhotonView attacker)
    {
        pv.RPC("UpdateObject_PRC", RpcTarget.All, objectId, damage, toolType, hitPos, attacker.ViewID);
    }

    [PunRPC]
    public void UpdateObject_PRC(string objectId, int damage, ToolType toolType, Vector3 hitPos, int attackerViewId)
    {
        PhotonView attacker = PhotonView.Find(attackerViewId);
        string[] idSubStrings = objectId.Split('_');
        foreach (TerrainChunk terrain in TerrainGenerator.Instance.visibleTerrainChunks)
        {

            if (terrain.id == idSubStrings[0])
            {
                int childCount = terrain.meshObject.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    SourceObject so = terrain.meshObject.transform.GetChild(i).GetComponent<SourceObject>();
                    HealthManager hm = terrain.meshObject.transform.GetChild(i).GetComponent<HealthManager>();

                    if (so != null)
                    {
                        if (so.id == objectId)
                        {
                            so.TakeDamage(damage, toolType, hitPos, attacker.gameObject);
                        }
                    }
                    else if (terrain.meshObject.transform.GetChild(i).GetComponent<BuildingMaterial>() != null)
                    {
                        if (terrain.meshObject.transform.GetChild(i).GetComponent<BuildingMaterial>().id == objectId && hm != null)
                        {
                            hm.TakeHit(damage, toolType, hitPos, attacker.gameObject);
                        }
                    }

                }

            }
        }
        // Your code to add or remove object
    }
    public void CallUpdateItemsRPC(string itemId)
    {
        pv.RPC("UpdateItems_RPC", RpcTarget.OthersBuffered, itemId);
    }

    [PunRPC]
    public void UpdateItems_RPC(string itemId)
    {
        Item[] items = FindObjectsOfType<Item>();
        foreach (Item item in items)
        {
            if (item.id == itemId)
            {
                bool isSaved = item.SaveItem(item.parentChunk, true);
                if (isSaved) Destroy(item.gameObject);
            }
        }
        // Your code to add or remove object
    }
    public void CallUpdateFirePitRPC(string firePitId)
    {
        pv.RPC("UpdateFirePit_RPC", RpcTarget.AllBuffered, firePitId);
    }

    [PunRPC]
    public void UpdateFirePit_RPC(string firePitId)
    {
        BuildingMaterial[] items = FindObjectsOfType<BuildingMaterial>();
        foreach (BuildingMaterial item in items)
        {
            if (item.id == firePitId)
            {
                item.GetComponent<FirePitInteraction>().StokeFire();
            }
        }
    }
}

public class LevelSaveData
{
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public float respawnPosX;
    public float respawnPosY;
    public float respawnPosZ;
    public float time;
    public float sunRot;
    public LevelSaveData(float playerPosX, float playerPosY, float playerPosZ, float respawnPosX, float respawnPosY, float respawnPosZ, float time, float sunRot)
    {
        this.playerPosX = playerPosX;
        this.playerPosY = playerPosY;
        this.playerPosZ = playerPosZ;
        this.respawnPosX = respawnPosX;
        this.respawnPosY = respawnPosY;
        this.respawnPosZ = respawnPosZ;
        this.time = time;
        this.sunRot = sunRot;
    }
}
public class TerrainChunkSaveData
{
    public string id;
    public string[] objects;
    public string[] removedObjects;
    public TerrainChunkSaveData(string id, string[] objects, string[] removedObjects)
    {
        this.id = id;
        this.objects = objects;
        this.removedObjects = removedObjects;
    }
}

public class TerrainObjectSaveData
{
    public int itemIndex;
    public float x;
    public float y;
    public float z;
    public float rx;
    public float ry;
    public float rz;
    public string id;
    public bool isItem;
    public TerrainObjectSaveData(int itemIndex, float x, float y, float z, float rx, float ry, float rz, string id, bool isItem)
    {
        this.itemIndex = itemIndex;
        this.x = x;
        this.y = y;
        this.z = z;
        this.rx = rx;
        this.ry = ry;
        this.rz = rz;
        this.id = id;
        this.isItem = isItem;
    }
}


public class ObjectPool
{
    public GameObject prefab;
    public Stack<GameObject> pool;

    public ObjectPool(GameObject prefab, int initialSize)
    {

        this.prefab = prefab;
        pool = new Stack<GameObject>(initialSize);

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = GameObject.Instantiate(prefab);
            obj.SetActive(false);
            pool.Push(obj);
        }
    }

    public GameObject GetObject()
    {
        if (pool.Count > 0)
        {
            GameObject newObj = pool.Pop();
            newObj.SetActive(true);
            return newObj;
        }
        else
        {
            GameObject obj = GameObject.Instantiate(prefab);
            obj.SetActive(true);
            return obj;
        }
    }

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        pool.Push(obj);
    }
}