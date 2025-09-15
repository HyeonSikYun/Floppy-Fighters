using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("�ִϸ��̼� ����")]
    public Animator animator;
    public string[] cheerAnimations = { "Cheer1", "Cheer2", "Cheer3" };

    [Header("���� ����")]
    public Color[] availableColors = {
        Color.red, Color.blue, Color.green, Color.yellow,
        Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f)
    };

    [Header("�ִϸ��̼� Ÿ�̹�")]
    public float minAnimationInterval = 2f;
    public float maxAnimationInterval = 8f;

    [Header("����ȭ ����")]
    public int playerIndex = 0; // Inspector���� ���� �ٸ��� ����

    private Renderer[] childRenderers;
    private Material[] originalMaterials;
    private System.Random syncedRandom;

    void Start()
    {
        // ������ �õ� ��� - ��� Ŭ���̾�Ʈ���� ������ ���
        InitializeSyncedRandom();

        if (animator == null)
            animator = GetComponent<Animator>();

        FindChildRenderers();
        ApplyDeterministicColors();
        StartDeterministicCheerAnimation();
    }

    void InitializeSyncedRandom()
    {
        // playerIndex�� ��ġ�� ������� ������ �õ� ����
        Vector3 pos = transform.position;
        int seed = playerIndex * 1000 +
                  Mathf.RoundToInt(pos.x * 100) +
                  Mathf.RoundToInt(pos.z * 100);

        syncedRandom = new System.Random(seed);
    }

    void FindChildRenderers()
    {
        childRenderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[childRenderers.Length];

        for (int i = 0; i < childRenderers.Length; i++)
        {
            if (childRenderers[i].material != null)
            {
                originalMaterials[i] = new Material(childRenderers[i].material);
            }
        }
    }

    void ApplyDeterministicColors()
    {
        if (childRenderers == null || availableColors.Length == 0) return;

        // ����ȭ�� �������� ���� ����
        Color selectedColor = availableColors[syncedRandom.Next(0, availableColors.Length)];

        foreach (Renderer renderer in childRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                Material materialInstance = new Material(renderer.material);
                materialInstance.color = selectedColor;
                renderer.material = materialInstance;

                if (materialInstance.HasProperty("_BaseColor"))
                    materialInstance.SetColor("_BaseColor", selectedColor);
                if (materialInstance.HasProperty("_Color"))
                    materialInstance.SetColor("_Color", selectedColor);
                if (materialInstance.HasProperty("_MainColor"))
                    materialInstance.SetColor("_MainColor", selectedColor);
            }
        }
    }

    void StartDeterministicCheerAnimation()
    {
        if (animator == null || cheerAnimations.Length == 0) return;

        PlayDeterministicCheerAnimation();

        // ����ȭ�� �������� ���� �ִϸ��̼� �ð� ����
        float nextAnimationTime = Mathf.Lerp(minAnimationInterval, maxAnimationInterval,
                                           (float)syncedRandom.NextDouble());
        Invoke(nameof(StartDeterministicCheerAnimation), nextAnimationTime);
    }

    void PlayDeterministicCheerAnimation()
    {
        if (animator == null || cheerAnimations.Length == 0) return;

        // ����ȭ�� �������� �ִϸ��̼� ����
        string randomAnimation = cheerAnimations[syncedRandom.Next(0, cheerAnimations.Length)];
        animator.SetTrigger(randomAnimation);
    }

    [ContextMenu("���� ���� �ִϸ��̼� ����")]
    public void TriggerRandomCheer()
    {
        PlayDeterministicCheerAnimation();
    }

    [ContextMenu("���� ���� ����")]
    public void TriggerRandomColor()
    {
        ApplyDeterministicColors();
    }

    [ContextMenu("���� ���� ����")]
    public void RestoreOriginalColors()
    {
        if (childRenderers == null || originalMaterials == null) return;

        for (int i = 0; i < childRenderers.Length; i++)
        {
            if (childRenderers[i] != null && originalMaterials[i] != null)
            {
                childRenderers[i].material = originalMaterials[i];
            }
        }
    }

    void OnDestroy()
    {
        CancelInvoke();
    }
}