using UnityEngine;

public class PACKET_SYSTEM_EXAMPLES : MonoBehaviour
{
    private PacketReceiver myReceiver;

    void Start()
    {
        myReceiver = GetComponent<PacketReceiver>();
    }

    public void ExampleBroadcastToAllDrones()
    {
        myReceiver.SendPacket("broadcast", "checkpoint_reached", "");
    }

    public void ExampleSendToSpecificDrone()
    {
        string offsetData = "x:2.5,z:3.0";
        myReceiver.SendPacket("drone2", "formation_update", offsetData);
    }

    public void ExampleRequestStatus()
    {
        myReceiver.SendPacket("drone3", "status_request", "");
    }

    public void ExamplePositionReport()
    {
        Vector3 pos = transform.position;
        string posData = $"{pos.x:F2},{pos.y:F2},{pos.z:F2}";
        myReceiver.SendPacket("all", "position_report", posData);
    }

    public void ExampleDirectPacketCreation()
    {
        Packet customPacket = new Packet(
            sender: "my_system",
            recipient: "drone1",
            messageType: "custom_command",
            data: "speed:5.0,altitude:10.0"
        );

        PacketHandler.Instance?.BroadcastPacket(customPacket);
    }

    public void ExampleCheckReceiverCount()
    {
        int count = PacketHandler.Instance.GetReceiverCount();
        Debug.Log($"Active receivers: {count}");
    }
}
