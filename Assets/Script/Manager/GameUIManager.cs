using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Fusion;

public class GameUIManager : MonoBehaviour
{
    [Header("Winner UI")]
    [SerializeField] GameObject winnerPanel;
    [SerializeField] TextMeshProUGUI winnerText;
    [SerializeField] Button exitButton;

    private bool gameEnded = false;
    private float gameStartTime;
    private float minGameTime = 3f;

    private void Start()
    {
        if (winnerPanel != null)
            winnerPanel.SetActive(false);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    private void Update()
    {
        if (!gameEnded)
        {
            CheckWinnerCondition();
        }
    }

    void CheckWinnerCondition()
    {
        // 게임 시작 후 최소 시간이 지나지 않으면 체크하지 않음
        if (Time.time - gameStartTime < minGameTime)
            return;

        NetworkPlayer[] allPlayers = FindObjectsOfType<NetworkPlayer>();

        // 플레이어가 아직 스폰되지 않았으면 대기
        if (allPlayers.Length == 0)
            return;

        int aliveCount = 0;
        NetworkPlayer lastAlive = null;

        foreach (NetworkPlayer player in allPlayers)
        {
            // 경기장에서 떨어지지 않은 플레이어만 생존자로 판단
            if (!player.IsFallen)
            {
                aliveCount++;
                lastAlive = player;
            }
        }

        // 최소 2명 이상의 플레이어가 있어야 승부 판정
        if (allPlayers.Length < 2)
            return;

        if (aliveCount == 1 && lastAlive != null)
        {
            ShowWinnerUI(lastAlive.Object.Id);
            gameEnded = true;
        }
        else if (aliveCount == 0)
        {
            ShowDrawUI();
            gameEnded = true;
        }
    }

    public void ShowWinnerUI(NetworkId winnerId)
    {
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(true);

            if (winnerText != null)
            {
                NetworkPlayer localPlayer = NetworkPlayer.Local;
                if (localPlayer != null && localPlayer.Object.Id == winnerId)
                {
                    winnerText.text = "YOU WIN!";
                    winnerText.color = Color.green;
                }
                else
                {
                    winnerText.text = $"Player {winnerId.Raw} Wins!";
                    winnerText.color = Color.red;
                }
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowDrawUI()
    {
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(true);

            if (winnerText != null)
            {
                winnerText.text = "DRAW!";
                winnerText.color = Color.yellow;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}