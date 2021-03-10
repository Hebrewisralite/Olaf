using UnityEngine;

[RequireComponent(typeof(Driving))]
public class DrivingwithFlowField : MonoBehaviour, IDriveable
{
    public float Handling;
    private Vector3 steering;
    private Vector3 distanceFromIntruders;
    private Vector3 sum;
    private Vector3 seperateVector;
    int divider;

    public float Arrive(Vector3 VectorToTarget, float Max, float Min, float CappedSpeed){
        return CappedSpeed * (VectorToTarget.magnitude - Min) / (Max - Min);
    }
    public Vector3 Seek(Vector3 DesireVelocity, Vector3 Velocity, float MaxSpeed){
        DesireVelocity.Normalize();
        DesireVelocity *= MaxSpeed;
        steering = DesireVelocity - Velocity;
        steering = Vector3.ClampMagnitude(steering, Handling);
        return steering;
    }
    public Vector3 Flee(Vector3[] Intruders, float Radius){
        sum *= 0;
        divider = 0;

        for(int i = 0; i < Intruders.Length; i++){

            if(Intruders[i] == transform.position){
                continue;
            }

            distanceFromIntruders = Intruders[i] - transform.position;
            if(distanceFromIntruders.sqrMagnitude < Radius * Radius){
                seperateVector = transform.position - Intruders[i];
                seperateVector.Normalize();
                seperateVector /= distanceFromIntruders.magnitude;
                sum += seperateVector;
                divider++;
            }
        }

        if(divider != 0){
            sum /= divider;
        }

        return sum;
    }
    public float ApplyWeight(float ForceWeight){
        return ForceWeight;
    }
    public Vector3 ApplyForce(Vector3 ForceToApply){
        return ForceToApply;
    }
}
