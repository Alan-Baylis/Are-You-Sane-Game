using UnityEngine;
using System.Collections.Generic;

public class VisitFloorTesting : MonoBehaviour
{
    public bool activeTesting;
    public bool enableOcclusion;
    public BuildingGeneration building;
    public PlayerHeuristics player;

    private List<int> visitedFloors = new List<int>();

    public void CheckResetList()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            visitedFloors.Clear();
        }
    }

    private KeyCode[] keyCodes =
    {
        KeyCode.Alpha0,
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9
    };

    private KeyCode[] onCodes =
    {
        KeyCode.F10,
        KeyCode.F1,
        KeyCode.F2,
        KeyCode.F3,
        KeyCode.F4,
        KeyCode.F5,
        KeyCode.F6,
        KeyCode.F7,
        KeyCode.F8,
        KeyCode.F9
    };

	// Use this for initialization
	void Start ()
    {
	    if (activeTesting)
        {
            if(building == null || player == null)
            {
                Debug.LogError("Must attached Player and Building to Test for Floor Regeneration");
                activeTesting = false;
            }
        }
	}

    void CheckOnFloorKeys()
    {
        for (int i = 0; i < onCodes.Length; i++)
        {
            if (Input.GetKeyDown(onCodes[i]))
            {
                if (player.CurrentFloor != i)
                {
                    player.CurrentFloor = i;

                    if (!building.floorBlocks[i].isVisited)
                    {
                        visitedFloors.Add(i);
                        building.floorBlocks[i].isVisited = true;
                    }

                    Debug.Log("Player is now on Floor " + i);  
                }
                else
                {
                    Debug.Log("Player is already on floor " + i);
                }
            }
        }

        
    }

    void CheckVisitKeys()
    {
        for (int i = 0; i < keyCodes.Length; i++)
        {
            if (Input.GetKeyDown(keyCodes[i]))
            {
                if (building.floorBlocks[i].isVisited)
                {
                    Debug.Log("You have already visited this floor");
                }
                else
                {
                    Debug.Log("Floor " + i + " has now been visited");
                    building.floorBlocks[i].isVisited = true;
                    visitedFloors.Add(i);
                }
            }
        }
    }

    void CheckForRecap()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RecapScenario();
        }
    }

    void RecapScenario()
    {
        if (visitedFloors.Count != 0)
        {
            visitedFloors.Sort();
            string debugMsg = "Player is on floor " + player.CurrentFloor + ". Floors visited are:";
            foreach (int floor in visitedFloors)
            {
                debugMsg += " [" + floor + "]";
            }

            Debug.Log(debugMsg);
        }
        else
        {
            Debug.Log("No Floors have been visited yet");
        }
    }

    void CheckRegenButton()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            building.RegenerateVisitedFloors(player.CurrentFloor);
            if (enableOcclusion)
                building.EnableBuildingOcculsion();

            visitedFloors.Clear();
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (activeTesting)
        {
            //CheckResetList();
            CheckVisitKeys();
            //CheckOnFloorKeys();
            CheckForRecap();
            CheckRegenButton();

            //if (Input.GetKeyDown(KeyCode.L))
            //{
            //    Debug.Log("Last Floor Visited");
            //    building.floorBlocks[11].isVisited = true;
            //    visitedFloors.Add(11);
            //}
        }    
    }
}
