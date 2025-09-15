using UnityEngine;

public class HandPunchHandler : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] float punchForce = 500f;
    [SerializeField] float punchRadius = 0.5f;

    // 두 타입 모두 저장
    NetworkPlayer networkPlayer;
    AIPlayer aiPlayer;
    LobbyPlayer lobbyPlayer;
    Rigidbody rb;

    bool isPunching = false;
    float punchDuration = 0.3f;
    float punchStartTime = 0f;

    private void Awake()
    {
        networkPlayer = transform.root.GetComponent<NetworkPlayer>();
        aiPlayer = transform.root.GetComponent<AIPlayer>();
        lobbyPlayer = transform.root.GetComponent<LobbyPlayer>();
        rb = GetComponent<Rigidbody>();

        if (networkPlayer == null && aiPlayer == null && lobbyPlayer == null)
        {
            Debug.LogError($"HandPunchHandler: No player component found on {transform.root.name}");
        }
    }

    public void UpdateState()
    {
        if (networkPlayer == null && aiPlayer == null && lobbyPlayer == null)
        {
            Debug.LogWarning($"HandPunchHandler: No valid player controller found on {gameObject.name}");
            return;
        }

        bool isPunchActive = false;

        if (networkPlayer != null)
        {
            isPunchActive = networkPlayer.IsPunchActive;
        }
        else if (aiPlayer != null)
        {
            isPunchActive = aiPlayer.IsPunchActive;
        }
        else if (lobbyPlayer != null)
        {
            isPunchActive = lobbyPlayer.IsPunchActive; // LobbyPlayer에서 공개한 프로퍼티 사용
        }

        if (isPunchActive && !isPunching)
            StartPunch();

        if (isPunching && Time.time - punchStartTime > punchDuration)
            EndPunch();
    }

    void StartPunch()
    {
        isPunching = true;
        punchStartTime = Time.time;

        if (anim != null)
            anim.SetTrigger("HitPunch"); // 펀치 애니메이션 트리거

        // 펀치 범위 내 오브젝트 검사
        CheckPunchHit();
    }

    void EndPunch()
    {
        isPunching = false;
    }

    void CheckPunchHit()
    {
        // 손 위치에서 구형 범위로 충돌 검사
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, punchRadius);

        // 현재 플레이어의 Transform 가져오기
        Transform playerTransform = null;
        if (networkPlayer != null)
        {
            playerTransform = networkPlayer.transform;
        }
        else if (aiPlayer != null)
        {
            playerTransform = aiPlayer.transform;
        }

        if (playerTransform == null)
            return;

        foreach (Collider hitCollider in hitColliders)
        {
            // 자기 자신은 제외
            if (hitCollider.transform.root == playerTransform)
                continue;

            // Rigidbody가 있는 오브젝트에 힘 가하기
            if (hitCollider.TryGetComponent(out Rigidbody targetRb))
            {
                Vector3 punchDirection = (hitCollider.transform.position - transform.position).normalized;
                targetRb.AddForce(punchDirection * punchForce, ForceMode.Impulse);

                // 다른 플레이어를 펀치한 경우 ragdoll 상태로 만들기
                var hitNetworkPlayer = hitCollider.transform.root.GetComponent<NetworkPlayer>();
                var hitAIPlayer = hitCollider.transform.root.GetComponent<AIPlayer>();

                if (hitNetworkPlayer != null)
                {
                    hitNetworkPlayer.OnPlayerBodyPartHit();
                }
                else if (hitAIPlayer != null)
                {
                    hitAIPlayer.OnPlayerBodyPartHit();
                }
            }
        }
    }

    // 디버그용 - 펀치 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, punchRadius);
    }
}