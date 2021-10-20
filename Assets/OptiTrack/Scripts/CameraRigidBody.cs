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
    Vector3 lastPos = Vector3.zero;
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


        // Smoothing for vision center object
        // Should be its own class but ehhh
        alignmentLastPos.Enqueue(this.transform.position + this.transform.TransformDirection(new Vector3(0, 0, 1)) * visionObjectDist);
        if (alignmentLastPos.Count > maxQueueSize)
        {
            alignmentLastPos.Dequeue();
        }

        // Average values
        Vector3 total = Vector3.zero;
        foreach (Vector3 pos in alignmentLastPos)
        {
            total += pos;
        }

        // Set pos to average
        visionAlignmentObject.transform.position = total / (float)alignmentLastPos.Count;

    }

    public Vector3 vec3OculusToMotiveRotation = new Vector3(0, 90, 0);
    public Vector3 eyePositionOffsetFromTracker = new Vector3(0, 0, 0);
    public GameObject visionAlignmentObject;

    Queue<Vector3> alignmentLastPos = new Queue<Vector3>();
    int maxQueueSize = 15;

    public float visionObjectDist = 0.7f;
    void UpdatePose()
    {
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState( RigidBodyId, NetworkCompensation);
        if ( rbState != null )
        {
            // Only pull position from Motive
            // Use average of last two calls
            // But only if you have a position input
            if (!rbState.Pose.Position.Equals(Vector3.zero))
            {
                lastPos += rbState.Pose.Position + eyePositionOffsetFromTracker;
                lastPos *= 0.5f;
            }


            this.transform.localPosition = lastPos;
            // this.transform.localPosition = rbState.Pose.Position;

            // Coordinate system hardocded adjustment
            this.transform.localRotation = Quaternion.Euler(this.transform.localRotation.eulerAngles + vec3OculusToMotiveRotation);
        }
    }
}
