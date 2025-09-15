using UnityEngine;

public class DetectColision : MonoBehaviour
{
    // �� ���� Ÿ�� ��� ����
    NetworkPlayer networkPlayer;
    AIPlayer aiPlayer;
    LobbyPlayer lobbyPlayer; // ?? �߰�

    Rigidbody hitRb;
    ContactPoint[] contactPoints = new ContactPoint[5];

    private void Awake()
    {
        // �� ������Ʈ ��� Ȯ��
        networkPlayer = GetComponentInParent<NetworkPlayer>();
        aiPlayer = GetComponentInParent<AIPlayer>();
        lobbyPlayer = GetComponentInParent<LobbyPlayer>(); // ?? �߰�
        hitRb = GetComponent<Rigidbody>();

        if (networkPlayer == null && aiPlayer == null && lobbyPlayer == null)
        {
            Debug.LogError($"DetectColision: No player component found in parent of {gameObject.name}");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // --- �÷��̾� Ÿ�� �Ǻ� ---
        bool hasStateAuthority = true; // �κ�� ��Ʈ��ũ X �� �׳� true
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
            // ?? �κ�� ��Ʈ��ũ ���� �����ϱ� authority ������ true
            hasStateAuthority = true;
            isActiveRagdoll = lobbyPlayer.IsActiveRagdoll;
            playerTransform = lobbyPlayer.transform;
        }
        else
        {
            return;
        }

        // --- ���� ó�� ---
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

            // Ÿ�Կ� �°� ȣ��
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
                lobbyPlayer.MakeRagdoll(); // ?? LobbyPlayer ���� ó��
            }

            Vector3 forceDirection = (contactImpulse + Vector3.up) * 0.5f;
            forceDirection = Vector3.ClampMagnitude(forceDirection, 30);

            if (hitRb != null)
                hitRb.AddForce(forceDirection, ForceMode.Impulse);
        }
    }
}
