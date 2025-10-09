using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeviceTesterCore.CustomAttributes
{
    public class IPAddressAttribute : ValidationAttribute
    {
        private static readonly Regex IPv4Regex =
        new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}"
                + @"([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");

        private static readonly Regex IPv6Regex =
            new Regex(@"^([0-9A-Fa-f]{1,4}:){7}[0-9A-Fa-f]{1,4}$");

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success; // Let [Required] handle nulls

            string ip = value.ToString();

            if (IPv4Regex.IsMatch(ip) || IPv6Regex.IsMatch(ip))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage ?? "Invalid IP address format");
        }
    }
}
