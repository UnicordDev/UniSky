// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.Toolkit.Uwp.UI.Controls
{
    /// <summary>
    /// The <see cref="AspectRatioConstraint"/> structure is used by the <see cref="ConstrainedBox"/> control to
    /// define a specific ratio to restrict its content.
    /// </summary>
    [Windows.Foundation.Metadata.CreateFromString(MethodName = "Microsoft.Toolkit.Uwp.UI.Controls.AspectRatioConstraint.ConvertToAspectRatio")]
    public readonly struct AspectRatioConstraint
    {
        /// <summary>
        /// Gets the width component of the aspect ratio or the aspect ratio itself (and height will be 1).
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Gets the height component of the aspect ratio.
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// Gets the raw numeriucal aspect ratio value itself (Width / Height).
        /// </summary>
        public double Value => Width / Height;

        /// <summary>
        /// Initializes a new instance of the <see cref="AspectRatioConstraint"/> struct with the provided width and height.
        /// </summary>
        /// <param name="width">Width side of the ratio.</param>
        /// <param name="height">Height side of the ratio.</param>
        public AspectRatioConstraint(double width, double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspectRatioConstraint"/> struct with the specific numerical aspect ratio.
        /// </summary>
        /// <param name="ratio">Raw Aspect Ratio, Height will be 1.</param>
        public AspectRatioConstraint(double ratio)
        {
            Width = ratio;
            Height = 1;
        }

        /// <summary>
        /// Implicit conversion operator to convert an <see cref="AspectRatioConstraint"/> to a <see cref="double"/> value.
        /// This lets you use them easily in mathmatical expressions.
        /// </summary>
        /// <param name="aspect"><see cref="AspectRatioConstraint"/> instance.</param>
        public static implicit operator double(AspectRatioConstraint aspect) => aspect.Value;

        /// <summary>
        /// Implicit conversion operator to convert a <see cref="double"/> to an <see cref="AspectRatioConstraint"/> value.
        /// This allows for x:Bind to bind to a double value.
        /// </summary>
        /// <param name="ratio"><see cref="double"/> value representing the <see cref="AspectRatioConstraint"/>.</param>
        public static implicit operator AspectRatioConstraint(double ratio) => new AspectRatioConstraint(ratio);

        /// <summary>
        /// Implicit conversion operator to convert a <see cref="int"/> to an <see cref="AspectRatioConstraint"/> value.
        /// Creates a simple aspect ratio of N:1, where N is int
        /// </summary>
        /// <param name="width"><see cref="int"/> value representing the <see cref="AspectRatioConstraint"/>.</param>
        public static implicit operator AspectRatioConstraint(int width) => new AspectRatioConstraint(width, 1.0);

        /// <summary>
        /// Converter to take a string aspect ration like "16:9" and convert it to an <see cref="AspectRatioConstraint"/> struct.
        /// Used automatically by XAML.
        /// </summary>
        /// <param name="rawString">The string to be converted in format "Width:Height" or a decimal value.</param>
        /// <returns>The <see cref="AspectRatioConstraint"/> struct representing that ratio.</returns>
        public static AspectRatioConstraint ConvertToAspectRatio(string rawString)
        {
            string[] ratio = rawString.Split(":");

            if (ratio.Length == 2)
            {
                double width = double.Parse(ratio[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                double height = double.Parse(ratio[1], NumberStyles.Float, CultureInfo.InvariantCulture);

                return new AspectRatioConstraint(width, height);
            }
            else if (ratio.Length == 1)
            {
                return new AspectRatioConstraint(double.Parse(ratio[0], NumberStyles.Float, CultureInfo.InvariantCulture));
            }

            return new AspectRatioConstraint(1);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Width + ":" + Height;
        }
    }
}
