using System;
using System.Collections.Generic;
using UnityEngine;

public enum GomoKuType
{
    None,
    Black,
    White,
    Draw
}

public class GomokuData
{
    public event Action OnGomokuDataChanged;

    private bool isPlayerBlack;
    public bool IsPlayerBlack
    {
        get => isPlayerBlack;
        set => SetProperty(ref isPlayerBlack, value);
    }

    private int currentTurn;
    public int CurrentTurn
    {
        get => currentTurn;
        set => SetProperty(ref currentTurn, value);
    }

    private GomoKuType[,] board = new GomoKuType[DeskConstants.ConstrainCount, DeskConstants.ConstrainCount];


    public void Init(bool isPlayerBlack)
    {
        int Size = DeskConstants.ConstrainCount;
        IsPlayerBlack = isPlayerBlack;
        CurrentTurn = 1;
        board = new GomoKuType[Size, Size];

        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                board[x, y] = GomoKuType.None;
    }

    public bool IsPlayerTurn()
    {
        return (CurrentTurn % 2 == 1 && IsPlayerBlack) ||
               (CurrentTurn % 2 == 0 && !IsPlayerBlack);
    }

    public bool IsBlackTurn()
    {
        return (CurrentTurn % 2 == 1);
    }

    public bool PlaceChess(int x, int y)
    {
        if (board[x, y] != GomoKuType.None) return false;

        board[x, y] = CurrentTurn % 2 == 1 ? GomoKuType.Black : GomoKuType.White;
        return true;
    }


    #region check Result
    public bool IsDraw()
    {
        int Size = DeskConstants.ConstrainCount;
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (board[x, y] == GomoKuType.None)
                    return false; 
            }
        }
        return true;
    }

    public GomoKuType CheckGameState(int lastX, int lastY)
    {
        GomoKuType winner = CheckVictory(lastX, lastY);
        if (winner != GomoKuType.None)
            return winner;

        if (IsDraw())
            return GomoKuType.Draw; 

        return GomoKuType.None;
    }

    private GomoKuType CheckVictory(int x, int y)
    {
        GomoKuType type = board[x, y];
        if (type == GomoKuType.None) return GomoKuType.None;

        int[][] directions = new int[][]
        {
            new int[]{1, 0},
            new int[]{0, 1},
            new int[]{1, 1},
            new int[]{1, -1}
        };

        foreach (var dir in directions)
        {
            int count = 1;
            count += CountInDirection(x, y, dir[0], dir[1], type);
            count += CountInDirection(x, y, -dir[0], -dir[1], type);

            if (count >= 5)
                return type;
        }

        return GomoKuType.None;
    }

    private int CountInDirection(int startX, int startY, int dx, int dy, GomoKuType type)
    {
        int count = 0;
        int x = startX + dx;
        int y = startY + dy;
        int Size = DeskConstants.ConstrainCount;
        while (x >= 0 && x < Size && y >= 0 && y < Size && board[x, y] == type)
        {
            count++;
            x += dx;
            y += dy;
        }

        return count;
    }
    #endregion

    public bool IsPlayerWin(GomoKuType winner)
    {
        bool isWinnerBlack = winner == GomoKuType.Black;
        return isWinnerBlack == isPlayerBlack;
    }

    public GomoKuType GetPlayerColor()
    {
        if (isPlayerBlack)
        {
            return GomoKuType.Black;
        } else
        {
            return GomoKuType.White;
        }
    }

    public void NextTurn() => CurrentTurn++;

    public GomoKuType GetCell(int x, int y) => board[x, y];

    private void SetProperty<T>(ref T field, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnGomokuDataChanged?.Invoke();
        }
    }
}
