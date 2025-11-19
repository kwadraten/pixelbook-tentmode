# Pixelbook2017 帐篷模式（Tentmode）守护程序

[English](./README.md) | 中文

在Pixelbook 2017原装的Chrome OS系统当中，当将笔记本折叠起来，然后倒转过来使用屏幕和键盘的边缘进行支撑时，也就是像“帐篷”一样支撑起来时，显示内容会自动发生180度翻转。然而，刷入Linux之后，Linux并未实现这个功能。本仓库当中的程序则实现了这个功能。它会自动检测位于显示器上的加速度传感器的数据，据此旋转显示内容。

## 如何使用
1. 编辑accel-monitor.desktop

```bash
vim accel-monitor.desktop
# 将其中的{your_username}替换为你的用户名
ExecStart=/home/{your_username}/.local/bin/accel_monitor
```

2. 在确保你有dotnet sdk的前提下，运行./install.sh
```bash
# 如果你没有安装dotnet sdk，请先安装
# 如果你不想使用linuxbrew，参见其他方法 https://learn.microsoft.com/ja-jp/dotnet/core/install/linux
brew install dotnet
# 然后执行./install.sh
# 切记不要使用sudo，这个程序不需要root权限
bash ./install.sh
```

当然你也可以使用AI或手工将AccelMonitor.fs翻译到你喜欢的任何编程语言，在这种情况下当然不需要任何dotnet sdk，不过你需要对accel-monitor.desktop进行更多的修改，然后参照./install.sh把文件放到正确的位置。

## 使用systemd管理该程序的运行

```bash
# 编辑accel-monitor.service
vim accel-monitor.service
# 然后修改这一行
ExecStart=/home/{your_username}/.local/bin/accel_monitor
# 编辑install.sh
# 取消以下行的注释
cp -v ./accel-monitor.service ${HOME}/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user start accel-monitor.service
# 注释以下行
cp -v ./accel-monitor.desktop ${HOME}/.config/autostart/
```

**注意**：若使用systemd，需要你每次启动手工执行`systemctl --user start accel-monitor.service`来启动它。不要使用systemd管理该程序的自启动，因为即使设置了延迟，仍然会自启动失败。

## 这个程序的原理
pixelbook 2017在底座（base）上和显示器（display）上各有一个三轴加速度传感器，其输出的x,y,z三轴坐标，可用于计算电脑的滚转角 (Roll)、俯仰角 (Pitch)、朝向等姿态。在linux系统中，这些设备被识别成iio设备，可在`/sys/bus/iio/devices`下查看。本程序利用显示器上的传感器的y轴数据判断屏幕是正放还是反放，进而使用xrandr旋转显示内容。

由于使用了xrandr，本程序仅适用于x11，不适用于wayland。

本程序仅是针对pixelbook 2017实现的，对于其他型号的设备，其iio设备的具体name、label以及坐标值均可能不同，需另行计算。如果你的发行版当中包含[iio-sensor-proxy](https://gitlab.freedesktop.org/hadess/iio-sensor-proxy/)，那么你可以使用monitor-sensor的输出直接确定笔记本的姿态，在此基础上修改本仓库的AccelMonitor.fs程序即可实现适配。
