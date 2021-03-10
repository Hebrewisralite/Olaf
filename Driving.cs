using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Driving : MonoBehaviour
{
    #region PublicVaribles
    //[HideInInspector]public int UnitIndex;
    //bool isalive;
    //public float speed;
    //public Transform obj;


    //Current FlowField This Driver is Following
    [HideInInspector]public FlowField MyField;
    //The Length of the Vectors
    public float Maxspeed;
    //The Weight that is Applied to the Steering Vector
    public float SteerWeight;
    //The Weight that is Applied to the Flee Vector
    public float SeperateWeight;
    //The T for the Lerp when Rotating
    public float Smoothing;
    //Used for Capping the Speed and Used for a lerp with the Arrive 
    public float CappedSpeed;
    //The Distance from the Target Before The Corotine stops
    public float StopDistance;
    //Min radius is used for spacing the Drivers and the Max is used to determine if we are going to calculate them in the flee script
    public float MinRadius,MaxRadius;
    //The Velocity we want to have
    [HideInInspector]public Vector3 DesireVelocity;
    #endregion


    #region Cache
    //Are We Moving?
    [HideInInspector]public bool ismoving = false;
    //Have we Passed the Arriving radius?
    [HideInInspector]public bool Arrived = true;
    //Used to stop the Driver
    private bool Stop;
    //Help to Calculate 
    Vector3 SeperateVector, Distancevector, DestinationVector, TargetVector;
    //Steering Vector
    Vector3 Steering;
    Vector3 Acceleration = Vector3.zero;
    Vector3 Velocity = Vector3.zero;
    //This is for Rotating the Driver
    Vector3 ToDesired;
    //For the Quaternion
    private float AngleRotation;
    //The Driving Action
    private IDriveable DrivingAction;
    //The Position that helps Driver into formation
    [HideInInspector]public Vector3 FakeTarget;
    //Used to Update the FlowFields Array of Vetcor3s
    [HideInInspector]public int PositionIndex;
    // Used to Calculate Arriving Speed. Its the Max in the lerp
    private Vector3 ArrivePosition;
    #endregion

    IEnumerator Transverse()
    {
        Stop = false;
        while (ismoving)
        {
            //Updating Drivers Position in The FlowFields Array of vector3
            MyField.Drivers[PositionIndex] = transform.position;
            //Rotating the Driver
            SteerRotation();

            //If we have not passed the Arrive Radius then Keep Distance
            if(!Arrived){
                SeperateVector = DrivingAction.Flee(MyField.Drivers, MinRadius);
                SeperateVector *= DrivingAction.ApplyWeight(SeperateWeight);
            }

            //Follows FlowField
            SeekFlowField();

            //Apply All Weights
            ApplyForces();
            Velocity += Acceleration;
            Velocity.Normalize();
            Velocity *= Maxspeed;

            //Clear The y Value
            Velocity.y *= 0;
            transform.position = Vector3.Lerp(transform.position, transform.position + Velocity, Smoothing * Time.deltaTime);
            Acceleration *= 0;

            //We have Arrived
            if(Stop && Arrived)
            {
                ismoving = false;
                FakeTarget = transform.position;
                break;
            }
            yield return null;
        }
    }
    public void StartMoving(FlowField FF)
    {
        MyField = FF;
        if(!ismoving)
        {
            Arrived = false;
            ismoving = true;
            StartCoroutine(Transverse());
        }
    }
    void ApplyForces(){
        Acceleration += DrivingAction.ApplyForce(Steering);
        Acceleration += DrivingAction.ApplyForce(SeperateVector);
    }
    void SeekFlowField(){
        if(!Arrived)
        {
            if(MyField != null){
                DestinationVector = MyField.TargetCell.WrldPos - transform.position;
                if(DestinationVector.sqrMagnitude > SelectionManager.Instance.ArriveRadius * SelectionManager.Instance.ArriveRadius)
                {
                    //If we are outside the circle around the target cell then continue to follow flow field
                    TargetVector = MyField.GetCellDirection(transform.position) + transform.position;
                    DesireVelocity = TargetVector - transform.position;
                    Maxspeed = CappedSpeed;
                }
                else{
                    //Inside the radius
                    ArrivePosition = FakeTarget - transform.position;
                    Arrived = true;
                    MyField.UnitsArrived++;
                    MyField.CheckProgress();
                }
            }
        }
        else
        {
            //Go to the FakeTarget
            DesireVelocity = FakeTarget - transform.position;
            Distancevector = DesireVelocity;
            if(Distancevector.sqrMagnitude < StopDistance * StopDistance){
                Stop = true;
            }
            Maxspeed = DrivingAction.Arrive(DesireVelocity, ArrivePosition.magnitude, 0, CappedSpeed);
        }

        Maxspeed = Mathf.Abs(Maxspeed);
        Steering = DrivingAction.Seek(DesireVelocity, Velocity, Maxspeed);
        Steering *= DrivingAction.ApplyWeight(SteerWeight);
    }

    public void PathInterrupted(){
        StopCoroutine(Transverse());
        MyField.UnitsArrived++;
        MyField.CheckProgress();
    }
    void SteerRotation(){
        ToDesired = (Velocity + transform.position) - transform.position;
        AngleRotation = Mathf.Atan2(ToDesired.z, ToDesired.x) * Mathf.Rad2Deg;
        //AngleRotation = Vector3.Angle(transform.position, ToDesired);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0,(transform.rotation.y - AngleRotation) + 90,0), Smoothing * Time.deltaTime);
    }
    private void OnDrawGizmos()
    {
        if(!Arrived){
            Gizmos.DrawSphere(transform.position + Vector3.up * 2, 1);
        }

        if(ismoving){
            Gizmos.DrawCube(transform.position + Vector3.up * 3, Vector3.one);
        }

        Gizmos.DrawWireSphere(transform.position, MaxRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, MinRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawCube(ToDesired + transform.position, Vector3.one * .25f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, FakeTarget);
        Gizmos.color = Color.black;
        Gizmos.DrawCube(transform.forward + transform.position, Vector3.one * .25f);
    }
    void OnEnable(){
        DrivingAction = GetComponent<IDriveable>();
    }
}
