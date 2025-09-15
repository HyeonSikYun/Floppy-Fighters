using Fusion;
using UnityEngine;

public class AISpawner : NetworkBehaviour
{
    [SerializeField] NetworkPrefabRef aiPlayerPrefab; // NetworkPrefabRef ���
    [SerializeField] int maxAICount = 3;
    [SerializeField] float spawnRadius = 5f;
    [SerializeField] bool spawnOnStart = true;

    private int currentAICount = 0;

    public override void Spawned()
    {
        if (Object.HasStateAuthority && spawnOnStart)
        {
            // 1�� �Ŀ� AI�� ���� (��Ʈ��ũ ����ȭ ���)
            Invoke(nameof(SpawnAIPlayers), 1f);
        }
    }

    [ContextMenu("Spawn AI Players")]
    public void SpawnAIPlayers()
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < maxAICount; i++)
        {
            if (currentAICount >= maxAICount) break;

            Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
            spawnPos.y = transform.position.y + 2f;

            // AI ���� �� ������ InputAuthority�� �������� ����
            NetworkObject aiObject = Runner.Spawn(aiPlayerPrefab, spawnPos, Quaternion.identity,
                                                  Runner.LocalPlayer, // InputAuthority�� ���� �÷��̾�� ����
                                                  null);

            if (aiObject != null)
            {
                currentAICount++;
                Utils.DebugLog($"AI Spawned {currentAICount}/{maxAICount} at {spawnPos}");
            }
        }
    }

    // AI�� ���ŵ� �� ȣ��
    public void OnAIDespawned()
    {
        currentAICount--;
        Utils.DebugLog($"AI Despawned. Remaining: {currentAICount}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 3, $"AI: {currentAICount}/{maxAICount}");
#endif
    }
}