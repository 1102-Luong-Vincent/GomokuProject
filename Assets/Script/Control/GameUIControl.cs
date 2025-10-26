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


    public float moveSpeed = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        InitButtons();
        InitSlider();
    }


    void InitSlider()
    {
        MoveSpeedSlider.value = moveSpeed;
        MoveSpeedSlider.maxValue = 10f;
        MoveSpeedSlider.minValue = 0f;
        MoveSpeedSlider.onValueChanged.AddListener(OnSliderValueChanged);
        SetSpeed(MoveSpeedSlider.value);
    }


    private void OnSliderValueChanged(float value)
    {
        SetSpeed(value);
    }

    void SetSpeed(float speed)
    {
        moveSpeed = speed;
        MoveSpeedText.text = $"Current Move Speed :{speed.ToString("N1")}";

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
        GomokuManager.Instance.gomokuData.OnGomokuDataChanged += UpdateTurnText;
        UpdateTurnText();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void UpdateTurnText()
    {
        var data = GomokuManager.Instance.gomokuData;
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

    private void OnDestroy()
    {
        if (GomokuManager.Instance.gomokuData != null)
            GomokuManager.Instance.gomokuData.OnGomokuDataChanged -= UpdateTurnText;
    }

}
