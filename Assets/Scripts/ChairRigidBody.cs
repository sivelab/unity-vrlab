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


/// <summary>
/// Implements live tracking of streamed OptiTrack rigid body data onto an object.
/// </summary>
public class ChairRigidBody : MonoBehaviour
{
    [Tooltip("The object containing the OptiTrackStreamingClient script.")]
    public OptitrackStreamingClient StreamingClient;

    [Tooltip("The Streaming ID of the rigid body in Motive")]
    public Int32 RigidBodyId;

    [Tooltip("Subscribes to this asset when using Unicast streaming.")]
    public bool NetworkCompensation = true;

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
    }

    Queue<Vector3> chairLastPosQueue = new Queue<Vector3>();
    Queue<Vector3> chairLastRotQueue = new Queue<Vector3>();

    int maxQueueSize = 15;

    void UpdatePose()
    {
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState( RigidBodyId, NetworkCompensation);
        if ( rbState != null )
        {
            // Smoothing for object
            // Should be its own class but ehhh
            chairLastPosQueue.Enqueue(rbState.Pose.Position);
            chairLastRotQueue.Enqueue(rbState.Pose.Orientation.eulerAngles);

            if (chairLastPosQueue.Count > maxQueueSize)
            {
                chairLastPosQueue.Dequeue();
            }

            if (chairLastRotQueue.Count > maxQueueSize)
            {
                chairLastRotQueue.Dequeue();
            }

            // Average values
            Vector3 totalpos = Vector3.zero;
            Vector3 totalrot = Vector3.zero;

            foreach (Vector3 pos in chairLastPosQueue)
            {
                totalpos += pos;
            }

            foreach (Vector3 rot in chairLastRotQueue)
            {
                totalrot += rot;
            }

            // Set pos to average
            this.transform.position = totalpos / (float)chairLastPosQueue.Count;
            // Only care about y rot
            this.transform.rotation = Quaternion.Euler(new Vector3(0, totalrot.y / (float)chairLastRotQueue.Count, 0));

            //this.transform.localPosition = rbState.Pose.Position;
            //this.transform.localRotation = rbState.Pose.Orientation;
        }
    }
}
