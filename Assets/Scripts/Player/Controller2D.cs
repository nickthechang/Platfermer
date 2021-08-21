using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : RaycastController
{
    float maxClimbAngle = 80;
    float maxDescendAngle = 75;

    bool haveKey = false;

    public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 playerInput;

    public override void Start()
    {
        base.Start();
        collisions.faceDir = 1;
    }

    public void Move(Vector2 deltaMove, bool standingOnPlatform)
    {
        Move(deltaMove, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector2 deltaMove, Vector2 input, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.deltaMoveOld = deltaMove;
        playerInput = input;

        if (deltaMove.y < 0)
        {
            DescendSlope(ref deltaMove);
        }
        if (deltaMove.x != 0)
        {
            collisions.faceDir = (int)Mathf.Sign(deltaMove.x);
            if (collisions.faceDir > 0)
                transform.localScale = Vector3.one;
            else if (collisions.faceDir < 0)
                transform.localScale = new Vector3(-1, 1, 1);
            anim.SetBool("run", true);
        }

        HorizontalCollisions(ref deltaMove);

        if(deltaMove.y != 0)
        {
            VerticalCollisions(ref deltaMove);
        }

        transform.Translate(deltaMove);

        if(standingOnPlatform)
        {
            collisions.below = true;
        }

        if ((deltaMove.x <= 0.005 && deltaMove.x >= -0.005))
            anim.SetBool("run", false);
        /*else if (collisions.below)
            anim.SetBool("grounded", true);*/
        Debug.Log(deltaMove);
    }

    void HorizontalCollisions(ref Vector2 deltaMove) //ref makes it so that whatever changes happen in the method will change deltaMove
    {
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(deltaMove.x) + skinWidth;
         
        if(Mathf.Abs(deltaMove.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            RaycastHit2D collectible = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collectibleMask);
            RaycastHit2D End = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, endMask);
            
            if(collectible)
            {
                collectible.collider.gameObject.GetComponent<Collectible>().EatMyAss();
                PossessKey();
                Debug.Log(haveKey);
            }
            if(End)
            {
                Debug.Log("bruh");
                if(haveKey)
                {
                    Application.Quit();
                }
            }
            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);

            if (hit)
            {

                if(hit.distance == 0)
                {
                    continue;
                }
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if(collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        deltaMove = collisions.deltaMoveOld;
                    }
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        deltaMove.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref deltaMove, slopeAngle);
                    deltaMove.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    deltaMove.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if(collisions.climbingSlope)
                    {
                        deltaMove.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(deltaMove.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    void VerticalCollisions(ref Vector2 deltaMove) //ref makes it so that whatever changes happen in the method will change deltaMove
    {
        float directionY = Mathf.Sign(deltaMove.y);
        float rayLength = Mathf.Abs(deltaMove.y) + skinWidth;
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + deltaMove.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
            RaycastHit2D Die = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, die);

            if(Die)
            {
                Debug.Log("Die");
                Application.Quit();
            }

            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);

            if(hit)
            {
                if(hit.collider.tag == "Through")
                {
                    if(directionY == 1 || hit.distance == 0)
                    {
                        continue;
                    }
                    if(collisions.fallingThroughPlatform)
                    {
                        continue;
                    }
                    if(playerInput.y == -1)
                    {
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", .5f);
                        continue;
                    }
                }

                deltaMove.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if(collisions.climbingSlope)
                {
                    deltaMove.x = deltaMove.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(deltaMove.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;

            }
        }
        if(collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(deltaMove.x);
            rayLength = Mathf.Abs(deltaMove.x + skinWidth);
            Vector2 rayOrigin = ((directionX == 1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * deltaMove.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if(hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if(slopeAngle != collisions.slopeAngle)
                {
                    deltaMove.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void ClimbSlope(ref Vector2 deltaMove, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(deltaMove.x);
        float climbdeltaMoveY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if(deltaMove.y <= climbdeltaMoveY)
        {
            deltaMove.y = climbdeltaMoveY;
            deltaMove.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(deltaMove.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendSlope(ref Vector2 deltaMove)
    {
        float directionX = Mathf.Sign(deltaMove.x);
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if(hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if(slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if(Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(deltaMove.x)) {
                        float moveDistance = Mathf.Abs(deltaMove.x);
                        float descenddeltaMoveY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        deltaMove.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(deltaMove.x);
                        deltaMove.y -= descenddeltaMoveY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    void ResetFallingThroughPlatform()
    {
        collisions.fallingThroughPlatform = false;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector2 deltaMoveOld;
        public int faceDir;
        public bool fallingThroughPlatform;
        
        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }

    public void PossessKey()
    {
        haveKey = true;
    }

    public bool KeyStatus()
    {
        return haveKey;
    }
}
