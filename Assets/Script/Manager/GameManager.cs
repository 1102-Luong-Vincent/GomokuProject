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
        GomokuManager.Instance.StartGame(isPlayerBlack);
        UIManager.Instance.StartGame();
        PotentialFieldsManager.Instance.ClearCurrentItems();
        isPlaying = true;
    }


    public bool IsPlayingGame()
    {
        return isPlaying;
    }

    public void EndGame(GomoKuType result)
    {
        isPlaying = false;
        UIManager.Instance.EndGame(result);
    }
}
