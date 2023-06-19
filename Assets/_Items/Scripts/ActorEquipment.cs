﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorEquipment : MonoBehaviour
{
    public GameObject equippedItem;
    public bool hasItem;
    private Item newItem;
    public Transform[] m_HandSockets = new Transform[2];
    private List<Item> grabableItems = new List<Item>();
    private PlayerInventoryManager inventoryManager;
    private bool showInventoryUI;
    CharacterManager characterManager;
    public Animator m_Animator;//public for debug
    public bool isPlayer = false;
    public TheseHands[] m_TheseHandsArray = new TheseHands[2];
    ItemManager m_ItemManager;

    public void Awake()
    {
        if (tag == "Player")
        {
            isPlayer = true;
        }
        characterManager = GetComponent<CharacterManager>();
        inventoryManager = GetComponent<PlayerInventoryManager>();
        m_ItemManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<ItemManager>();

        hasItem = false;
        m_Animator = GetComponentInChildren<Animator>();
        m_TheseHandsArray = GetComponentsInChildren<TheseHands>();
        m_HandSockets = new Transform[2];
        GetHandSockets(transform);
        if (equippedItem != null)
        {
            GameObject newEquipment = Instantiate(equippedItem);
            EquipItem(equippedItem.GetComponent<Item>());
        }
    }

    void Start()
    {

        if (equippedItem != null)
        {
            if (tag == "Player")
            {
                hasItem = true;
                inventoryManager.UpdateUiWithEquippedItem(equippedItem.GetComponent<Item>().icon);
            }
        }
    }


    void GetHandSockets(Transform _transform)
    {
        foreach (Transform t in _transform.GetComponentInChildren<Transform>())
        {
            if (t.gameObject.tag == "HandSocket")
            {
                if (t.gameObject.name == "LeftHandSocket")
                {
                    m_HandSockets[0] = t;
                }
                else
                {
                    m_HandSockets[1] = t;
                }
            }
            else
            {
                if (t.childCount > 0)
                {
                    GetHandSockets(t);
                }
            }
        }
    }

    public void AddItemToInventory(Item item)
    {
        if (item.fitsInBackpack)
        {
            inventoryManager.AddItem(item, 1);
            item.gameObject.SetActive(false);
        }
        if (isPlayer) characterManager.SaveCharacter();

    }

    public void EquipItem(GameObject item)
    {
        Item _item = item.GetComponent<Item>();
        if (_item.isEquipable)
        {
            hasItem = true;
            int handSocketIndex = _item.itemAnimationState == 1 ? 0 : 1;
            GameObject newItem = Instantiate(m_ItemManager.GetPrefabByItem(_item), m_HandSockets[handSocketIndex].position, m_HandSockets[handSocketIndex].rotation, m_HandSockets[handSocketIndex]);
            equippedItem = newItem;
            Item[] itemScripts = equippedItem.GetComponents<Item>();
            foreach (Item itm in itemScripts)
            {
                itm.OnEquipped(this.gameObject);
                itm.gameObject.SetActive(true);
            }
            equippedItem.GetComponent<Item>().OnEquipped(this.gameObject);
            equippedItem.gameObject.SetActive(true);
            //Change the animator state to handle the item equipped
            m_Animator.SetInteger("ItemAnimationState", _item.itemAnimationState);
            //Destroy(item);
            ToggleTheseHands(false);
        }
    }
    public void EquipItem(Item item)
    {
        if (item.isEquipable)
        {
            hasItem = true;
            int handSocketIndex = item.itemAnimationState == 1 ? 0 : 1;
            GameObject newItem = Instantiate(m_ItemManager.GetPrefabByItem(item), m_HandSockets[handSocketIndex].position, m_HandSockets[handSocketIndex].rotation, m_HandSockets[handSocketIndex]);
            equippedItem = newItem;
            equippedItem.GetComponent<Item>().OnEquipped(this.gameObject);
            equippedItem.gameObject.SetActive(true);
            //Change the animator state to handle the item equipped
            m_Animator.SetInteger("ItemAnimationState", item.itemAnimationState);
            ToggleTheseHands(false);
        }
        if (isPlayer) characterManager.SaveCharacter();
    }

    void ToggleTheseHands(bool toggle)
    {
        foreach (TheseHands th in m_TheseHandsArray)
        {
            th.gameObject.GetComponent<SphereCollider>().enabled = toggle;
        }
    }

    public void UnequipItem()
    {
        hasItem = false;
        equippedItem.GetComponent<Item>().OnUnequipped();
        equippedItem.GetComponent<Item>().inventoryIndex = -1;
        equippedItem.transform.parent = null;
        m_Animator.SetInteger("ItemAnimationState", 0);

        ToggleTheseHands(true);
        if (isPlayer) characterManager.SaveCharacter();

    }
    public void UnequipItem(bool spendItem)
    {
        hasItem = false;
        equippedItem.GetComponent<Item>().OnUnequipped();
        Object.Destroy(equippedItem.gameObject);
        m_Animator.SetInteger("ItemAnimationState", 0);

        ToggleTheseHands(true);
        if (isPlayer) characterManager.SaveCharacter();

    }

    public void UnequippedToInventory()
    {
        if (equippedItem.GetComponent<Item>().fitsInBackpack)
        {
            AddItemToInventory(equippedItem.GetComponent<Item>());
            //Set animator state to unarmed
            m_Animator.SetInteger("ItemAnimationState", 0);
            // Turn these hands on
            ToggleTheseHands(true);
            Destroy(equippedItem);
            equippedItem = null;
            hasItem = false;
            //If this is not an npc, save the character
            if (isPlayer) characterManager.SaveCharacter();

        }
        else
        {
            //If this item is not able to fit in the back pack, unequip
            UnequipItem();
        }

    }

    public void SpendItem()
    {
        Debug.Log("### spending item");
        Item item = equippedItem.GetComponent<Item>();
        if (equippedItem.GetComponent<Item>().inventoryIndex >= 0 && inventoryManager.items[equippedItem.GetComponent<Item>().inventoryIndex].count > 0)
        {
            Debug.Log("### here 1 " + equippedItem.GetComponent<Item>().inventoryIndex);

            inventoryManager.RemoveItem(equippedItem.GetComponent<Item>().inventoryIndex, 1);
            if (isPlayer) characterManager.SaveCharacter();
        }
        else
        {
            Debug.Log("### here");
            UnequipItem(true);
        }

    }


    // Finds all not equiped items in the screen that are close enough to the player to grab and adds them to the grabableItems list. This function also returns the closest
    Item GatherAllItemsInScene()
    {
        Item[] allItems = GameObject.FindObjectsOfType<Item>();
        Item closestItem = null;
        float closestDist = 5;
        foreach (Item item in allItems)
        {
            if (!item.isEquipped)
            {
                float currentItemDist = Vector3.Distance(transform.position, item.gameObject.transform.position);

                if (currentItemDist < 3)
                {
                    if (currentItemDist < closestDist)
                    {
                        //TODO check for player direction as well to stop players from picking up unintended items

                        closestDist = currentItemDist;
                        closestItem = item;
                        Outline outline = closestItem.GetComponent<Outline>();
                        if (outline != null)
                        {
                            outline.enabled = true;
                        }
                    }
                    else
                    {
                        if (item.GetComponent<Outline>() != null)
                        {
                            item.GetComponent<Outline>().enabled = false;
                        }
                    }

                    grabableItems.Add(item);//TODO a list?
                }
            }
        }

        if (grabableItems.Count <= 0)
            return null;
        else
            return closestItem;
    }

    public void GrabItem()
    {
        newItem = GatherAllItemsInScene();
        if (newItem.itemName == "Fire Pit")
        {
            return;
        }
        if (hasItem)
        {
            if (newItem != null)
            {
                if (newItem.fitsInBackpack)
                {
                    AddItemToInventory(newItem);
                    if (isPlayer) characterManager.SaveCharacter();

                }
                else
                {
                    UnequipItem();
                    EquipItem(m_ItemManager.GetPrefabByItem(newItem));
                    Destroy(newItem.gameObject);
                    if (isPlayer) characterManager.SaveCharacter();

                }
            }
        }
        else
        {
            if (newItem != null)
            {
                newItem.inventoryIndex = -1;
                EquipItem(m_ItemManager.GetPrefabByItem(newItem));
                Destroy(newItem.gameObject);
                if (isPlayer) characterManager.SaveCharacter();
            }
        }
    }
}
