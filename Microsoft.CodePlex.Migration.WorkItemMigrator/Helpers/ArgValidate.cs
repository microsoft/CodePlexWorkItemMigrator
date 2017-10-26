using System;

namespace Microsoft.CodePlex.Migration.WorkItems
{
    internal static class ArgValidate
    {
        /// <summary>
        /// Checks that the object is not null. Throws ArgumentNullException.
        /// </summary>
        public static void IsNotNull(object o, string argName)
        {
            if (object.ReferenceEquals(o, null))
            {
                throw new ArgumentNullException(argName);
            }
        }

        /// <summary>
        /// Checks that the string is not null or empty. Throws either ArgumentNullExcpetion or ArgumentException.
        /// </summary>
        public static void IsNotNullNotEmpty(string s, string argName)
        {
            ArgValidate.IsNotNull(s, argName);

            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentException(message: Resources.StringCannotBeEmpty, paramName: argName);
            }
        }

        /// <summary>
        /// Checks that the string is not null, empty or whitespace. Throws either ArgumentNullExcpetion or ArgumentException.
        /// </summary>
        public static void IsNotNullNotEmptyNotWhiteSpace(string s, string argName)
        {
            ArgValidate.IsNotNullNotEmpty(s, argName);

            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentException(message: Resources.StringCannotBeAllWhiteSpace, paramName: argName);
            }
        }

        /// <summary>
        /// Checks that the integer is within the range [min, max] (inclusive)
        /// </summary>
        public static void IsInRange(int x, string argName, int min, int max)
        {
            if (x < min || x > max)
            {
                throw new ArgumentException(message: Resources.ValueIsNotWithinAllowedRange, paramName: argName);
            }
        }
    }
}