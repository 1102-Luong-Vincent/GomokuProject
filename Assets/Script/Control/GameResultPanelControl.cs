using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public static class ResultPanelConstants
{
    public const string GameTitle = "Formation 5";
    public const string DrawTitle = "Draw";
    public const string LoseTitle = "You Lose";
    public const string WinTitle = "You Win";

}


public class GameResultPanelControl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static GameResultPanelControl Instance;

    [SerializeField] GameObject GameResultPanelTitle;
    [SerializeField] GameObject GameResultWinTitle;
    [SerializeField] GameObject GameResultLoseTitle;
    [SerializeField] GameObject GameResultDrawTitle;


    [SerializeField] Image LevelImage;


    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject gameUI;

    [SerializeField] Button RandomStartButton;
    [SerializeField] Button BlackStartButton;
    [SerializeField] Button WhiteStartButton;
    [SerializeField] Button ExitButton;
    [SerializeField] Button BackButton;

    [SerializeField] Button LevelsButton;
    [SerializeField] Button StartButton;
    [SerializeField] Button EasyButton;
    [SerializeField] Button MediumButton;
    [SerializeField] Button HardButton;


    private void Awake()
    {
        if (Instance == null) Instance = this;

        InitButton();
        EasyButton.gameObject.SetActive(false);
        MediumButton.gameObject.SetActive(false);
        HardButton.gameObject.SetActive(false);
        RandomStartButton.gameObject.SetActive(false);
        BlackStartButton.gameObject.SetActive(false);
        WhiteStartButton.gameObject.SetActive(false);
        BackButton.gameObject.SetActive(false);

        gamePanel.SetActive(true);
        gameUI.SetActive(false);
    }

    void Start()
    {
        SetLevel(GomokuConstants.EasyLevelDepth);
    }

    #region Button

    void InitButton()
    {
        StartButton.onClick.AddListener(OnStartButtonClick);
        RandomStartButton.onClick.AddListener(OnRandomStartButtonClick);
        BlackStartButton.onClick.AddListener(OnBlackStartButtonClick);
        WhiteStartButton.onClick.AddListener(OnWhiteStartButtonClick);
        EasyButton.onClick.AddListener(() => SetLevel(GomokuConstants.EasyLevelDepth));
        MediumButton.onClick.AddListener(() => SetLevel(GomokuConstants.MediumLevelDepth));
        HardButton.onClick.AddListener(() => SetLevel(GomokuConstants.HardLevelDepth));
        LevelsButton.onClick.AddListener(OnLevelsButtonClick);
        ExitButton.onClick.AddListener(OnExitButtonClick);
        BackButton.onClick.AddListener(OnBackButtonClick);

    }

     public void SetLevel(int level)
    {
        GomokuManager.Instance.SetCurrentLevel(level);
        LevelImage.sprite = UIManager.Instance.GetLevelNumSprite(level);
    }

    void OnStartButtonClick()
    {

        StartButton.gameObject.SetActive(false);
        LevelsButton.gameObject.SetActive(false);

        RandomStartButton.gameObject.SetActive(true);
        BlackStartButton.gameObject.SetActive(true);
        WhiteStartButton.gameObject.SetActive(true);
        BackButton.gameObject.SetActive(true);
    }
    void OnRandomStartButtonClick()
    {
        bool isBlackFirst = (Random.value > 0.5f); 
        GameManager.Instance.StartGame(isBlackFirst);
        StartButton.gameObject.SetActive(false);
    }

    void OnBlackStartButtonClick()
    {
        GameManager.Instance.StartGame(true);
        StartButton.gameObject.SetActive(false);
    }

    void OnWhiteStartButtonClick()
    {
        GameManager.Instance.StartGame(false);
        StartButton.gameObject.SetActive(false);
    }

    void OnLevelsButtonClick()
    {
        LevelsButton.gameObject.SetActive(false);
        StartButton.gameObject.SetActive(false);

        EasyButton.gameObject.SetActive(true);
        MediumButton.gameObject.SetActive(true);
        HardButton.gameObject.SetActive(true);
        BackButton.gameObject.SetActive(true);
    }
    void OnExitButtonClick()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void OnBackButtonClick()
    {
        RandomStartButton.gameObject.SetActive(false);
        BlackStartButton.gameObject.SetActive(false);
        WhiteStartButton.gameObject.SetActive(false);

        EasyButton.gameObject.SetActive(false);
        MediumButton.gameObject.SetActive(false);
        HardButton.gameObject.SetActive(false);

        BackButton.gameObject.SetActive(false);

        StartButton.gameObject.SetActive(true);
        LevelsButton.gameObject.SetActive(true);
    }

    #endregion

    #region Show Panel

    void ShowPanel()
    {
        gameObject.SetActive(true);
    }

    public void ShowStart()
    {
        GameResultPanelTitle.SetActive(true);
        GameResultWinTitle.SetActive(false);
        GameResultLoseTitle.SetActive(false);
        GameResultDrawTitle.SetActive(false);

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
        GameResultPanelTitle.SetActive(false);
        GameResultWinTitle.SetActive(false);
        GameResultLoseTitle.SetActive(false);
        GameResultDrawTitle.SetActive(true);
    }



    void ShowLoseResult()
    {
        GameResultPanelTitle.SetActive(false);
        GameResultWinTitle.SetActive(false);
        GameResultDrawTitle.SetActive(false);
        GameResultLoseTitle.SetActive(true);
    }

    void ShowWinResult()
    {
        GameResultPanelTitle.SetActive(false);
        GameResultWinTitle.SetActive(true);
        GameResultDrawTitle.SetActive(false);
        GameResultLoseTitle.SetActive(false);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        
    }
}
