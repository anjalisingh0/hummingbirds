using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.sensors;
using UnityEngine;

//A hummingbird Machine Learning Agent. Makes decisions using neural networks. 
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")] 
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f; 

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip; 

    [Tooltip("The agent's camera")]
    public Camera agentCamera; //for playing the game (player uses camera)

    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainingMode; 

    // The rigidbody of the agent
    new private Rigidbody rigidbody;

    // The flower area that the agent is in
    private FlowerArea flowerArea; 

    // The nearest flower to the agent
    private Flower nearestFlower; 

    // Allows for smoother pitch changes
    private float smoothPitchChange = 0f;

    // Allows for smoother yaw changes
    private float smoothYawChange = 0f; 

    // Maximum angle that the bird can pitch up or down
    private const float MaxPitchAngle = 80f;

    // Maximum distance from the beak tip to accept nectar collision 
    private const float beakTipRadius = 0.008f; 

    // Whether the agent is frozen (intentionally not flying)
    private bool frozen = false; 

    // The amount of nectar the agent has obtained this episode 
    public float NectarObtained{get; private set;} 

    // Initialize the agent
    public override void Initialize(){
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        // If not training mode, no max step, play forever
        if (!trainingMode) MaxStep = 0;

    }

    //Reset the agent when an episode begins
    public override void OnEpisodeBegin(){
        if (trainingMode){
            //only reset flowers in training when there is one agent per area
            flowerArea.ResetFlowers();
        }
        //reset nectar obtained
        NectarObtained = 0f; 

        // Zero out velocities so that movement stops before a new episode begins
        // Important for ML Agents
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero 

        //Default to spawning in front of a flower
        bool inFrontOfFlower = true; 
        if (trainingMode){
            //Spawn in front of flower 50% of the time during training
            inFrontOfFlower = UnityEngine.Random.value>0.5f;
        }
        // Move the agent to a new random position
        MoveToSafeRandomPosition(inFrontOfFlower);

        // Recalculate the nearest flower now that the agent has moved
        UpdateNearestFlower();

    }

    /// Called when an action is received from either the player input or the neural network. 
    /// vectorAction[i] represents:
    /// Index 0: move vector x (+1 = right, -1= left)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move vector z (+1 = forward, -1 = backward)
    /// Index 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// Index 4: yaw angle (+1 = turn right, -1= turn left)
    /// 
    public override void onActionReceived(float[] vectorAction){

        //Don't take actions if frozen
        if (frozen) return;
        // Calculate movement vector
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        
        //Add force in the direction of the move vector
        rigidbody.AddForce(move * moveForce);

        // Get the current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles; 

        // Calculate pitch and yaw rotation
        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4]; 

        // Calculate smooth rotation changes 
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime); //more frequently

        //Calculate new pitch and yaw based on smoothed values
        //Clamp pitch to avoid flipping upside down
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed; 
        if (pitch>180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed; 
        
        // Apply the new rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// Collect vector observations from the environment
    public override void CollectObservations(VectorSensor sensor){
            // Observe the agent's local rotation (4 observations)
            sensor.AddObservation(transform.localRotation.normalized);

            //Get a vector from the beak tip to the nearest flower
            Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

            //Observe a normalized vector pointing to the nearest flower (3 observations)
            sensor.AddObservation(toFlower.normalized);



    }

    // Move the agent to a safe random position (i.e. does not collide with anything)
    // If in front of flower, also point the beak at the flower
    private void MoveToSafeRandomPosition(bool inFrontOfFlower){
        
        bool safePositionFound = false;
        int attemptsRemaining = 100; // Prevent an infinite loop 
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion;

        // Loop until a safe position is found or we run out of attempts
        while(!safePositionFound && attemptsRemaining >0){
            attemptsRemaining--;
            if (inFrontOfFlower){
                //Pick a random flower
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.count)];

                // Position 10 to 20 cm in front of the flower
                float distanceFromFlower = UnityEngine.Random.Range(.1f,.2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                // Point beak at flower (bird's head is center of transform)
                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition; //vector from the potential
                //position where we're going to spawn to the center of the flower 

                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else {
                // Pick a random height from the ground
                float height = UnityEngine.Random.Range(1.2f,  2.5f);

                // Pick a random radius from the center of the area
                float radius = UnityEngine.Random.Range(2f,7f);

                // Pick a random direction rotated around the y axis
                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f,180f),0f);

                //Combine height, radius, and direction to pick a potential position
                potentialPosition = flowerArea.transform.position + Vector3.up*height+direction*Vector3.forward*radius;

                //Choose and set random starting pitch and yaw
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEnginer.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch,yaw,0f);

            }

            // Check to see if the agent will collide with anything
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f); //at least a bubble 10 cm across for this bird to fit inside
            
            //Safe position has been found if no colliders are overlapped
            safePositionFound = colliders.Length==0;

            }

            Debug.Assert(safePositionFound, "Cound not find a safe position to spawn");
            //Set the position and rotation
            transform.position = potentialPosition;
            transform.rotation = potentialRotation; 
        }

// Update the nearest flower to the agent
private void UpdateNearestFlower(){
    foreach (Flower flower in flowerArea.Flowers) { //every flower that's in the same area the bird is in
        if (nearestFlower == null && flower.HasNectar){
            //No current nearest flower and this flower has nectar, so set to this flower
            nearestFlower = flower;
        }
        else if (flower.HasNectar){
            // Calculate distance to this flower and distance to the current nearest flower
            float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
            float distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);
            
            //If current nearest flower is empty or this flower is closer, update nearest flower
            if (!nearestFlower.HasNectar || distanceToFlower<distanceToCurrentNearestFlower){
                nearestFlower = flower; 
            }
        }
    }
}


}
