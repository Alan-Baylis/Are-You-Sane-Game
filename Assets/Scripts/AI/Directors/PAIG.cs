using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

// Predisposition of AI Generation / AI Director
public class PAIG : MonoBehaviour
{
    public static PAIG AIDirector;

    public int minSound = 10;
    public int maxSound = 50;

    public bool AIHumanTesting;

    private float soundTimer = 10f;
    private const string m_stairTag = "Stairs";
    private const string m_wallTag = "Wall";
    private PlayerHeuristics m_player;
    private BuildingGeneration buildingGen;
    private ZombieManager zombieGen;
    private AIResources resources;
    public SoundResources sounds;
    private Pathfinder m_Pathfinder;

    private bool annieSpawned = false;
    public LAObject AnnieObject;


    private const float MAX_MOVE_FLOOR_TIME = 15f;
    private float m_moveFloorTimer = MAX_MOVE_FLOOR_TIME;


    

    // Use this for initialization
    void Start ()
    {
        resources = GetComponent<AIResources>();
        m_player = GameObject.FindGameObjectWithTag(GameTag.Player).GetComponent<PlayerHeuristics>();
        buildingGen = GameObject.Find("BuildingNode").GetComponent<BuildingGeneration>();
        zombieGen = buildingGen.GetComponent<ZombieManager>();
        m_Pathfinder = GetComponent<Pathfinder>();

        AIDirector = this;
    }



    private BlockPiece closestNode = null;
    private float playerDistance = 500f;
    private bool lastAbove = true;
    private float evaluatedMultiplier = 0.0f;

    GameObject m_ClosestDeltaFloorNode = null;
    float m_ClosestDeltaNodeDistance = 0.0f;


    public void MovementTriggerCallBack()
    {
        if (annieSpawned)
        {
            float deltaY = Mathf.Abs(m_player.CurrentFloor - AnnieObject.Movement.currentFloor);

            // If the player is within one floor of the AI and there has been a change in position
            if (deltaY <= 1)
            {
                if (m_player.CurrentFloor > AnnieObject.Movement.currentFloor)
                {
                    // Get the closest node to the connecting floor
                    m_ClosestDeltaNodeDistance = 1000f;
                    foreach (BlockPiece node in buildingGen.Floors[m_player.CurrentFloor].doorBlocks)
                    {
                        float d = Vector3.Distance(node.transform.position, m_player.BlockPosition.transform.position);
                        if (d < m_ClosestDeltaNodeDistance)
                        {
                            m_ClosestDeltaNodeDistance = d;
                            m_ClosestDeltaFloorNode = node.gameObject;
                        }
                    }

                    m_Pathfinder.SetOnNode(m_player.BlockPosition);
                    if (m_Pathfinder.GetPathFullTraversal(m_ClosestDeltaFloorNode, true))
                    {
                        float nearestF = m_ClosestDeltaFloorNode.GetComponent<BlockPiece>().f;
                        AnnieObject.Audio.DirectorSetVolume(nearestF);
                    }
                    else
                    {
                        if (m_Pathfinder.GetPath(m_ClosestDeltaFloorNode, x => true))
                        {
                            float nearestF = m_ClosestDeltaFloorNode.GetComponent<BlockPiece>().f;
                            AnnieObject.Audio.DirectorSetVolume(nearestF);
                        }
                    }
                }
                else if (m_player.CurrentFloor < AnnieObject.Movement.currentFloor)
                {
                    // Get the closest node to the connecting floor
                    m_ClosestDeltaNodeDistance = 1000f;
                    foreach (BlockPiece node in buildingGen.Floors[m_player.CurrentFloor].stairBlocks)
                    {
                        float d = Vector3.Distance(node.transform.position, m_player.BlockPosition.transform.position);
                        if (d < m_ClosestDeltaNodeDistance)
                        {
                            m_ClosestDeltaNodeDistance = d;
                            m_ClosestDeltaFloorNode = node.ParentPath;
                        }
                    }

                    m_Pathfinder.SetOnNode(m_player.BlockPosition);
                    if (m_Pathfinder.GetPathFullTraversal(m_ClosestDeltaFloorNode, true))
                    {
                        float nearestF = m_ClosestDeltaFloorNode.GetComponent<BlockPiece>().f;
                        AnnieObject.Audio.DirectorSetVolume(nearestF);
                    }
                    else
                    {
                        if (m_Pathfinder.GetPath(m_ClosestDeltaFloorNode, x => true))
                        {
                            float nearestF = m_ClosestDeltaFloorNode.GetComponent<BlockPiece>().f;
                            AnnieObject.Audio.DirectorSetVolume(nearestF);
                        }
                    }
                }
            }
        }
       
    }

