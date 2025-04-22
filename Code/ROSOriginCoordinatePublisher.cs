using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using GPS;
using RosMessageTypes.DusRos2;
public class ROSOriginCoordinatePublisher : MonoBehaviour
{
    private string Topic = "/origin_coordinate";

    [SerializeField] 
    double m_PublishRateHz = 20f;
    double m_LastPublishTimeSeconds;
    double PublishPeriodSeconds => 1.0f / m_PublishRateHz;
    bool ShouldPublishMessage => Clock.NowTimeInSeconds > m_LastPublishTimeSeconds + PublishPeriodSeconds;

    private ROSConnection m_ROS;

    UIManager guiRos;

    // Start is called before the first frame update
    void Start()
    {
        m_LastPublishTimeSeconds = Clock.time + PublishPeriodSeconds;

        guiRos = GameObject.Find("Canvas").GetComponent<UIManager>();
        m_ROS = ROSConnection.GetOrCreateInstance();
        m_ROS.RegisterPublisher<GeoPointMsg>(Topic);
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

        var geoPointMsg = new GeoPointMsg{
            latitude = GPSClass.originLat,
            longitude = GPSClass.originLon,
            altitude = 0.0
        };
        m_ROS.Publish(Topic, geoPointMsg);
    }
}
