// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Toolkit.Uwp.UI.Media.Geometry.Parsers;

namespace Microsoft.Toolkit.Uwp.UI.Media.Geometry
{
    /// <summary>
    /// Helper Class for creating Win2d objects.
    /// </summary>
    public static class CanvasPathGeometry
    {
        /// <summary>
        /// Parses the Path data string and converts it to CanvasGeometry.
        /// </summary>
        /// <param name="pathData">Path data</param>
        /// <returns><see cref="CanvasGeometry"/></returns>
        public static CanvasGeometry CreateGeometry(string pathData)
        {
            return CreateGeometry(null, pathData);
        }

        /// <summary>
        /// Parses the Path data string and converts it to CanvasGeometry.
        /// </summary>
        /// <param name="resourceCreator"><see cref="ICanvasResourceCreator"/></param>
        /// <param name="pathData">Path data</param>
        /// <returns><see cref="CanvasGeometry"/></returns>
        public static CanvasGeometry CreateGeometry(ICanvasResourceCreator resourceCreator, string pathData)
        {
            using (new CultureShield("en-US"))
            {
                // Get the CanvasGeometry from the path data
                return CanvasGeometryParser.Parse(resourceCreator, pathData);
            }
        }
    }
}