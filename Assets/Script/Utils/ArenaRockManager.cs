using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Fusion;

public class ArenaRockManager : NetworkBehaviour
{
    [Header("���� ����")]
    public Transform[] rockObjects;
    public LayerMask rockLayerMask = -1;

    [Header("Ÿ�̹� ����")]
    public float warningTime = 3f;
    public float dropInterval = 5f;

    [Header("���� ����")]
    public Color warningColor = Color.red;
    public Color normalColor = Color.white;

    [Header("������ ����")]
    public float torqueForce = 15f;
    public float destroyDelay = 5f;

    [Header("��Ƽ���� ����")]
    public Material warningMaterial;

    [Header("�����ڸ� ���� ����")]
    public float cubeSize = 1f;
    public bool include3D = false;

    // ��Ʈ��ũ ������
    [Networked] public int ActiveRockCount { get; set; }
    [Networked] public TickTimer NextDropTimer { get; set; }

    private List<GameObject> activeRocks;
    private Dictionary<GameObject, Material> originalMaterials;
    private Dictionary<GameObject, Renderer> rockRenderers;
    private Dictionary<GameObject, int> rockToIndex; // ���� �� ���� �ε��� ����
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
        // ������ Ŭ���̾�Ʈ(Host)������ ���� ����߸��� ����
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
                rockToIndex[rock] = i; // ���� �ε��� ����

                Renderer[] renderers = rock.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Renderer mainRenderer = GetMainRenderer(renderers);
                    rockRenderers[rock] = mainRenderer;

                    Material originalInstance = new Material(mainRenderer.material);
                    originalMaterials[rock] = originalInstance;
                    mainRenderer.material = originalInstance;
                }

                // �Ϲ� Rigidbody�� ��� (NetworkRigidbody ����)
                Rigidbody rb = rock.GetComponent<Rigidbody>();
                if (rb == null) rb = rock.AddComponent<Rigidbody>();

                rb.isKinematic = true;
                rb.mass = 5f;
                rb.useGravity = false;
            }
        }

        Debug.Log($"�� {activeRocks.Count}���� ������ �ʱ�ȭ�Ǿ����ϴ�.");
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

            if (blockedSides <= 2) // �� �� ���� �� �����ڸ�
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

                Debug.Log($"���� {originalRockIndex} ����߸�! ��ũ: {torqueVector}");
            }
        }
    }

    IEnumerator ShowWarningAndDropSingle(GameObject rockToDrop, int originalRockIndex)
    {
        if (rockToDrop == null) yield break;

        // ���� ��Ƽ���� ��������
        if (!rockRenderers.ContainsKey(rockToDrop) || !originalMaterials.ContainsKey(rockToDrop))
            yield break;

        Renderer renderer = rockRenderers[rockToDrop];
        Material originalMat = originalMaterials[rockToDrop];

        // ���� ��Ƽ���� ����
        Material warningMat = new Material(originalMat);
        renderer.material = warningMat;

        // ���� ������ �������� �ٲٱ�
        yield return StartCoroutine(FadeInWarningSingle(warningMat, originalMat.color, warningColor, warningTime));

        // ������ ����߸��� - �����Ϳ����� ���� �����ϰ� RPC�� ����
        if (Object.HasStateAuthority)
        {
            // ������ �õ�� ��ũ ����
            Random.State oldState = Random.state;
            Random.InitState(originalRockIndex + Runner.Tick);
            Vector3 torque = Random.insideUnitSphere * torqueForce;
            Random.state = oldState;

            // ��� Ŭ���̾�Ʈ���� ������ ���� ����
            RPC_DropRockPhysics(originalRockIndex, torque);

            ActiveRockCount--;
            Debug.Log($"���� ����: {ActiveRockCount}");
        }

        // activeRocks ����Ʈ���� ����
        if (activeRocks.Contains(rockToDrop))
        {
            activeRocks.Remove(rockToDrop);
        }

        // ���� �ð� �� �ı�
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
            Debug.Log($"���� {originalRockIndex} �ı���");
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