using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceTesterCore.CustomAttributes
{
    public class IPAddressAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is null) return true;
            string ip = value.ToString();
            return System.Net.IPAddress.TryParse(ip, out _);
        }
    }
}
