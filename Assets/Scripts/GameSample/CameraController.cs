using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private struct PointInSpace
    {
        public Vector3 Position ;
        public float Time ;
    }
    
    [Tooltip("The transform to follow")]
    public Transform playerTransform;
     
    [SerializeField]
    [Tooltip("The offset between the target and the camera")]
    private Vector3 offset;
	
    [Tooltip("The delay before the camera starts to follow the target")]
    [SerializeField]
    private float delay = 0.5f;
     
    [SerializeField]
    [Tooltip("The speed used in the lerp function when the camera follows the target")]
    private float speed = 5;

    private Vector3 velocity = Vector3.zero;
     
    ///<summary>
    /// Contains the positions of the target for the last X seconds
    ///</summary>
    private Queue<PointInSpace> pointsInSpace = new Queue<PointInSpace>();
 
    void FixedUpdate ()
    {
        if (playerTransform == null) return;    
        // Add the current target position to the list of positions
        pointsInSpace.Enqueue( new PointInSpace() { Position = playerTransform.position, Time = Time.fixedDeltaTime } ) ;
		
        // Move the camera to the position of the target X seconds ago 
        while( pointsInSpace.Count > 0 && pointsInSpace.Peek().Time <= Time.time - delay + Mathf.Epsilon )
        {
            transform.position = Vector3.Lerp( transform.position, pointsInSpace.Dequeue().Position + offset, Time.fixedDeltaTime * speed);
        }
    }

    public void setTarget(Transform target)
    {
        playerTransform = target;
    }
}

