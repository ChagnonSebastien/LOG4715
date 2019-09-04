using UnityEngine;

public class PlatformerCharacter2D : MonoBehaviour 
{
	bool facingRight = true;							// For determining which way the player is currently facing.

	[SerializeField] float maxSpeed = 10f;				// The fastest the player can travel in the x axis.
	[SerializeField] float jumpForce = 600f;            // Amount of force added when the player jumps.
    [SerializeField] float wallJumpForce = 600f;        // Amount of force added when the player wall jumps.
    [SerializeField] float wallJumpTime = 2f;           // Amount of time in seconds it take for the player to regain full control after wall jump.

    [Range(0, 1)]
	[SerializeField] float crouchSpeed = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
	
	[SerializeField] bool airControl = false;			// Whether or not a player can steer while jumping;
	[SerializeField] LayerMask whatIsGround;            // A mask determining what is ground to the character
    [SerializeField] int maxMidairs = 1;

    float walljumpControlDelay = 0;
    int midairsCounter = 0;

	Transform groundCheck;                              // A position marking where to check if the player is grounded.
    Transform ceilingCheck;								// A position marking where to check for ceilings
    Transform backWallCheck;                           // A position marking where to check if the player touching the right wall.
    Transform frontWallCheck;					        // A position marking where to check if the player touching the left wall.
    Animator anim;										// Reference to the player's animator component.
    CapsuleCollider2D coll;                             // Reference to the player's collider component.

    float groundedRadius = .1f;							// Radius of the overlap circle to determine if grounded
	bool grounded = false;								// Whether or not the player is grounded.
    bool previouslyGrounded = false;
	float ceilingRadius = .18f;                         // Radius of the overlap circle to determine if the player can stand up
    float wallCheckRadius = .15f;                       // Radius of the overlap circle to determine if the player can stand up

    Vector2 initialColliderOffset;
    Vector2 initialColliderSize;
    Vector2 croutchedColliderOffset;
    Vector2 croutchedColliderSize;


    void Awake()
	{
		// Setting up references.
		groundCheck = transform.Find("GroundCheck");
		ceilingCheck = transform.Find("CeilingCheck");
        backWallCheck = transform.Find("BackWallCheck");
        frontWallCheck = transform.Find("FrontWallCheck");

        anim = GetComponent<Animator>();
        coll = GetComponent<CapsuleCollider2D>();

        initialColliderSize = coll.size;
        initialColliderOffset = coll.offset;
        croutchedColliderOffset = new Vector2(initialColliderOffset.x, initialColliderOffset.y - initialColliderSize.y / 6);
        croutchedColliderSize = new Vector2(initialColliderSize.x, initialColliderSize.y * 2 / 3);
    }


	void FixedUpdate()
	{
        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        previouslyGrounded = grounded;
		grounded = Physics2D.OverlapCircle(groundCheck.position, groundedRadius, whatIsGround);
		anim.SetBool("Ground", grounded);

		// Set the vertical animation
		anim.SetFloat("vSpeed", GetComponent<Rigidbody2D>().velocity.y);
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
    }

    public void Move(float move, bool crouch, bool jump)
	{
		// If crouching, check to see if the character can stand up
		if(!crouch && anim.GetBool("Crouch"))
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if( Physics2D.OverlapCircle(ceilingCheck.position, ceilingRadius, whatIsGround))
				crouch = true;
		}

		// Set whether or not the character is crouching in the animator
		anim.SetBool("Crouch", crouch);
        coll.size = crouch ? croutchedColliderSize : initialColliderSize;
        coll.offset = crouch ? croutchedColliderOffset : initialColliderOffset;

        //only control the player if grounded or airControl is turned on
        if (grounded || airControl)
		{
            // Reduce the speed if crouching by the crouchSpeed multiplier
            move = (crouch ? move * crouchSpeed : move);

			// The Speed animator parameter is set to the absolute value of the horizontal input.
			anim.SetFloat("Speed", Mathf.Abs(move));

            // Move the character
            float controlPercentage = 1 - walljumpControlDelay / wallJumpTime;
			GetComponent<Rigidbody2D>().velocity = new Vector2(
                controlPercentage * move * maxSpeed + (1 - controlPercentage) * GetComponent<Rigidbody2D>().velocity.x,
                GetComponent<Rigidbody2D>().velocity.y
                );
			
			// If the input is moving the player right and the player is facing left...
			if(move > 0 && !facingRight)
				// ... flip the player.
				Flip();
			// Otherwise if the input is moving the player left and the player is facing right...
			else if(move < 0 && facingRight)
				// ... flip the player.
				Flip();
		}

        // Walljump reset
        if (walljumpControlDelay > 0) walljumpControlDelay = Time.deltaTime < walljumpControlDelay ? walljumpControlDelay - Time.deltaTime : 0;

        if (jump)
        {
            float xForce = 0f;
            float yMultiplier = 1f;

            // If the player should jump...
            if (grounded) yMultiplier = 4f / 3;
            else if (Physics2D.OverlapCircle(backWallCheck.position, wallCheckRadius, whatIsGround))
            {
                walljumpControlDelay = wallJumpTime;
                xForce = wallJumpForce * (facingRight ? 1 : -1);
            }
            else if (Physics2D.OverlapCircle(frontWallCheck.position, wallCheckRadius, whatIsGround))
            {
                walljumpControlDelay = wallJumpTime;
                xForce = wallJumpForce * (facingRight ? -1 : 1);
            }
            else if (midairsCounter++ < maxMidairs) { }
            else jump = false;

            // Add a vertical force to the player.
            if (jump)
            {
                anim.SetBool("Ground", false);
                GetComponent<Rigidbody2D>().velocity = new Vector2(GetComponent<Rigidbody2D>().velocity.x, 0);
                GetComponent<Rigidbody2D>().AddForce(new Vector2(xForce, jumpForce * yMultiplier));
            }
        }

        // Midair reset
        if (grounded && !previouslyGrounded) midairsCounter = 0;
    }

	
	void Flip ()
	{
		// Switch the way the player is labelled as facing.
		facingRight = !facingRight;
		
		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
