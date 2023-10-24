using UnityEngine;
using TMPro;
using System.Collections.Generic;
public class SpawnMotionDriver : MonoBehaviour
{
    private Vector3 initialVelocity;
    public float gravity = 40f;
    bool isFalling = false;
    private float time = .5f;
    private Vector3 startPos;
    public bool hasSaved = true;
    private string fallType = "default";

    private void Start()
    {
        startPos = transform.position;
    }

    public void Fall(Vector3 _initialVelocity, string _fallType = "default")
    {
        isFalling = true;
        hasSaved = false;
        initialVelocity = _initialVelocity;
        fallType = _fallType;
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (isFalling)
        {
            time += Time.deltaTime;

            switch (fallType.ToLower())
            {
                case "tree":
                    // Emulating a tree falling, by rotating the tree along the x-axis as it falls
                    transform.Rotate(Vector3.right, Time.deltaTime * 50);
                    if (transform.rotation.eulerAngles.x > 90)
                    {
                        transform.rotation = Quaternion.Euler(Vector3.right * 90);
                    }
                    FallWithGravity();
                    break;

                default:
                    FallWithGravity();
                    break;
            }

            if (transform.position.y <= 0)
            {
                Land();
            }
        }
    }

    private void FallWithGravity()
    {
        float y = startPos.y + initialVelocity.y * time - 0.5f * gravity * Mathf.Pow(time, 2);
        float x = startPos.x + initialVelocity.x * time;
        float z = startPos.z + initialVelocity.z * time;
        transform.position = new Vector3(x, y, z);
    }


    //TODO: I think we can get rid of this but we need to verify
    void Land()
    {
        TerrainChunk chunk = LevelManager.Instance.currentTerrainChunk;
        isFalling = false;
        Item item = GetComponent<Item>();
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        if (fallType == "tree") transform.rotation = Quaternion.Euler(Vector3.right * 90);
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        PackableItem packable = GetComponent<PackableItem>();
        string stateData = "";
        if (packable != null)
        {
            GetComponent<MeshCollider>().isTrigger = false;
            if (packable.packed)
            {
                stateData = "Packed";
            }
        }
        item.transform.position = new Vector3((int)item.transform.position.x, (int)item.transform.position.y, (int)item.transform.position.z);
        item.id = $"{(int)chunk.coord.x},{(int)chunk.coord.y}_{ItemManager.Instance.GetItemIndex(item)}_{(int)transform.position.x}_{(int)transform.position.z}_{(int)0}_{true}_{stateData}";
        Debug.Log("### ID: " + item.id);
        item.parentChunk = chunk;
        item.transform.parent = LevelManager.Instance.currentTerrainChunk.meshObject.transform;
        item.SaveItem(chunk, false);
        item.hasLanded = true;
        hasSaved = true;
    }
    // private void OnTriggerEnter(Collider col)
    // {
    //     if (col.gameObject.CompareTag("WorldTerrain") && hasSaved == false) // Replace "Ground" with the tag you use for the ground.
    //     {
    //         TerrainChunk chunk = col.gameObject.GetComponent<TerrainChunkRef>().terrainChunk;
    //         isFalling = false;
    //         Item item = GetComponent<Item>();
    //         Rigidbody rb = GetComponent<Rigidbody>();
    //         rb.isKinematic = true;
    //         rb.useGravity = false;
    //         if (fallType == "tree") transform.rotation = Quaternion.Euler(Vector3.right * 90);
    //         transform.position = new Vector3(transform.position.x, 0, transform.position.z);
    //         transform.position = new Vector3(transform.position.x, 0, transform.position.z);
    //         PackableItem packable = GetComponent<PackableItem>();
    //         string stateData = "";
    //         if (packable != null)
    //         {
    //             GetComponent<MeshCollider>().isTrigger = false;
    //             if (packable.packed)
    //             {
    //                 stateData = "Packed";
    //             }
    //         }
    //         item.id = $"{(int)chunk.coord.x},{(int)chunk.coord.y}_{ItemManager.Instance.GetItemIndex(item)}_{(int)transform.position.x}_{(int)transform.position.z}_{(int)0}_{true}_{stateData}";
    //         item.parentChunk = chunk;
    //         item.transform.parent = col.gameObject.transform;
    //         item.SaveItem(chunk, false);
    //         item.hasLanded = true;
    //         hasSaved = true;
    //     }
    // }
}

