﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StateController : MonoBehaviour
{
    public State currentState;
    public EnemyStats enemyStats;
    public Transform eyes;
    public GameObject wayPointParent;
    [SerializeField] public List<Transform> wayPointList;
    public State remainState;

    // [HideInInspector] public NavMeshAgent navMeshAgent;
    [HideInInspector] public NavMeshAgent navMeshAgent;
    /*[HideInInspector] */
    public int nextWayPoint;
    public Transform target;
    public Transform lastTarget;
    [HideInInspector] public bool focusOnTarget;
    [HideInInspector] public SphereCollider sphereCollider;
    [HideInInspector] public ActorEquipment equipment;
    [HideInInspector] public Rigidbody rigidbodyRef;
    [HideInInspector] public EnemyManager enemyManager;

    [HideInInspector] public AIMover aiMover;
    public bool aiActive;

    private void Awake()
    {
        // navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        sphereCollider = GetComponent<SphereCollider>();
        equipment = GetComponent<ActorEquipment>();
        rigidbodyRef = GetComponent<Rigidbody>();
        enemyManager = GetComponent<EnemyManager>();
        aiMover = GetComponent<AIMover>();
        if (wayPointParent != null)
        {
            for (int i = 0; i < wayPointParent.transform.childCount; i++)
            {
                wayPointList.Add(wayPointParent.transform.GetChild(i).transform);
            }
        }

    }

    private void Start()
    {
        //SetupAI(true);
    }

    public void SetupAI(bool aiActivationFromTankManager)
    {
        aiActive = aiActivationFromTankManager;

        if (aiActive)
        {
            // navMeshAgent.enabled = true;
            navMeshAgent.enabled = true;
        }
        else
        {
            // navMeshAgent.enabled = false;
            navMeshAgent.enabled = false;
        }
    }

    private void Update()
    {
        if (PlayersManager.Instance.GetDistanceToClosestPlayer(transform) > 50 && !CompareTag("Beast"))
        {
            aiActive = false;
        }
        else
        {
            aiActive = true;
        }

        if (!aiActive)
        {
            return;
        }
        else
        {
            currentState.UpdateState(this);
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            //Gizmos.DrawSphere(navMeshAgent.destination, 1);
            Gizmos.DrawSphere(navMeshAgent.destination, 1);

            if (currentState != null)
            {
                Gizmos.color = currentState.SceneGizmoColor;
                Gizmos.DrawWireSphere(eyes.position, enemyStats.lookSphereCastRadius);
            }
        }

    }

    public void TransitionToState(State nextState)
    {
        if (nextState != remainState)
        {
            currentState = nextState;
        }
    }

}