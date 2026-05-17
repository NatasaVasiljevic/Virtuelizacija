using System.Runtime.Serialization;
using System.ServiceModel;

namespace DroneWcfService
{
    //TACKA 1 - Skica sistema i pravila protokola
    //TACKA 2 - WCF servis, konfiguracija i ugovori
    [ServiceContract]
    public interface IDroneService
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFaultDetail))]
        [FaultContract(typeof(ValidationFaultDetail))]
        string StartSession(DroneSessionMeta meta);


        [OperationContract]
        [FaultContract(typeof(DataFormatFaultDetail))]
        [FaultContract(typeof(ValidationFaultDetail))]
        string PushSample(DroneSample sample);

        [OperationContract]
        string EndSession();
    }

    [DataContract]
    public class DroneSessionMeta
    {
        [DataMember] public string SessionId { get; set; }
        [DataMember] public string StartTime { get; set; }
    }

    [DataContract]
    public class DroneSample
    {
        [DataMember] public double LinearAccelerationX { get; set; }
        [DataMember] public double LinearAccelerationY { get; set; }
        [DataMember] public double LinearAccelerationZ { get; set; }
        [DataMember] public double WindSpeed { get; set; }
        [DataMember] public double WindAngle { get; set; }
        [DataMember] public string Time { get; set; }
    }
}