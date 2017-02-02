using UnityEngine;
using System.Collections;

public class SceneGeneration : MonoBehaviour
{
    private PlayerHeuristics h_player;
    private BuildingGeneration buildingGen;


    // -------------- TEST VARIABLES --------------------
    //public GameObject stairPiece;

	// Use this for initialization
	void Start ()
    {
        GameData.SetDifficulty(GameDifficulty.Psychotic);

        h_player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHeuristics>();
        buildingGen = GameObject.Find("BuildingNode").GetComponent<BuildingGeneration>();
        GenerateScene();
        //TestStuff();
	}
	
	// Update is called once per frame
	void Update ()
    {
	    
	}

    private void TestStuff()
    {

    }

    private void GenerateScene()
    {
        buildingGen.GenerateBuilding();

        if (!buildingGen.ColorTesting)
        {
            buildingGen.EnableBuildingOcculsion();
            buildingGen.MovePlayerToStart();
        }
        else
        {
            h_player.gameObject.SetActive(false);
        }
        

    }
}
