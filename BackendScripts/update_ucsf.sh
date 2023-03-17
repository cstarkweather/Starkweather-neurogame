#!/bin/bash
set -eu

cd /var/neuropace

update_unity_build ()
{
  rm -f build.zip app/ WebGL/
  wget https://cat-astrophe-games.party/neuropace/app/neuropace-release/master/LATEST/build.zip \
    --user=neuropace --password=`cat neuropace_builds_password.txt`
  unzip build.zip
  mv WebGL/neuropace/ app/
  rm -Rf WebGL/ # empty useless dir
}

update_backend_repo ()
{
  if [ ! -d 'repo' ]; then
    git clone https://github.com/cat-astrophe-games/neuropace-behavioral-fps/ repo
  else
    cd repo/
    git pull --rebase
    cd ../
  fi

  if [ '!' -f repo/Backend/settings.json ]; then
    cp repo/Backend/settings_default.json repo/Backend/settings.json
    chmod a+rw- repo/Backend/settings.json
  fi
}

set_permissions ()
{
  chown root:root -R .
  chown www-data:www-data -R repo/Backend
  chmod -R a+rX .
}

update_unity_build
update_backend_repo
set_permissions
