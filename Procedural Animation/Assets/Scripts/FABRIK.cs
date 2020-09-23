using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FABRIK : MonoBehaviour {
    public int chainLength = 1;
    public Transform target;
    public Transform pole;

    [Header("Solver Parameter")]
    public int iteration = 10;
    public float tolerance = 0.001f;
    [Range(0, 1)]
    public float snapBack = 1f;

    private float[] boneLength;
    private float totalLength;
    private Transform[] joints;
    private Vector3[] positions;
    private Vector3[] startDirectionSucc;
    private Quaternion[] startRotationBone;
    private Quaternion startRotationTarget;
    private Transform root;

    private void Awake() {
        Init();
    }

    void Init() {
        joints = new Transform[chainLength + 1];
        positions = new Vector3[chainLength + 1];
        boneLength = new float[chainLength];
        startDirectionSucc = new Vector3[chainLength + 1];
        startRotationBone = new Quaternion[chainLength + 1];

        /////////////////////////////////////////////////////
        //find root
            root = transform;
            for (var i = 0; i <= chainLength; i++) {
                if (root == null)
                    throw new UnityException("The chain value is longer than the ancestor chain!");
                root = root.parent;
            }

            //init target
            if (target == null) {
                target = new GameObject(gameObject.name + " Target").transform;
                SetPositionRootSpace(target, GetPositionRootSpace(transform));
            }
            startRotationTarget = GetRotationRootSpace(target);
        /////////////////////////////////////////////////////

        totalLength = 0;
        var current = this.transform;
        for(var i = joints.Length - 1; i >= 0; --i) {
            joints[i] = current;
            startRotationBone[i] = GetRotationRootSpace(current);

            if(i == joints.Length - 1) {
                startDirectionSucc[i] = GetPositionRootSpace(target) - GetPositionRootSpace(current);
            }
            else {
                startDirectionSucc[i] = GetPositionRootSpace(joints[i + 1]) - GetPositionRootSpace(current);
                boneLength[i] = (joints[i + 1].position - current.position).magnitude;
                totalLength += boneLength[i];
            }
            current = current.parent;
        }
    }

    private void LateUpdate() {
        ResolveIK();
    }

    void ResolveIK() {
        if(target == null)
            return;
        if(boneLength.Length != chainLength)
            Init();

        //get joints position
        for(var i = 0; i < joints.Length; ++i) {
            positions[i] = GetPositionRootSpace(joints[i]);
        }

        var targetPosition = GetPositionRootSpace(target);
        var targetRotation = GetRotationRootSpace(target);
        var inRange = (targetPosition - GetPositionRootSpace(joints[0])).magnitude <= totalLength;

        if(!inRange) {
            var direction = (targetPosition - positions[0]).normalized;
            for(var i = 1; i < positions.Length; ++i) {
                positions[i] = positions[i - 1] + direction * boneLength[i - 1];
            }
        }
        else {
            for (int i = 0; i < positions.Length - 1; i++) {
                positions[i + 1] = Vector3.Lerp(positions[i + 1], positions[i] + startDirectionSucc[i], snapBack);
            }

            for(var ite = 0; ite < iteration; ++iteration) {
                ForwardReach(targetPosition);
                BackwardReach();

                if((positions[positions.Length - 1] - targetPosition).sqrMagnitude < tolerance * tolerance)
                    break;
            }
        }
        if(pole != null)
            MoveTowardPole();

        //set position & rotation
            for (int i = 0; i < positions.Length; i++)
            {
                if (i == positions.Length - 1)
                    SetRotationRootSpace(joints[i], Quaternion.Inverse(targetRotation) * startRotationTarget * Quaternion.Inverse(startRotationBone[i]));
                else
                    SetRotationRootSpace(joints[i], Quaternion.FromToRotation(startDirectionSucc[i], positions[i + 1] - positions[i]) * Quaternion.Inverse(startRotationBone[i]));
                SetPositionRootSpace(joints[i], positions[i]);
            }

    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    void ForwardReach(Vector3 targetPosition) {
        for(var i = positions.Length - 1; i > 0; --i) {
            if(i == positions.Length - 1) {
                positions[i] = targetPosition;
            }
            else {
                var direction = (positions[i] - positions[i + 1]).normalized;
                positions[i] = positions[i + 1] + direction * boneLength[i];
            }
        }
    }

    void BackwardReach() {
        for(var i = 1; i < positions.Length; ++i) {
            var direction = (positions[i] - positions[i - 1]).normalized;
            positions[i] = positions[i - 1] + direction * boneLength[i - 1];
        }
    }
    
    void MoveTowardPole() {
        var polePosition = GetPositionRootSpace(pole);
        for(var i = 1; i < positions.Length - 1; ++i) {
            var plane = new Plane(positions[i + 1] - positions[i - 1], positions[i - 1]);
            var projectPole = plane.ClosestPointOnPlane(pole.position);
            var projectedJoint = plane.ClosestPointOnPlane(positions[i]);
            var angle = Vector3.SignedAngle(projectedJoint - positions[i - 1], projectPole - positions[i - 1], plane.normal);
            positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (positions[i] - positions[i - 1]) + positions[i - 1];
        }
    }
    
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private Vector3 GetPositionRootSpace(Transform current) {
            if (root == null)
                return current.position;
            else
                return Quaternion.Inverse(root.rotation) * (current.position - root.position);
        }

        private void SetPositionRootSpace(Transform current, Vector3 position) {
            if (root == null)
                current.position = position;
            else
                current.position = root.rotation * position + root.position;
        }

        private Quaternion GetRotationRootSpace(Transform current) {
            if (root == null)
                return current.rotation;
            else
                return Quaternion.Inverse(current.rotation) * root.rotation;
        }

        private void SetRotationRootSpace(Transform current, Quaternion rotation)
        {
            if (root == null)
                current.rotation = rotation;
            else
                current.rotation = root.rotation * rotation;
        }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void OnDrawGizmos() {
        var current = this.transform;
        for(var i = 0; i < chainLength && current != null && current.parent != null; ++i) {
            // var scale = Vector3.Distance(current.position, current.parent.position) * 0.1f;
            // Handles.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector3.up, current.parent.position - current.position), new Vector3(scale, Vector3.Distance(current.parent.position, current.position), scale));
            // Handles.color = Color.green;
            // Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
            // current = current.parent;
            Debug.DrawLine(current.position, current.parent.position, Color.green);
            current = current.parent;
        } 
    }
}
