#!/bin/bash
echo - fetching changes
git fetch --all
git reset --hard
git pull
echo - building distribution
dotnet build --output build
echo - restarting service
systemctl restart poderebot.service
systemctl status poderebot.service --no-pager