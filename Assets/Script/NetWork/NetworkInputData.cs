using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput;
    public bool isJumpPressed;
    public bool isRevivePressed;
    public bool isGrabPressed;
    public bool isPunchPressed;

    // 추가: 카메라의 Y 회전 값 (플레이어가 바라보는 방향)
    public float cameraYRotation;
}
