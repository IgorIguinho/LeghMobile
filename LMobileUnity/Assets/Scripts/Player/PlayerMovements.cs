using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Data;

public class PlayerMovements : MonoBehaviour
{

    PlayerControl controls;
    float directionControls;

    Rigidbody2D rb;
    Animator animator;
    RewindObj rewindObj;

    [Header("Movimento on ground")]
    public float speed;
    public float speedOnAir;
    public int direction;
    public bool isFaceRight;
    bool canMove = true;

    [Header("Jump")]
    public float jumpForce;
    public int numberJump;
    public bool isGrounded;
    public Vector2 lengthGroundedCheck;
    public Transform groundChecker;
    public LayerMask groundMask;

    [Header("WallJump")]
    public float wallJumpForce;
    public float wallHorizontalJumpForce;
    public float wallFallForce;
    [Tooltip("Tempo para conseguir se mover ap�s realizar o pulo")] public float timeWallJump;
    public bool isWall;
    public Vector2 lengthWallCheck;
    public Transform wallChecker;
    public LayerMask wallMask;

    [Header("Dash")]
    public float dashForce;
    public float timeDash;
    public float dashCooldown;
    public GameObject trailObject;
    private bool canDash = true;
    private bool isDash;
    public GameObject buttonDash;
    public Color canDashColor;
    public Color notCanDashColor;

    [Header("Prototipo da corda")]
    public bool isRope;
    public float ropeJumpForce;
    public float ropeHorizontalJumpForce;
    public float ropeFall;
    public LayerMask layerRope;

    private void Awake()
    {
        controls = new PlayerControl();
        controls.Enable();

        controls.Land.Move.performed += ctx =>
        {
            directionControls = ctx.ReadValue<float>();
        };

        controls.Land.Jump.performed += ctx =>  Jump();

        controls.Land.Dash.performed += ctx =>
        {
            if (canDash) { StartCoroutine(Dash()); }
        };
    }
    // Start is called before the first frame update
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        //animator = gameObject.GetComponent<Animator>();
        rewindObj = gameObject.GetComponent<RewindObj>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!rewindObj.isRewind)
        {
            if (canMove) { Moviment(); }
            CheckGround();
            WallFall();
            InRope();
            //SetAnimatorVariables();
        }
    }


    void Moviment()
    {
        if (isDash) { return; }
        if (isGrounded == true) 
        { rb.linearVelocity = new Vector2(speed * directionControls , rb.linearVelocity.y); }
        else { rb.linearVelocity = new Vector2(speedOnAir * directionControls , rb.linearVelocity.y); }
        //animator.SetFloat("speed", Mathf.Abs(directionControls));
       if (rb.linearVelocity.x * direction < 0f)
        {
            Flip();
        }
    }

    void Jump()
    {
        if (isDash) { return; }
        if (isGrounded  ) 
        {
            numberJump = 0;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(0f, jumpForce ), ForceMode2D.Impulse);
            //animator.Play("Jump_Player");
            //animator.SetBool("DoubleJump", false);
            numberJump++;
        }
        else if (isWall)
        {
            numberJump = 0;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(wallHorizontalJumpForce * -direction , wallJumpForce),ForceMode2D.Impulse);
            //animator.SetBool("DoubleJump", true);
            Flip();
            numberJump++;
            StartCoroutine(StopMove());
        }
        else if (isRope)
        {
            numberJump = 0;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(ropeHorizontalJumpForce * direction, ropeJumpForce), ForceMode2D.Impulse);
            //animator.SetBool("DoubleJump", true);
            
        }
        else
        {
            //if (numberJump == 1)
            //{
            //    rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            //    animator.SetBool("DoubleJump", true);
            //    numberJump++;
            //}else { return; }
            return;
        }
    }

    IEnumerator Dash()
    { 

        isDash = true;
        canDash = false;

        float gravityScale = rb.gravityScale;
        rb.gravityScale = 0;

        rb.linearVelocity = Vector2.zero;
        if (isWall) 
        {
            rb.linearVelocity = new Vector2(dashForce * -direction , 0); 
            Flip(); 
        }
        else { rb.linearVelocity = new Vector2(dashForce * direction , 0); }

        trailObject.SetActive(true);
        buttonDash.gameObject.GetComponent<Image>().color = notCanDashColor;

        yield return new WaitForSeconds(timeDash);

        isDash = false;
        trailObject.SetActive(false);
        rb.gravityScale = gravityScale;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        buttonDash.gameObject.GetComponent<Image>().color = canDashColor;
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapBox(groundChecker.position, lengthGroundedCheck,0, groundMask);
        isWall = Physics2D.OverlapBox(wallChecker.position, lengthWallCheck, 0, wallMask);
        isRope = Physics2D.OverlapBox(wallChecker.position, lengthWallCheck, 0, layerRope);
    }

    void WallFall()
    {
        if (isWall)
        {
            if (rb.linearVelocity.y < wallFallForce)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallFallForce);
            }
        }
    }

    void InRope()
    {
        if (isRope)
        {
            if (rb.linearVelocity.y < ropeFall)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -ropeFall);
            }

        }
       
            
    }

    void SetAnimatorVariables()
    {
        animator.SetBool("OnGround", isGrounded);
        animator.SetBool("WallFall", isWall);
        animator.SetBool("IsDash", isDash);
        

        if (animator.GetBool("DoubleJump") == true && rb.linearVelocity.y < 0.1f)
        {
            animator.SetBool("DoubleJump", false);
        }
    }

    void Flip()
    {
        direction *= -1;
        isFaceRight = !isFaceRight;
        transform.Rotate(0, 180f, 0);
    }

    IEnumerator StopMove()
    {
        canMove = false;
        yield return new WaitForSeconds(timeWallJump);
        canMove = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundChecker.position, lengthGroundedCheck);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(wallChecker.position, lengthWallCheck);
    }
}
