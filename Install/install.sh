#!bin/bash
chmod +x ../run.sh
cp poderebot.service /etc/systemd/system/poderebot.service
systemctl daemon-reload
systemctl enable poderebot.service
systemctl restart poderebot.service