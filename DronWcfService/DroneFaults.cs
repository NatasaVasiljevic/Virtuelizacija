using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DronWcfService
{
    [DataContract]
    public class DataFormatFaultDetail
    {
        [DataMember] public string FieldName { get; set; }
        [DataMember] public string Message { get; set; }
    }

    [DataContract]
    public class ValidationFaultDetail
    {
        [DataMember] public string FieldName { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public double ActualValue { get; set; }
        [DataMember] public string AllowedRange { get; set; }
    }
}