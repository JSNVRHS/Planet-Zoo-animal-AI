using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DayNightCycleStuff;


namespace herbivores
{
    public class AI_Sheep : MonoBehaviour
    {
        public Animator animator;
        public string walkForwardAnimation = "walk_forward";

        //[SerializeField] private DayNightCycle dayNightCycle;

        public string walkBackwardAnimation = "walk_backwards";
        public string runForwardAnimation = "run_forward";
        public string turn90LAnimation = "turn_90_L";
        public string turn90RAnimation = "turn_90_R";
        public string trotAnimation = "trot_forward";
        public string sittostandAnimation = "sit_to_stand";
        public string standtositAnimation = "stand_to_sit";
        public string eatingAnimation = "GoatSheep_eating";

        public GameObject dayNightCycle;

        public GameObject sheep;
        public float moveSpeed = 50.0f;

        Vector3 stopPosition;

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
        public float hungerIncreaseRate = 0.005f;
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
        public GameObject sheepPrefab; // Assign your sheep prefab in Inspector
        private float matingDistance = 3f;
        private bool isMating = false;


        public bool isGoat;

        public bool locationChip = false;

        public bool isItNight = true;





        void Start()
        {
            animator = GetComponent<Animator>();

            walkTime = Random.Range(3, 6);
            waitTime = Random.Range(5, 7);

            waitCounter = waitTime;
            walkCounter = walkTime;

            StartCoroutine(DelaySitTostand());  // Start idle
        }

        /*
        void Update()
        {
            if (isWalking && !isPerformingAnimation)
            {
                animator.Play(walkForwardAnimation);
                walkCounter -= Time.deltaTime;

                // Move the sheep forward
                Vector3 forward = sheep.transform.forward;
                forward.y = 0f;
                sheep.transform.position += forward.normalized * moveSpeed * Time.deltaTime;

                if (!borderCheck(currentLocation())) {
                    walkCounter = 0;
                }

                if (walkCounter <= 0)
                {
                    stopPosition = sheep.transform.position;
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
        */

        void Update()
        {
            if (isDead || isEating || isPerformingAnimation || isMating) return;
           //duringNight();
            UpdateScaleBasedOnAge();
            horny += hornyIncreaseRate;
            age += ageIncreaseRate;

            if (age >= maxAge)
            {
                StartCoroutine(DieFromAge());
                return;
            }

            hunger += hungerIncreaseRate;
            thirst += thirstIncreaseRate;

            if (thirst >= thirstDeathThreshold)
            {
                StartCoroutine(DieFromThirst());
                return;
            }

            if (hunger >= hungerDeathThreshold)
            {
                StartCoroutine(DieFromHunger());
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
                targetFood = FindNearestNature();
            }

            if (hunger >= hungerThreshold && targetFood != null)
            {
                MoveTowardFood();
                return;
            }

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

       


        GameObject FindMate()
        {
            if (isGoat)
            {
                foreach (GameObject obj in GameObject.FindGameObjectsWithTag("animalGoat"))
                {
                    if (obj == this.gameObject) continue;

                    AI_Sheep otherSheep = obj.GetComponent<AI_Sheep>();
                    if (otherSheep != null && otherSheep.horny >= hornyThreshold && otherSheep.isAdult)
                    {
                        return obj;
                    }
                }
            }


            else
            {
                foreach (GameObject obj in GameObject.FindGameObjectsWithTag("animalSheep"))
                {
                    if (obj == this.gameObject) continue;

                    AI_Sheep otherSheep = obj.GetComponent<AI_Sheep>();
                    if (otherSheep != null && otherSheep.horny >= hornyThreshold && otherSheep.isAdult)
                    {
                        return obj;
                    }
                }
            }
            return null;
        }

        void MoveTowardMate()
        {
            if (mateTarget == null) return;

            Vector3 targetPos = mateTarget.transform.position;
            Vector3 direction = (targetPos - sheep.transform.position).normalized;

            // ✅ Smooth rotation
            Quaternion targetRot = Quaternion.LookRotation(direction);
            sheep.transform.rotation = Quaternion.Slerp(sheep.transform.rotation, targetRot, Time.deltaTime * 5f);

            animator.Play(GetMovementAnimation());
            sheep.transform.position += direction * moveSpeed * Time.deltaTime;

            if (Vector3.Distance(sheep.transform.position, targetPos) < matingDistance)
            {
                StartCoroutine(Mate());
            }
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
                isItNight=false;
                SetAnimalVisible(true);
            }   
        }

