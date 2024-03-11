namespace PropertyGenerator;

public static class SourceGenerationHelper
{
    public const string AttributeName = "UnrealLib.UProperty.UPropertyAttribute";
    public const string AttributeData =
        """
        using System;

        namespace UnrealLib.UProperty;

        [AttributeUsage(AttributeTargets.Field)]
        public sealed class UPropertyAttribute : Attribute
        {
            // DefaultValue is now handled through field initializer

            /// <summary>
            /// Alternative names which can be used to refer to this property.
            /// </summary>
            /// <remarks>
            /// Reserved; not currently implemented!
            /// Intended to redirect deprecated UProperties.
            /// </remarks>
            public string[] Aliases { get; set; }

            /// <remarks>To be used to mark object indices.</remarks>
            public bool Pointer { get; set; }

            /// <summary>
            /// Ugly testing. Forces the use of 'ArrayProperty' type on array types when reading/writing.
            /// Use this for array type uc properties, i.e. 'array<float>'. Do not use this for properties such as 'float[4]'!
            /// </summary>
            public bool ArrayProperty { get; set; }

            /// <summary>
            /// Ugly testing. Forces the use of 'ObjectProperty' type when reading/writing.
            /// Should be specified for UObject fields, because I haven't decided how to properly handle them yet.
            /// </summary>
            public bool ObjectProperty { get; set; }
        }

        """;

    public const string CollectionExtensionsName = "UnrealLib.CollectionExtensions";
    public const string CollectionExtensionsData =
        """
        using System.Collections.Generic;

        namespace UnrealLib;

        public static class CollectionExtensions
        {
            /// <remarks>Does not support arrays with more than one dimension!/// </remarks>
            public static bool DeepEquals<T>(this T[] self, T[] other)
            {
                if (ReferenceEquals(self, other)) return true;
                if (self is null || other is null) return false;

                if (self.Length != other.Length) return false;

                for (int i = 0; i < self.Length; i++)
                {
                    if (!EqualityComparer<T>.Default.Equals(self[i], other[i])) return false;
                }

                return true;
            }
        }
        """;
}
