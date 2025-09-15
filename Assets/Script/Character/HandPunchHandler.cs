using UnityEngine;

public class HandPunchHandler : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] float punchForce = 500f;
    [SerializeField] float punchRadius = 0.5f;

    // �� Ÿ�� ��� ����
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
            isPunchActive = lobbyPlayer.IsPunchActive; // LobbyPlayer���� ������ ������Ƽ ���
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
            anim.SetTrigger("HitPunch"); // ��ġ �ִϸ��̼� Ʈ����

        // ��ġ ���� �� ������Ʈ �˻�
        CheckPunchHit();
    }

    void EndPunch()
    {
        isPunching = false;
    }

    void CheckPunchHit()
    {
        // �� ��ġ���� ���� ������ �浹 �˻�
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, punchRadius);

        // ���� �÷��̾��� Transform ��������
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
            // �ڱ� �ڽ��� ����
            if (hitCollider.transform.root == playerTransform)
                continue;

            // Rigidbody�� �ִ� ������Ʈ�� �� ���ϱ�
            if (hitCollider.TryGetComponent(out Rigidbody targetRb))
            {
                Vector3 punchDirection = (hitCollider.transform.position - transform.position).normalized;
                targetRb.AddForce(punchDirection * punchForce, ForceMode.Impulse);

                // �ٸ� �÷��̾ ��ġ�� ��� ragdoll ���·� �����
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

    // ����׿� - ��ġ ���� �ð�ȭ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, punchRadius);
    }
}