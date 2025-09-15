using Fusion;
using UnityEngine;

public class AISpawner : NetworkBehaviour
{
    [SerializeField] NetworkPrefabRef aiPlayerPrefab; // NetworkPrefabRef 사용
    [SerializeField] int maxAICount = 3;
    [SerializeField] float spawnRadius = 5f;
    [SerializeField] bool spawnOnStart = true;

    private int currentAICount = 0;

    public override void Spawned()
    {
        if (Object.HasStateAuthority && spawnOnStart)
        {
            // 1초 후에 AI들 스폰 (네트워크 안정화 대기)
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

            // AI 스폰 시 서버가 InputAuthority도 가지도록 설정
            NetworkObject aiObject = Runner.Spawn(aiPlayerPrefab, spawnPos, Quaternion.identity,
                                                  Runner.LocalPlayer, // InputAuthority를 서버 플레이어로 설정
                                                  null);

            if (aiObject != null)
            {
                currentAICount++;
                Utils.DebugLog($"AI Spawned {currentAICount}/{maxAICount} at {spawnPos}");
            }
        }
    }

    // AI가 제거될 때 호출
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