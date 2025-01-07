using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{

    [EditorBrowsable(EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    internal class IsExternalInit { }

    [AttributeUsage(
           AttributeTargets.Class |
           AttributeTargets.Struct |
           AttributeTargets.Field |
           AttributeTargets.Property,
           AllowMultiple = false,
           Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    internal sealed class RequiredMemberAttribute : Attribute { }

    /// <summary>
    /// Indicates that compiler support for a particular feature is required for the location where this attribute is applied.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    [ExcludeFromCodeCoverage]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CompilerFeatureRequiredAttribute"/> type.
        /// </summary>
        /// <param name="featureName">The name of the feature to indicate.</param>
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        /// <summary>
        /// The name of the compiler feature.
        /// </summary>
        public string FeatureName { get; }

        /// <summary>
        /// If true, the compiler can choose to allow access to the location where this attribute is applied if it does not understand <see cref="FeatureName"/>.
        /// </summary>
        public bool IsOptional { get; set; }

        /// <summary>
        /// The <see cref="FeatureName"/> used for the ref structs C# feature.
        /// </summary>
        public const string RefStructs = nameof(RefStructs);

        /// <summary>
        /// The <see cref="FeatureName"/> used for the required members C# feature.
        /// </summary>
        public const string RequiredMembers = nameof(RequiredMembers);
    }
}