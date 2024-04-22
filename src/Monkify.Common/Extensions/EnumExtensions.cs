using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Monkify.Common.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Retrieves the value of the first attribute of type <see cref="DescriptionAttribute" />
        /// </summary>
        /// <param name="value">The enum to get the description from.</param>
        /// <returns>The value of the first attribute of type <see cref="DescriptionAttribute" /></returns>
        public static string StringValueOf(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        public static bool ContainsAttribute<T>(this Enum value) where T : Attribute
        {
            var field = value.GetType().GetField(value.ToString());
            return field.IsDefined(typeof(T), false);
        }
    }
}
