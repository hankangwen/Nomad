using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;

public class FireHeadBoss : MonoBehaviour
{
    public HealthManager m_HealthManager;
    public Transform m_TargetPillar;
    public float m_CurrentHealthThreshold;
    ActorSpawner[] spawners;
    bool canSpawn = true;
    void Start()
    {
        m_HealthManager = GetComponent<HealthManager>();
        m_CurrentHealthThreshold = m_HealthManager.maxHealth / 3 * 2;
        spawners = FindObjectsOfType<ActorSpawner>();
    }

    void Update()
    {
        if (transform.position.y < 5)
        {
            if (!canSpawn)
            {
                foreach (ActorSpawner spawner in spawners)
                {
                    spawner.maxActorCount = 2;
                }
                canSpawn = true;
            }

        }
        else
        {
            if (canSpawn)
            {
                foreach (ActorSpawner spawner in spawners)
                {
                    spawner.maxActorCount = 0;
                }
                canSpawn = false;
            }
        }
    }
}
