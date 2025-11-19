open System.IO
open System.Diagnostics
open System

let targetPath = "/sys/bus/iio/devices"

// use code below to check devices path and names
for path in (Directory.GetDirectories targetPath) do
    let startInfo = new ProcessStartInfo()
    startInfo.FileName <- "cat"
    startInfo.Arguments <- $"{path}/name"
    startInfo.RedirectStandardOutput <- true
    let p = Process.Start startInfo
    let out = p.StandardOutput.ReadToEnd()
    p.WaitForExit()
    printfn $"{path}: {out}"

// paths and names
// warning: when reboot, the sequence will change
// /sys/bus/iio/devices/iio:device3: cros-ec-gyro
// /sys/bus/iio/devices/iio:device1: cros-ec-accel
// /sys/bus/iio/devices/iio:device4: cros-ec-mag
// /sys/bus/iio/devices/iio:device2: cros-ec-accel
// /sys/bus/iio/devices/iio:device0: cros-ec-light

// and we also have label, which is necessary to find lid sensor
for path in (Directory.GetDirectories targetPath) do
    let startInfo = new ProcessStartInfo()
    startInfo.FileName <- "cat"
    startInfo.Arguments <- $"{path}/label"
    startInfo.RedirectStandardOutput <- true
    let p = Process.Start startInfo
    let out = p.StandardOutput.ReadToEnd()
    p.WaitForExit()
    printfn $"{path}: {out}"

// /sys/bus/iio/devices/iio:device3: accel-base
// /sys/bus/iio/devices/iio:device1: accel-display
// /sys/bus/iio/devices/iio:device4: accel-base
// /sys/bus/iio/devices/iio:device2: accel-base
// /sys/bus/iio/devices/iio:device0: accel-display

// see speific accel sensor's output (x, y, z value, roll and pitch degree)
let deviceNum = "device0"
let accelFiles = [ "in_accel_x_raw"; "in_accel_y_raw"; "in_accel_z_raw" ]

let accelValues =
    accelFiles
    |> List.map (fun file -> $"{targetPath}/iio:{deviceNum}/{file}" |> File.ReadAllText)

let x = float accelValues.[0]
let y = float accelValues.[1]
let z = float accelValues.[2]

"The device's current three-axis acceleration values are, "
+ $"x: {x}, y: {y}, z {z}"
|> printfn "%s"

let rollRadian = atan2 -y z
let pitchRadian = atan2 x (sqrt (y * y + z * z))

let rad2deg (rad: float) = rad * (180.0 / Math.PI)
printfn $"The Roll is {rad2deg rollRadian}, The Pitch is {rad2deg pitchRadian}."

// now we get the data of every state.
// 1. laptop (clamshell) mode
// The device's current three-axis acceleration values are, x: 16, y: 14800, z 4416
// The Roll is -73.38603420002687, The Pitch is 0.05935548950326604.
// 2. stand mode
// The device's current three-axis acceleration values are, x: -336, y: 14528, z 6000
// The Roll is -67.55959035587152, The Pitch is -1.2245939422528478.
// 3. tent mode
// The device's current three-axis acceleration values are, x: -512, y: -15360, z 4960
// The Roll is 72.10386707496076, The Pitch is -1.8168418537034687.
