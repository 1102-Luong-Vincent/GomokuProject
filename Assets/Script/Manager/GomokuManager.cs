using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public static class GomokuConstants
{
    public const float chessPositionY = 0.03f;
    public const float AIThinkTime = 1f;

    public const int EasyLevelDepth = 1;
    public const int MediumLevelDepth = 2;
    public const int HardLevelDepth = 3;
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
    private bool isMoving = false; // ????????

    int currentLevel = GomokuConstants.EasyLevelDepth;

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
            if (!GameManager.Instance.IsPlayingGame() || !gomokuData.IsPlayerTurn() || aiThinking || isMoving)
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
                PlayerMoveCoroutines(x, y);
            }
        }
    }

    void PlayerMoveCoroutines(int x, int y)
    {
        (int, int) endPoint = (x, y);
        List<(int, int)> Path = AStarAlgorithm.AStarFindWayPoint(gomokuData.GetBoard(), endPoint);
        StartCoroutine(MovePieceAlongPath(Path));
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

    GameObject CreateChess(Vector3 boardPosition, int x, int y)
    {
        AudioManager.Instance.PlayClick();

        bool isBlackTurn = gomokuData.IsBlackTurn();
        var prefab = isBlackTurn ? blackChessPrefab : whiteChessPrefab;
        ChessControl newChess = Instantiate(prefab, deskControl.transform);
        newChess.transform.position = boardPosition;
        currentChessesOnBoard.Add(newChess);
        newChess.Init(gomokuData.CurrentTurn, isShowNumber);
        return newChess.gameObject;
    }





    public void SetCurrentLevel(int newLevel)
    {
        currentLevel = newLevel;
    }

    #region AI

    public void AIMove(float thinkTime)
    {
        StartCoroutine(AIMoveCoroutines(thinkTime));
    }

    private IEnumerator AIMoveCoroutines(float thinkTime)
    {
        while (isMoving)
        {
            yield return null;
        }

        aiThinking = true;

        yield return new WaitForSeconds(thinkTime);

        List<(int, int)> Path = GomokuAI.Instance.FindBestMovePathWithRetries(
            gomokuData.GetBoard(),
            gomokuData.GetAIColor(),
            currentLevel
        );

        if (Path == null || Path.Count == 0)
        {
            aiThinking = false;
            yield break;
        }

        yield return StartCoroutine(MovePieceAlongPath(Path));

        aiThinking = false;
    }

    private IEnumerator MovePieceAlongPath(List<(int x, int y)> Path)
    {
        if (Path == null || Path.Count == 0)
            yield break;

        if (!PlaceChess(Path))
            yield break;

        isMoving = true;

        int startX = Path[0].x, startY = Path[0].y;
        Vector3 startPos = deskControl.GetGridCell(startX, startY).transform.position;
        GameObject newPiece = CreateChess(startPos, startX, startY);
        List<Vector3> waypoints = new List<Vector3>();
        foreach (var point in Path)
        {
            Vector3 pos = deskControl.GetGridCell(point.x, point.y).transform.position;
            waypoints.Add(pos);
        }

        PotentialFieldsManager.Instance.AddCurrentItems(newPiece);
        yield return StartCoroutine(PotentialFieldsManager.Instance.StartPFMovement(waypoints, newPiece));



        var last = FindPathLastPoint(Path);
        GomoKuType result = gomokuData.CheckGameState(last.Item1, last.Item2);
        if (result != GomoKuType.None)
        {
            GameManager.Instance.EndGame(result);
            isMoving = false;
            yield break;
        }

        gomokuData.NextTurn();

        if (!gomokuData.IsPlayerTurn())
            AIMove(GomokuConstants.AIThinkTime);

        isMoving = false;
    }












    bool PlaceChess(List<(int x,int y)> Path)
    {
        int lastX = FindPathLastPoint(Path).Item1;
        int lastY = FindPathLastPoint(Path).Item2;
        return gomokuData.PlaceChess(lastX, lastY);
    }

    (int,int) FindPathLastPoint(List<(int x, int y)> Path)
    {
        var last = Path[Path.Count - 1];
        int lastX = last.x;
        int lastY = last.y;
        return last;
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
