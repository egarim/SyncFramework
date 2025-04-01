using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    public class NodeNotFoundException: Exception
    {
        public NodeNotFoundException(string NodeId) : base($"Node with id {NodeId} not found")
        {
        }
    }
}
