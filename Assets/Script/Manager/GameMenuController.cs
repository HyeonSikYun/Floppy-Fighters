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
        // ������ �� �޴� �г� ��Ȱ��ȭ
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // ��ư �̺�Ʈ ����
        SetupButtonEvents();

        // ���� ���� �� �ð� ���� ���
        Time.timeScale = 1f;
    }

    void Update()
    {
        // ESC Ű �Է� ����
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

        // �޴��� ������ ���� �Ͻ�����, ������ �簳
        Time.timeScale = isMenuOpen ? 0f : 1f;

        // Ŀ�� ���̱�/����� (�ʿ��� ���)
        Cursor.visible = isMenuOpen;
        Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void GoToMainMenu()
    {
        // �ð� ������ ����ȭ
        Time.timeScale = 1f;

        // �κ������ �̵�
        SceneManager.LoadScene(lobbySceneName);
    }

    public void ResumeGame()
    {
        // �޴� �ݱ�
        isMenuOpen = false;
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // ���� �簳
        Time.timeScale = 1f;

        // Ŀ�� �����
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void QuitGame()
    {
        // �ð� ������ ����ȭ
        Time.timeScale = 1f;

        // �����Ϳ����� �÷��� ��� ����, ���忡���� �� ����
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // �ٸ� ��ũ��Ʈ���� �޴� ���¸� Ȯ���� �� ���
    public bool IsMenuOpen()
    {
        return isMenuOpen;
    }
}