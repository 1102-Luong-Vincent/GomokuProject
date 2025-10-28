using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    bool isPlaying = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        EnterGame();   
    }

    void EnterGame()
    {
        UIManager.Instance.EnterGame();
    }

    public void StartGame(bool isPlayerBlack)
    {
        isPlaying = true;
        GomokuManager.Instance.StartGame(isPlayerBlack);
        UIManager.Instance.StartGame();

    }


    public bool IsPlayingGame()
    {
        return isPlaying;
    }

    public void EndGame(GomoKuType result)
    {
        isPlaying = false;
        GomokuManager.Instance.ForceStopAllMovements();
        UIManager.Instance.EndGame(result);
    }
}
