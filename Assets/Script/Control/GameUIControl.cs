using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameUIControl : MonoBehaviour
{
    public static GameUIControl Instance;
    [SerializeField] GameObject AITurnGameObject;
    [SerializeField] Image AIurnTens;
    [SerializeField] Image AIurnDigits;
    [SerializeField] GameObject PlayerTurnGameObject;
    [SerializeField] Image PlayerTurnTens;
    [SerializeField] Image PlayerTurnDigits;


    [SerializeField] Button LevelButton;


    [SerializeField] Button ShowGirdButton;
    [SerializeField] Button ShowNumberButton;
    [SerializeField] Button ResignButton;
    [SerializeField] Button ResetButton;

    [SerializeField] Image MoveSpeedImage;
    [SerializeField] Slider MoveSpeedSlider;
    [SerializeField] Button PlusButton;
    [SerializeField] Button MinusButton;
    private int currentSpeedLevel = 0;
    private readonly float[] sliderValues = { 0f, 0.25f, 0.5f, 0.75f, 1.1f };
    private readonly float[] speedLevels = { 0.05f, 0.1f, 0.25f, 0.5f, 1f }; 

    private float moveSpeed = 1f;


    private bool isShowUI = true;
    [SerializeField] GameObject RightDownCorner;
    [SerializeField] GameObject LeftDownCorner;
    [SerializeField] GameObject RightUpCorner;
    [SerializeField] GameObject LeftUpCorner;

    [SerializeField] TextMeshProUGUI SongTitle;
    [SerializeField] Button NextSongButton;
    [SerializeField] Button PreviousSongButton;


    private void Awake()
    {
        if (Instance == null) Instance = this;

        InitButtons();
        //InitSlider();
    }


    void Start()
    {
        GomokuManager.Instance.GetGomokuData().OnGomokuDataChanged += UpdateTurnText;
        UpdateTurnText();
        SetSpeed((int)MoveSpeedSlider.value);
        SetCornerUIActive(true);

    }


    //void InitSlider()
    //{
    //    MoveSpeedSlider.minValue = 0;
    //    MoveSpeedSlider.maxValue = speedLevels.Length - 1;
    //    MoveSpeedSlider.wholeNumbers = true; 
    //    MoveSpeedSlider.value = currentSpeedLevel;
    //}



    void SetSpeed(int levelIndex)
    {
        if (levelIndex > speedLevels.Length - 1 || levelIndex < 0) return;
        currentSpeedLevel = levelIndex;
        moveSpeed = speedLevels[levelIndex];
        MoveSpeedSlider.value = sliderValues[levelIndex];
        MoveSpeedImage.sprite = UIManager.Instance.GetLevelNumSprite(levelIndex);
    }

    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    #region Buttons
    void InitButtons()
    {

        LevelButton.onClick.AddListener(OnLevelButtonClick);

        ShowNumberButton.onClick.AddListener(OnShowNumberClick);
        ResignButton.onClick.AddListener(OnResignButtonClick);
        ResetButton.onClick.AddListener(OnRestButtonClick);

        PlusButton.onClick.AddListener(() => SetSpeed(currentSpeedLevel+1));
        MinusButton.onClick.AddListener(() => SetSpeed(currentSpeedLevel - 1));

        NextSongButton.onClick.AddListener(OnNextSongButtonClick);
        PreviousSongButton.onClick.AddListener(OnPreviousSongButtonClick);
        ShowGirdButton.onClick.AddListener(OnShowGirdButtonClick);

    }

    private void OnLevelButtonClick()
    {
        switch (GomokuManager.Instance.currentLevel)
        {
            case GomokuConstants.EasyLevelDepth: GameResultPanelControl.Instance.SetLevel(GomokuConstants.MediumLevelDepth); break;
            case GomokuConstants.MediumLevelDepth: GameResultPanelControl.Instance.SetLevel(GomokuConstants.HardLevelDepth); break;
            case GomokuConstants.HardLevelDepth: GameResultPanelControl.Instance.SetLevel(GomokuConstants.EasyLevelDepth); break;
        }
    }


    void OnShowGirdButtonClick()
    {
        GomokuManager.Instance.isShowAllGrid = !GomokuManager.Instance.isShowAllGrid;
    }


    void OnNextSongButtonClick()
    {
        AudioManager.Instance.PlayNextBGM();
    }

    void OnPreviousSongButtonClick()
    {
        AudioManager.Instance.PlayPreviousBGM();
    }

    public void SetBGMTitle()
    {
        SongTitle.text = AudioManager.Instance.GetCurrentBGMName();
    }

    void OnShowNumberClick()
    {
        GomokuManager.Instance.SwitchShowNumber();
    }

    void OnResignButtonClick()
    {
        GomoKuType winner = GomokuManager.Instance.GetPlayerColor() == GomoKuType.White
                            ? GomoKuType.Black
                            : GomoKuType.White;
        GameManager.Instance.EndGame(winner);
    }

    void OnRestButtonClick()
    {
        CameraControl.Instance.ResetCamera();
    }
    
    #endregion


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1)) SetCornerUIActive(!isShowUI);   
    }



    private void UpdateTurnText()
    {
        var data = GomokuManager.Instance.GetGomokuData();
        int current = data.CurrentTurn;

        bool isPlayerTurn = data.IsPlayerTurn();


        AITurnGameObject.SetActive(!isPlayerTurn);
        PlayerTurnGameObject.SetActive(isPlayerTurn);


        int tens = current / 10;   
        int digits = current % 10; 

        if (isPlayerTurn)
        {
            PlayerTurnTens.sprite = UIManager.Instance.GetLevelNumSprite(tens);
            PlayerTurnDigits.sprite = UIManager.Instance.GetLevelNumSprite(digits); 
        }
        else
        {
            AIurnTens.sprite = UIManager.Instance.GetLevelNumSprite(tens);
            AIurnDigits.sprite = UIManager.Instance.GetLevelNumSprite(digits);
        }
    }


    public void ShowPanel()
    {
        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    public void ShowGameUI()
    {
        gameObject.SetActive(true);
    }
    public void HideGameUI()
    {
        gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        if (GomokuManager.Instance.GetGomokuData() != null)
            GomokuManager.Instance.GetGomokuData().OnGomokuDataChanged -= UpdateTurnText;
    }

    void SetCornerUIActive(bool isActive)
    {
         isShowUI = isActive;
         RightDownCorner.SetActive(isActive);
         LeftDownCorner.SetActive(isActive);
         RightUpCorner.SetActive(isActive);
         LeftUpCorner.SetActive(isActive);

    }

}
