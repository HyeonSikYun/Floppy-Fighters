using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshPro ���ӽ����̽� �߰�

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
    [SerializeField] TextMeshProUGUI[] manualTexts; // TextMeshPro�� ����
    [SerializeField] Button manualPrevButton;
    [SerializeField] Button manualNextButton;
    [SerializeField] TextMeshProUGUI pageNumberText; // TextMeshPro�� ����

    [Header("Scene Names")]
    [SerializeField] string singleSceneName = "SingleScene";
    [SerializeField] string multiSceneName = "MultiScene";

    private int currentManualPage = 0;
    private string[] manualContents = new string[]
    {
        "�⺻ ���۹�\n\n" +
        "WASD - �̵�\n" +
        "Space - ����\n" +
        "R - ��Ȱ(�̱��÷��� ����)\n" +
        "���콺 - ī�޶� ȸ��\n\n" +
        "���� ���� ������������!",

        "���� �ý���\n\n" +
        "��Ŭ�� - ��ġ\n" +
        "GŰ (Ȧ��) - ���\n\n" +
        "�ٸ� �÷��̾ ������Ʈ��\n" +
        "��� ���� �� �־��!",

        "���� �ý���\n\n" +
        "�ٸ� ������Ʈ�� �¾� ����� ������\n" +
        "�ൿ�Ҵ� ���°� �˴ϴ�.\n" +
        "3�� �� �ڵ� �����ſ�!",

        "���� ��ǥ\n\n" +
        "�ٸ� �÷��̾ �о��\n" +
        "�� ������ ����߸���\n" +
        "���������� ��Ƴ���\n" +
        "�̱� �÷��̷� ������ �غ�����\n\n" +
        "�غ�Ǽ̳���? ������ �����ϼ���!"
    };

    void Start()
    {
        // ��ư �̺�Ʈ ����
        singlePlayButton.onClick.AddListener(OnSinglePlayClicked);
        multiPlayButton.onClick.AddListener(OnMultiPlayClicked);
        manualButton.onClick.AddListener(OnManualClicked);
        exitButton.onClick.AddListener(OnExitClicked);

        manualCloseButton.onClick.AddListener(OnManualCloseClicked);
        manualPrevButton.onClick.AddListener(OnManualPrevClicked);
        manualNextButton.onClick.AddListener(OnManualNextClicked);

        // �ʱ� ����
        manualPanel.SetActive(false);
        currentManualPage = 0;

        // ���콺 Ŀ�� ǥ��
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // ESC Ű�� �Ŵ��� �ݱ�
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (manualPanel.activeInHierarchy)
            {
                OnManualCloseClicked();
            }
        }
    }

    // === ���� �޴� ��ư�� ===
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

        // �Ŵ��� �� �� ���콺 ǥ��
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

    // === �Ŵ��� �ý��� ===
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
        // ���� ������ �ؽ�Ʈ ������Ʈ
        if (manualTexts.Length > 0 && currentManualPage < manualContents.Length)
        {
            manualTexts[0].text = manualContents[currentManualPage];
        }

        // ������ ��ȣ ������Ʈ
        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentManualPage + 1} / {manualContents.Length}";
        }

        // ��ư Ȱ��ȭ ���� ������Ʈ
        manualPrevButton.interactable = currentManualPage > 0;
        manualNextButton.interactable = currentManualPage < manualContents.Length - 1;
    }

    // === ���� �޼��� (�ٸ� ��ũ��Ʈ���� ȣ�� ����) ===
    public void ShowManual()
    {
        OnManualClicked();
    }

    public void HideManual()
    {
        OnManualCloseClicked();
    }
}