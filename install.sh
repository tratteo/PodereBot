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

# ===== SCRIPTS GENERATION
echo - generating scripts
run="#!bin/bash
./home/$user/PodereBot/build/PodereBot"
echo -e "$run" > ./run.sh
chmod +x ./run.sh

patch="#!bin/bash
git fetch --all
git reset --hard
git clean -fd
git pull
dotnet build --output build
sudo systemctl restart poderebot.service
sudo systemctl status poderebot.service"
echo -e "$patch" > ./patch.sh
chmod +x ./patch.sh

# ===== BUILD
echo - patching build
dotnet build --output build

# ===== SYSTEMD SERVICE SETUP
echo - writing .service file
service="[Unit]
Description=Telegram Podere bot
After=network.target network-online.target

[Service]
Type=simple
ExecStart=/home/$user/PodereBot/run.sh

[Install]
WantedBy=multi-user.target"
echo -e "$service" > /etc/systemd/system/poderebot.service

echo - reloading and starting daemon
systemctl daemon-reload
systemctl enable poderebot.service
systemctl restart poderebot.service
systemctl status poderebot.service
echo - installation completed