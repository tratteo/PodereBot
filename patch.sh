#!bin/bash

git fetch --all
git reset --hard
git clean -fd
git pull
dotnet build
sudo systemctl restart poderebot.service
sudo systemctl status poderebot.service