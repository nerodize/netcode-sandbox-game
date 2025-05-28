using Unity.Netcode;
using UnityEngine;
using IngameDebugConsole;

public class PlayerMovement : NetworkBehaviour
{
    #region Variables
    [Header("Movement")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private float speed = 12f;
    [SerializeField] private float jumpHeight = 3f;
    
    [Header("Jumping")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float gravity = -9.81f;
    
    private Vector3 _velocity;
    private bool _isGrounded;
    #endregion
    
    void Update()
    {
        if (!IsOwner) return;

        if (DebugLogManager.IsConsoleOpen)
            return;
        
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if(_isGrounded && _velocity.y < 0)
            _velocity.y = -2f;
        
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);
        
        if(Input.GetButtonDown("Jump") && _isGrounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        
        _velocity.y += gravity * Time.deltaTime;
        
        controller.Move(_velocity * Time.deltaTime);
    }
}