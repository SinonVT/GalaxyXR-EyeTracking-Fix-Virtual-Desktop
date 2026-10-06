# Galaxy XR eye tracking fix for Virtual Desktop (VRCFaceTracking)

A fix for the [Virtual Desktop VRCFaceTracking module](https://github.com/guygodin/VirtualDesktop.VRCFaceTracking) that stops head tilt from corrupting eye gaze on **Android XR headsets (Galaxy XR)**.

> [!NOTE]
> This is an unofficial fork and is not affiliated with Virtual Desktop, Inc. The fix is confirmed on a Galaxy XR by the author. Other headsets are untested. Use at your own risk.

## The bug

On Galaxy XR, eye gaze is only correct while your head is level. When your head is tilted, the direction your eyes look gets rotated. At about 90 degrees of head tilt:

| Head tilted left, you look | Avatar's eyes look |
|---|---|
| up | left |
| down | right |
| left | down |
| right | up |

Tilting right is the mirror image (up looks right, down looks left, left looks up, right looks down). Rolling your head back to level is smooth, with no snapping.

The eye poses coming from the Android XR runtime are wrong when the head is not level. This repository works around that in the module. It does not fix the runtime.

## The fix

The module used to convert the eye's rotation (a quaternion) directly into pitch and yaw angles. That conversion also picks up any twist around the line of sight, which is what head tilt adds, so the tilt leaked into the gaze values.

Now the module first works out the **direction the eye points**, then takes pitch and yaw from that direction. Twisting around the line of sight cannot change the direction, so head tilt no longer affects gaze. With a level head the output matches the original conversion.

The change to the code is small:

- `GazeFix.cs` (new file): the direction-based gaze conversion.
- `TrackingModule.cs`: `UpdateEyeData` calls `GazeFix.FromDirection(...)` instead of `Quaternion.Cartesian()` for each eye.

## Install

Requires the Virtual Desktop Streamer **v1.30 or later** and VRCFaceTracking.

1. Download `VirtualDesktop.FaceTracking.dll` from the [latest release](../../releases/latest).
2. Close VRCFaceTracking completely and check Task Manager for leftover processes.
3. If you haven't already, install the official **Virtual Desktop** module once from VRCFaceTracking's module registry. This creates the module folder.
4. Back up the existing DLL, then copy the downloaded one into:

   ```
   %AppData%\VRCFaceTracking\CustomLibs\91a90618-b020-4064-8832-809b2ca2b3bc\
   ```

   The filename must stay exactly `VirtualDesktop.FaceTracking.dll` (with a dot).
5. If Windows blocked the download, right-click the DLL, choose **Properties**, and tick **Unblock**.
6. Start VRCFaceTracking.

To check the download, compare its SHA-256 with `SHA256.txt` on the release page:

```powershell
Get-FileHash .\VirtualDesktop.FaceTracking.dll -Algorithm SHA256
```

To go back, put your backed-up DLL back, or reinstall the module from the registry.

### Check that it works

With the headset on, tilt your head about 90 degrees left and look up, down, left and right. The avatar's eyes should look the same directions as when your head is level. Then try the same tilted right.

## Build from source

You need the .NET SDK. The project targets `net7.0-windows`, and a newer SDK such as .NET 8 can build it.

```
dotnet build VirtualDesktop.FaceTracking.csproj -c Release
```

The DLL is written to `bin/Release/net7.0-windows/`. The reference `VRCFaceTracking.Core.dll` is in `3rdParty/`.

Releases are built by GitHub Actions (`.github/workflows/build-release.yml`) whenever a tag starting with `v` is pushed, for example `v1.0.0`.

## Technical details

<details>
<summary>The math</summary>

The original conversion (`Quaternion.Cartesian()`) normalizes the quaternion `(x, y, z, w)` and returns:

```
pitch = asin(2(xz - wy))
yaw   = atan2(2(yz + wx), w² - x² - y² + z²)
```

The new conversion computes the eye direction (the quaternion applied to the forward axis `(0, 0, -1)`), up to a constant scale:

```
dx = -2(xz + wy)
dy =  2(wx - yz)
c  =  1 - 2(x² + y²)        (this is -dz)
```

and returns the same two outputs in the same order, now built from that direction:

```
first  = atan2(dx, c)
second = atan2(dy, sqrt(dx² + c²))
```

Properties:

- The angles are split the same way the original did (exact for a yaw rotation applied after a pitch rotation), so with a level head the result matches the original, including at large diagonal gaze angles.
- Only the direction is used, so spin about the line of sight has no effect.
- It needs no normalization, because `atan2` ignores overall scale.
- There is no `asin`, so no NaN from a value slightly outside [-1, 1].
- A zero quaternion returns `(0, 0)`.

</details>

<details>
<summary>Verification</summary>

Checked with a numeric simulation of eye rotations, not with live headset data:

- On gaze angles up to ±60 degrees on both axes with a level head, the new conversion matches the original formulas to within 0.00001 degrees.
- Spinning the eye about its line of sight from 0 to 180 degrees changes the new output by at most about 0.00001 degrees. The original conversion changed by up to 94 degrees in the same test.
- Outside the eye conversion, the compiled module's other methods are unchanged from the original release build.

The fix is confirmed on a Galaxy XR by the author using the tilt test above.

</details>

## Known limitations

- Only tested on Galaxy XR. Other headsets are untested.
- If Google fixes the Android XR runtime, the fix should keep behaving like the original with a level head, but I haven't tested against a fixed runtime, so re-check it then.
- VRCFaceTracking can check for module updates, and an update can replace this DLL with the stock one. Keep a copy of the fixed DLL.

## Credits

- Virtual Desktop module by [Virtual Desktop, Inc.](https://www.vrdesktop.net) ([upstream repository](https://github.com/guygodin/VirtualDesktop.VRCFaceTracking)).
- OpenXR to VRCFaceTracking shape conversion originally credited in the module to [regzo2's QuestProOpenXR module](https://github.com/regzo2/VRCFaceTracking-QuestProOpenXR).

## License

See [LICENSE](LICENSE).
