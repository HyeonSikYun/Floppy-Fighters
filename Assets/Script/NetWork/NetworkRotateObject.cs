using UnityEngine;
using Fusion;

public class NetworkRotateObject : NetworkBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] Vector3 rotationAmount;
    [SerializeField] float smoothSpeed = 10f; // 보간 속도

    [Networked] public Quaternion NetworkedRotation { get; set; }

    // 로컬 보간용
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
            // 더 부드러운 회전 계산
            float rotationSpeed = rotationAmount.magnitude;
            Vector3 rotateBy = NetworkedRotation.eulerAngles + rotationAmount * Runner.DeltaTime;
            NetworkedRotation = Quaternion.Euler(rotateBy);
        }
    }

    public override void Render()
    {
        // 부드러운 시각적 보간
        if (Object.HasStateAuthority)
        {
            // Host는 바로 적용
            visualRotation = NetworkedRotation;
        }
        else
        {
            // 클라이언트는 보간하여 부드럽게
            visualRotation = Quaternion.Lerp(visualRotation, NetworkedRotation,
                smoothSpeed * Time.deltaTime);
        }

        // 최종 적용
        if (rb != null)
            rb.MoveRotation(visualRotation);
        else
            transform.rotation = visualRotation;
    }
}