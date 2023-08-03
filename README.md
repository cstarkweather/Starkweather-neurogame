# neuropace-behavioral-fps

Behavioral data collection by FPS game for patients

## Application test deployment on CAG

Application frontend:

- on https://cat-astrophe-games.party/neuropace/app/
- enter sudirectories corresponding to versions, like https://cat-astrophe-games.party/neuropace/app/neuropace-debug/master/LATEST/ .

The backend (with configurable settings JSON and logs):

- Edit settings JSON online: https://cat-astrophe-games.party/neuropace/edit/ .
- Logs can be viewed online: https://cat-astrophe-games.party/neuropace/logs/ .

## Application deployment on UCSF

Application frontend:

- Public: https://neurogame.ucsf.edu/

- Through VPN: On qcpcpslws001.ucsf.edu, access by http://qcpcpslws001.ucsf.edu/ when in UCSF VPN.

The backend:

- Requires a username/password (see email Michalis->Clara _"Deployment of application on UCSF server - almost done"_ on 2023-03-20).

- Edit settings JSON online: https://neurogame.ucsf.edu/backend/edit/ .
- Logs can be viewed online: https://neurogame.ucsf.edu/backend/logs/

## Updating on UCSF server

Login to server using SSH.

Everything is in `/var/neuropace/` and can be updated with root access (like `sudo`).

There's `update_ucsf.sh` script inside that does everything, so just execute

```
sudo /var/neuropace/repo/BackendScripts/update_ucsf.sh
```

to update the build to latest master from https://github.com/cat-astrophe-games/neuropace-behavioral-fps/ .

What the script does:

1. Gets the build, from https://cat-astrophe-games.party/neuropace/app/neuropace-release/master/LATEST/build.zip (we deploy "release" version).

  The password to get builds must be in `/var/neuropace/neuropace_builds_password.txt`

2. Gets or updates repo from https://github.com/cat-astrophe-games/neuropace-behavioral-fps/

  Note: UCSF internal network seems to block SSH connections to GitHub. So we cannot use GitHub deploy keys ( https://docs.github.com/en/authentication/connecting-to-github-with-ssh/managing-deploy-keys ).

  So we use git over HTTPS.

  Use "fine-grained personal access token" on GitHub to have a password only for this repo. CAG GitHub organizations allows such tokens, with repository "Contents" as read-only.

3. Sets permissions (to be secure, but also allow users access) using chown/chmod.

## Apache

The Apache config (just alias + opening 2 directories to public) is in `/etc/apache2/conf-available/neuropace.conf` .

Enabled with

```
a2enconf neuropace
systemctl reload apache2
```

## Logs

Are available in `/var/neuropace/repo/Backend/logs/` .
