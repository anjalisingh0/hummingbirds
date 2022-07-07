using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Manages a single flower with nectar

public class Flower : MonoBehaviour
{
   [Tooltip("The color when the flower is full")]
   public Color fullFlowerColor = new Color(1f, 0f, .3f);

   [Tooltip("The color when the flower is empty")]
   public Color emptyFlowerColor = new Color(.5f, 0f, 1f);

   /// The trigger collider representing the nectar 
   [HideInInspector] //make sure you can't see it in the inspector
   public Collider nectarCollider;

   // The solid collider representing the flower petals
   private Collider flowerCollider; 

   // The flower's material
   private Material flowerMaterial; 

    /// A vector pointing straight out of the flower 
   public Vector3 FlowerUpVector {
        get {
            return nectarCollider.transform.up;
        }

   }

    /// The center position of the nectar collider 
   public Vector3 FlowerCenterPosition{
        get {
            return nectarCollider.transform.position;
        }
   }

    /// The amount of nectar remaining in the flower 
   public float NectarAmount {get; private set;}

    /// Tells us whether the flower has any nectar remaining 
   public bool HasNectar{
    get{
        return NectarAmount > 0f;
    }
   }

    /// Attempts to remove nectar from the flower. Param amount is amount of nectar to remove.
    /// Returns the actual amount successfully removed. 
   public float Feed(float amount){
        //Track how much nectar was successfully taken (cannot take more than is available)
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

        //Subtract the nectar
        NectarAmount -= amount; 

        if (NectarAmount<=0){
            // No nectar remaining
            NectarAmount = 0;

            //Disable the flower and nectar colliders
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            //change flower clower to indicate that it is empty
            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);

        }
        //return amount of nectar that was taken
        return nectarTaken;

   }

    // Resets the flower 
    public void ResetFlower(){
        // Refill the nectar
        NectarAmount = 1f;

        //Enable the flower and nectar colliders
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);

        //Change flower color to indicate that it is full
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);

    }

    /// Called when the flower wakes up
    private void Awake(){
        // Find the flower's mesh renderer and get the main material
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        // Find flower and nectar colliders
        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
        
    } 



}
