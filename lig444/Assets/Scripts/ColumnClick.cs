using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ColumnClickHandler : MonoBehaviour
{
    [SerializeField] private int columnIndex;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (GameManager.Instance.OnlineMode &&
            GameManager.Instance.CurrentPlayer != GameManager.Instance.LocalPlayer)
            return;

        GameManager.Instance.TryMakeMove(columnIndex);
    }
}
