# 使用 --self-contained 阻止大部分卫星程序集的生成
# 卫星程序集 (Satellite Assemblies) 即是本地化资源
dotnet publish -c Release --self-contained true -o ./publish

mkdir -p ~/.config/systemd/user/
mkdir -p ~/.local/bin/
# cp -v ./accel-monitor.service ${HOME}/.config/systemd/user/
cp -v ./publish/accel_monitor ${HOME}/.local/bin/
# systemctl --user daemon-reload
# systemctl --user enable accel-monitor.service
# systemctl --user start accel-monitor.service

cp -v ./accel-monitor.desktop ${HOME}/.config/autostart/
