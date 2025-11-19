# Pixelbook 2017 Tent Mode Daemon

English | [中文](./README_zh.md)

In the original Chrome OS system on the Pixelbook 2017, the display content automatically rotates 180 degrees when the laptop is folded and inverted, resting on the edges of the screen and keyboard, forming a "tent." However, after flashing Linux, this functionality is not implemented. The program in this repository realizes this feature. It automatically detects data from the accelerometer located on the display and rotates the screen content accordingly.

## How to Use

1.  Edit accel-monitor.desktop

<!-- end list -->

```bash
vim accel-monitor.desktop
# Replace {your_username} with your username
ExecStart=/home/{your_username}/.local/bin/accel_monitor
```

2.  Run `./install.sh`, ensuring you have the dotnet SDK installed

<!-- end list -->

```bash
# If you haven't installed the dotnet SDK, please install it first
# If you don't want to use linuxbrew, see other methods here: https://learn.microsoft.com/ja-jp/dotnet/core/install/linux
brew install dotnet
# Then execute ./install.sh
# Remember not to use sudo; this program does not require root privileges
bash ./install.sh
```

Of course, you can also use AI or manually translate `AccelMonitor.fs` into any programming language you prefer. In that case, the dotnet SDK will not be needed, but you will have to make more modifications to `accel-monitor.desktop` and then refer to `./install.sh` to place the files in the correct location.

## Managing Program Execution with systemd

```bash
# Edit accel-monitor.service
vim accel-monitor.service
# Then modify this line
ExecStart=/home/{your_username}/.local/bin/accel_monitor
# Edit install.sh
# Uncomment the following lines
cp -v ./accel-monitor.service ${HOME}/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user start accel-monitor.service
# Comment out the following line
cp -v ./accel-monitor.desktop ${HOME}/.config/autostart/
```

**Note**: If you use systemd, you will need to manually execute `systemctl --user start accel-monitor.service` every time you start up. Do not use systemd to manage the program's autostart, as it will fail even with a delay set.

## Principle of the Program

The Pixelbook 2017 has a three-axis accelerometer on both the base and the display. The outputted x, y, and z coordinates can be used to calculate the device's roll, pitch, orientation, and other poses. In Linux systems, these devices are identified as IIO (Industrial I/O) devices and can be viewed under `/sys/bus/iio/devices`. This program uses the y-axis data from the display's sensor to determine if the screen is upright or inverted, and subsequently uses `xrandr` to rotate the display content.

Because it uses `xrandr`, this program is only applicable to X11 and **not** Wayland.

This program is implemented specifically for the Pixelbook 2017. For other device models, the specific name, label, and coordinate values of the IIO devices may differ and require separate calculation. If your Linux distro includes [iio-sensor-proxy](https://gitlab.freedesktop.org/hadess/iio-sensor-proxy/), you can directly use the output of `monitor-sensor` to determine the laptop's pose. By modifying the `AccelMonitor.fs` program in this repository based on that, you can achieve adaptation for your device.
