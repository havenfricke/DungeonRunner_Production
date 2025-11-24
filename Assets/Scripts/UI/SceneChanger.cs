using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void GameScene()
    {
        SceneManager.LoadScene("DemoLevel");
        Debug.Log("Button Pressed");
    }
    public void ControlScene()
    {
        SceneManager.LoadScene("ControlsScene");
    }
        public void StartScene()
    {
        SceneManager.LoadScene("StartScreen");
    }
}
