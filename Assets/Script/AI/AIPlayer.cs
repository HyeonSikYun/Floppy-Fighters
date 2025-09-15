using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class AIPlayer : NetworkBehaviour, IPlayerLeft
{
    public static AIPlayer[] AllAIs = new AIPlayer[10];
    static int aiCount = 0;

    [SerializeField] Rigidbody rb;
    [SerializeField] NetworkRigidbody3D networkRigidbody3D;
    [SerializeField] ConfigurableJoint mainJoint;
    [SerializeField] Animator anim;

    [Header("AI Settings")]
    [SerializeField] float followDistance = 3f;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float detectionRange = 10f;
    [SerializeField] float jumpCooldown = 2f;
    [SerializeField] float punchCooldown = 1f;
    [SerializeField] float punchChance = 0.3f; // ��ġ�� Ȯ�� (30%)
    [SerializeField] float grabChance = 0.15f; // �׷��� Ȯ�� (15%)

    // AI ����
    NetworkPlayer targetPlayer;
    float lastJumpTime = 0f;
    float lastPunchTime = 0f;

    // ���� NetworkPlayer�� ������ ������
    Vector2 moveInputVector = Vector2.zero;
    bool isJumpButtonPressed = false;
    bool isReviveButtonPressed = false;
    bool isGrabButtonPressed = false;
    bool isPunchButtonPressed = false;

    float maxSpeed = 3f;
    bool isGrounded = false;
    bool isActiveRagdoll = true;
    public bool IsActiveRagdoll => isActiveRagdoll;
    bool isGrabingActive = false;
    public bool IsGrabingActive => isGrabingActive;
    bool isPunchActive = false;
    public bool IsPunchActive => isPunchActive;

    RaycastHit[] raycastHits = new RaycastHit[18];
    SyncPhysicsObject[] syncPhysicsObjects;
    HandGrabHandler[] handGrabHandlers;
    HandPunchHandler[] handPunchHandlers;

    [Networked, Capacity(10)] public NetworkArray<Quaternion> networkPhysicsSyncedRotations { get; }

    float startSlerpPositionSpring = 0.0f;
    float lastTimeBecameRagdol = 0;

    private void Awake()
    {
        syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
        handGrabHandlers = GetComponentsInChildren<HandGrabHandler>();
        handPunchHandlers = GetComponentsInChildren<HandPunchHandler>();
    }

    void Start()
    {
        startSlerpPositionSpring = mainJoint.slerpDrive.positionSpring;
    }

    void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        UpdateAIBehavior();
    }

    void UpdateAIBehavior()
    {
        // Ÿ�� �÷��̾� ã��
        if (targetPlayer == null)
        {
            FindTargetPlayer();
            return;
        }

        // Ÿ���� ��ȿ���� Ȯ��
        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy)
        {
            targetPlayer = null;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.transform.position);

        // ���� ���� ���� ������ ����ٴϱ�
        if (distanceToTarget <= detectionRange)
        {
            FollowTarget(distanceToTarget);

            // ������ ������ ��ġ�� �׷�
            if (distanceToTarget <= 2.5f)
            {
                DoRandomAction();
            }
        }
        else
        {
            StopMoving();
        }
    }

    void FindTargetPlayer()
    {
        // ���� �÷��̾� �켱
        if (NetworkPlayer.Local != null)
        {
            targetPlayer = NetworkPlayer.Local;
            return;
        }

        // ���� ����� �÷��̾� ã��
        NetworkPlayer[] allPlayers = FindObjectsOfType<NetworkPlayer>();
        float closestDistance = float.MaxValue;
        NetworkPlayer closestPlayer = null;

        foreach (NetworkPlayer player in allPlayers)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        targetPlayer = closestPlayer;
    }

    void FollowTarget(float distanceToTarget)
    {
        if (targetPlayer == null) return;

        Vector3 targetPos = targetPlayer.transform.position;
        Vector3 directionToTarget = (targetPos - transform.position).normalized;

        Utils.DebugLog($"AI {transform.name} following target. Distance: {distanceToTarget}, FollowDistance: {followDistance}");

        // �Ÿ��� �ָ� ���󰡱�
        if (distanceToTarget > followDistance)
        {
            moveInputVector = new Vector2(directionToTarget.x, directionToTarget.z);
            Utils.DebugLog($"AI {transform.name} moving with input: {moveInputVector}");

            // ���� ���̰� ������ ����
            if (targetPos.y > transform.position.y + 0.5f && isGrounded &&
                Runner.SimulationTime - lastJumpTime > jumpCooldown)
            {
                isJumpButtonPressed = true;
                lastJumpTime = Runner.SimulationTime;
                Utils.DebugLog($"AI {transform.name} jumping!");
            }
        }
        else
        {
            StopMoving();
            Utils.DebugLog($"AI {transform.name} close enough, stopping");
        }
    }

    void DoRandomAction()
    {
        // ��ġ
        if (Runner.SimulationTime - lastPunchTime > punchCooldown && Random.Range(0f, 1f) < punchChance)
        {
            isPunchButtonPressed = true;
            lastPunchTime = Runner.SimulationTime;
        }

        // �׷�
        if (Random.Range(0f, 1f) < grabChance)
        {
            isGrabButtonPressed = !isGrabButtonPressed;
        }
    }

    void StopMoving()
    {
        moveInputVector = Vector2.zero;
    }

    public override void FixedUpdateNetwork()
    {
        Vector3 localVelocifyVsForward = Vector3.zero;
        float localForwardVelocity = 0;

        if (Object.HasStateAuthority)
        {
            // Ground check
            isGrounded = false;
            int numberOfHits = Physics.SphereCastNonAlloc(rb.position, 0.1f, Vector3.down, raycastHits, 0.5f);

            for (int i = 0; i < numberOfHits; i++)
            {
                if (raycastHits[i].transform.root == transform) continue;
                isGrounded = true;
                break;
            }
            if (!isGrounded)
                rb.AddForce(Vector3.down * 10);

            localVelocifyVsForward = transform.forward * Vector3.Dot(transform.forward, rb.linearVelocity);
            localForwardVelocity = localVelocifyVsForward.magnitude;

            ProcessAIInput();
        }

        // �ִϸ��̼� & ����ȭ
        if (Object.HasStateAuthority)
        {
            anim.SetFloat("movementSpeed", localForwardVelocity * 0.4f);

            for (int i = 0; i < syncPhysicsObjects.Length; i++)
            {
                if (isActiveRagdoll)
                {
                    syncPhysicsObjects[i].UpdateJointFromAnimation();
                }
                networkPhysicsSyncedRotations.Set(i, syncPhysicsObjects[i].transform.localRotation);
            }

            // �������� ������
            if (transform.position.y < -10)
            {
                networkRigidbody3D.Teleport(Vector3.zero, Quaternion.identity);
                MakeActiveRagdoll();
            }

            // �ڵ鷯 ������Ʈ
            foreach (HandGrabHandler handGrabHandler in handGrabHandlers)
            {
                handGrabHandler.UpdateState();
            }

            foreach (HandPunchHandler handPunchHandler in handPunchHandlers)
            {
                handPunchHandler.UpdateState();
            }

            if (isPunchActive && Runner.SimulationTime - lastPunchTime > 0.2f)
            {
                isPunchActive = false;
            }
        }
    }

    void ProcessAIInput()
    {
        float inputMagnitude = moveInputVector.magnitude;

        if (isActiveRagdoll)
        {
            if (inputMagnitude != 0)
            {
                Vector3 moveDirection = new Vector3(moveInputVector.x, 0, moveInputVector.y).normalized;

                if (moveDirection != Vector3.zero)
                {
                    Quaternion desiredRot = Quaternion.LookRotation(moveDirection, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Runner.DeltaTime * 10f);
                    mainJoint.targetRotation = Quaternion.Inverse(transform.rotation);

                    Vector3 localVelocityForward = transform.forward * Vector3.Dot(transform.forward, rb.linearVelocity);
                    float localForwardVelocity = localVelocityForward.magnitude;

                    if (localForwardVelocity < maxSpeed)
                    {
                        rb.AddForce(moveDirection * inputMagnitude * moveSpeed * 10);
                    }
                }
            }

            // ����
            if (isGrounded && isJumpButtonPressed)
            {
                rb.AddForce(Vector3.up * 15, ForceMode.Impulse);
            }

            // ��ġ
            if (isPunchButtonPressed)
            {
                isPunchActive = true;
            }

            // �׷�
            isGrabingActive = isGrabButtonPressed;
        }
        else
        {
            // ���׵����� 3�� �� �Ͼ��
            if (Runner.SimulationTime - lastTimeBecameRagdol > 3)
            {
                MakeActiveRagdoll();
            }
        }

        // �Է� ����
        isJumpButtonPressed = false;
        isPunchButtonPressed = false;
    }

    public override void Render()
    {
        if (!Object.HasStateAuthority)
        {
            var interpolated = new NetworkBehaviourBufferInterpolator(this);
            for (int i = 0; i < syncPhysicsObjects.Length; i++)
            {
                syncPhysicsObjects[i].transform.localRotation =
                    Quaternion.Slerp(syncPhysicsObjects[i].transform.localRotation,
                                     networkPhysicsSyncedRotations.Get(i), interpolated.Alpha);
            }
        }
    }

    public void OnPlayerBodyPartHit()
    {
        if (!isActiveRagdoll) return;
        SoundManager.Instance.PlayHitSound();
        MakeRagdoll();
    }

    void MakeRagdoll()
    {
        if (!Object.HasStateAuthority) return;

        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = 0;
        mainJoint.slerpDrive = jointDrive;

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
            syncPhysicsObjects[i].MakeRagdoll();

        lastTimeBecameRagdol = Runner.SimulationTime;
        isActiveRagdoll = false;
        isGrabingActive = false;
        isPunchActive = false;
    }

    void MakeActiveRagdoll()
    {
        if (!Object.HasStateAuthority) return;

        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = startSlerpPositionSpring;
        mainJoint.slerpDrive = jointDrive;

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
            syncPhysicsObjects[i].MakeActiveRagdoll();

        isActiveRagdoll = true;
        isGrabingActive = false;
        isPunchActive = false;
    }

    public override void Spawned()
    {
        if (aiCount < AllAIs.Length)
        {
            AllAIs[aiCount] = this;
            aiCount++;
        }

        transform.name = $"AI_{Object.Id}";

        // AI�� ������ StateAuthority�� InputAuthority�� ��� ����
        Utils.DebugLog($"AI Spawned: {transform.name}, HasState: {Object.HasStateAuthority}, HasInput: {Object.HasInputAuthority}");
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Object.InputAuthority == player)
            Runner.Despawn(Object);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < AllAIs.Length; i++)
        {
            if (AllAIs[i] == this)
            {
                AllAIs[i] = null;
                break;
            }
        }
    }
}