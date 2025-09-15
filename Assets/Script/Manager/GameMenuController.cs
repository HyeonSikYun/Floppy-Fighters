using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuPanel;
    public Button mainMenuButton;
    public Button resumeButton;
    public Button quitButton;

    [Header("Settings")]
    public string lobbySceneName = "LobbyScene";

    private bool isMenuOpen = false;

    void Start()
    {
        // 시작할 때 메뉴 패널 비활성화
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // 버튼 이벤트 연결
        SetupButtonEvents();

        // 게임 시작 시 시간 정상 재생
        Time.timeScale = 1f;
    }

    void Update()
    {
        // ESC 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    void SetupButtonEvents()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (menuPanel != null)
            menuPanel.SetActive(isMenuOpen);

        // 메뉴가 열리면 게임 일시정지, 닫히면 재개
        Time.timeScale = isMenuOpen ? 0f : 1f;

        // 커서 보이기/숨기기 (필요한 경우)
        Cursor.visible = isMenuOpen;
        Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void GoToMainMenu()
    {
        // 시간 스케일 정상화
        Time.timeScale = 1f;

        // 로비씬으로 이동
        SceneManager.LoadScene(lobbySceneName);
    }

    public void ResumeGame()
    {
        // 메뉴 닫기
        isMenuOpen = false;
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // 게임 재개
        Time.timeScale = 1f;

        // 커서 숨기기
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void QuitGame()
    {
        // 시간 스케일 정상화
        Time.timeScale = 1f;

        // 에디터에서는 플레이 모드 종료, 빌드에서는 앱 종료
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // 다른 스크립트에서 메뉴 상태를 확인할 때 사용
    public bool IsMenuOpen()
    {
        return isMenuOpen;
    }
}