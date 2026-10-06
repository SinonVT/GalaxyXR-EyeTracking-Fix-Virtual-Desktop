using System;
using VRCFaceTracking.Core.Types;

namespace VirtualDesktop.FaceTracking
{
    /// <summary>
    /// Builds the (first, second) gaze pair from the direction the eye points,
    /// not from the eye quaternion's full orientation.
    ///
    /// Any spin about the line of sight (e.g. head roll baked into the
    /// quaternion as torsion) cannot change the direction, so it does not
    /// leak into the gaze values.
    ///
    /// The two angles are decomposed the same way the module's original
    /// conversion did (exact for Ry(yaw) * Rx(pitch)), so with a level head
    /// the output is identical to the original, including large diagonals.
    ///
    /// Matches the patched IL in VirtualDesktop.FaceTracking.dll exactly:
    ///   - no normalisation (atan2 is scale-invariant)
    ///   - no asin, so no NaN risk
    ///   - zero quaternion returns (0, 0)
    /// </summary>
    internal static class GazeFix
    {
        public static Vector2 FromDirection(Quaternion q)
        {
            float x = q.X, y = q.Y, z = q.Z, w = q.W;

            // Eye direction (quaternion applied to forward axis 0,0,-1), up to a constant scale:
            //   dx = -2(xz + wy),  dy = 2(wx - yz),  dz = -c
            float dx = -2f * (x * z + w * y);
            float dy = 2f * (w * x - y * z);
            float c  = 1f - 2f * (x * x + y * y);   // = -dz

            // First output (horizontal, same slot as the original asin term).
            float first = (float)Math.Atan2(dx, c);

            // Second output (vertical, same slot as the original atan2 term).
            float second = (float)Math.Atan2(dy, Math.Sqrt(dx * dx + c * c));

            return new Vector2(first, second);
        }
    }
}
