using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI winText;

    private void Start()
    {
        GameManager.Instance.OnTurnChanged += UpdateTurnText;
        GameManager.Instance.OnGameOver += ShowGameOver;
        UpdateTurnText(GameManager.Instance.CurrentPlayer);
    }

    private void UpdateTurnText(Player current)
    {
        turnText.text = current == Player.Red ? "Vez do Vermelho" : "Vez do Amarelo";
    }

    private void ShowGameOver(Player winner)
    {
        winPanel.SetActive(true);
        if (winner == Player.None)
            winText.text = "Empate!";
        else
            winText.text = winner == Player.Red ? "Vermelho venceu!" : "Amarelo venceu!";
    }

    public void OnRestartClicked()
    {
        GameManager.Instance.RestartGame();
        winPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= UpdateTurnText;
            GameManager.Instance.OnGameOver -= ShowGameOver;
        }
    }
}