        void UpdateScaleBasedOnAge()
        {
            if (!isGoat)
            {
                if (isCub)
                {
                    transform.localScale = new Vector3(15, 15, 15);
                }
                else if (isAdult)
                {
                    transform.localScale = new Vector3(30, 30, 30);
                }
                else if (isOld)
                {
                    transform.localScale = new Vector3(35, 35, 35);
                }
            }
            else
            {
                if (isCub)
                {
                    transform.localScale = new Vector3(30, 17, 20);
                }
                else if (isAdult)
                {
                    transform.localScale = new Vector3(60, 35, 40);
                }
                else if (isOld)
                {
                    transform.localScale = new Vector3(65, 40, 45);
                }
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

            animator.Play(standtositAnimation);
            yield return new WaitForSeconds(2f);

            // Spawn baby
            if (sheepPrefab != null)
            {
                if (Random.Range(0, 3) == 1 || Random.Range(0, 3) == 2)
                {
                    Vector3 spawnPos = transform.position + transform.right;
                    GameObject babySheep = Instantiate(sheepPrefab, spawnPos, Quaternion.identity);
                    if (!isGoat)
                    {
                        babySheep.transform.localScale = new Vector3(15, 15, 15);
                    }
                    else
                    {
                        babySheep.transform.localScale = new Vector3(30, 17, 20);
                    }
                    AI_Sheep babyScript = babySheep.GetComponent<AI_Sheep>();
                    babyScript.age = 0f;

                    Debug.Log("🐑 A baby sheep is born!");
                }
            }

            // Reset both sheep
            horny = 0f;

            AI_Sheep otherSheep = mateTarget.GetComponent<AI_Sheep>();
            if (otherSheep != null)
            {
                otherSheep.horny = 0f;
                otherSheep.isMating = false;
                otherSheep.mateTarget = null;
                otherSheep.isPerformingAnimation = false; // ✅ This was missing
            }

            mateTarget = null;
            isMating = false;
            isPerformingAnimation = false;
        }







        string GetMovementAnimation()
        {
            if (isInHeat || horny >= hornyThreshold)
            {
                return trotAnimation; // Trot when horny
            }

            if (isOld)
            {
                return walkForwardAnimation; // Walk when old
            }

            return runForwardAnimation; // Default walk if not horny
        }



        public IEnumerator DieFromAge()
        {
            isDead = true;
            isPerformingAnimation = true;
            isWalking = false;

            Debug.Log($"{gameObject.name} died of old age 🪦");

            animator.Play(standtositAnimation);
            yield return new WaitForSeconds(2f);

            Destroy(gameObject);
        }

        void HandleWandering()
        {
            if (isWalking && !isPerformingAnimation)
            {
                //animator.Play(walkForwardAnimation);
                animator.Play(GetMovementAnimation());

                walkCounter -= Time.deltaTime;

                Vector3 forward = sheep.transform.forward;
                forward.y = 0f;
                sheep.transform.position += forward.normalized * moveSpeed * Time.deltaTime;

                if (!borderCheck(currentLocation()))
                {
                    walkCounter = 0;
                }

                if (walkCounter <= 0)
                {
                    stopPosition = sheep.transform.position;
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

        GameObject FindNearestWater()
        {
            float closestDistance = Mathf.Infinity;
            GameObject closestWater = null;

            foreach (GameObject obj in detector.detectedObjects)
            {
                if (obj == null) continue;
                if (!obj.CompareTag("water")) continue;

                float dist = Vector3.Distance(sheep.transform.position, obj.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestWater = obj;
                }
            }

            return closestWater;
        }
        void MoveTowardWater()
        {
            if (targetWater == null) return;

            Vector3 targetPos = targetWater.transform.position;
            targetPos.y = 0f;
            Vector3 dir = (targetPos - sheep.transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(dir);
            float angle = Quaternion.Angle(sheep.transform.rotation, targetRotation);

            if (angle > 5f)
            {
                StartCoroutine(RotateToward(targetRotation));
                return;
            }

            //animator.Play(walkForwardAnimation);
            animator.Play(GetMovementAnimation());

            sheep.transform.position += dir * moveSpeed * Time.deltaTime;

            if (Vector3.Distance(sheep.transform.position, targetPos) <= eatingDistance)
            {
                StartCoroutine(DrinkWater());
            }
        }

        IEnumerator DrinkWater()
        {
            isEating = true;
            isWalking = false;
            isPerformingAnimation = true;

            animator.Play(standtositAnimation);
            yield return new WaitForSeconds(2f); // Or duration of "drinking"

            thirst = 0f;
            targetWater = null;

            animator.Play(sittostandAnimation);
            yield return new WaitForSeconds(1f);

            isEating = false;
            isPerformingAnimation = false;
            waitCounter = waitTime;
        }

        //new stuff
        GameObject FindNearestNature()
        {
            if (detector == null || detector.detectedObjects == null) return null;

            GameObject nearest = null;
            float minDist = Mathf.Infinity;

            foreach (GameObject obj in detector.detectedObjects)
            {
                if (obj == null) continue;
                if (obj.CompareTag("nature"))
                {
                    float dist = Vector3.Distance(sheep.transform.position, obj.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = obj;
                    }
                }
            }

            return nearest;
        }
        IEnumerator DieFromThirst()
        {
            isDead = true;
            isPerformingAnimation = true;
            isWalking = false;

            Debug.Log($"{gameObject.name} has died of thirst 💀");

            animator.Play(standtositAnimation);
            yield return new WaitForSeconds(2f); // wait for animation

            // Then destroy the sheep GameObject
            Destroy(gameObject);
        }

        IEnumerator DieFromHunger()
        {
            isDead = true;
            isPerformingAnimation = true;
            isWalking = false;

            Debug.Log($"{gameObject.name} has died of hunger 💀");

            animator.Play(standtositAnimation);
            yield return new WaitForSeconds(2f); // wait for animation

            // Then destroy the sheep GameObject
            Destroy(gameObject);
        }


        void MoveTowardFood()
        {
            if (targetFood == null) return;

            // Calculate direction to target
            Vector3 targetPos = targetFood.transform.position;
            targetPos.y = 0f;
            Vector3 directionToTarget = (targetPos - sheep.transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            float angle = Quaternion.Angle(sheep.transform.rotation, targetRotation);

            // If not facing the food, rotate first
            if (angle > 5f)
            {
                StartCoroutine(RotateToward(targetRotation));
                return;
            }

            // Already facing, move forward
            //animator.Play(walkForwardAnimation);
            animator.Play(GetMovementAnimation());

            sheep.transform.position += directionToTarget * moveSpeed * Time.deltaTime;

            if (Vector3.Distance(sheep.transform.position, targetPos) <= eatingDistance)
            {
                StartCoroutine(EatFood(targetFood));
            }
        }

        IEnumerator RotateToward(Quaternion targetRot)
        {
            isPerformingAnimation = true;

            float angleDiff = Quaternion.Angle(sheep.transform.rotation, targetRot);
            string animationToPlay = turn90RAnimation;

            if (Vector3.Cross(sheep.transform.forward, targetRot * Vector3.forward).y < 0)
                animationToPlay = turn90LAnimation;

            // Play turn animation
            animator.Play(animationToPlay);
            yield return new WaitForSeconds(2f); // Adjust to match animation duration

            // Snap rotation (or use smooth rotate if you want to tween it)
            sheep.transform.rotation = targetRot;

            isPerformingAnimation = false;
        }

        IEnumerator EatFood(GameObject food)
        {
            isEating = true;
            isWalking = false;
            isPerformingAnimation = true;

            animator.Play(standtositAnimation); // Or eating animation if you have one
            yield return new WaitForSeconds(2f);

            detector.detectedObjects.Remove(food);
            Destroy(food);
            targetFood = null;

            hunger = 0f; // Reset hunger after eating

            animator.Play(sittostandAnimation);
            yield return new WaitForSeconds(1f);

            isEating = false;
            isPerformingAnimation = false;
            waitCounter = waitTime;
        }




        IEnumerator RotateThenWalk()
        {
            WalkDirection = Random.Range(0, 4);
            isPerformingAnimation = true;

            switch (WalkDirection)
            {
                case 0:
                    Debug.Log("No turn");
                    sheep.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);
                    break;

                case 1:
                    Debug.Log("Turning right");
                    animator.Play(turn90RAnimation);
                    yield return new WaitForSeconds(2f);
                    animator.Play(turn90RAnimation);
                    yield return new WaitForSeconds(2f);
                    currentY += 90f;
                    sheep.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);

                    break;

                case 2:
                    Debug.Log("Turning left");
                    animator.Play(turn90LAnimation);
                    yield return new WaitForSeconds(2f);
                    animator.Play(turn90LAnimation);
                    yield return new WaitForSeconds(2f);
                    currentY -= 90f;
                    sheep.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);
                    break;

                case 3:
                    Debug.Log("Turning 180");
                    Debug.Log("Turning first turn 90");
                    animator.Play(turn90LAnimation);
                    yield return new WaitForSeconds(2f);
                    animator.Play(turn90LAnimation);
                    yield return new WaitForSeconds(2f);
                    currentY -= 90f;
                    sheep.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);
                    Debug.Log("Turning second turn 90");
                    animator.Play(turn90LAnimation);
                    yield return new WaitForSeconds(2f);
                    animator.Play(turn90LAnimation);
                    yield return new WaitForSeconds(2f);
                    currentY -= 90f;
                    sheep.transform.localRotation = Quaternion.Euler(currentX, currentY, currentZ);

                    break;
            }

            isPerformingAnimation = false;
            isWalking = true;

            walkCounter = walkTime;
        }


        public Vector3 currentLocation()
        {
            return sheep.transform.position;
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
                animator.Play(sittostandAnimation);
            }
            else
            {
                animator.Play(standtositAnimation);
            }

            yield return new WaitForSeconds(2f);

            isPerformingAnimation = false;
        }
    }
}
