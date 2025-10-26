using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;
using static AStarAlgorithm;

public class GomokuAI : MonoBehaviour
{
    public static GomokuAI Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    public List<(int, int)> FindBestMovePathWithRetries(GomoKuType[,] board, GomoKuType aiPiece, int depth,int maxRetries = 100)
    {
        List<(int, int)> path = null;
        int retries = 0;

        while (retries < maxRetries)
        {
            var bestMove = FindBestMoveByMinMax(board, aiPiece, depth);

            if (bestMove == (-1, -1))break; 

            path = AStarAlgorithm.AStarFindWayPoint(board, bestMove);

            if (path != null && path.Count > 0)
            {
                break;
            }

            board[bestMove.Item1, bestMove.Item2] = GomoKuType.Draw;
            retries++;
        }

        return path;
    }




    public List<(int,int)> FindBestMovePathByMinMax(GomoKuType[,] board, GomoKuType aiPiece, int depth)
    {
        (int, int) bestMove = (-1, -1);
        int bestScore = int.MinValue;

        GomoKuType playerPiece = GetPlayerPiece(aiPiece);

        var moves = GetSmartMoves(board);

        foreach (var move in moves)
        {
            board[move.x, move.y] = aiPiece;

            int score = MinMax(board, depth, false, aiPiece, playerPiece, int.MinValue, int.MaxValue);

            board[move.x, move.y] = GomoKuType.None;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        if (bestMove == (-1, -1) && moves.Count > 0)
        {
            bestMove = moves[0];
        }

        return AStarAlgorithm.AStarFindWayPoint(board,bestMove);
    }



    public (int, int) FindBestMoveByMinMax(GomoKuType[,] board, GomoKuType aiPiece, int depth)
    {
        (int, int) bestMove = (-1, -1);
        int bestScore = int.MinValue;

        GomoKuType playerPiece = GetPlayerPiece(aiPiece);

        var moves = GetSmartMoves(board);

        foreach (var move in moves)
        {
            board[move.x, move.y] = aiPiece;

            int score = MinMax(board, depth, false, aiPiece, playerPiece, int.MinValue, int.MaxValue);

            board[move.x, move.y] = GomoKuType.None;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        if (bestMove == (-1, -1) && moves.Count > 0)
        {
            bestMove = moves[0];
        }

        return bestMove;
    }













    static GomoKuType GetPlayerPiece(GomoKuType aiPiece)
    {
        return (aiPiece == GomoKuType.Black) ? GomoKuType.White : GomoKuType.Black;
    }

    private static int MinMax(GomoKuType[,] board, int depth, bool isMaximizing, GomoKuType aiPiece, GomoKuType playerPiece, int alpha, int beta)
    {
        if (CheckWin(board, aiPiece))
            return 10000 + depth;

        if (CheckWin(board, playerPiece))
            return -10000 - depth; 

        if (depth == 0 || IsGameOver(board))
            return EvaluateBoard(board, aiPiece, playerPiece);

        if (isMaximizing)
            return Maximize(board, depth, aiPiece, playerPiece, ref alpha, ref beta);
        else
            return Minimize(board, depth, aiPiece, playerPiece, ref alpha, ref beta);
    }

    private static int Maximize(GomoKuType[,] board, int depth, GomoKuType aiPiece, GomoKuType playerPiece, ref int alpha, ref int beta)
    {
        int maxEval = int.MinValue;

        foreach (var move in GetSmartMoves(board))
        {
            board[move.x, move.y] = aiPiece;
            int eval = MinMax(board, depth - 1, false, aiPiece, playerPiece, alpha, beta);
            board[move.x, move.y] = GomoKuType.None;

            maxEval = Math.Max(maxEval, eval);

            if (ShouldPruneByAB(ref alpha, ref beta, maxEval, true))
                break;
        }

        return maxEval;
    }

    private static int Minimize(GomoKuType[,] board, int depth, GomoKuType aiPiece, GomoKuType playerPiece, ref int alpha, ref int beta)
    {
        int minEval = int.MaxValue;

        foreach (var move in GetSmartMoves(board))
        {
            board[move.x, move.y] = playerPiece;
            int eval = MinMax(board, depth - 1, true, aiPiece, playerPiece, alpha, beta);
            board[move.x, move.y] = GomoKuType.None;

            minEval = Math.Min(minEval, eval);

            if (ShouldPruneByAB(ref alpha, ref beta, minEval, false))
                break;
        }

        return minEval;
    }

    private static bool ShouldPruneByAB(ref int alpha, ref int beta, int currentValue, bool isMaximizing)
    {
        if (isMaximizing)
        {
            alpha = Math.Max(alpha, currentValue);
            if (beta <= alpha)
                return true;
        }
        else
        {
            beta = Math.Min(beta, currentValue);
            if (beta <= alpha)
                return true;
        }
        return false;
    }


    private static List<(int x, int y)> GetSmartMoves(GomoKuType[,] board)
    {
        int width = board.GetLength(0);
        int height = board.GetLength(1);
        HashSet<(int, int)> moves = new HashSet<(int, int)>();

        bool hasAnyPiece = false;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] != GomoKuType.None)
                {
                    hasAnyPiece = true;
                    break;
                }
            }
            if (hasAnyPiece) break;
        }

        if (!hasAnyPiece)
        {
            moves.Add((width / 2, height / 2));
            return new List<(int, int)>(moves);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] != GomoKuType.None)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height && board[nx, ny] == GomoKuType.None)
                            {
                                moves.Add((nx, ny));
                            }
                        }
                    }
                }
            }
        }

        return new List<(int, int)>(moves);
    }

    private static List<(int x, int y)> GetAvailableMoves(GomoKuType[,] board)
    {
        List<(int, int)> moves = new List<(int, int)>();
        for (int x = 0; x < board.GetLength(0); x++)
        {
            for (int y = 0; y < board.GetLength(1); y++)
            {
                if (board[x, y] == GomoKuType.None)
                    moves.Add((x, y));
            }
        }
        return moves;
    }

    private static bool IsGameOver(GomoKuType[,] board)
    {
        for (int x = 0; x < board.GetLength(0); x++)
            for (int y = 0; y < board.GetLength(1); y++)
                if (board[x, y] == GomoKuType.None)
                    return false;
        return true;
    }

    private static bool CheckWin(GomoKuType[,] board, GomoKuType piece)
    {
        int width = board.GetLength(0);
        int height = board.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] == piece)
                {
                    if (x + 4 < width && CheckLine(board, piece, x, y, 1, 0))
                        return true;
                    if (y + 4 < height && CheckLine(board, piece, x, y, 0, 1))
                        return true;
                    if (x + 4 < width && y + 4 < height && CheckLine(board, piece, x, y, 1, 1))
                        return true;
                    if (x - 4 >= 0 && y + 4 < height && CheckLine(board, piece, x, y, -1, 1))
                        return true;
                }
            }
        }

        return false;
    }


    private static bool CheckLine(GomoKuType[,] board, GomoKuType piece, int x, int y, int dx, int dy)
    {
        for (int i = 0; i < 5; i++)
        {
            if (board[x + i * dx, y + i * dy] != piece)
                return false;
        }
        return true;
    }

    private static int EvaluateBoard(GomoKuType[,] board, GomoKuType aiPiece, GomoKuType playerPiece)
    {
        int aiScore = EvaluatePiece(board, aiPiece);
        int playerScore = EvaluatePiece(board, playerPiece);

        return aiScore - playerScore;
    }

    private static int EvaluatePiece(GomoKuType[,] board, GomoKuType piece)
    {
        int score = 0;
        int width = board.GetLength(0);
        int height = board.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] == piece)
                {
                    score += EvaluateDirection(board, piece, x, y, 1, 0);  
                    score += EvaluateDirection(board, piece, x, y, 0, 1);  
                    score += EvaluateDirection(board, piece, x, y, 1, 1);  
                    score += EvaluateDirection(board, piece, x, y, 1, -1); 
                }
            }
        }

        return score;
    }


    private static int EvaluateDirection(GomoKuType[,] board, GomoKuType piece, int x, int y, int dx, int dy)
    {
        int width = board.GetLength(0);
        int height = board.GetLength(1);
        int count = 0;
        int emptyCount = 0;


        for (int i = 0; i < 5; i++)
        {
            int nx = x + i * dx;
            int ny = y + i * dy;

            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                break;

            if (board[nx, ny] == piece)
                count++;
            else if (board[nx, ny] == GomoKuType.None)
                emptyCount++;
            else
                break; 
        }

        return count switch
        {
            4 => 1000,  
            3 => 100,   
            2 => 10,    
            1 => 1,     
            _ => 0
        };
    }
}