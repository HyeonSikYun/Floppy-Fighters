using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player1 : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform cam;   // ���� ī�޶� Transform �Ҵ�

    [Header("Movement Settings")]
    public float speed = 3f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Camera Settings")]
    public float mouseSensitivity = 200f; // ���콺 ����
    public float camDistance = 5f;        // �÷��̾�� ī�޶� �Ÿ�
    public float camHeight = 2f;          // ī�޶� ����
    private float xRotation = 0f;
    private float yRotation = 0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (controller == null)
            controller = GetComponent<CharacterController>();
        if (cam == null)
            cam = Camera.main.transform; // ���� ī�޶� �ڵ� �Ҵ�

        // groundCheck�� �Ҵ���� �ʾҴٸ� �ڵ� ����
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, 0, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    private void Update()
    {
        CheckGround();
        HandleMovement();
        HandleJump();
        HandleCamera();

        // ESC�� ���콺 ��� ���� (�׽�Ʈ��)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // ���� ��Ұ� �������� �ִٸ� velocity ����
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // �ణ�� ���������� �����Ͽ� ���� �پ��ֵ���
        }
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + yRotation;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);

            anim.SetBool("Walk", true);
        }
        else
        {
            anim.SetBool("Walk", false);
        }
    }

    void HandleJump()
    {
        // �����̽��ٷ� ����
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // ���� �ִϸ��̼� Ʈ����
            anim.SetTrigger("Jump");
        }

        // �߷� ����
        velocity.y += gravity * Time.deltaTime;

        // ���� �̵� ����
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleCamera()
    {
        // ���콺 �Է�
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -30f, 60f); // ���Ʒ� ����

        // ī�޶� ��ġ ���
        Quaternion camRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        Vector3 camPosition = transform.position - camRotation * Vector3.forward * camDistance + Vector3.up * camHeight;

        cam.position = camPosition;
        cam.rotation = camRotation;
    }

    // Gizmo�� Ground Check ���� ǥ�� (Scene �信�� Ȯ�ο�)
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}