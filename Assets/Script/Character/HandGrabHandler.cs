using UnityEngine;

public class HandGrabHandler : MonoBehaviour
{
    [SerializeField] Animator anim;
    FixedJoint fixedJoint;
    Rigidbody rb;

    NetworkPlayer networkPlayer;
    AIPlayer aiPlayer;
    LobbyPlayer lobbyPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.solverIterations = 255;

        networkPlayer = transform.root.GetComponent<NetworkPlayer>();
        aiPlayer = transform.root.GetComponent<AIPlayer>();
        lobbyPlayer = transform.root.GetComponent<LobbyPlayer>();

        if (networkPlayer == null && aiPlayer == null && lobbyPlayer == null)
        {
            Debug.LogError($"HandGrabHandler: No player component found on {transform.root.name}");
        }
    }

    public void UpdateState()
    {
        if (networkPlayer == null && aiPlayer == null && lobbyPlayer == null)
        {
            Debug.LogWarning($"HandGrabHandler: No valid player controller found on {gameObject.name}");
            return;
        }

        bool isGrabingActive = false;
        Transform playerTransform = null;

        if (networkPlayer != null)
        {
            isGrabingActive = networkPlayer.IsGrabingActive;
            playerTransform = networkPlayer.transform;
        }
        else if (aiPlayer != null)
        {
            isGrabingActive = aiPlayer.IsGrabingActive;
            playerTransform = aiPlayer.transform;
        }
        else if (lobbyPlayer != null)
        {
            isGrabingActive = lobbyPlayer.IsGrabingActive; // 수정: IsGrabPressed → IsGrabingActive
            playerTransform = lobbyPlayer.transform;
        }

        if (isGrabingActive)
        {
            if (anim != null)
                anim.SetBool("isGrabbing", true);
        }
        else
        {
            if (fixedJoint != null)
            {
                if (fixedJoint.connectedBody != null)
                {
                    float forceAmountMultiplier = 0.1f;

                    // 연결된 오브젝트 타입 확인
                    var connectedNetworkPlayer = fixedJoint.connectedBody.transform.root.GetComponent<NetworkPlayer>();
                    var connectedAIPlayer = fixedJoint.connectedBody.transform.root.GetComponent<AIPlayer>();
                    var connectedLobbyPlayer = fixedJoint.connectedBody.transform.root.GetComponent<LobbyPlayer>();

                    if (connectedNetworkPlayer != null)
                    {
                        forceAmountMultiplier = connectedNetworkPlayer.IsActiveRagdoll ? 10f : 15f;
                    }
                    else if (connectedAIPlayer != null)
                    {
                        forceAmountMultiplier = connectedAIPlayer.IsActiveRagdoll ? 10f : 15f;
                    }
                    else if (connectedLobbyPlayer != null)
                    {
                        forceAmountMultiplier = connectedLobbyPlayer.IsActiveRagdoll ? 10f : 15f;
                    }

                    Vector3 forceDirection = playerTransform.forward + Vector3.up * 0.25f;
                    fixedJoint.connectedBody.AddForce(forceDirection * forceAmountMultiplier, ForceMode.Impulse);
                }
                Destroy(fixedJoint);
            }

            if (anim != null)
            {
                anim.SetBool("isCarrying", false);
                anim.SetBool("isGrabbing", false);
            }
        }
    }

    bool TryCarryObject(Collision collision)
    {
        // 어느 플레이어 타입을 사용할지 결정
        bool hasStateAuthority = false;
        bool isActiveRagdoll = false;
        bool isGrabingActive = false;
        Transform playerTransform = null;

        if (networkPlayer != null)
        {
            hasStateAuthority = networkPlayer.Object.HasStateAuthority;
            isActiveRagdoll = networkPlayer.IsActiveRagdoll;
            isGrabingActive = networkPlayer.IsGrabingActive;
            playerTransform = networkPlayer.transform;
        }
        else if (aiPlayer != null)
        {
            hasStateAuthority = aiPlayer.Object.HasStateAuthority;
            isActiveRagdoll = aiPlayer.IsActiveRagdoll;
            isGrabingActive = aiPlayer.IsGrabingActive;
            playerTransform = aiPlayer.transform;
        }
        else if (lobbyPlayer != null)
        {
            hasStateAuthority = true; // 로비 플레이어는 항상 권한을 가짐
            isActiveRagdoll = lobbyPlayer.IsActiveRagdoll;
            isGrabingActive = lobbyPlayer.IsGrabingActive;
            playerTransform = lobbyPlayer.transform;
        }
        else
        {
            return false; // 모두 없으면 실패
        }

        if (!hasStateAuthority)
            return false;
        if (!isActiveRagdoll)
            return false;
        if (!isGrabingActive)
            return false;
        if (fixedJoint != null)
            return false;
        if (collision.transform.root == playerTransform)
            return false;
        if (!collision.collider.TryGetComponent(out Rigidbody otherObjectRigidbody))
            return false;

        fixedJoint = transform.gameObject.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = otherObjectRigidbody;
        fixedJoint.autoConfigureConnectedAnchor = false;
        fixedJoint.connectedAnchor = collision.transform.InverseTransformPoint(collision.GetContact(0).point);

        if (anim != null)
            anim.SetBool("isCarrying", true);

        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCarryObject(collision);
    }
}