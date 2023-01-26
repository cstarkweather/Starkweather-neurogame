#!/bin/bash
set -eu

cd neuropace-behavioral-fps
git checkout *
git pull --rebase

if [ '!' -f ~/public_html/settings.json ]; then
  cp ~/public_html/settings_default.json ~/public_html/settings.json
  chmod a+rw- ~/public_html/settings.json
fi

chmod -R a+rx ~/public_html/
chmod a+rwx ~/public_html/logs/

