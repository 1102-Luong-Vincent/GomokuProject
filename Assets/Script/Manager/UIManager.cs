using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static UIManager Instance;
    [SerializeField] GameUIControl gameUIControl;
    [SerializeField] GameResultPanelControl gameResultPanelControl;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void EnterGame()
    {
        gameResultPanelControl.ShowStart();
        gameUIControl.ClosePanel();
    }


    public void StartGame()
    {
        gameResultPanelControl.ClosePanel();
        gameUIControl.ShowPanel();
    }

    public void EndGame(GomoKuType result)
    {
        gameUIControl.ClosePanel();
        gameResultPanelControl.ShowResult(result);
    }

}
