using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public static class GomokuConstants
{
    public const float chessPositionY = 0.03f;
    public const float AIThinkTime = 1f;

    public const int EasyLevelDepth = 3;
}

public class GomokuManager : MonoBehaviour
{
    public static GomokuManager Instance;
    private bool isShowNumber = false;

    [SerializeField] ChessControl blackChessPrefab;
    [SerializeField] ChessControl whiteChessPrefab;

    List<ChessControl> currentChessesOnBoard = new List<ChessControl>();

    public GomokuData gomokuData;

    [SerializeField] DeskControl deskControl;
    private bool aiThinking = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitGomoKuData();
    }

    void InitGomoKuData()
    {
        gomokuData = new GomokuData();
    }

    public bool IsPlayerWin(GomoKuType winner)
    {
        return gomokuData.IsPlayerWin(winner);
    }

    public void StartGame(bool isPlayerBlack)
    {
        ClearBoard();
        gomokuData.Init(isPlayerBlack);
        if (!gomokuData.IsPlayerTurn()) AIMove(0);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!GameManager.Instance.IsPlayingGame() || !gomokuData.IsPlayerTurn() || aiThinking)
                return;

            TryPlaceChess();
        }
    }

    void TryPlaceChess()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            if (deskControl.IsAtGridCells(hitObj, out int x, out int y))
            {
                Vector3 cellCenter = deskControl.GetGridCell(x, y).transform.position;
                cellCenter.y = GomokuConstants.chessPositionY;
                CreateChess(cellCenter, x, y);
            }
        }
    }


    public void OnGridClicked(int x, int y)
    {
        if (!GameManager.Instance.IsPlayingGame() || !gomokuData.IsPlayerTurn() || aiThinking)
            return;
        Vector3 cellCenter = deskControl.GetGridCell(x, y).transform.position;
        cellCenter.y = GomokuConstants.chessPositionY;

        CreateChess(cellCenter, x, y);
    }


    void ClearBoard()
    {
        foreach (var OlderChess in currentChessesOnBoard)
        {
            Destroy(OlderChess.gameObject);
        }
        currentChessesOnBoard.Clear();
    }

    public void SwitchShowNumber()
    {
        isShowNumber = !isShowNumber;
        foreach (var currentChess in currentChessesOnBoard)
        {
            currentChess.ShowNumText(isShowNumber);
        }
    }

    public GomoKuType GetPlayerColor()
    {
        return gomokuData.GetPlayerColor();
    }

    void CreateChess(Vector3 boardPosition, int x, int y)
    {
        if (!gomokuData.PlaceChess(x, y)) return;
        AudioManager.Instance.PlayClick();

        bool isBlackTurn = gomokuData.IsBlackTurn();
        var prefab = isBlackTurn ? blackChessPrefab : whiteChessPrefab;
        ChessControl newChess = Instantiate(prefab, deskControl.transform);
        newChess.transform.position = boardPosition;
        currentChessesOnBoard.Add(newChess);
        newChess.Init(gomokuData.CurrentTurn, isShowNumber);

        GomoKuType result = gomokuData.CheckGameState(x, y);
        if (result != GomoKuType.None)
        {
            GameManager.Instance.EndGame(result);
            return;
        }

        gomokuData.NextTurn();

        if (!gomokuData.IsPlayerTurn())
        {
            AIMove(GomokuConstants.AIThinkTime);
        }
    }

    #region AI

    public void AIMove(float thinkTime)
    {
        StartCoroutine(AIMoveCoroutines(thinkTime));
    }

    private IEnumerator AIMoveCoroutines(float thinkTime)
    {
        if (aiThinking) yield break;
        aiThinking = true;

        yield return new WaitForSeconds(thinkTime);

        (int x, int y) = GomokuAI.Instance.FindBestMoveByMinMax(
            gomokuData.GetBoard(),
            gomokuData.GetAIColor(),
            GomokuConstants.EasyLevelDepth
        );

        if (x == -1 || y == -1)
        {
            aiThinking = false;
            yield break;
        }

        Vector3 cellCenter = deskControl.GetGridCell(x, y).transform.position;
        cellCenter.y = GomokuConstants.chessPositionY;
        CreateChess(cellCenter, x, y);

        aiThinking = false;
    }

    private (int x, int y) AIRandomMove()
    {
        int size = DeskConstants.ConstrainCount;
        List<(int x, int y)> emptyCells = new List<(int x, int y)>();

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (gomokuData.GetCell(x, y) == GomoKuType.None)
                {
                    emptyCells.Add((x, y));
                }
            }
        }

        if (emptyCells.Count == 0) return (-1, -1);
        return emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)];
    }

    #endregion
}