using UnityEngine;

public class DetectColision : MonoBehaviour
{
    // 세 가지 타입 모두 저장
    NetworkPlayer networkPlayer;
    AIPlayer aiPlayer;
    LobbyPlayer lobbyPlayer; // ?? 추가

    Rigidbody hitRb;
    ContactPoint[] contactPoints = new ContactPoint[5];

    private void Awake()
    {
        // 세 컴포넌트 모두 확인
        networkPlayer = GetComponentInParent<NetworkPlayer>();
        aiPlayer = GetComponentInParent<AIPlayer>();
        lobbyPlayer = GetComponentInParent<LobbyPlayer>(); // ?? 추가
        hitRb = GetComponent<Rigidbody>();

        if (networkPlayer == null && aiPlayer == null && lobbyPlayer == null)
        {
            Debug.LogError($"DetectColision: No player component found in parent of {gameObject.name}");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // --- 플레이어 타입 판별 ---
        bool hasStateAuthority = true; // 로비는 네트워크 X → 그냥 true
        bool isActiveRagdoll = false;
        Transform playerTransform = null;

        if (networkPlayer != null)
        {
            hasStateAuthority = networkPlayer.Object.HasStateAuthority;
            isActiveRagdoll = networkPlayer.IsActiveRagdoll;
            playerTransform = networkPlayer.transform;
        }
        else if (aiPlayer != null)
        {
            hasStateAuthority = aiPlayer.Object.HasStateAuthority;
            isActiveRagdoll = aiPlayer.IsActiveRagdoll;
            playerTransform = aiPlayer.transform;
        }
        else if (lobbyPlayer != null)
        {
            // ?? 로비는 네트워크 개념 없으니까 authority 무조건 true
            hasStateAuthority = true;
            isActiveRagdoll = lobbyPlayer.IsActiveRagdoll;
            playerTransform = lobbyPlayer.transform;
        }
        else
        {
            return;
        }

        // --- 공통 처리 ---
        if (!hasStateAuthority)
            return;
        if (!isActiveRagdoll)
            return;
        if (!collision.collider.CompareTag("CauseDamage"))
            return;
        if (collision.collider.transform.root == playerTransform)
            return;

        int numberOfContacts = collision.GetContacts(contactPoints);
        for (int i = 0; i < numberOfContacts; i++)
        {
            ContactPoint contactPoint = contactPoints[i];
            Vector3 contactImpulse = contactPoint.impulse / Time.fixedDeltaTime;
            if (contactImpulse.magnitude < 15)
                continue;

            // 타입에 맞게 호출
            if (networkPlayer != null)
            {
                networkPlayer.OnPlayerBodyPartHit();
            }
            else if (aiPlayer != null)
            {
                aiPlayer.OnPlayerBodyPartHit();
            }
            else if (lobbyPlayer != null)
            {
                lobbyPlayer.MakeRagdoll(); // ?? LobbyPlayer 전용 처리
            }

            Vector3 forceDirection = (contactImpulse + Vector3.up) * 0.5f;
            forceDirection = Vector3.ClampMagnitude(forceDirection, 30);

            if (hitRb != null)
                hitRb.AddForce(forceDirection, ForceMode.Impulse);
        }
    }
}
