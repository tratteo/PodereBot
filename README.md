# Installation

## Linux

Give permissions:

```sh
chmod +x ./install.sh
```

Execute installation:

```sh
sudo ./install.sh
```

Now the service will automatically start at boot.

---

Manually start service

```sh
sudo systemctl restart poderebot.service
```

Read service status

```sh
sudo systemctl status poderebot.service --no-pager
```

Patch and update

```sh
sudo ./patch.sh
```