    // Update is called once per frame
    void Update ()
    {
        //if (Input.GetKeyDown(KeyCode.G))
        //{
        //    if (buildingGen != null && h_player != null)
        //    {
        //        buildingGen.RegenerateBuilding(true);
        //        MoveToRandomNode(h_player.gameObject);
        //        StartCoroutine(EvaluatePlayerLevel());
        //        Debug.LogWarning("Regenerated Floors!");
        //    }
        //}
        if (annieSpawned)
        {
            // Director Setting volume for player audio filtering

            float deltaY = Mathf.Abs(m_player.transform.position.y - AnnieObject.transform.position.y);
            

            

        }



        if (AIHumanTesting)
        {
            if (Input.GetKeyDown(KeyCode.M) && !annieSpawned)
            {
                BlockPiece node = buildingGen.Floors[1].routeBlocks[3];
                AnnieObject.Activate(m_player.gameObject, node, buildingGen);
                annieSpawned = true;
            }

            if (Input.GetKeyDown(KeyCode.Minus))
            {
                //Get Annie to traverse down a floor
                List<BlockPiece> corridorNodesBelow = buildingGen.Floors[AnnieObject.Movement.currentFloor - 1].routeBlocks.FindAll(n => !n.isStairNode);

                if (corridorNodesBelow.Count != 0)
                {
                    BlockPiece randomNode = corridorNodesBelow[UnityEngine.Random.Range(0, corridorNodesBelow.Count)];
                    Debug.Log("Annie Current Node: Floor[" + AnnieObject.Movement.currentFloor + "] Node (" + AnnieObject.Movement.currentNodePosition.GetX() + ", " + AnnieObject.Movement.currentNodePosition.GetZ() + ")");
                    Debug.Log("Below Move Desintation for Annie: Floor [" + randomNode.GetY() + "] Node (" + randomNode.GetX() + ", " + randomNode.GetZ() + ")");

                    AnnieObject.Movement.SelectMovementPath(randomNode); // This should be alright?
                }
            }

            if (Input.GetKeyDown(KeyCode.Equals))
            {
                //Get annie to travese up a floor
                //Get Annie to traverse down a floor
                List<BlockPiece> corridorNodesAbove = buildingGen.Floors[AnnieObject.Movement.currentFloor + 1].routeBlocks.FindAll(n => !n.isStairNode);

                if (corridorNodesAbove.Count != 0)
                {
                    BlockPiece randomNode = corridorNodesAbove[UnityEngine.Random.Range(0, corridorNodesAbove.Count)];
                    Debug.Log("Annie Current Node: Floor[" + AnnieObject.Movement.currentFloor + "] Node (" + AnnieObject.Movement.currentNodePosition.GetX() + ", " + AnnieObject.Movement.currentNodePosition.GetZ() + ")");
                    Debug.Log("Below Above Desintation for Annie: Floor [" + randomNode.GetY() + "] Node (" + randomNode.GetX() + ", " + randomNode.GetZ() + ")");

                    AnnieObject.Movement.SelectMovementPath(randomNode); // This should be alright?
                }
            }

            //if (Input.GetKeyDown(KeyCode.Alpha1))
            //{
            //    BlockStairsBelowPlayer(true);
            //}

            //if (Input.GetKeyDown(KeyCode.Alpha2))
            //{
            //    BlockStairsAbovePlayer(true);
            //}

            //if (Input.GetKeyDown(KeyCode.Alpha9))
            //{
            //    BlockStairsBelowPlayer(false);
            //}

            //if (Input.GetKeyDown(KeyCode.Alpha0))
            //{
            //    BlockStairsAbovePlayer(false);
            //}

            //if (Input.GetKeyDown(KeyCode.B))
            //{
            //    foreach (BlockPiece node in buildingGen.floorBlocks[h_player.CurrentFloor].floorBlocks)
            //    {
            //        if (node.isCorridor && node.lightBeam != null)
            //        {
            //            node.lightBeam.ToggleBlinking();
            //        }
            //    }
            //}
        }
        


        //EvaluateRandomSounds();

        //if (AnnieObject.Active)
        //{
        //    if (AnnieObject.Movement.currentFloor != h_player.CurrentFloor)
        //    {
        //        if (m_moveFloorTimer > 0)
        //        {
        //            m_moveFloorTimer -= Time.deltaTime;
        //        }
        //        else
        //        {
        //            MoveAnnieToPlayerFloor();
        //            m_moveFloorTimer = MAX_MOVE_FLOOR_TIME;
        //        }
        //    }
        //    else
        //    {
        //        if (m_moveFloorTimer != MAX_MOVE_FLOOR_TIME)
        //            m_moveFloorTimer = MAX_MOVE_FLOOR_TIME;
        //    }            
        //}

    }


