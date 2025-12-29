using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using herbivores;
using DayNightCycleStuff;



public class AI_Tiger : MonoBehaviour
{
    public Animator animator;
    public string walkForwardAnimation = "walk";
    public string runForwardAnimation = "run";
    public string attackAnimation = "hit";
    public string idleAnimation = "idle";
    public string roarAnimation = "sound";
    public static string mainMoveAnimation;
    public static string mainidleAnimation;

    //[SerializeField] private DayNightCycle dayNightCycle;


    /*public string walkBackwardAnimation = "walk_backwards";
    public string runForwardAnimation = "run_forward";
    public string turn90LAnimation = "turn_90_L";
    public string turn90RAnimation = "turn_90_R";
    public string trotAnimation = "trot_forward";
    public string sittostandAnimation = "sit_to_stand";
    public string standtositAnimation = "stand_to_sit";
    */
    public GameObject carnivore;

    public static float normalSpeed = 50.0f;
    public static float hungrySpeed = 100.0f;
    public static float moveSpeed = normalSpeed;

    //public float hunger = 0;

    Vector3 stopPosition;


    public GameObject dayNightCycle;

    float walkTime;
    public float walkCounter;
    float waitTime;
    public float waitCounter;

    int WalkDirection;

    public bool isWalking = false;
    public bool isPerformingAnimation = false;

    //rotation coordinates
    public float currentX = 0;
    public float currentY = 0;
    public float currentZ = 0;


    // eating fields
    public ColliderDetector detector;
    public float eatingDistance = 1.5f;

    private GameObject targetFood = null;
    private bool isEating = false;

    public float hunger = 0f;
    public float hungerIncreaseRate = 0.010f;
    public float hungerThreshold = 70f;
    public float hungerDeathThreshold = 140f;

    //thirst
    public float thirst = 0f;
    public float thirstIncreaseRate = 0.005f * 2f; // 2x hunger rate
    public float thirstWarningThreshold = 70f;
    public float thirstDeathThreshold = 140f;

    private GameObject targetWater;
    private bool isDead = false;

    // Horny stat
    public float horny = 0f;
    public float hornyIncreaseRate = 0.005f * 1.5f;
    public float hornyThreshold = 50f;
    private bool isInHeat = false;

    // Age
    public float age = 0f;
    public float ageIncreaseRate = 0.005f / 2f; // slower than hunger
    public float maxAge = 140f;

    // Age stages
    public bool isCub => age < 20f;
    public bool isAdult => age >= 20f && age < 101f;
    public bool isOld => age >= 101f && age < maxAge;

    // Mating
    private GameObject mateTarget;
    public GameObject carnivorePrefab; // Assign your sheep prefab in Inspector
    private float matingDistance = 3f;
    private bool isMating = false;


    public bool isWolf;

    public bool locationChip = false;
    public bool isItNight = true;



    void Start()
    {
        
        animator = GetComponent<Animator>();
        mainidleAnimation = idleAnimation;
        mainMoveAnimation = walkForwardAnimation;
        walkTime = Random.Range(3, 6);
        waitTime = Random.Range(5, 7);

        waitCounter = waitTime;
        walkCounter = walkTime;

        StartCoroutine(DelaySitTostand());  // Start idle
    }

    void Update()
    {
       
        if (isDead || isEating || isPerformingAnimation || isMating) return;
        GetMovementAnimation();
        UpdateScaleBasedOnAge();
        horny += hornyIncreaseRate;
        age += ageIncreaseRate;

        if (age >= maxAge)
        {
            StartCoroutine(Die());
            return;
        }

        hunger += hungerIncreaseRate;
        thirst += thirstIncreaseRate;

        if (thirst >= thirstDeathThreshold)
        {
            StartCoroutine(Die());
            return;
        }

        if (hunger >= hungerDeathThreshold)
        {
            StartCoroutine(Die());
            return;
        }
        

        if (thirst >= thirstWarningThreshold && targetWater == null)
        {
            targetWater = FindNearestWater();
        }

        if (thirst >= thirstWarningThreshold && targetWater != null)
        {
            MoveTowardWater();
            return;
        }
        
        if (hunger >= hungerThreshold && targetFood == null)
        {
            targetFood = FindNearestHerbivore();
        }
        
        if (hunger >= hungerThreshold && targetFood != null)
        {
            MoveTowardFood();
            return;
        }
        //
        // ✅ Fix: Only find mate if we don’t have one already and we’re not mating
        if (mateTarget == null && horny >= hornyThreshold && isAdult)
        {
            GameObject foundMate = FindMate();
            if (foundMate != null)
            {
                mateTarget = foundMate;
            }
        }

        if (mateTarget != null)
        {
            MoveTowardMate();
            return;
        }
        duringNight();
        HandleWandering();
    }


