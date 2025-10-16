using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChessControl : MonoBehaviour
{
    [SerializeField] bool isBlack;
    [SerializeField] TextMeshProUGUI NumText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        SetTextColor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(int number,bool isShowNum)
    {
        NumText.text = number.ToString();
        ShowNumText(isShowNum);
    }

    void SetTextColor()
    {
        if (isBlack)
        {
            NumText.color = Color.white;
        }
        else
        {
            NumText.color = Color.black;
        }
    }

    public void ShowNumText(bool isShowNum)
    {
        NumText.gameObject.SetActive(isShowNum);
    }
}
