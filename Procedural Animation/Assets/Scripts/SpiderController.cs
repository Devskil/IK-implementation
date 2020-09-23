using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderController : MonoBehaviour {
    //Head variables
    [SerializeField] Transform target;
    [SerializeField] Transform headBone;
    [SerializeField] float headMaxTurnAngle;
    [SerializeField] float headTrackingSpeed;

    //Eyes variables
    [SerializeField] Transform[] EyesBones;

    [SerializeField] float eyeTrackingSpeed;
    [SerializeField] float eyeMaxYRotation = 50;
    [SerializeField] float eyeMinYRotation = -50;
    [SerializeField] float eyeMaxXRotation = 50;
    [SerializeField] float eyeMinXRotation = -50;

    void Start() {
        if(target == null)
            return;
        if(headBone == null)
            return;
        if(EyesBones == null)
            return;
    }

    void LateUpdate() {
        HeadTrackingUpdate();
        EyesTrackingUpdate();
    }

    void HeadTrackingUpdate() {
        // Store the current head rotation since we will be resetting it
        Quaternion currentLocalRotation = headBone.localRotation;
        // Reset the head rotation so our world to local space transformation will use the head's zero rotation. 
        // Note: Quaternion.Identity is the quaternion equivalent of "zero"
        headBone.localRotation = Quaternion.identity;

        Vector3 targetWorldLookDir = target.position - headBone.position;
        Vector3 targetLocalLookDir = headBone.InverseTransformDirection(targetWorldLookDir);

        // Apply angle limit
        targetLocalLookDir = Vector3.RotateTowards(
            Vector3.forward,
            targetLocalLookDir,
            Mathf.Deg2Rad * headMaxTurnAngle, // Note we multiply by Mathf.Deg2Rad here to convert degrees to radians
            0 // We don't care about the length here, so we leave it at zero
        );

        // Get the local rotation by using LookRotation on a local directional vector
        Quaternion targetLocalRotation = Quaternion.LookRotation(targetLocalLookDir, Vector3.up);

        // Apply smoothing
        headBone.localRotation = Quaternion.Slerp(
            currentLocalRotation,
            targetLocalRotation, 
            1 - Mathf.Exp(-headTrackingSpeed * Time.deltaTime)
        );
    }

    void EyesTrackingUpdate() {
        // Note: We use head position here just because the gecko doesn't
        // look so great when cross eyed. To make it relative to the eye 
        // itself, subtract the eye's position instead of the head's.
        Quaternion targetEyeRotation = Quaternion.LookRotation(
        target.position - headBone.position, // toward target
        transform.up
        );

        foreach(var eye in EyesBones) {
            eye.rotation = Quaternion.Slerp(
            eye.rotation,
            targetEyeRotation,
            1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime)
            );
        }

        // This code is called after we set the rotations in the previous block
        foreach(var eye in EyesBones) {
            float EyeCurrentYRotation = eye.localEulerAngles.y;
            if (EyeCurrentYRotation > 180) {
                EyeCurrentYRotation -= 360;
            }
            float EyeClampedYRotation = Mathf.Clamp(
            EyeCurrentYRotation,
            eyeMinYRotation,
            eyeMaxYRotation
            );
            eye.localEulerAngles = new Vector3(
            eye.localEulerAngles.x,
            EyeClampedYRotation,
            eye.localEulerAngles.z
            );    
        }
    }

}
