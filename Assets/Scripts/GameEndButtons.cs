using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameEndButtons : MonoBehaviour
{
    private Color32 redColor = Color.red;
    private Color32 whiteColor = Color.white;
    private Color32 greenColor = Color.green;
    private Color32 greyColor = new Color32(50, 50, 50, 255);
	
    public void EnterMouse()
    {
        GetComponent<Text>().color = redColor;
    }

    public void ExitMouse()
    {
        GetComponent<Text>().color = greyColor;
    }

    public void MenuLoad()
    {
        //Application.LoadLevel("TestMenu");
        SceneManager.LoadScene("TestMenu");
    }

    public void SceneReload()
    {
        //Application.LoadLevel("TestScene");
        SceneManager.LoadScene("TestScene");
    }

    public void ExitGame()
    {
        Application.Quit();
    }

}
