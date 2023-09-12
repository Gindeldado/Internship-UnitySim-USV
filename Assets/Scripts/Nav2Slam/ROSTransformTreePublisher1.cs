using System;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.Tf2;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.Robotics.SlamExample;
using UnityEngine;


//this function sends the 
public class ROSTransformTreePublisher1 : MonoBehaviour
{
    [SerializeField]
    private string Topic = "/tf";
    
    [SerializeField]
    private double m_PublishRateHz = 20f;
    [SerializeField]
    private List<string> m_GlobalFrameIds = new List<string> { "map"};
    [SerializeField]
    private GameObject m_RootGameObject;
    
    private double m_LastPublishTimeSeconds;

    private TransformTreeNode m_TransformRoot;

    ROSConnection m_ROS;

    double PublishPeriodSeconds => 1.0f / m_PublishRateHz;

    bool ShouldPublishMessage => Clock.NowTimeInSeconds > m_LastPublishTimeSeconds + PublishPeriodSeconds;

    // Start is called before the first frame update
    void Start()
    {
        if (m_RootGameObject == null)
        {
            Debug.LogWarning($"No GameObject explicitly defined as {nameof(m_RootGameObject)}, so using {name} as root.");
            m_RootGameObject = gameObject;
        }

        m_ROS = ROSConnection.GetOrCreateInstance();
        m_TransformRoot = new TransformTreeNode(m_RootGameObject);
        m_ROS.RegisterPublisher<TFMessageMsg>(Topic);
        m_LastPublishTimeSeconds = Clock.time + PublishPeriodSeconds;
    }

    static void PopulateTFList(List<TransformStampedMsg> tfList, TransformTreeNode tfNode)
    {
        // TODO: Some of this could be done once and cached rather than doing from scratch every time
        // Only generate transform messages from the children, because This node will be parented to the global frame
        foreach (var childTf in tfNode.Children)
        {
            tfList.Add(TransformTreeNode.ToTransformStamped(childTf));

            if (!childTf.IsALeafNode)
            {
                PopulateTFList(tfList, childTf);
            }
        }
    }

    void PublishMessage()
    {
        var tfMessageList = new List<TransformStampedMsg>();

        if (m_GlobalFrameIds.Count > 0)
        {
            var tfRootToGlobal = new TransformStampedMsg(
                new HeaderMsg(new TimeStamp(Clock.time), m_GlobalFrameIds.Last()),
                m_TransformRoot.name,
                m_TransformRoot.Transform.To<FLU>());
            tfMessageList.Add(tfRootToGlobal);
        }
        else
        {
            Debug.LogWarning($"No {m_GlobalFrameIds} specified, transform tree will be entirely local coordinates.");
        }
        
        // In case there are multiple "global" transforms that are effectively the same coordinate frame, 
        // treat this as an ordered list, first entry is the "true" global
        for (var i = 1; i < m_GlobalFrameIds.Count; ++i)
        {
            var tfGlobalToGlobal = new TransformStampedMsg(
                new HeaderMsg(new TimeStamp(Clock.time), m_GlobalFrameIds[i - 1]),
                m_GlobalFrameIds[i],
                // Initializes to identity transform
                new TransformMsg(new Vector3Msg(0,0,0),new QuaternionMsg(0,0,0,1)));
            
            tfMessageList.Add(tfGlobalToGlobal);
        }

        PopulateTFList(tfMessageList, m_TransformRoot);

        var tfMessage = new TFMessageMsg(tfMessageList.ToArray());
        m_ROS.Publish(Topic, tfMessage);
        m_LastPublishTimeSeconds = Clock.FrameStartTimeInSeconds;
    }

    void Update()
    {
        if (ShouldPublishMessage)
        {
            PublishMessage();
        }

    }
}
