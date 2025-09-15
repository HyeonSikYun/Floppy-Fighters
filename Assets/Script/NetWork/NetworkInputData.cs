using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput;
    public bool isJumpPressed;
    public bool isRevivePressed;
    public bool isGrabPressed;
    public bool isPunchPressed;

    // �߰�: ī�޶��� Y ȸ�� �� (�÷��̾ �ٶ󺸴� ����)
    public float cameraYRotation;
}