    private void MoveAnnieToPlayerFloor()
    {
        if (!m_player.BlockPosition.isStairNode && m_player.BlockPosition.isCorridor)
        {
            Debug.Log("Moving Annie To Player Node now");
            AnnieObject.Movement.SelectMovementPath(m_player.BlockPosition); // This should be alright?
        }
        else
        {
            List<BlockPiece> corridorNodes = buildingGen.Floors[m_player.CurrentFloor].routeBlocks.FindAll(n => !n.isStairNode);
            if (corridorNodes.Count != 0)
            {
                BlockPiece randomNode = corridorNodes[UnityEngine.Random.Range(0, corridorNodes.Count)];
                Debug.Log("Annie Current Node: Floor[" + AnnieObject.Movement.currentFloor + "] Node (" + AnnieObject.Movement.currentNodePosition.GetX() + ", " + AnnieObject.Movement.currentNodePosition.GetZ() + ")");
                Debug.Log("Below Above Desintation for Annie: Floor [" + randomNode.GetY() + "] Node (" + randomNode.GetX() + ", " + randomNode.GetZ() + ")");
                AnnieObject.Movement.SelectMovementPath(randomNode); // This should be alright?
            }
        }
        
    }

    private IEnumerator EvaluatePlayerLevel()
    {
        yield return new WaitForSeconds(1f);
        //BlockStairsBelowPlayer(h_player.CurrentFloor);

        // True means wall ARE active
        if (m_player.CurrentFloor > buildingGen.Height / 2)
        {
            Debug.Log("Blocked Below");
            BlockStairsBelowPlayer(true);
        }
        else
        {
            Debug.Log("Blocked Above");
            BlockStairsAbovePlayer(true);
        }


    }

    private void MoveToRandomNode(GameObject obj)
    {
        BlockPiece randN = buildingGen.GetRandomNode();
        Debug.Log("Spawned at Floor [" + randN.GetY() + "] : " + randN);
        obj.transform.position = randN.transform.position + new Vector3(0f, 1.3f, 0f);
    }

    private void ConfigureStairWalls(int y, bool active)
    {
        for (int s = 0; s < buildingGen.Floors[y].stairBlocks.Count; s++)
        {
            BlockPiece block = buildingGen.Floors[y].stairBlocks[s];
            foreach (Transform child in block.transform)
            {
                if (child.tag == m_stairTag)
                {
                    MyHelper.SetActiveChildrenWithTag(child.gameObject, m_wallTag, active);
                }
            }

            block.GetComponent<BlockPiece>().isStairBlocked = active;
        }
    }

    private void BlockStairsBelowPlayer(bool active)
    {
        if (m_player.CurrentFloor != 0)
        {
            for (int y = 0; y < m_player.CurrentFloor; y++)
            {
                if (active)
                {
                    Debug.Log("Blocked Floor " + y);
                }
                else
                {
                    Debug.Log("Free Floor " + y);
                }

                ConfigureStairWalls(y, active);
            }
        }
    }

    private void BlockStairsAbovePlayer(bool active)
    {
        for (int y = m_player.CurrentFloor; y < buildingGen.Height - 1; y++)
        {
            if (active)
            {
                Debug.Log("Blocked Floor " + y);
            }
            else
            {
                Debug.Log("Free Floor " + y);
            }

            ConfigureStairWalls(y, active);
        }
    }









}
