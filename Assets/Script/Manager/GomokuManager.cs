using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
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

    private GomokuData gomokuData;

    [SerializeField] DeskControl deskControl;
    private bool aiThinking = false;
    private bool isMoving = false; // ????????

    int currentLevel = GomokuConstants.EasyLevelDepth;

    private Coroutine currentMoveCoroutine = null;
    private Coroutine currentAICoroutine = null;


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

    public GomokuData GetGomokuData()
    {
        return gomokuData; 
    }

    public bool IsPlayerWin(GomoKuType winner)
    {
        return gomokuData.IsPlayerWin(winner);
    }

    public void StartGame(bool isPlayerBlack)
    {
        ForceStopAllMovements();
        ClearBoard();
        PotentialFieldsManager.Instance.Init();
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
        int layerMask = LayerMask.GetMask("ClickablePoint");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
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
        MultiAStarPaths Path = AStarAlgorithm.Instance.GetPathsToTarget(endPoint);
        Debug.Log($"Path :{Path.TargetPoint} {Path.MainPath.Count} AND  {Path.SurroundPaths.Count} ");
        currentMoveCoroutine = StartCoroutine(MovePiecePaths(Path));
    }

    void ClearBoard()
    {
        foreach (var OlderChess in currentChessesOnBoard)
        {
          if (OlderChess != null) Destroy(OlderChess.gameObject);
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

    GameObject CreateChess(Vector3 boardPosition)
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
        currentAICoroutine = StartCoroutine(AIMoveCoroutines(thinkTime));
    }

    private IEnumerator AIMoveCoroutines(float thinkTime)
    {
        while (isMoving)
        {
            yield return null;
        }

        aiThinking = true;

        yield return new WaitForSeconds(thinkTime);

        MultiAStarPaths Paths = GomokuAI.Instance.FindBestMovePathWithRetries(
            gomokuData.GetBoard(),
            gomokuData.GetAIColor(),
            currentLevel
        );

        if (Paths == null)
        {
            aiThinking = false;
            yield break;
        }

        currentMoveCoroutine = StartCoroutine(MovePiecePaths(Paths));
        yield return currentMoveCoroutine;

        aiThinking = false;
    }


    private IEnumerator MovePiecePaths(MultiAStarPaths Paths)
    {
        if (Paths == null || Paths.MainPath == null || Paths.MainPath.Count == 0)
            yield break;

        if (!PlaceChess(Paths.TargetPoint))
            yield break;

        isMoving = true;

        List<GameObject> sidePieces = new List<GameObject>();
        GameObject mainPiece = null;

        var mainPath = Paths.MainPath;
        int startX = Paths.TargetPoint.Item1, startY = Paths.TargetPoint.Item2;
        Vector3 startPos = deskControl.GetGridCell(startX, startY).transform.position;

        mainPiece = CreateChess(startPos);

        List<Vector3> mainWaypoints = Paths.MainPath;
        //foreach (var point in mainPath)
        //    mainWaypoints.Add(deskControl.GetGridCell(point.Item1, point.Item2).transform.position);

        List<GameObject> allPieces = new List<GameObject> { mainPiece };
        List<List<Vector3>> allWaypoints = new List<List<Vector3>> { mainWaypoints };

        PotentialFieldsManager.Instance.AddCurrentItems(mainPiece);

        if (Paths.SurroundPaths != null && Paths.SurroundPaths.Count > 0)
        {
            foreach (var path in Paths.SurroundPaths)
            {
                if (path == null || path.Count == 0)
                    continue;

                Vector3 sPos = path[0];
                GameObject sidePiece = CreateChess(sPos);
                sidePieces.Add(sidePiece);

                List<Vector3> sideWaypoints = new List<Vector3>();

                allPieces.Add(sidePiece);
                allWaypoints.Add(sideWaypoints);

                PotentialFieldsManager.Instance.AddCurrentItems(sidePiece);
            }
        }

        yield return StartCoroutine(
            PotentialFieldsManager.Instance.StartIndependentPFMovement(allPieces, allWaypoints)
        );

        Vector3 finalPos = deskControl.GetGridCell(Paths.TargetPoint.Item1, Paths.TargetPoint.Item2).transform.position;

        foreach (var piece in sidePieces)
            if (piece != null) piece.transform.position = finalPos;

        GomoKuType result = gomokuData.CheckGameState(Paths.TargetPoint.Item1, Paths.TargetPoint.Item2);
        if (result != GomoKuType.None)
        {
            GameManager.Instance.EndGame(result);
            foreach (var s in sidePieces)
                Destroy(s);
            isMoving = false;
            yield break;
        }

        foreach (var s in sidePieces)
        {
            PotentialFieldsManager.Instance.RemoveCurrentItem(s);
            Destroy(s);
        }

        gomokuData.NextTurn();

        if (!gomokuData.IsPlayerTurn())
            AIMove(GomokuConstants.AIThinkTime);

        isMoving = false;
    }

  

    bool PlaceChess((int x,int y) Point)
    {
        return gomokuData.PlaceChess(Point.x, Point.y);
    }

    public void ForceStopAllMovements()
    {
        if (currentMoveCoroutine != null)
        {
            StopCoroutine(currentMoveCoroutine);
            currentMoveCoroutine = null;
        }

        if (currentAICoroutine != null)
        {
            StopCoroutine(currentAICoroutine);
            currentAICoroutine = null;
        }

        isMoving = false;
        aiThinking = false;
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
