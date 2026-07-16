using System;
using UnityEngine;

public enum Player
{
    None = 0,
    Red = 1,
    Yellow = 2
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int rows = 6;
    [SerializeField] private int columns = 7;

    public Player[,] Board { get; private set; }

    public Player CurrentPlayer { get; private set; } = Player.Red;

    public bool IsGameOver { get; private set; }

    public bool OnlineMode = false;

    public Player LocalPlayer = Player.Red;

    public Player Winner { get; private set; } = Player.None;

    public event Action<Player[,]> OnBoardChanged;
    public event Action<Player> OnTurnChanged;
    public event Action<Player> OnGameOver;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Board = new Player[rows, columns];
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool TryMakeMove(int column, bool receivedFromNetwork = false)
    {
        if (IsGameOver)
            return false;

        if (OnlineMode && !receivedFromNetwork && CurrentPlayer != LocalPlayer)
            return false;

        if (column < 0 || column >= columns)
            return false;

        for (int row = rows - 1; row >= 0; row--)
        {
            if (Board[row, column] == Player.None)
            {
                Board[row, column] = CurrentPlayer;

                if (OnlineMode && !receivedFromNetwork)
                    LightNetwork.Instance.SendMove(column);

                if (CheckWin(row, column))
                {
                    IsGameOver = true;
                    Winner = CurrentPlayer;

                    OnBoardChanged?.Invoke(Board);
                    OnGameOver?.Invoke(Winner);

                    return true;
                }

                if (IsBoardFull())
                {
                    IsGameOver = true;
                    Winner = Player.None;

                    OnBoardChanged?.Invoke(Board);
                    OnGameOver?.Invoke(Player.None);

                    return true;
                }

                CurrentPlayer =
                    CurrentPlayer == Player.Red ?
                    Player.Yellow :
                    Player.Red;

                OnBoardChanged?.Invoke(Board);
                OnTurnChanged?.Invoke(CurrentPlayer);

                return true;
            }
        }

        return false;
    }

    public void RestartGame()
    {
        Board = new Player[rows, columns];
        IsGameOver = false;
        Winner = Player.None;
        CurrentPlayer = Player.Red;

        OnBoardChanged?.Invoke(Board);
        OnTurnChanged?.Invoke(CurrentPlayer);
    }

    private bool CheckWin(int row, int col)
    {
        Player p = Board[row, col];

        if (p == Player.None)
            return false;

        return
            CountDirection(row, col, 1, 0, p) +
            CountDirection(row, col, -1, 0, p) >= 3 ||

            CountDirection(row, col, 0, 1, p) +
            CountDirection(row, col, 0, -1, p) >= 3 ||

            CountDirection(row, col, 1, 1, p) +
            CountDirection(row, col, -1, -1, p) >= 3 ||

            CountDirection(row, col, 1, -1, p) +
            CountDirection(row, col, -1, 1, p) >= 3;
    }

    private int CountDirection(int startRow, int startCol, int dRow, int dCol, Player player)
    {
        int count = 0;

        int r = startRow + dRow;
        int c = startCol + dCol;

        while (r >= 0 &&
               r < rows &&
               c >= 0 &&
               c < columns &&
               Board[r, c] == player)
        {
            count++;
            r += dRow;
            c += dCol;
        }

        return count;
    }

    private bool IsBoardFull()
    {
        for (int c = 0; c < columns; c++)
        {
            if (Board[rows - 1, c] == Player.None)
                return false;
        }

        return true;
    }
}
