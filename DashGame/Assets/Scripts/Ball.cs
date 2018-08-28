﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{

    public Color32 startColor; // start color should usually be this slightly grayish white : EAEAEA
    public Color32 boostColor;
    public GameObject ghost;

    SpriteRenderer ballSprite;
    ParticleSystem collisionEffect;
    ParticleSystem fallEffect;
    ParticleSystem firstImpact;
    ParticleSystem boostEffect;
    TrailRenderer hostTrail;
    Animator animator;
    GameManager game;
    Ray2D ray;
    Vector3 rayOffsetVector = new Vector3(0, 0.147f); // used to offset ray a bit so that it does not start from the enemy's transfrom.position which is also the contactpoint
    bool atCenter = false;
    Rigidbody2D rigidbody;
    TargetController target;
    bool shouldBoost = false;
    bool ShouldShrink = false;
    bool invulnerable = false;
    bool wallHit = false;
    bool cantCollide = false;
    bool shouldAbsorb = false;
    bool canAbsorb; //ball will only be absorbed after it is deflectd off of the paddle;
    BallController ballC;
    bool firstCollision = true; //first collision with paddle
    float initialVelocity, boostVelocity, absorbSpeed;
    bool firstTriggerCollision = true; //for first collision with a target
    GameObject ghostBall1;
    GameObject ghostBall2;
    TrailRenderer ghost1Trail, ghost2Trail;
    Animator ghost1AnimC, ghost2AnimC;
    string targetHit; //Name of the target that was hit;
    bool wrappingEnabled = false;
    bool wrappingRightNow = false;
    bool pauseAllCoroutines = false;
    float cameraRadius;
    float ballRadius = 0.147f; //got from measuring ball collider radius
    Vector2 newPos;
    bool spawnedIn = false;
    TrailRenderer tempTrail,tempTrail2;
    bool wrappedAround = false;

    public delegate void BallDelegate();
    public static event BallDelegate PlayerMissed;
    public static event BallDelegate AbsorbDone; // specific event for TargetController so its animation matches up with the balls
    public static event BallDelegate AbsorbDoneAndRichochet;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        ballSprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
        collisionEffect = transform.GetChild(1).GetComponent<ParticleSystem>();
        fallEffect = transform.GetChild(2).GetComponent<ParticleSystem>();
        firstImpact = transform.GetChild(3).GetComponent<ParticleSystem>();
        hostTrail = transform.Find("Trail").GetComponent<TrailRenderer>();

        ghostBall1 = Instantiate(ghost, Vector2.right * 600, Quaternion.Euler(0, 0, 0));
        ghostBall1.SetActive(false);
        ghost1AnimC = ghostBall1.GetComponent<Animator>();
        ghost1Trail = ghostBall1.transform.Find("Trail").GetComponent<TrailRenderer>();
        ghostBall2 = Instantiate(ghost, Vector2.right * 600, Quaternion.Euler(0, 0, 0));
        ghostBall2.SetActive(false);
        ghost2AnimC = ghostBall2.GetComponent<Animator>();
        ghost2Trail = ghostBall2.transform.Find("Trail").GetComponent<TrailRenderer>();

        rigidbody = GetComponent<Rigidbody2D>();

        game = GameManager.Instance;
        target = TargetController.Instance;
        ballC = BallController.Instance;

        cameraRadius = (Camera.main.aspect * Camera.main.orthographicSize);
    }

    private void OnEnable()
    {
        rigidbody.simulated = true;
        ballSprite.color = startColor;
        ShouldShrink = false;
        Physics2D.IgnoreLayerCollision(10, 11);
        Physics2D.IgnoreLayerCollision(11, 12, false);
        atCenter = false;
        invulnerable = false;
        shouldBoost = false;
        canAbsorb = false;
        firstCollision = true;
        firstTriggerCollision = true;
        cantCollide = false;
        wrappingEnabled = false;

        SetAnimTrigs("Boost",true);
        SetAnimTrigs("ImmediateShrink", true);
    }

    public void Spawn(float initialVel, float boostVel, float absorbSpd, Vector2 position, Quaternion rotation, bool wrapping)
    {
        transform.position = position;
        transform.rotation = rotation;

        initialVelocity = initialVel;
        boostVelocity = boostVel;
        absorbSpeed = absorbSpd;

        if (wrapping)
        {
            wrappingEnabled = true;

            ghostBall1.SetActive(true);
            ghostBall2.SetActive(true);
        }
        else
        {
            ghostBall1.SetActive(false);
            ghostBall2.SetActive(false);

        }

        StartCoroutine(DropDelay());
    }

    void SetAnimTrigs(string trigger, bool reset = false)
    {
        if (!reset)
        {
            if (wrappingEnabled)
            {
                animator.SetTrigger(trigger);
                ghost1AnimC.SetTrigger(trigger);
                ghost2AnimC.SetTrigger(trigger);
            }
            else
            {
                animator.SetTrigger(trigger);
            }
        }
        else
        {
            if (wrappingEnabled)
            {
                animator.ResetTrigger(trigger);
                ghost1AnimC.ResetTrigger(trigger);
                ghost2AnimC.ResetTrigger(trigger);
            }
            else
            {
                animator.ResetTrigger(trigger);
            }
        }
    }

    void SetAnimBools(string boolName, bool value)
    {
        if (wrappingEnabled)
        {
            animator.SetBool(boolName, value);
            ghost1AnimC.SetBool(boolName, value);
            ghost2AnimC.SetBool(boolName, value);
        }
        else
        {
            animator.SetBool(boolName, value);
        }
    }

    void PositionGhosts()
    {
        ghostBall1.transform.position = new Vector2(transform.position.x - cameraRadius * 2, transform.position.y);  //ghost1 is always to the left

        ghostBall2.transform.position = new Vector2(transform.position.x + cameraRadius * 2, transform.position.y); //ghost2 is always to the right
    }

    void SwapWithGhost()
    {
        if (OnScreen(ghostBall1.transform.position))
        {
            hostTrail.transform.parent = null;
            ghost1Trail.transform.parent = null;
            ghost2Trail.transform.parent = null;
            ghost2Trail.Clear();
            ghost2Trail.gameObject.SetActive(false);

            transform.position = ghostBall1.transform.position;
            ghostBall1.transform.position = new Vector2(transform.position.x - cameraRadius * 2, transform.position.y);
            ghostBall2.transform.position = new Vector2(transform.position.x + cameraRadius * 2, transform.position.y);

            tempTrail = hostTrail;
            tempTrail2 = ghost2Trail;

            ghost1Trail.transform.SetParent(transform, true);
            hostTrail = ghost1Trail;

            tempTrail.transform.SetParent(ghostBall2.transform, true);
            ghost2Trail = tempTrail;

            tempTrail2.transform.SetParent(ghostBall1.transform, false);
            ghost1Trail = tempTrail2;
            ghost1Trail.transform.localPosition = Vector2.zero;
            ghost1Trail.gameObject.SetActive(true);
        }
        else
        {
            hostTrail.transform.parent = null;
            ghost1Trail.transform.parent = null;
            ghost2Trail.transform.parent = null;
            ghost1Trail.Clear();
            ghost1Trail.gameObject.SetActive(false);

            transform.position = ghostBall2.transform.position;
            ghostBall1.transform.position = new Vector2(transform.position.x - cameraRadius * 2, transform.position.y);
            ghostBall2.transform.position = new Vector2(transform.position.x + cameraRadius * 2, transform.position.y);

            tempTrail = hostTrail;
            tempTrail2 = ghost1Trail;

            ghost2Trail.transform.SetParent(transform, true);
            hostTrail = ghost2Trail;

            tempTrail.transform.SetParent(ghostBall1.transform,true);
            ghost1Trail = tempTrail;

            tempTrail2.transform.SetParent(ghostBall2.transform,false);
            ghost2Trail = tempTrail2;
            ghost2Trail.transform.localPosition = Vector2.zero;
            ghost2Trail.gameObject.SetActive(true);
        }
    }

    bool OnScreen(Vector2 pos)
    {
        if (pos.x > -(cameraRadius + ballRadius) && pos.x < cameraRadius + ballRadius)
        {
            return true;
        }
        return false;
    }

    public void Wrap()
    {
        PositionGhosts();
        if (OnScreen(transform.position))
        {
            wrappingRightNow = false;
            return;
        }

        newPos = transform.position;

        if (!wrappingRightNow)
        {
            newPos = -newPos;

            wrappingRightNow = true;
        }
        SwapWithGhost();
    }

    IEnumerator DropDelay()
    {
        yield return new WaitForSeconds(1);
        while (pauseAllCoroutines || game.Paused)
        {
            yield return null;
        }
        spawnedIn = true;
        rigidbody.velocity = initialVelocity * Vector2.down;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            pauseAllCoroutines = true;
        }
        else
        {
            pauseAllCoroutines = false;
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            pauseAllCoroutines = true;
        }
        else
        {
            pauseAllCoroutines = false;
        }
    }

    private void Update()
    {
        SetAnimBools("ShouldShrink", ShouldShrink);
    }

    private void FixedUpdate()
    {
        ray = new Ray2D(transform.position + rayOffsetVector, -transform.up);

        if (atCenter)
        {
            transform.position = target.GetCurrentTargetPos;
        }

        if (shouldAbsorb)
        {
            Absorb();
        }

        if (wrappingEnabled)
        {
            Wrap();
        }
    }

    public void GoAway()
    {
        if ((!atCenter || shouldAbsorb) && invulnerable)
        {
            if (wallHit || wrappedAround)
            {
                wallHit = false;
                wrappedAround = false;

                ballC.SetTargetHit(targetHit);
                AbsorbDoneAndRichochet();
                return;
            }
            else
            {
                ballC.SetTargetHit(targetHit);
                AbsorbDone();
                return;
            }
        }

        rigidbody.velocity = Vector2.zero;
        ghostBall1.SetActive(false);
        ghostBall2.SetActive(false);
        gameObject.SetActive(false);
    }

    void Absorb()
    {
        if (canAbsorb)
        {
            if (target.IsMoving())
            {
                transform.position = Vector2.MoveTowards(transform.position, target.GetCurrentTargetPos, Time.deltaTime * (target.getTravelSpeed + 1));
                print(target.GetCurrentTargetPos);
                print(transform.position);
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, target.GetCurrentTargetPos, Time.deltaTime * absorbSpeed);
                print(target.GetCurrentTargetPos);
                print(transform.position);
            }

            if (transform.position == target.GetCurrentTargetPos)
            {
                atCenter = true;
                shouldAbsorb = false;
            }

            if (!shouldAbsorb)
            {
                if (wallHit || wrappedAround)
                {
                    wallHit = false;
                    wrappedAround = false;

                    ballC.SetTargetHit(targetHit);
                    AbsorbDoneAndRichochet();
                    return;
                }
                else
                {
                    ballC.SetTargetHit(targetHit);
                    AbsorbDone();
                    return;
                }
            }
        }
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        print(collision.gameObject.name);
        if (!cantCollide)
        {
            canAbsorb = true;
            if (collision.gameObject.tag == "Paddle" || collision.gameObject.tag == "Floor")
            {
                Physics2D.IgnoreLayerCollision(10, 11, false);
                Physics2D.IgnoreLayerCollision(11, 12);        // this makes it so that the paddle cant hit the ball again before it hits another collider
            }
            else
            {
                Physics2D.IgnoreLayerCollision(11, 12, false);
            }

            if (firstCollision)
            {
                FirstCollision();
                firstCollision = false;
            }

            if (collision.gameObject.tag == "Wall")
            {
                wallHit = true;
            }

            if (!atCenter && !shouldAbsorb)
            {
                collisionEffect.Play();
            }

            ContactPoint2D cp = collision.contacts[0]; // 0 indicates the first contact point between the colliders. Since there is only one contact point a higher index would cause a runtime error
            Vector2 reflectDir = Vector2.Reflect(ray.direction, cp.normal);

            float rotation = 90 + Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rotation);
            rigidbody.velocity = -transform.up.normalized * boostVelocity;
        }
    }

    void FirstCollision()
    {
        SetAnimTrigs("Boost");

        ballC.FlashWhite();

        ballC.CameraShake();

        ballSprite.color = boostColor;
        fallEffect.Stop();
        fallEffect.Play();
        shouldBoost = true;
        firstImpact.Play();
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (canAbsorb)
        {
            targetHit = collision.gameObject.name;
            if (firstTriggerCollision)
            {
                if (collision.gameObject.layer == 8)
                {
                    Physics2D.IgnoreLayerCollision(10, 11);
                    invulnerable = true;
                    rigidbody.velocity = Vector2.zero;
                    rigidbody.angularVelocity = 0;
                    shouldAbsorb = true;
                    ShouldShrink = true;
                    firstTriggerCollision = false;
                    cantCollide = true;

                    Debug.Log("ball should definetely be absorbing right now since\n shouldAbsorb = " + shouldAbsorb + ", and ontriggerenter2d has been called");
                    Debug.Log("also the trigger the ball hit was " + collision.gameObject.name);
                    Debug.Log("velocity of ball = " + rigidbody.velocity);
                }
            }
        }
        if (collision.gameObject.layer == 9)
        {
            if (!invulnerable)
            {
                rigidbody.velocity = Vector2.zero;
                rigidbody.simulated = false;
                SetAnimTrigs("ImmediateShrink");
                PlayerMissed();
            }
        }
    }
}