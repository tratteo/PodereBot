#!/bin/bash

confirm() {
    read -p "$1 (y/n) [n]: " answer
    case $answer in
        [Yy]* ) return 0;;
        * ) return 1;;
    esac
}

error_handler() {
    echo "error occurred at line $1"
    exit 1
}

if [ "$#" -eq 0 ]; then
  echo "no user provided"
  echo "sudo ./install.sh <user>"
  exit 1
fi
user="$1"

trap 'error_handler $LINENO' ERR

if ! id -u "$user" &> /dev/null; then
    echo "user [$user] does not exist in the current system"
    exit 1
fi

# ===== SCRIPTS GENERATION
echo - generating scripts
script="#!/bin/bash
cd /home/$user/PodereBot/build
./PodereBot"
echo -e "$script" > ./run.sh
chmod +x ./run.sh

script="#!/bin/bash
echo - fetching changes
git fetch --all
git reset --hard
git pull
echo - building distribution
dotnet build /p:OutputPath=build
echo - restarting service
systemctl restart poderebot.service
systemctl status poderebot.service --no-pager"
echo -e "$script" > ./patch.sh
chmod +x ./patch.sh

# ===== BUILD
echo - patching build
dotnet build /p:OutputPath=build

# ===== SYSTEMD SERVICE SETUP
if confirm "setup systemd services?"; then
  echo - writing .service file
  service="[Unit]
Description=Telegram Podere bot
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
ExecStart=/home/$user/PodereBot/run.sh

[Install]
WantedBy=multi-user.target"
  echo -e "$service" > /etc/systemd/system/poderebot.service

  echo - writing watchdog .service files
  service="[Unit]
Description=Telegram Podere bot watchdog service
After=poderebot.service
Wants=network-online.target

[Service]
Type=simple
ExecStart=/bin/sh -c 'curl -m 5 -f http://localhost:5050/api/status || systemctl restart poderebot.service'

[Install]
WantedBy=multi-user.target"
  echo -e "$service" > /etc/systemd/system/poderebot-wd.service

  service="[Unit]
Description=Telegram Podere bot watchdog timer

[Timer]
OnBootSec=1m
OnUnitActiveSec=10m
AccuracySec=1s 

[Install]
WantedBy=timers.target"
  echo -e "$service" > /etc/systemd/system/poderebot-wd.timer

  echo - reloading daemon
  sudo systemctl daemon-reload
  echo - enabling services
  sudo systemctl enable poderebot.service
  sudo systemctl enable poderebot-wd.service
  sudo systemctl enable poderebot-wd.timer

  if confirm "start systemd services?"; then
    echo - restarting services
    sudo systemctl restart poderebot.service
    sudo systemctl restart poderebot-wd.service
    sudo systemctl restart poderebot-wd.timer
  fi

fi

echo - installation completed