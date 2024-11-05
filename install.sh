#!/bin/bash

error_handler() {
    echo "error occurred at line $1"
    exit 1
}

if [ "$#" -eq 0 ]; then
  echo "no user provided"
  echo "sudo ./install.sh <user> <no-service (skip service generation)>"
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
dotnet build --output build
echo - restarting service
systemctl restart poderebot.service
systemctl status poderebot.service --no-pager"
echo -e "$script" > ./patch.sh
chmod +x ./patch.sh

# ===== BUILD
echo - patching build
dotnet build --output build

# ===== SYSTEMD SERVICE SETUP
if ["$2" != "no-service"]
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
fi

echo - reloading daemon
sudo systemctl daemon-reload
echo - enabling service
sudo systemctl enable poderebot.service
echo - installation completed