    public void activateChip()
    {
        locationChip = true;

    }
    public void SetAnimalVisible(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = visible;
        }
    }
    public void duringNight()
    {
        DayNight tempo = dayNightCycle.GetComponent<DayNight>();
        if (!tempo.isDaytime)
        {
            isItNight = true;
            if (locationChip)
            {
                //GetComponent<Renderer>().enabled = true;
                SetAnimalVisible(true);
            }
            else
            {
                SetAnimalVisible(false);
                //GetComponent<Renderer>().enabled = false;
            }
        }
        else
        {
            isItNight = false;
            SetAnimalVisible(true);
        }
    }

    GameObject FindMate()
    {
        if (!isWolf)
        {
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag("animalTiger"))
            {
                if (obj == this.gameObject) continue;

                AI_Tiger otherCarnivore = obj.GetComponent<AI_Tiger>();
                if (otherCarnivore != null && otherCarnivore.horny >= hornyThreshold && otherCarnivore.isAdult)
                {
                    return obj;
                }
            }
        }


        else
        {
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag("animalWolf"))
            {
                if (obj == this.gameObject) continue;

                AI_Tiger otherCarnivore = obj.GetComponent<AI_Tiger>();
                if (otherCarnivore != null && otherCarnivore.horny >= hornyThreshold && otherCarnivore.isAdult)
                {
                    return obj;
                }
            }
        }
        return null;
    }




    void HandleWandering()
    {
        if (isWalking && !isPerformingAnimation)
        {
            //animator.Play(walkForwardAnimation);
            GetMovementAnimation();
            animator.Play(mainMoveAnimation);

            walkCounter -= Time.deltaTime;

            Vector3 forward = carnivore.transform.forward;
            forward.y = 0f;
            carnivore.transform.position += forward.normalized * moveSpeed * Time.deltaTime;

            if (!borderCheck(currentLocation()))
            {
                walkCounter = 0;
            }

            if (walkCounter <= 0)
            {
                stopPosition = carnivore.transform.position;
                isWalking = false;
                StartCoroutine(DelaySitTostand());
                waitCounter = waitTime;
            }
        }
        else if (!isWalking && !isPerformingAnimation)
        {
            waitCounter -= Time.deltaTime;

            if (waitCounter <= 0)
            {
                StartCoroutine(RotateThenWalk());
            }
        }
    }

    void MoveTowardMate()
    {
        if (mateTarget == null) return;

        Vector3 targetPos = mateTarget.transform.position;
        Vector3 direction = (targetPos - carnivore.transform.position).normalized;

        // ✅ Smooth rotation
        Quaternion targetRot = Quaternion.LookRotation(direction);
        carnivore.transform.rotation = Quaternion.Slerp(carnivore.transform.rotation, targetRot, Time.deltaTime * 5f);

        GetMovementAnimation();
        animator.Play(mainMoveAnimation);
        carnivore.transform.position += direction * moveSpeed * Time.deltaTime;

        if (Vector3.Distance(carnivore.transform.position, targetPos) < matingDistance)
        {
            StartCoroutine(Mate());
        }
    }


    IEnumerator Mate()
    {
        if (mateTarget == null || isMating) yield break;

        isWalking = false;
        isPerformingAnimation = true;
        isInHeat = false;
        isMating = true;

        // Face mate
        Vector3 directionToMate = (mateTarget.transform.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToMate);
        transform.rotation = targetRotation;

        animator.Play(roarAnimation);
        yield return new WaitForSeconds(2f);

        // Spawn baby
        if (carnivorePrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.right;
            if (Random.Range(0, 2) == 1)
            {
                GameObject babyCarnivore = Instantiate(carnivorePrefab, spawnPos, Quaternion.identity);
                if (!isWolf)
                {
                    babyCarnivore.transform.localScale = new Vector3(30, 25, 35);
                }
                else
                {
                    babyCarnivore.transform.localScale = new Vector3(30, 17, 20);
                }
                AI_Tiger babyScript = babyCarnivore.GetComponent<AI_Tiger>();
                babyScript.age = 0f;

                Debug.Log("🐑 A baby is born!");
            }
        }

        // Reset both sheep
        horny = 0f;

        AI_Tiger otherCarnivore = mateTarget.GetComponent<AI_Tiger>();
        if (otherCarnivore != null)
        {
            otherCarnivore.horny = 0f;
            otherCarnivore.isMating = false;
            otherCarnivore.mateTarget = null;
            otherCarnivore.isPerformingAnimation = false; // ✅ This was missing
        }

        mateTarget = null;
        isMating = false;
        isPerformingAnimation = false;
    }





    GameObject FindNearestWater() // DONE
    {
        float closestDistance = Mathf.Infinity;
        GameObject closestWater = null;

        foreach (GameObject obj in detector.detectedObjects)
        {
            if (obj == null) continue;
            if (!obj.CompareTag("water")) continue;

            float dist = Vector3.Distance(carnivore.transform.position, obj.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestWater = obj;
            }
        }

        return closestWater;
    }
    void MoveTowardWater() // DONE
    {
        if (targetWater == null) return;

        Vector3 targetPos = targetWater.transform.position;
        targetPos.y = 0f;
        Vector3 dir = (targetPos - carnivore.transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        float angle = Quaternion.Angle(carnivore.transform.rotation, targetRotation);

        if (angle > 5f)
        {
            StartCoroutine(RotateToward(targetRotation));
            return;
        }

        //animator.Play(walkForwardAnimation);
        GetMovementAnimation();
        animator.Play(mainMoveAnimation);

        carnivore.transform.position += dir * moveSpeed * Time.deltaTime;

        if (Vector3.Distance(carnivore.transform.position, targetPos) <= eatingDistance)
        {
            StartCoroutine(DrinkWater());
        }
    }

    IEnumerator DrinkWater() // DONE
    {
        isEating = true;
        isWalking = false;
        isPerformingAnimation = true;

        animator.Play(attackAnimation);
        yield return new WaitForSeconds(2f); // Or duration of "drinking"

        thirst = 0f;
        targetWater = null;

        animator.Play(roarAnimation);
        yield return new WaitForSeconds(1f);

        isEating = false;
        isPerformingAnimation = false;
        waitCounter = waitTime;
    }
    IEnumerator Die()
    {
        isDead = true;
        isPerformingAnimation = true;
        isWalking = false;

        Debug.Log($"{gameObject.name} has died 💀");

        animator.Play(roarAnimation);
        yield return new WaitForSeconds(2f); // wait for animation

        // Then destroy the sheep GameObject
        Destroy(gameObject);
    }

    GameObject FindNearestHerbivore() // DONE
    {
        if (detector == null || detector.detectedObjects == null) return null;

        GameObject nearest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject obj in detector.detectedObjects)
        {
            if (obj == null) continue;
            if (obj.CompareTag("animalGoat") || obj.CompareTag("animalSheep"))
            {
                float dist = Vector3.Distance(carnivore.transform.position, obj.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = obj;
                }
            }
        }

        return nearest;
    }


    void UpdateScaleBasedOnAge() // DONE
    {
        if (!isWolf)
        {
            if (isCub)
            {
                transform.localScale = new Vector3(30, 25, 35);
            }
            else if (isAdult)
            {
                transform.localScale = new Vector3(60, 50, 70);
            }
            else if (isOld)
            {
                transform.localScale = new Vector3(65, 65, 75);
            }
        }
        else
        {
            if (isCub)
            {
                transform.localScale = new Vector3(10, 7, 10);
            }
            else if (isAdult)
            {
                transform.localScale = new Vector3(20, 15, 20);
            }
            else if (isOld)
            {
                transform.localScale = new Vector3(25, 20, 25);
            }
        }
    }

    void MoveTowardFood() // done
    {
        if (targetFood == null) return;

        // Calculate direction to target
        Vector3 targetPos = targetFood.transform.position;
        targetPos.y = 0f;
        Vector3 directionToTarget = (targetPos - carnivore.transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        float angle = Quaternion.Angle(carnivore.transform.rotation, targetRotation);

        // If not facing the food, rotate first
        if (angle > 5f)
        {
            StartCoroutine(RotateToward(targetRotation));
            return;
        }

        // Already facing, move forward
        //animator.Play(walkForwardAnimation);
        GetMovementAnimation();
        animator.Play(mainMoveAnimation);

        carnivore.transform.position += directionToTarget * moveSpeed * Time.deltaTime;

        if (Vector3.Distance(carnivore.transform.position, targetPos) <= eatingDistance)
        {
            StartCoroutine(EatFood(targetFood));
        }
    }

    IEnumerator RotateToward(Quaternion targetRot)
    {
        isPerformingAnimation = true;

        float angleDiff = Quaternion.Angle(carnivore.transform.rotation, targetRot);
        //string animationToPlay = turn90RAnimation;

        if (Vector3.Cross(carnivore.transform.forward, targetRot * Vector3.forward).y < 0)
     //      animationToPlay = turn90LAnimation;

        // Play turn animation
        //animator.Play(animationToPlay);
        yield return new WaitForSeconds(2f); // Adjust to match animation duration

        // Snap rotation (or use smooth rotate if you want to tween it)
        carnivore.transform.rotation = targetRot;

        isPerformingAnimation = false;
    }

    IEnumerator EatFood(GameObject food) // maybe done
    {
        isEating = true;
        isWalking = false;
        isPerformingAnimation = true;

        animator.Play(attackAnimation); // Or eating animation if you have one
        yield return new WaitForSeconds(1f);

        if (food != null)
        {
            AI_Sheep herbivoreScript = food.GetComponent<AI_Sheep>(); // Or AI_Goat if it's a goat
            if (herbivoreScript != null)
            {
                herbivoreScript.thirst = 139.999999f; // Call the Die method on the herbivore
            }
        }

        detector.detectedObjects.Remove(food);
         
        //Destroy(food);
        targetFood = null;

        hunger = 0f; // Reset hunger after eating

        animator.Play(attackAnimation);
        yield return new WaitForSeconds(2f);

        isEating = false;
        isPerformingAnimation = false;
        waitCounter = waitTime;
        GetMovementAnimation();
    }

    IEnumerator RotateThenWalk()
    {
        WalkDirection = Random.Range(0, 4);
        isPerformingAnimation = true;

        switch (WalkDirection)
        {
            case 0:
                Debug.Log("No turn");
                carnivore.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);
                break;

            case 1:
                Debug.Log("Turning right");
                // animator.Play(turn90RAnimation);
                yield return new WaitForSeconds(2f);
                currentY += 90f;
                carnivore.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);

                break;

            case 2:
                Debug.Log("Turning left");
                //animator.Play(turn90LAnimation);
                yield return new WaitForSeconds(2f);
                currentY -= 90f;
                carnivore.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);
                break;

            case 3:
                Debug.Log("Turning 180");
                Debug.Log("Turning first turn 90");
                //animator.Play(turn90LAnimation);
                yield return new WaitForSeconds(2f);
                currentY -= 90f;
                carnivore.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);
                Debug.Log("Turning second turn 90");
                //animator.Play(turn90LAnimation);
                yield return new WaitForSeconds(2f);
                currentY -= 90f;
                carnivore.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);

                break;
        }

        isPerformingAnimation = false;
        isWalking = true;

        walkCounter = walkTime;
    }

    public void hungerDepletion()
    {

        hunger += 0.00005f;
    }

    public bool isHungry()
    {
        if (hunger > 70)
        {

            return true;
        }
        else
        {
            return false;
        }
    }

    void GetMovementAnimation()
    {
        if (isInHeat || horny >= hornyThreshold)
        {
            moveSpeed = normalSpeed;
            mainMoveAnimation = walkForwardAnimation;
            mainidleAnimation = roarAnimation; 
        } 

        if (isOld)
        {
            moveSpeed = normalSpeed;
            mainMoveAnimation = walkForwardAnimation;
            mainidleAnimation = idleAnimation;
        }

        if (isHungry())
        {
            moveSpeed = hungrySpeed;
            mainMoveAnimation = runForwardAnimation;
            mainidleAnimation = roarAnimation;
        }
    }

    /*public void hungerState()
    {
        if (isHungry())
        {
            moveSpeed = hungrySpeed;
            mainMoveAnimation = runForwardAnimation;
            mainidleAnimation = roarAnimation;
        }
        else
        {
            moveSpeed = normalSpeed;
            mainMoveAnimation = walkForwardAnimation;
            mainidleAnimation = idleAnimation;
        }
    }*/

    public Vector3 currentLocation()
    {
        return carnivore.transform.position;
    }


    bool borderCheck(Vector3 currentLocation)
    {
        if (currentLocation.x > 460 || currentLocation.x < -473 || currentLocation.z > 1455 || currentLocation.z < 557)
        {
            return false;
        }
        else
        {
            return true;
        }

    }





    IEnumerator DelaySitTostand()
    {
        isPerformingAnimation = true;

        if (isWalking)
        {
            GetMovementAnimation();
            animator.Play(mainidleAnimation);
        }
        else
        {
            GetMovementAnimation();
            animator.Play(mainidleAnimation);
        }

        yield return new WaitForSeconds(2f);

        isPerformingAnimation = false;
    }
}
