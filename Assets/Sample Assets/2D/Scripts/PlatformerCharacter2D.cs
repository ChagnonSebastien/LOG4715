﻿using UnityEngine;

public class PlatformerCharacter2D : MonoBehaviour 
{
	bool facingRight = true;							// For determining which way the player is currently facing.

	[SerializeField] float maxSpeed = 10f;				// The fastest the player can travel in the x axis.
	[SerializeField] float jumpVerticalForce = 800f;            // Amount of force added when the player jumps.
    [SerializeField] float midairVerticalForce = 600f; // Amount of force added when the player wall jumps.
    [SerializeField] float wallJumpVerticalForce = 600f; // Amount of force added when the player wall jumps.
    [SerializeField] float walljumpHorizontalForce = 500f;
    [SerializeField] float wallJumpTotalTime = 0.25f;           // Amount of time in seconds it take for the player to regain full control after wall jump.
    [Range(0, 1)]
    [SerializeField] float walljumpUncontrolableTime = 0.3f;
    [SerializeField] int maxMidairs = 1;
    [SerializeField] float maxWindupTime = 3f;
    [SerializeField] float maxWindupMultiplier = 1.5f;

    [Range(0, 1)]
	[SerializeField] float crouchSpeed = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
	
	[SerializeField] bool airControl = false;			// Whether or not a player can steer while jumping;
	[SerializeField] LayerMask whatIsGround;            // A mask determining what is ground to the character

    float walljumpControlDelay = 0;
    int midairsCounter = 0;
	bool grounded = false;								// Whether or not the player is grounded.
    bool previouslyGrounded = false;
    float windupTime = 0f;
    int windupParticleAmount = 60;


    Transform groundCheck;                              // A position marking where to check if the player is grounded.
    Transform ceilingCheck;								// A position marking where to check for ceilings
    Transform backWallCheck;                            // A position marking where to check if the player touching the right wall.
    Transform frontWallCheck;					        // A position marking where to check if the player touching the left wall.
    Animator anim;										// Reference to the player's animator component.
    CapsuleCollider2D coll;                             // Reference to the player's collider component.
    ParticleSystem windupParticles;                     // Reference to the player's particle component.
    ParticleSystem walljumpParticles;                   // A position marking where to check if the player touching the right wall.

    float groundedRadius = .1f;							// Radius of the overlap circle to determine if grounded
	float ceilingRadius = .18f;                         // Radius of the overlap circle to determine if the player can stand up
    float wallCheckRadius = .15f;                       // Radius of the overlap circle to determine if the player can stand up

    Vector2 initialColliderOffset;
    Vector2 initialColliderSize;
    Vector2 croutchedColliderOffset;
    Vector2 croutchedColliderSize;

    public bool isGrounded() => grounded;

    void Awake()
	{
		// Setting up references.
		groundCheck = transform.Find("GroundCheck");
		ceilingCheck = transform.Find("CeilingCheck");
        backWallCheck = transform.Find("BackWallCheck");
        frontWallCheck = transform.Find("FrontWallCheck");

        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem p in particleSystems)
        {
            switch(p.name)
            {
                case "WindupParticles":
                    windupParticles = p;
                    break;
                case "WalljumpParticles":
                    walljumpParticles = p;
                    break;
            }
        }

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

    public void Move(float move, bool crouch, bool jump, bool winding)
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
            float controlPercentage = 1;
            if (walljumpControlDelay > wallJumpTotalTime * (1 - walljumpUncontrolableTime)) controlPercentage = 0;
            else if (walljumpControlDelay > 0) controlPercentage = Mathf.Pow((wallJumpTotalTime - walljumpControlDelay) / wallJumpTotalTime, 3);

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
        if (walljumpControlDelay > 0) GetComponent<Rigidbody2D>().AddForce(new Vector2(0, -Physics.gravity.y));

        if (windupTime > 0 && winding)
        {
            windupTime += Time.deltaTime;
            ParticleSystem.EmissionModule emission = windupParticles.emission;
            emission.rateOverTime = windupParticleAmount * Mathf.Min(maxWindupTime, windupTime) / maxWindupTime;
        }

        if (jump && crouch && grounded)
        {
            windupTime += Time.deltaTime;
        }
        else if ( jump || ( !winding && windupTime > 0 ))
        {
            bool cancelHorizontalVelocity = false;
            float horizontalForce = 0f;
            float verticalForce = 0f;

            // If the player should jump...
            if (grounded)
            {
                verticalForce = jumpVerticalForce * (1 + ((maxWindupMultiplier - 1) * (Mathf.Min(maxWindupTime, windupTime) / maxWindupTime)));
                windupTime = 0f;
                ParticleSystem.EmissionModule emission = windupParticles.emission;
                emission.rateOverTime = 0f;
            }
            else if (Physics2D.OverlapCircle(backWallCheck.position, wallCheckRadius, whatIsGround))
            {
                walljumpControlDelay = wallJumpTotalTime;
                cancelHorizontalVelocity = true;
                horizontalForce = walljumpHorizontalForce * (facingRight ? 1 : -1);
                verticalForce = wallJumpVerticalForce;
                walljumpParticles.Play();
            }
            else if (Physics2D.OverlapCircle(frontWallCheck.position, wallCheckRadius, whatIsGround))
            {
                walljumpControlDelay = wallJumpTotalTime;
                cancelHorizontalVelocity = true;
                horizontalForce = walljumpHorizontalForce * (facingRight ? -1 : 1);
                verticalForce = wallJumpVerticalForce;
                walljumpParticles.Play();
            }
            else if (midairsCounter++ < maxMidairs)
            {
                verticalForce = midairVerticalForce;
            }

            // Add a vertical force to the player.
            if (verticalForce > 0)
            {
                anim.SetBool("Ground", false);
                GetComponent<Rigidbody2D>().velocity = new Vector2(cancelHorizontalVelocity ? 0 : GetComponent<Rigidbody2D>().velocity.x, 0);
                GetComponent<Rigidbody2D>().AddForce(new Vector2(horizontalForce, verticalForce));
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
