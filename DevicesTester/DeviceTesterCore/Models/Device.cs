using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceTesterCore.Models
{
    public class Device
    {
        public string Agent { get; set; }
        public string DeviceId { get; set; }
        public string SolutionId { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool UseSecureConnection { get; set; }
    }
}
