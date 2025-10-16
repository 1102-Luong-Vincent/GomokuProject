using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ResultPanelConstants
{
    public const string GameTitle = "Gomoku Game";
    public const string DrawTitle = "Draw";
    public const string LoseTitle = "You Lose";
    public const string WinTitle = "You Win";

}


public class GameResultPanelControl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] TextMeshProUGUI GameResultPanelTitle;

    [SerializeField] Button RandomStartButton;
    [SerializeField] Button BlackStartButton;
    [SerializeField] Button WhiteStartButton;
    [SerializeField] Button ExitButton;


    private void Awake()
    {
        InitButton();
    }


    #region Button

    void InitButton()
    {
        RandomStartButton.onClick.AddListener(OnRandomStartButtonClick);
        BlackStartButton.onClick.AddListener(OnBlackStartButtonClick);
        WhiteStartButton.onClick.AddListener(OnWhiteStartButtonClick);
        ExitButton.onClick.AddListener(OnExitButtonClick);

    }

    void OnRandomStartButtonClick()
    {
        bool isBlackFirst = (Random.value > 0.5f); 
        GameManager.Instance.StartGame(isBlackFirst);
    }

    void OnBlackStartButtonClick()
    {
       GameManager.Instance.StartGame(true);
    }

    void OnWhiteStartButtonClick()
    {
        GameManager.Instance.StartGame(false);
    }

    void OnExitButtonClick()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    #endregion

    #region Show Panel

    void ShowPanel()
    {
        gameObject.SetActive(true);
    }

    public void ShowStart()
    {
        GameResultPanelTitle.text = ResultPanelConstants.GameTitle;
        ShowPanel();
    }


    public void ShowResult(GomoKuType result)
    {
        if (result == GomoKuType.Draw)
        {
            ShowDrawResult();
        }else if (GomokuManager.Instance.IsPlayerWin(result))
        {
            ShowWinResult();
        }
        else
        {
            ShowLoseResult();
        }
        ShowPanel();
    }

    void ShowDrawResult()
    {
        GameResultPanelTitle.text = ResultPanelConstants.DrawTitle;
    }



    void ShowLoseResult()
    {
        GameResultPanelTitle.text = ResultPanelConstants.LoseTitle;
    }

    void ShowWinResult()
    {
        GameResultPanelTitle.text = ResultPanelConstants.WinTitle;
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    #endregion

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
