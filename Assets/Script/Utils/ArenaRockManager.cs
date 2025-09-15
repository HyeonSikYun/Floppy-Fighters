using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Fusion;

public class ArenaRockManager : NetworkBehaviour
{
    [Header("바위 설정")]
    public Transform[] rockObjects;
    public LayerMask rockLayerMask = -1;

    [Header("타이밍 설정")]
    public float warningTime = 3f;
    public float dropInterval = 5f;

    [Header("색상 설정")]
    public Color warningColor = Color.red;
    public Color normalColor = Color.white;

    [Header("떨어짐 설정")]
    public float torqueForce = 15f;
    public float destroyDelay = 5f;

    [Header("머티리얼 설정")]
    public Material warningMaterial;

    [Header("가장자리 판정 설정")]
    public float cubeSize = 1f;
    public bool include3D = false;

    // 네트워크 변수들
    [Networked] public int ActiveRockCount { get; set; }
    [Networked] public TickTimer NextDropTimer { get; set; }

    private List<GameObject> activeRocks;
    private Dictionary<GameObject, Material> originalMaterials;
    private Dictionary<GameObject, Renderer> rockRenderers;
    private Dictionary<GameObject, int> rockToIndex; // 바위 → 원본 인덱스 매핑
    private bool isGameActive = true;

    public override void Spawned()
    {
        InitializeRocks();

        if (Object.HasStateAuthority)
        {
            ActiveRockCount = activeRocks.Count;
            NextDropTimer = TickTimer.CreateFromSeconds(Runner, dropInterval);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // 마스터 클라이언트(Host)에서만 바위 떨어뜨리기 결정
        if (!Object.HasStateAuthority) return;

        if (NextDropTimer.Expired(Runner) && isGameActive && ActiveRockCount > 1)
        {
            List<GameObject> edgeRocks = FindEdgeRocks();
            if (edgeRocks.Count == 0)
                edgeRocks = new List<GameObject>(activeRocks.Where(r => r != null));

            if (edgeRocks.Count > 0)
            {
                GameObject selectedRock = edgeRocks[Random.Range(0, edgeRocks.Count)];
                int originalIndex = rockToIndex[selectedRock];
                RPC_ShowWarningAndDrop(originalIndex);
            }

            NextDropTimer = TickTimer.CreateFromSeconds(Runner, dropInterval);
        }
    }

    void InitializeRocks()
    {
        activeRocks = new List<GameObject>();
        originalMaterials = new Dictionary<GameObject, Material>();
        rockRenderers = new Dictionary<GameObject, Renderer>();
        rockToIndex = new Dictionary<GameObject, int>();

        if (rockObjects == null || rockObjects.Length == 0)
            FindAllRocks();

        for (int i = 0; i < rockObjects.Length; i++)
        {
            Transform rockTransform = rockObjects[i];
            if (rockTransform != null)
            {
                GameObject rock = rockTransform.gameObject;
                activeRocks.Add(rock);
                rockToIndex[rock] = i; // 원본 인덱스 저장

                Renderer[] renderers = rock.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Renderer mainRenderer = GetMainRenderer(renderers);
                    rockRenderers[rock] = mainRenderer;

                    Material originalInstance = new Material(mainRenderer.material);
                    originalMaterials[rock] = originalInstance;
                    mainRenderer.material = originalInstance;
                }

                // 일반 Rigidbody만 사용 (NetworkRigidbody 제거)
                Rigidbody rb = rock.GetComponent<Rigidbody>();
                if (rb == null) rb = rock.AddComponent<Rigidbody>();

                rb.isKinematic = true;
                rb.mass = 5f;
                rb.useGravity = false;
            }
        }

        Debug.Log($"총 {activeRocks.Count}개의 바위가 초기화되었습니다.");
    }

    Renderer GetMainRenderer(Renderer[] renderers)
    {
        if (renderers.Length == 1) return renderers[0];

        Renderer mainRenderer = renderers[0];
        float maxSize = GetRendererSize(mainRenderer);

        for (int i = 1; i < renderers.Length; i++)
        {
            float size = GetRendererSize(renderers[i]);
            if (size > maxSize)
            {
                maxSize = size;
                mainRenderer = renderers[i];
            }
        }
        return mainRenderer;
    }

    float GetRendererSize(Renderer renderer)
    {
        Bounds bounds = renderer.bounds;
        return bounds.size.x * bounds.size.y * bounds.size.z;
    }

    void FindAllRocks()
    {
        GameObject[] foundRocks = GameObject.FindGameObjectsWithTag("Rock");

        if (foundRocks.Length == 0)
        {
            foundRocks = FindObjectsOfType<GameObject>()
                .Where(obj => obj.name.ToLower().Contains("rock") || obj.name.ToLower().Contains("cube"))
                .ToArray();
        }

        rockObjects = foundRocks.Select(rock => rock.transform).ToArray();
    }

    List<GameObject> FindEdgeRocks()
    {
        List<GameObject> edgeRocks = new List<GameObject>();

        foreach (GameObject rock in activeRocks)
        {
            if (rock == null) continue;
            int blockedSides = GetBlockedSidesCount(rock);

            if (blockedSides <= 2) // 두 면 이하 → 가장자리
                edgeRocks.Add(rock);
        }

        return edgeRocks;
    }

