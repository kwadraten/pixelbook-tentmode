open System
open System.IO
open System.Diagnostics
open System.Threading

// --- 配置参数 ---
let BASE_IIO_DIR = "/sys/bus/iio/devices/"
let TARGET_DEVICE_NAME = "cros-ec-accel" // 目标设备名称
let TARGET_DEVICE_LABEL = "accel-display" // 目标设备位置
let TARGET_CHANNEL_FILE = "in_accel_y_raw" // 目标传感器通道文件名

let CHECK_INTERVAL_MS = 500
let ROTATION_THRESHOLD_COUNT = 6

let XRANDR_CMD = "/usr/bin/xrandr"
let SCREEN_OUTPUT = "eDP-1"

// --- 核心逻辑函数 ---

let findLidSensorPath () : string option =
    try
        // 1. 获取所有 iio:deviceX 目录
        let deviceDirs = Directory.GetDirectories(BASE_IIO_DIR, "iio:device*") |> Seq.toList

        // 2. 遍历寻找匹配项
        let fullPathOption =
            deviceDirs
            |> List.tryPick (fun deviceDir ->
                let namePath = Path.Combine(deviceDir, "name")
                let labelPath = Path.Combine(deviceDir, "label")

                // 必须同时存在 name 和 label 文件才进行判断
                if File.Exists(namePath) && File.Exists(labelPath) then
                    let deviceName = File.ReadAllText(namePath).Trim()
                    let deviceLabel = File.ReadAllText(labelPath).Trim()

                    // === 核心修改逻辑 ===
                    // 条件1: 设备名称必须是加速度计 (Pixelbook 上通常为 "cros-ec-accel")
                    //       使用 Contains 以防未来驱动名称微调，同时明确排除 "light"
                    let isAccelerometer = deviceName = TARGET_DEVICE_NAME

                    // 条件2: Label 必须表明是在屏幕侧 ("accel-display")
                    let isLidSensor = deviceLabel = TARGET_DEVICE_LABEL

                    if isAccelerometer && isLidSensor then
                        // 找到目标设备，检查是否包含需要的数据通道 (例如 Y 轴)
                        let accelChannelPath = Path.Combine(deviceDir, TARGET_CHANNEL_FILE)

                        if File.Exists(accelChannelPath) then
                            Some accelChannelPath
                        else
                            None
                    else
                        None
                else
                    None)

        fullPathOption

    with e ->
        eprintfn "Failed to search IIO devices: %s" e.Message
        None

/// 读取 IIO 设备文件的原始整数值
let readIioValue (path: string) : int option =
    try
        File.ReadAllText(path).Trim()
        |> Int32.TryParse
        |> function
            | true, value -> Some value
            | _ -> None
    with e ->
        eprintfn "Error reading IIO file %s: %s" path e.Message
        None

/// 执行 xrandr 命令进行屏幕旋转
let rotateScreen (rotation: string) =
    // 在用户服务中，DISPLAY 和 XAUTHORITY 会被自动继承
    let arguments = [| "--output"; SCREEN_OUTPUT; "--rotate"; rotation |]

    try
        use p = new Process()
        p.StartInfo.FileName <- XRANDR_CMD
        p.StartInfo.Arguments <- String.Join(" ", arguments)
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.RedirectStandardError <- true
        p.StartInfo.UseShellExecute <- false // 必须设置为 false
        p.StartInfo.CreateNoWindow <- true

        let started = p.Start()

        if started then
            p.WaitForExit(3000) |> ignore

            if p.ExitCode <> 0 then
                // 打印错误到 journalctl
                eprintfn "xrandr failed (Exit code %d). Error: %s" p.ExitCode (p.StandardError.ReadToEnd())
            else
                printfn "Screen rotated to %s." rotation
        else
            eprintfn "Failed to start xrandr process."
    with e ->
        eprintfn "Exception running xrandr: %s" e.Message


// --- 主循环 ---
[<EntryPoint>]
let main argv =
    printfn "F# Service: Starting IIO dynamic monitor."

    let IIO_Y_PATH =
        match findLidSensorPath () with
        | Some path -> path
        | None ->
            eprintfn
                "FATAL ERROR: Could not find target device '%s' at '%s' with channel '%s'."
                TARGET_DEVICE_NAME
                TARGET_DEVICE_LABEL
                TARGET_CHANNEL_FILE

            exit 1

    printfn "Monitoring dynamic path: %s" IIO_Y_PATH

    let mutable consecutiveNegativeCount = 0
    let mutable currentRotation = "normal"

    while true do
        match readIioValue IIO_Y_PATH with
        | Some yValue ->
            if yValue < 0 then
                consecutiveNegativeCount <- consecutiveNegativeCount + 1
            else
                consecutiveNegativeCount <- 0

            // 持续负值达到阈值，切换到倒置
            if
                consecutiveNegativeCount >= ROTATION_THRESHOLD_COUNT
                && currentRotation <> "inverted"
            then
                rotateScreen "inverted"
                currentRotation <- "inverted"
                consecutiveNegativeCount <- 0

            // 恢复正常（从倒置状态回来）
            elif consecutiveNegativeCount = 0 && currentRotation <> "normal" then
                // 仅当目前处于倒置状态时才执行恢复操作
                if currentRotation = "inverted" then
                    rotateScreen "normal"
                    currentRotation <- "normal"

        | None -> ()

        Thread.Sleep(CHECK_INTERVAL_MS)

    0
