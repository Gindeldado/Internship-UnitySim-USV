using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using GPS;
using System;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.Tf2;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.Robotics.SlamExample;
public class ROSTf2Publisher : MonoBehaviour
{
    private string Topic = "/tf";

    [SerializeField] 
    double m_PublishRateHz = 20f;
    double m_LastPublishTimeSeconds;
    double PublishPeriodSeconds => 1.0f / m_PublishRateHz;
    bool ShouldPublishMessage => Clock.NowTimeInSeconds > m_LastPublishTimeSeconds + PublishPeriodSeconds;

    private ROSConnection m_ROS;
    public GameObject body;
    TransformTreeNode bodyT;

    UIManager guiRos;

    // Start is called before the first frame update
    void Start()
    {
        m_LastPublishTimeSeconds = Clock.time + PublishPeriodSeconds;

        m_ROS = ROSConnection.GetOrCreateInstance();
        m_ROS.RegisterPublisher<TFMessageMsg>(Topic);

        guiRos = GameObject.Find("Canvas").GetComponent<UIManager>();

        bodyT = new TransformTreeNode(body, "body");
    }

    // Update is called once per frame
    void Update()
    {
        if(ShouldPublishMessage && guiRos.publishRosMsg)
        { 
            PublishMessage();
        }
    }

    void PublishMessage(){
        var tfMessageList = new List<TransformStampedMsg>();
        
        var mapFrame = new TransformStampedMsg(
            new HeaderMsg(new TimeStamp(Clock.time), "world"),
            "map",
            new TransformMsg());
        var vesselFrame = new TransformStampedMsg(
            new HeaderMsg(new TimeStamp(Clock.time), "map"),
            "body",
            bodyT.Transform.To<ENU>());//Accounts for north being to the right
        var lidarFrame = new TransformStampedMsg(
            new HeaderMsg(new TimeStamp(Clock.time), "body"),
            "lidar",
            new TransformMsg());//uses body refrence frame/as origin?/!
                                

        tfMessageList.Add(mapFrame);
        tfMessageList.Add(vesselFrame);
        tfMessageList.Add(lidarFrame);
        var tfMessage = new TFMessageMsg(tfMessageList.ToArray());
        m_ROS.Publish(Topic, tfMessage);
    }
}
