using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Data;

public class PlayerMovements : MonoBehaviour
{


    InputReader input;

    Rigidbody2D rb;
    Animator animator;
    RewindObj rewindObj;

    [Header("Movimento on ground")]
    public float speed;
    public float speedOnAir;
    public int direction;
    public bool isFaceRight;
    public bool canMove = true;
    

    [Header("Swtich speed")]
    public bool switchSpeedSlow;
    bool isSwtichSpeed = false;
    public float speedSwitch;
    public LayerMask swtichSpeedMask;

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
    [Tooltip("Tempo para conseguir se mover após realizar o pulo")] public float timeWallJump;
    public bool isWall;
    public Vector2 lengthWallCheck;
    public Transform wallChecker;
    public LayerMask wallMask;

    [Header("Dash")]
    public float dashForce;
    public float timeDash;
    public float dashCooldown;
    public GameObject trailObject;
    public bool canDash = true;
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

    private void OnEnable()
    {
        if (input != null)
        {
            input.JumpTriggered += OnJumpInput;
            input.DashTriggered += OnDashInput;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.JumpTriggered -= OnJumpInput;
            input.DashTriggered -= OnDashInput;
        }
    }

    private void Awake()
    {
        input = GetComponent<InputReader>();
    }

    void OnJumpInput() => Jump();
    void OnDashInput()
    {
        if (canDash) { StartCoroutine(Dash()); }
    }

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        animator = gameObject.GetComponent<Animator>();
        rewindObj = gameObject.GetComponent<RewindObj>();
    }

    void FixedUpdate()
    {
        if (!rewindObj.isRewind)
        {
            if (canMove) { Moviment(); }
            CheckGround();
            WallFall();
            InRope();
        }
    }

    void Moviment()
    {
        if (isDash) return; 
        float currentDirection = (input != null) ? input.Direction : 0f;
        float switchSpeed = switchSpeedSlow ? (speed / speedSwitch) : (speed * speedSwitch);

        if (isGrounded  && !isSwtichSpeed) // movimento normal no chão
        { rb.linearVelocity = new Vector2(speed * currentDirection , rb.linearVelocity.y); }

        else if (isSwtichSpeed) //movimento modificado pelo terreno de switch speed
        { rb.linearVelocity = new Vector2(switchSpeed * currentDirection, rb.linearVelocity.y);  }

        else { rb.linearVelocity = new Vector2(speedOnAir * currentDirection , rb.linearVelocity.y); } //Movimento norma no ar
        animator.SetFloat("speed", Mathf.Abs(currentDirection));
        
        if (rb.linearVelocity.x * direction < 0f)
        {
            Flip();
        }
    }

    void Jump()
    {
        if (isDash) { return; }
        if (isGrounded) 
        {
            numberJump = 0;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(0f, jumpForce ), ForceMode2D.Impulse);
            numberJump++;
        }
        else if (isWall)
        {
            numberJump = 0;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(wallHorizontalJumpForce * -direction , wallJumpForce), ForceMode2D.Impulse);
            Flip();
            numberJump++;
            StartCoroutine(StopMove());
        }
        else if (isRope)
        {
            numberJump = 0;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(ropeHorizontalJumpForce * direction, ropeJumpForce), ForceMode2D.Impulse);
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
        animator.SetFloat("speed", Mathf.Abs((input != null) ? input.Direction : 0f));
        trailObject.SetActive(true);
        buttonDash.gameObject.GetComponent<Image>().color = notCanDashColor;
        yield return new WaitForSeconds(timeDash);
        isDash = false;
        trailObject.SetActive(false);
        rb.gravityScale = gravityScale;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        animator.SetFloat("speed", Mathf.Abs((input != null) ? input.Direction : 0f));
        buttonDash.gameObject.GetComponent<Image>().color = canDashColor;
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapBox(groundChecker.position, lengthGroundedCheck, 0, groundMask);
        isSwtichSpeed = Physics2D.OverlapBox(groundChecker.position, lengthGroundedCheck, 0, swtichSpeedMask); 
        
        isWall = Physics2D.OverlapBox(wallChecker.position, lengthWallCheck, 0, wallMask);
        isRope = Physics2D.OverlapBox(wallChecker.position, lengthWallCheck, 0, layerRope);
    }

    void WallFall()
    {
        if (isWall && rb.linearVelocity.y < wallFallForce)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallFallForce);
        }
    }

    void InRope()
    {
        if (isRope && rb.linearVelocity.y < ropeFall)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -ropeFall);
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