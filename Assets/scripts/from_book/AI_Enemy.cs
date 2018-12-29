﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AI_Enemy : MonoBehaviour
{
    public enum ENEMY_STATE { PATROL, CHASE, ATTACK };
    //------------------------------------------
    public ENEMY_STATE CurrentState
    {
        get { return currentstate; }
        set
        {
            //Update current state
            currentstate = value;
            //Stop all running coroutines
            StopAllCoroutines();
            switch (currentstate)
            {
                case ENEMY_STATE.PATROL:
                    StartCoroutine(AIPatrol());
                    break;
                case ENEMY_STATE.CHASE:
                    StartCoroutine(AIChase());
                    break;
                case ENEMY_STATE.ATTACK:
                    StartCoroutine(AIAttack());
                    break;
            }
        }
    }//------------------------------------------
    [SerializeField]
    private ENEMY_STATE currentstate = ENEMY_STATE.PATROL;
    //Reference to line of sight component
    private LineSight ThisLineSight = null;
    //Reference to nav mesh agent
    private NavMeshAgent ThisAgent = null;
    //Reference to player transform
    private Transform PlayerTransform = null;

    private Health PlayerHealth = null;

    public float MaxDamage = 10f;



    Transform PatrolDestination = null;
    //------------------------------------------
    void Awake()
    {
        ThisLineSight = GetComponent<LineSight>();
        ThisAgent = GetComponent<NavMeshAgent>();
        PlayerTransform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        PlayerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
    }//------------------------------------------
    void Start()
    {
        //Get random destination
        GameObject[] Destinations = GameObject.FindGameObjectsWithTag("dest");
        PatrolDestination = Destinations[Random.Range(0, Destinations.Length)].GetComponent<Transform>();

        //Configure starting state
        CurrentState = ENEMY_STATE.PATROL;
    }//------------------------------------------
    public IEnumerator AIPatrol()
    {
        //Loop while patrolling
        while (currentstate == ENEMY_STATE.PATROL)
        {
            //Set strict search
            ThisLineSight.Sensitity = LineSight.SightSensitivity.STRICT;
            //Chase to patrol position
            ThisAgent.isStopped = false;
            ThisAgent.SetDestination(PatrolDestination.position);
            //Wait until path is computed
            while (ThisAgent.pathPending)
            {
                yield return null;
            }
            //If we can see the target then start chasing
            if (ThisLineSight.CanSeeTarget)
            {
                ThisAgent.isStopped = true;
                CurrentState = ENEMY_STATE.CHASE;
                yield break;
            }
            // Wait until next frame
            yield return null;
        }
    }
        //------------------------------------------
    public IEnumerator AIChase()
    {
        //Loop while chasing
        while (currentstate == ENEMY_STATE.CHASE)
        {
            //Set loose search
            ThisLineSight.Sensitity = LineSight.SightSensitivity.LOOSE;
            //Chase to last known position
            ThisAgent.isStopped = false;
            ThisAgent.SetDestination(ThisLineSight.LastKnowSighting);
            //Wait until path is computed
            while (ThisAgent.pathPending)
                yield return null;
            //Have we reached destination?
            if (ThisAgent.remainingDistance <= ThisAgent.stoppingDistance)
            {
                //Stop agent
                ThisAgent.isStopped = true;
                //Reached destination but cannot see player
                if (!ThisLineSight.CanSeeTarget)
                    CurrentState = ENEMY_STATE.PATROL;
                else //Reached destination and can see player. Reached    attacking distance
                    CurrentState = ENEMY_STATE.ATTACK;

                yield break;
            } 
            
            // Wait until next frame
            yield return null;
        }

    }//------------------------------------------
    public IEnumerator AIAttack()
    {
        //Loop while chasing and attacking
        while (currentstate == ENEMY_STATE.ATTACK)
        {
            //Chase to player position
            ThisAgent.isStopped = false;
            ThisAgent.SetDestination(PlayerTransform.position);
            //Wait until path is computed
            while (ThisAgent.pathPending)
                yield return null;
            //Has player run away?
            if (ThisAgent.remainingDistance > ThisAgent.stoppingDistance)
            {
                //Change back to chase
                CurrentState = ENEMY_STATE.CHASE;
                yield break;
            }
            else
            {
                //Attack
                PlayerHealth.HealthPoints -= MaxDamage * Time.deltaTime;
            } // Wait until next frame
            yield return null;
        }
        yield break;

    }
}