    int GetBlockedSidesCount(GameObject rock)
    {
        Vector3 rockPos = rock.transform.position;

        List<Vector3> directions = new List<Vector3>
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back
        };

        if (include3D)
        {
            directions.Add(Vector3.up);
            directions.Add(Vector3.down);
        }

        int blockedSides = 0;

        foreach (Vector3 direction in directions)
        {
            Vector3 checkPos = rockPos + direction * (cubeSize + 0.1f);

            foreach (GameObject otherRock in activeRocks)
            {
                if (otherRock == null || otherRock == rock) continue;

                float distance = Vector3.Distance(checkPos, otherRock.transform.position);
                if (distance < cubeSize * 0.7f)
                {
                    blockedSides++;
                    break;
                }
            }
        }

        return blockedSides;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ShowWarningAndDrop(int originalRockIndex)
    {
        if (originalRockIndex < 0 || originalRockIndex >= rockObjects.Length) return;

        GameObject rockToDrop = rockObjects[originalRockIndex].gameObject;
        if (rockToDrop != null && activeRocks.Contains(rockToDrop))
        {
            StartCoroutine(ShowWarningAndDropSingle(rockToDrop, originalRockIndex));
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_DropRockPhysics(int originalRockIndex, Vector3 torqueVector)
    {
        if (originalRockIndex < 0 || originalRockIndex >= rockObjects.Length) return;

        GameObject rock = rockObjects[originalRockIndex].gameObject;
        if (rock != null)
        {
            Rigidbody rb = rock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddTorque(torqueVector, ForceMode.VelocityChange);

                Debug.Log($"바위 {originalRockIndex} 떨어뜨림! 토크: {torqueVector}");
            }
        }
    }

    IEnumerator ShowWarningAndDropSingle(GameObject rockToDrop, int originalRockIndex)
    {
        if (rockToDrop == null) yield break;

        // 원본 머티리얼 가져오기
        if (!rockRenderers.ContainsKey(rockToDrop) || !originalMaterials.ContainsKey(rockToDrop))
            yield break;

        Renderer renderer = rockRenderers[rockToDrop];
        Material originalMat = originalMaterials[rockToDrop];

        // 경고용 머티리얼 복사
        Material warningMat = new Material(originalMat);
        renderer.material = warningMat;

        // 색상 서서히 경고색으로 바꾸기
        yield return StartCoroutine(FadeInWarningSingle(warningMat, originalMat.color, warningColor, warningTime));

        // 실제로 떨어뜨리기 - 마스터에서만 물리 적용하고 RPC로 전달
        if (Object.HasStateAuthority)
        {
            // 동일한 시드로 토크 생성
            Random.State oldState = Random.state;
            Random.InitState(originalRockIndex + Runner.Tick);
            Vector3 torque = Random.insideUnitSphere * torqueForce;
            Random.state = oldState;

            // 모든 클라이언트에서 동일한 물리 적용
            RPC_DropRockPhysics(originalRockIndex, torque);

            ActiveRockCount--;
            Debug.Log($"남은 바위: {ActiveRockCount}");
        }

        // activeRocks 리스트에서 제거
        if (activeRocks.Contains(rockToDrop))
        {
            activeRocks.Remove(rockToDrop);
        }

        // 일정 시간 후 파괴
        if (Object.HasStateAuthority)
        {
            RPC_DestroyRockAfterDelay(originalRockIndex, destroyDelay);
        }
    }

    IEnumerator FadeInWarningSingle(Material mat, Color startColor, Color targetColor, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            mat.color = Color.Lerp(startColor, targetColor, t);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.Lerp(Color.black, targetColor, t * 0.5f));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        mat.color = targetColor;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_DestroyRockAfterDelay(int originalRockIndex, float delay)
    {
        StartCoroutine(DestroyRockCoroutine(originalRockIndex, delay));
    }

    IEnumerator DestroyRockCoroutine(int originalRockIndex, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (originalRockIndex < rockObjects.Length && rockObjects[originalRockIndex] != null)
        {
            GameObject rock = rockObjects[originalRockIndex].gameObject;

            if (originalMaterials.ContainsKey(rock))
                originalMaterials.Remove(rock);
            if (rockRenderers.ContainsKey(rock))
                rockRenderers.Remove(rock);
            if (rockToIndex.ContainsKey(rock))
                rockToIndex.Remove(rock);

            Destroy(rock);
            Debug.Log($"바위 {originalRockIndex} 파괴됨");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StopGame()
    {
        isGameActive = false;
        StopAllCoroutines();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RestartGame()
    {
        StopAllCoroutines();
        isGameActive = true;

        if (Object.HasStateAuthority)
        {
            NextDropTimer = TickTimer.CreateFromSeconds(Runner, dropInterval);
        }
    }

    public void StopGame()
    {
        if (Object.HasStateAuthority)
        {
            RPC_StopGame();
        }
    }

    public void RestartGame()
    {
        if (Object.HasStateAuthority)
        {
            RPC_RestartGame();
        }
    }
}