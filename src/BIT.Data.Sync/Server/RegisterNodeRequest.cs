using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    [DataContract]
    public class Option
    {
        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public object Value { get; set; }
        public Option(string key, object value)
        {
            Key = key;
            Value = value;
        }
        public Option()
        {
        }
    }

    [DataContract]
    public class RegisterNodeRequest
    {
        private List<Option> options = new List<Option>();

        [DataMember]
        public List<Option> Options { get => options; set => options = value; }

        //Dictionary<string, string> options = new Dictionary<string, string>();
        //[DataMember]
        //public Dictionary<string, string> Options { get => options; set => options = value; }
        public RegisterNodeRequest()
        {
        }
    }
}
