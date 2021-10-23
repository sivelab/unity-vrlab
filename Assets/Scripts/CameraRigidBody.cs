/* 
Copyright © 2016 NaturalPoint Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. 
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Implements live tracking of streamed OptiTrack rigid body data onto an object.
/// </summary>
public class CameraRigidBody : MonoBehaviour
{
    [Tooltip("The object containing the OptiTrackStreamingClient script.")]
    public OptitrackStreamingClient StreamingClient;

    [Tooltip("The Streaming ID of the rigid body in Motive")]
    public Int32 RigidBodyId;

    [Tooltip("Subscribes to this asset when using Unicast streaming.")]
    public bool NetworkCompensation = true;


    // Smooth position
    Vector3 motiveHeadsetPos = Vector3.zero;
    Vector3 halfScalar = new Vector3(0.5f, 0.5f, 0.5f);

    List<UnityEngine.XR.InputDevice> inputDevices = new List<UnityEngine.XR.InputDevice>();


    void Start()
    {
        // If the user didn't explicitly associate a client, find a suitable default.
        if ( this.StreamingClient == null )
        {
            this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();

            // If we still couldn't find one, disable this component.
            if ( this.StreamingClient == null )
            {
                Debug.LogError( GetType().FullName + ": Streaming client not set, and no " + typeof( OptitrackStreamingClient ).FullName + " components found in scene; disabling this component.", this );
                this.enabled = false;
                return;
            }
        }

        this.StreamingClient.RegisterRigidBody( this, RigidBodyId );

        UnityEngine.XR.InputDevices.GetDevices(inputDevices);

        foreach (var device in inputDevices)
        {
            Debug.Log(string.Format("Device found with name '{0}' and role '{1}'", device.name, device.role.ToString()));
        }

    }


#if UNITY_2017_1_OR_NEWER
    void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }


    void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }


    void OnBeforeRender()
    {
        UpdatePose();
    }
#endif


    void Update()
    {
        UpdatePose();


        //// Smoothing for vision center object
        //// Should be its own class but ehhh
        //alignmentLastPos.Enqueue(this.transform.position + this.transform.TransformDirection(new Vector3(0, 0, 1)) * visionObjectDist);
        //if (alignmentLastPos.Count > maxQueueSize)
        //{
        //    alignmentLastPos.Dequeue();
        //}

        //// Average values
        //Vector3 total = Vector3.zero;
        //foreach (Vector3 pos in alignmentLastPos)
        //{
        //    total += pos;
        //}

        //// Set pos to average
        //visionAlignmentObject.transform.position = total / (float)alignmentLastPos.Count;

    }

    public Vector3 vec3OculusToMotiveRotation = new Vector3(0, 90, 0);
    public Vector3 eyePositionOffsetFromTracker = new Vector3(0, 0, 0);
    public GameObject visionAlignmentObject;
    Queue<Vector3> alignmentLastPos = new Queue<Vector3>();
    int maxQueueSize = 15;
    public float visionObjectDist = 0.7f;

    public Vector3 headsetPos;
    public Quaternion headsetRot, motiveHeadsetRot;

    public Vector3 leftControllerPos;
    
    public Vector3 rightControllerPos;

    public Vector3 motiveTablePos;
    public Quaternion motiveTableRot;

    public Quaternion headsetRotToMotiveHeadsetRot;

    public Vector3 offsetMotiveTableToMotiveHeadset;
    public Vector3 offsetHeadsetToLeft, offsetHeadsetToRight;
    public Vector3 offsetHeadsetToMotiveHeadset;

    public GameObject leftHand, rightHand;
    public GameObject table;

    public bool trackHead = true;

    void UpdatePose()
    {
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState( RigidBodyId, NetworkCompensation);
        if ( rbState != null )
        {
    
            motiveHeadsetPos = rbState.Pose.Position;
            motiveHeadsetRot = rbState.Pose.Orientation;
            motiveTableRot = table.transform.rotation;

            if (motiveHeadsetPos.Equals(Vector3.zero))
            {
                Debug.LogError("Camera recieved position zero! This is probably bad!");
            }

            if (motiveHeadsetRot.Equals(Quaternion.identity))
            {
                Debug.LogError("Camera recieved identity quaternion! This is probably bad!");
            }

            // Get reported position from headset
            headsetPos = InputTracking.GetLocalPosition(XRNode.Head);
            headsetRot = InputTracking.GetLocalRotation(XRNode.Head);

            leftControllerPos = InputTracking.GetLocalPosition(XRNode.LeftHand);
            rightControllerPos = InputTracking.GetLocalPosition(XRNode.RightHand);

            // Set hand offsets
            offsetHeadsetToLeft = leftControllerPos - headsetPos;
            offsetHeadsetToRight = rightControllerPos - headsetPos;

            // Get table position
            motiveTablePos = table.transform.position;

            // Calculate the offset vector from the table to the headset (motive FoR)
            offsetMotiveTableToMotiveHeadset = motiveHeadsetPos - motiveTablePos;

            // Get pos offset from rift space to motive space
            offsetHeadsetToMotiveHeadset = motiveHeadsetPos - headsetPos;

            // Get rot offset from rift space to motive space
            headsetRotToMotiveHeadsetRot = Quaternion.FromToRotation(headsetRot.eulerAngles.normalized, motiveHeadsetRot.eulerAngles.normalized);


            Debug.Log(string.Format("Rift Pos: {0} {1} {2}", leftControllerPos, headsetPos, rightControllerPos));

            // Shift the pos
            this.transform.position = headsetPos + offsetHeadsetToMotiveHeadset;
            leftHand.transform.position = leftControllerPos + offsetHeadsetToMotiveHeadset;
            rightHand.transform.position = rightControllerPos + offsetHeadsetToMotiveHeadset;

            Debug.Log(string.Format("->Motive Pos: {0} {1} {2}", leftHand.transform.position, this.transform.position, rightHand.transform.position));
            Debug.Log(string.Format("Rift Rot: {0}", this.transform.rotation.eulerAngles));

            // Shift the rot
            this.transform.rotation *= headsetRotToMotiveHeadsetRot;

            Debug.Log(string.Format("->Motive Rot: {0}", this.transform.rotation.eulerAngles));

            Debug.Log(string.Format("->Mpos: Head: {0} Table: {1} Left: {2} Right: {3}", this.transform.position, table.transform.position, leftHand.transform.position, rightHand.transform.position));





        }
    }
}
