using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIControl : MonoBehaviour
{

    public static GameUIControl Instance;
    [SerializeField] TextMeshProUGUI TurnText;
    [SerializeField] Button ShowNumberButton;
    [SerializeField] Button ResignButton;

    [SerializeField] TextMeshProUGUI MoveSpeedText;
    [SerializeField] Slider MoveSpeedSlider;
    private readonly float[] speedLevels = { 0.001f, 0.01f, 0.05f, 0.1f, 0.15f }; 

    private float moveSpeed = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        InitButtons();
        InitSlider();
    }


    void InitSlider()
    {
        MoveSpeedSlider.minValue = 0;
        MoveSpeedSlider.maxValue = speedLevels.Length - 1;
        MoveSpeedSlider.wholeNumbers = true; 
        MoveSpeedSlider.value = 1;
        MoveSpeedSlider.onValueChanged.AddListener(OnSliderValueChanged);
        SetSpeed((int)MoveSpeedSlider.value);
    }


    private void OnSliderValueChanged(float value)
    {
        int index = Mathf.RoundToInt(value); 
        SetSpeed(index);
    }

    void SetSpeed(int levelIndex)
    {
        levelIndex = Mathf.Clamp(levelIndex, 0, speedLevels.Length - 1);
        moveSpeed = speedLevels[levelIndex];
        MoveSpeedText.text = $"Current Move Speed: {moveSpeed.ToString("N3")}";
    }

    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    #region Buttons
    void InitButtons()
    {
        ShowNumberButton.onClick.AddListener(OnShowNumberClick);
        ResignButton.onClick.AddListener(OnResignButtonClick);
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

    #endregion

    private void Start()
    {
        GomokuManager.Instance.GetGomokuData().OnGomokuDataChanged += UpdateTurnText;
        UpdateTurnText();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void UpdateTurnText()
    {
        var data = GomokuManager.Instance.GetGomokuData();
        int current = data.CurrentTurn;

        bool isPlayerTurn = data.IsPlayerTurn();

        string turnInfo = isPlayerTurn
            ? $"Turn {current}: Your Turn!"
            : $"Turn {current}: AI Turn!";

        TurnText.text = turnInfo;
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

}
