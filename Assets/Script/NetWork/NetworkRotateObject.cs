using UnityEngine;
using Fusion;

public class NetworkRotateObject : NetworkBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] Vector3 rotationAmount;
    [SerializeField] float smoothSpeed = 10f; // ���� �ӵ�

    [Networked] public Quaternion NetworkedRotation { get; set; }

    // ���� ������
    private Quaternion visualRotation;

    public override void Spawned()
    {
        NetworkedRotation = transform.rotation;
        visualRotation = transform.rotation;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // �� �ε巯�� ȸ�� ���
            float rotationSpeed = rotationAmount.magnitude;
            Vector3 rotateBy = NetworkedRotation.eulerAngles + rotationAmount * Runner.DeltaTime;
            NetworkedRotation = Quaternion.Euler(rotateBy);
        }
    }

    public override void Render()
    {
        // �ε巯�� �ð��� ����
        if (Object.HasStateAuthority)
        {
            // Host�� �ٷ� ����
            visualRotation = NetworkedRotation;
        }
        else
        {
            // Ŭ���̾�Ʈ�� �����Ͽ� �ε巴��
            visualRotation = Quaternion.Lerp(visualRotation, NetworkedRotation,
                smoothSpeed * Time.deltaTime);
        }

        // ���� ����
        if (rb != null)
            rb.MoveRotation(visualRotation);
        else
            transform.rotation = visualRotation;
    }
}