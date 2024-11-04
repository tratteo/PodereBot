#!/bin/bash

error_handler() {
    echo "error occurred at line $1"
    exit 1
}

if [ "$#" -eq 0 ]; then
  echo "no user provided"
  exit 1
fi

user="$1"

trap 'error_handler $LINENO' ERR

echo - patching build
chmod +x run.sh
chmod +x patch.sh

dotnet build --output build

echo - writing .service file
service="
[Unit]
Description=Telegram Podere bot
After=network.target network-online.target

[Service]
Type=simple
ExecStart=/home/$user/PodereBot/run.sh

[Install]
WantedBy=multi-user.target
"
echo -e "$service" > /etc/systemd/system/poderebot.service

echo - reloading and starting daemon
systemctl daemon-reload
systemctl enable poderebot.service
systemctl restart poderebot.service
systemctl status poderebot.service
echo - installation completed