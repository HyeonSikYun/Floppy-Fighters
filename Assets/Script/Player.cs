using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("애니메이션 설정")]
    public Animator animator;
    public string[] cheerAnimations = { "Cheer1", "Cheer2", "Cheer3" };

    [Header("색상 설정")]
    public Color[] availableColors = {
        Color.red, Color.blue, Color.green, Color.yellow,
        Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f)
    };

    [Header("애니메이션 타이밍")]
    public float minAnimationInterval = 2f;
    public float maxAnimationInterval = 8f;

    [Header("동기화 설정")]
    public int playerIndex = 0; // Inspector에서 각각 다르게 설정

    private Renderer[] childRenderers;
    private Material[] originalMaterials;
    private System.Random syncedRandom;

    void Start()
    {
        // 고정된 시드 사용 - 모든 클라이언트에서 동일한 결과
        InitializeSyncedRandom();

        if (animator == null)
            animator = GetComponent<Animator>();

        FindChildRenderers();
        ApplyDeterministicColors();
        StartDeterministicCheerAnimation();
    }

    void InitializeSyncedRandom()
    {
        // playerIndex와 위치를 기반으로 고정된 시드 생성
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

        // 동기화된 랜덤으로 색상 선택
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

        // 동기화된 랜덤으로 다음 애니메이션 시간 결정
        float nextAnimationTime = Mathf.Lerp(minAnimationInterval, maxAnimationInterval,
                                           (float)syncedRandom.NextDouble());
        Invoke(nameof(StartDeterministicCheerAnimation), nextAnimationTime);
    }

    void PlayDeterministicCheerAnimation()
    {
        if (animator == null || cheerAnimations.Length == 0) return;

        // 동기화된 랜덤으로 애니메이션 선택
        string randomAnimation = cheerAnimations[syncedRandom.Next(0, cheerAnimations.Length)];
        animator.SetTrigger(randomAnimation);
    }

    [ContextMenu("랜덤 응원 애니메이션 실행")]
    public void TriggerRandomCheer()
    {
        PlayDeterministicCheerAnimation();
    }

    [ContextMenu("랜덤 색상 적용")]
    public void TriggerRandomColor()
    {
        ApplyDeterministicColors();
    }

    [ContextMenu("원본 색상 복원")]
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