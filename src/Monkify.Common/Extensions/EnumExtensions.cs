using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Monkify.Common.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Retrieves the attribute from the specified enum
        /// </summary>
        /// <param name="value">The enum to get the attribute from.</param>
        /// <returns>The attribute object specified in the type.</returns>
        /// <returns>null if the specified Enum does not have the attribute of the specified type.</returns>
        public static T GetAttribute<T>(this Enum value) where T : Attribute
        {
            FieldInfo campo = value.GetType().GetField(value.ToString());
            return (T)campo.GetCustomAttribute(typeof(T), true);
        }

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

        /// <summary>
        /// Retrieves a list with all items of an enumerator
        /// </summary>
        public static IEnumerable<T> ListFrom<T>() where T : Enum
            => Enum.GetValues(typeof(T)).Cast<T>();
    }
}
