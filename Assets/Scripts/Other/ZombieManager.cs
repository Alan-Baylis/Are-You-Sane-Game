using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class ZombieManager : MonoBehaviour
{
    public GameObject zombieClusterPrefab;
    public GameObject zombieSingleton;
    public GameObject zombieInfest;

    private const int corridorChanceAmount = 2;
    private static int _corridorLowest = 12;
    private static int _corridorLow = 23;
    private static int _corridorHigh = 40;
    private static int _corridorHighest = 62;

    private static int[] corridorCountBounds = new int[4] { _corridorLowest, _corridorLow, _corridorHigh, _corridorHighest };
    private static List<GameObject> zombies = new List<GameObject>();

    // Used for spawning just above the ground of the floor
    private Vector3 offSetY = new Vector3(0f, 1.5f, 0f);

    // Our Array of inital predicates which will categorise this floor's corridor length
    private Predicate<int>[] countMatch = new Predicate<int>[5]
    {
            x => x < _corridorLowest,
            x => x >= _corridorLowest && x < _corridorLow,
            x => x >= _corridorLow && x < _corridorHigh,
            x => x >= _corridorHigh && x < _corridorHighest,
            x => x >= _corridorHighest
    };

    

    // Save and Store the nodes we spawn on so we can make Objects and models avoid those particular nodes
    public List<GameObject> usedSpawns = new List<GameObject>();

    public static void DestroyAllZombies()
    {
        while (zombies.Count != 0)
        {
            GameObject z = zombies[0];
            zombies.RemoveAt(0);
            Destroy(z);
            
        }

        //zombies.Clear();
    }

	// Use this for initialization
	void Start ()
    {
	
	}
	
	// Update is called once per frame
	void Update ()
    {
	
	}

    

    public void SpawnZombies(List<List<GameObject>>[] rooms)
    {
        
        //List<GameObject>[] routesAll = GetComponent<BuildingGeneration>().BuildingRoutes;

        //// For all rooms on all floors
        //for (int y = 0; y < rooms.Length; y++)
        //{

        //    // SPAWN ZOMBIES IN ROOMS ---------------------------------------------------------------------------------------------------------

        //    // For each room on this Floor
        //    for (int roomIndex = 0; roomIndex < rooms[y].Count; roomIndex++)
        //    {
        //        List<GameObject> avaliableRoomNodes = rooms[y][roomIndex];

        //        // If the room is large enough then we can potentially have a chance of spawning an Infested Room
        //        if (rooms[y][roomIndex].Count > 10)
        //        {
        //            //// This is our 40% chance to Infest the room, not the chance to spawn zombies
        //            //int _chanceInfest40 = UnityEngine.Random.Range(0, 10);
        //            //if (_chanceInfest40 == 1 || _chanceInfest40 == 3 || _chanceInfest40 == 8 || _chanceInfest40 == 9)
        //            //{
        //            //    // 20% - 50% of the room will be infest so the player cannot know immediately
        //            //    int lowestCount = Mathf.RoundToInt(rooms[y][roomIndex].Count * 0.2f);
        //            //    int upperCount = Mathf.RoundToInt(rooms[y][roomIndex].Count * 0.5f);
        //            //    int clusterCount = UnityEngine.Random.Range(lowestCount, upperCount + 1);

        //            //    for (int z = 0; z < clusterCount; z++)
        //            //    {
        //            //        int randomNode = UnityEngine.Random.Range(0, avaliableRoomNodes.Count);
        //            //        GameObject zGroup = GameObject.Instantiate(zombieClusterPrefab, avaliableRoomNodes[randomNode].transform.position, Quaternion.identity) as GameObject;
        //            //        usedSpawns.Add(avaliableRoomNodes[randomNode].gameObject);
        //            //        avaliableRoomNodes.Remove(avaliableRoomNodes[randomNode]);
        //            //        zombies.Add(zGroup);
        //            //    }

        //            //    break;
        //            //}

        //        }


        //        //// All rooms will at least have 1 block, therefore we dont need the List Count check here
        //        //// 20% chance to spawn Cluster Zombies in room
        //        //int _chanceClusterRoom30 = UnityEngine.Random.Range(0, 10);
        //        //if (_chanceClusterRoom30 == 1 || _chanceClusterRoom30 == 6 || _chanceClusterRoom30 == 4)
        //        //{
        //        //    int randomNode = UnityEngine.Random.Range(0, avaliableRoomNodes.Count);

        //        //    GameObject zGroup = GameObject.Instantiate(zombieClusterPrefab, avaliableRoomNodes[randomNode].transform.position, Quaternion.identity) as GameObject;
        //        //    usedSpawns.Add(avaliableRoomNodes[randomNode].gameObject);
        //        //    avaliableRoomNodes.Remove(avaliableRoomNodes[randomNode]);
        //        //    zombies.Add(zGroup);

        //        //}




        //    } // ---------------------------------------------------------------------------------------------------------------------------------


        //    Debug.Log("Floor [" + y + "] Route Blocks : " + routesAll[y].Count);
        //    List<GameObject> avaliableRouteBlocks = routesAll[y];
        //    List<BlockPiece> avaliableRouteNodes = new List<BlockPiece>();

        //    foreach (GameObject block in avaliableRouteBlocks)
        //    {
        //        BlockPiece node = block.GetComponent<BlockPiece>();
        //        if (!node.stairNode && !node.mainEntranceNode)
        //        {
        //            avaliableRouteNodes.Add(node);
        //        }
        //    }


        //    for (int predicateIndex = 0; predicateIndex < countMatch.Length; predicateIndex++)
        //    {
        //        // Predicate index determinds how many zombies have the chance to spawn
        //        if (countMatch[predicateIndex].Invoke(routesAll[y].Count))
        //        {
        //            // FOREACH ZOMBIE - that has a chance to spawn
        //            for (int spawnNumber = 0; spawnNumber < predicateIndex + 1; spawnNumber++)
        //            {
        //                // Make a list of numbers to pick from which correspond to the random Range
        //                List<int> indexList = Enumerable.Range(0, 9).ToList();
                        
        //                // Make an array of predicates for the zombie
        //                Predicate<int>[] chanceMatches = new Predicate<int>[(spawnNumber + corridorChanceAmount)];

        //                // For each spawn we spawn, the chance will increase that at least ONE will spawn
        //                for (int boolIndex = 0; boolIndex < chanceMatches.Length; boolIndex++)
        //                {
        //                    // Make the match equal to a number randomly picked from the list of unpicked matches
        //                    int randomIndex = UnityEngine.Random.Range(0, indexList.Count);

        //                    // Store a local copy otherwise indexing will go out of range due to removal
        //                    int randomNumber = indexList[randomIndex];

        //                    // Assign the predicate to the match condition
        //                    chanceMatches[boolIndex] = x => x == randomNumber;

        //                    // Remove the match number from the list so it cannot be picked again for this same Zombie
        //                    indexList.Remove(randomNumber);
                            
        //                }

        //                // Pick the number to match any of the predicates
        //                int _chanceSpawn = UnityEngine.Random.Range(0, 10);

        //                for (int matchIndex = 0; matchIndex < chanceMatches.Length; matchIndex++)
        //                {
        //                    // If any of our matches have been found - operating OR for booleans in the array of predicates
        //                    if (chanceMatches[matchIndex].Invoke(_chanceSpawn))
        //                    {
        //                        // Spawn an Idle Zombie at a random Location that is not already taking in the corridor(s) of this floor
        //                        int randomNode = UnityEngine.Random.Range(0, avaliableRouteNodes.Count);

        //                        GameObject zIdle = GameObject.Instantiate(zombieSingleton, avaliableRouteNodes[randomNode].transform.position + offSetY, Quaternion.identity) as GameObject;
        //                        zIdle.GetComponent<ZombieTracker>().SetOnNode(avaliableRouteNodes[randomNode].gameObject);
        //                        zIdle.GetComponent<ZombieTracker>().ConfigurePatrolBlocks(routesAll[y]);
                                
                                

        //                        usedSpawns.Add(avaliableRouteNodes[randomNode].gameObject);
        //                        avaliableRouteNodes.Remove(avaliableRouteNodes[randomNode]);
        //                        zombies.Add(zIdle);

        //                        zIdle.GetComponent<ZombieTracker>().OccludeTrackingToggle();
        //                        zIdle.GetComponent<ZombieTracker>().SwitchToPatrol();
        //                        break;
                                
        //                    }
                            
        //                }

        //            }

        //        }

        //    }

        //}


        //foreach (GameObject zombie in zombies)
        //{
        //    zombie.GetComponent<ZombieTracker>().SwitchToPatrol();
        //}
    }

}
