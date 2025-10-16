using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIControl : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI TurnText;
    [SerializeField] Button ShowNumberButton;
    [SerializeField] Button ResignButton;

    private void Awake()
    {
        InitButtons();
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
