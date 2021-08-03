using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public CharacterController controller;
    public Transform shootOrigin;

    [Header("Player attributes")]
    public int id;
    public string username;
    public string playerTeam;
    public int score;
    public bool fatigued;

    [Header("Player health")]
    public float health;
    public float maxHealth = 100f;

    [Header("Player movement")]
    public float gravity = -9.81f;
    public float moveSpeed = 6f;
    public float sprintSpeed;
    public float jumpSpeed = 5f;
    public float stamina;
    public float maxStamina = 50f;
    public float rechargeRate = 5f;
    public float drainRate = 5f;


    private bool[] inputs;
    private float yVelocity = 0;

    private void Start()
    {
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        sprintSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;
    }

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        score = 0;

        sprintSpeed = (float)(moveSpeed * 1.65);
        stamina = maxStamina;
        health = maxHealth;
        fatigued = false;

        inputs = new bool[6];
    }

    public void SetInput(bool[] _inputs, Quaternion _rotation)
    {
        inputs = _inputs;
        transform.rotation = _rotation;
    }

    public void FixedUpdate()
    {
        if (health <= 0f)
        {
            return;
        }

        Vector2 _inputDirection = Vector2.zero;
        
        if (inputs[0])  // W
        {
            _inputDirection.y += 1;
        }
        if (inputs[1])  // A
        {
            _inputDirection.x -= 1;
        }
        if (inputs[2])  // S
        {
            _inputDirection.y -= 1;
        }
        if (inputs[3])  // D
        {
            _inputDirection.x += 1;
        }

        Move(_inputDirection);
    }

    public void OnTriggerEnter(Collider other)
    {
        // kills the player if they touch the death barrier
        if (other.CompareTag("DeathBarrier"))
        {
            TakeDamage(maxHealth);
        }
    }

    private void Move(Vector2 _inputDirection)
    {
        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;

        // if the player presses "LeftShift"
        if (inputs[5] && !inputs[2] && stamina > 0f)
        {
            _moveDirection *= sprintSpeed;

            // stamina only drains while player is moving
            if (_inputDirection != Vector2.zero)
            {
                DrainStamina();
            }
            else
            {
                // stamina recharges while player is not moving
                if (stamina < maxStamina && !fatigued)
                {
                    RegenStamina();
                }
            }
        }
        else
        {
            _moveDirection *= moveSpeed;

            // stamina recharges while not sprinting and the player is not fatigued
            if (stamina < maxStamina && !fatigued)
            {
                RegenStamina();           
            }
        }

        

        // handle jump when player is grounded
        if (controller.isGrounded)
        {
            yVelocity = 0f;
            // if the player pressed "Space"
            if (inputs[4] && stamina > 5f)
            {
                yVelocity = jumpSpeed;
                stamina -= 5f;
            }
        }
        yVelocity += gravity;
        _moveDirection.y = yVelocity;

        
        controller.Move(_moveDirection);

        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);

        ServerSend.PlayerStamina(id, this);
    }

    public void Shoot(Vector3 _viewDirection)
    {
        // prevents player from shooting after dying
        if (health <= 0f)
        {
            return;
        }
        
        // TODO: set range based on a player's weapon
        if (Physics.Raycast(shootOrigin.position, _viewDirection, out RaycastHit _hit, 100f))
        {
            Debug.DrawLine(shootOrigin.position, _hit.point);
            if (_hit.collider.CompareTag("Player"))
            {
                _hit.collider.GetComponent<Player>().TakeDamage(20f);

                // if the other player is killed, increase score by one
                if (_hit.collider.GetComponent<Player>().health <= 0f)
                {
                    score += 1;
                    Debug.Log("Score value: " + score);
                    ServerSend.PlayerScored(this);
                }
            }
        }
        
    }

    private void DrainStamina()
    {
        stamina -= Time.deltaTime * drainRate;

        if (stamina <= 0f)
        {
            fatigued = true;
            StartCoroutine(Fatigued());
        }
    }

    private void RegenStamina()
    {
        stamina += Time.deltaTime * rechargeRate;
    }

    /// <summary>
    /// Subtract from the player's current health.
    /// </summary>
    /// <param name="_damage">Amount of damage player took.</param>
    public void TakeDamage(float _damage)
    {
        if (health <= 0f)
        {
            return;
        }

        health -= _damage;
        if (health <= 0f)
        {
            // to avoid negative health values
            health = 0f;
            controller.enabled = false;

            StartCoroutine(Respawn());
        }

        ServerSend.PlayerHealth(this);
    }

    
    /// <summary>Waits 3 seconds before allowing player to regen stamina again.</summary>
    private IEnumerator Fatigued()
    {
        yield return new WaitForSeconds(3f);

        fatigued = false;
    }

    /// <summary>
    /// After a player dies, wait five seconds before respawning them.
    /// </summary>
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(5f);

        // handle where the player will respawn

        // handle where the player will spawn
        int choice = Random.Range(0, 7);
        transform.position = NetworkManager.instance.spawnPoints[choice].transform.position;

        ServerSend.PlayerPosition(this);

        health = maxHealth;
        stamina = maxStamina;
        controller.enabled = true;
        ServerSend.PlayerRespawned(this);
    }
}
