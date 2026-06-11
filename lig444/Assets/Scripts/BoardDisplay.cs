using UnityEngine;
using UnityEngine.UI;

public class BoardDisplay : MonoBehaviour
{
    [SerializeField] private Transform gridParent; // o objeto com GridLayoutGroup (BoardPanel)
    [SerializeField] private Sprite circleSprite;  // sprite branco do círculo

    private Image[,] cellImages;
    private int rows;
    private int columns;

    private void Start()
    {
        // A grid já deve ter sido construída com 42 filhos (7 col * 6 linhas)
        rows = GameManager.Instance.Board.GetLength(0);
        columns = GameManager.Instance.Board.GetLength(1);
        cellImages = new Image[rows, columns];

        int childIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Image img = gridParent.GetChild(childIndex).GetComponent<Image>();
                cellImages[r, c] = img;
                childIndex++;
            }
        }

        GameManager.Instance.OnBoardChanged += UpdateBoard;
        UpdateBoard(GameManager.Instance.Board);
    }

    private void UpdateBoard(Player[,] board)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                switch (board[r, c])
                {
                    case Player.None:
                        cellImages[r, c].sprite = circleSprite;
                        cellImages[r, c].color = Color.white;
                        break;
                    case Player.Red:
                        cellImages[r, c].sprite = circleSprite;
                        cellImages[r, c].color = Color.red;
                        break;
                    case Player.Yellow:
                        cellImages[r, c].sprite = circleSprite;
                        cellImages[r, c].color = Color.yellow;
                        break;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnBoardChanged -= UpdateBoard;
    }
}