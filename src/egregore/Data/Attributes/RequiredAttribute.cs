using System;
using System.ComponentModel.DataAnnotations;

namespace egregore.Data.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RequiredAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == default)
                return false;
            return true;
        }
    }
}
