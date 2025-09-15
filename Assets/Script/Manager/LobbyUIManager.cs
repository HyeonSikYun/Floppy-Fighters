using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshPro 네임스페이스 추가

public class LobbyUIManager : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    [SerializeField] Button singlePlayButton;
    [SerializeField] Button multiPlayButton;
    [SerializeField] Button manualButton;
    [SerializeField] Button exitButton;

    [Header("Manual System")]
    [SerializeField] GameObject manualPanel;
    [SerializeField] Button manualCloseButton;
    [SerializeField] TextMeshProUGUI[] manualTexts; // TextMeshPro로 변경
    [SerializeField] Button manualPrevButton;
    [SerializeField] Button manualNextButton;
    [SerializeField] TextMeshProUGUI pageNumberText; // TextMeshPro로 변경

    [Header("Scene Names")]
    [SerializeField] string singleSceneName = "SingleScene";
    [SerializeField] string multiSceneName = "MultiScene";

    private int currentManualPage = 0;
    private string[] manualContents = new string[]
    {
        "기본 조작법\n\n" +
        "WASD - 이동\n" +
        "Space - 점프\n" +
        "R - 부활(싱글플레이 한정)\n" +
        "마우스 - 카메라 회전\n\n" +
        "지금 직접 움직여보세요!",

        "전투 시스템\n\n" +
        "좌클릭 - 펀치\n" +
        "G키 (홀드) - 잡기\n\n" +
        "다른 플레이어나 오브젝트를\n" +
        "잡고 던질 수 있어요!",

        "물리 시스템\n\n" +
        "다른 오브젝트에 맞아 충격을 받으면\n" +
        "행동불능 상태가 됩니다.\n" +
        "3초 후 자동 복구돼요!",

        "게임 목표\n\n" +
        "다른 플레이어를 밀어내기\n" +
        "맵 밖으로 떨어뜨리기\n" +
        "마지막까지 살아남기\n" +
        "싱글 플레이로 연습을 해보세요\n\n" +
        "준비되셨나요? 게임을 시작하세요!"
    };

    void Start()
    {
        // 버튼 이벤트 연결
        singlePlayButton.onClick.AddListener(OnSinglePlayClicked);
        multiPlayButton.onClick.AddListener(OnMultiPlayClicked);
        manualButton.onClick.AddListener(OnManualClicked);
        exitButton.onClick.AddListener(OnExitClicked);

        manualCloseButton.onClick.AddListener(OnManualCloseClicked);
        manualPrevButton.onClick.AddListener(OnManualPrevClicked);
        manualNextButton.onClick.AddListener(OnManualNextClicked);

        // 초기 설정
        manualPanel.SetActive(false);
        currentManualPage = 0;

        // 마우스 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // ESC 키로 매뉴얼 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (manualPanel.activeInHierarchy)
            {
                OnManualCloseClicked();
            }
        }
    }

    // === 메인 메뉴 버튼들 ===
    void OnSinglePlayClicked()
    {
        SceneManager.LoadScene(singleSceneName);
    }

    void OnMultiPlayClicked()
    {
        SceneManager.LoadScene(multiSceneName);
    }

    void OnManualClicked()
    {
        manualPanel.SetActive(true);
        currentManualPage = 0;
        UpdateManualDisplay();

        // 매뉴얼 열 때 마우스 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnExitClicked()
    {

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // === 매뉴얼 시스템 ===
    void OnManualCloseClicked()
    {
        manualPanel.SetActive(false);
    }

    void OnManualPrevClicked()
    {
        if (currentManualPage > 0)
        {
            currentManualPage--;
            UpdateManualDisplay();
        }
    }

    void OnManualNextClicked()
    {
        if (currentManualPage < manualContents.Length - 1)
        {
            currentManualPage++;
            UpdateManualDisplay();
        }
    }

    void UpdateManualDisplay()
    {
        // 현재 페이지 텍스트 업데이트
        if (manualTexts.Length > 0 && currentManualPage < manualContents.Length)
        {
            manualTexts[0].text = manualContents[currentManualPage];
        }

        // 페이지 번호 업데이트
        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentManualPage + 1} / {manualContents.Length}";
        }

        // 버튼 활성화 상태 업데이트
        manualPrevButton.interactable = currentManualPage > 0;
        manualNextButton.interactable = currentManualPage < manualContents.Length - 1;
    }

    // === 공개 메서드 (다른 스크립트에서 호출 가능) ===
    public void ShowManual()
    {
        OnManualClicked();
    }

    public void HideManual()
    {
        OnManualCloseClicked();
    }
}