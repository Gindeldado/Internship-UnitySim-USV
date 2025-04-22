using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using GPS;
using RosMessageTypes.Nav;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
public class ROSOdometryPublisher : MonoBehaviour
{
    private string Topic = "/odometry";

    [SerializeField]
    double m_PublishRateHz = 20f;
    double m_LastPublishTimeSeconds;
    double PublishPeriodSeconds => 1.0f / m_PublishRateHz;
    bool ShouldPublishMessage => Clock.NowTimeInSeconds > m_LastPublishTimeSeconds + PublishPeriodSeconds;
    private ROSConnection m_ROS; 

    private string parent_frame_id = "map"; 
    private string child_frame_id = "body";
    UIManager guiRos;

    // Start is called before the first frame update
    void Start()
    {
        m_LastPublishTimeSeconds = Clock.time + PublishPeriodSeconds;
        guiRos = GameObject.Find("Canvas").GetComponent<UIManager>();
        m_ROS = ROSConnection.GetOrCreateInstance();
        m_ROS.RegisterPublisher<OdometryMsg>(Topic);
    }

    // Update is called once per frame
    void Update()
    {
        if(ShouldPublishMessage && guiRos.publishRosMsg)
        { 
            PublishMessage();
        }
    }

/// <summary>
/// Create and publish odometry message.
/// </summary>
    void PublishMessage(){
        Vector3<ENU> position = GPSClass.position.To<ENU>();                     
        Vector3<ENU>localVelocity = IMU.localVelocity.To<ENU>();              
        Vector3<ENU> localAngVelocity = IMU.angVelocity.To<ENU>();
        Quaternion<ENU> localRotation = IMU.rotation.To<ENU>();

        //noise
        double[] covariancePose = new double[36];
        covariancePose[0] = 0;
        covariancePose[7] = 0;
        covariancePose[14] = 0;
        covariancePose[21] = 0;
        covariancePose[28] = 0;
        covariancePose[35] = 0;
        double[] covarianceTwist = new double[36];
        covarianceTwist[0] = 0;
        covarianceTwist[7] = 0;
        covarianceTwist[14] = 0;
        covarianceTwist[21] = 0;
        covarianceTwist[28] = 0;
        covarianceTwist[35] = 0;

        //creating nav_msgs/msg/Odometry Message
        var odoMessage = new OdometryMsg{
            header = new HeaderMsg(new TimeStamp(Clock.time), parent_frame_id),
            child_frame_id = child_frame_id,
            pose = new PoseWithCovarianceMsg(
                new PoseMsg(
                    new PointMsg(position.x , position.y, position.z), 
                    new QuaternionMsg(localRotation.x, localRotation.y, localRotation.z, localRotation.w)
                    ),
                covariancePose
            ),
            twist = new TwistWithCovarianceMsg(
                new TwistMsg(
                    new Vector3Msg(localVelocity.x, localVelocity.y, localVelocity.z), 
                    new Vector3Msg(localAngVelocity.x, localAngVelocity.y, localAngVelocity.z)
                    ),
                covarianceTwist
            )
        };
        m_ROS.Publish(Topic, odoMessage);
    }
}
