using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject gameOverUI;

    void Awake()
    {
        Instance = this;
    }

    public void GameOver()
{
    Debug.Log("GameOver() called");

    gameOverUI.SetActive(true);

    Time.timeScale = 0f;
}
}