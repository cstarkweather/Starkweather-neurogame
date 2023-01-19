#!/bin/bash
set -eu

cd neuropace-behavioral-fps
git pull --rebase
chmod -R a+rx ~/public_html/
chmod a+rw- ~/public_html/settings.json
chmod a+rwx ~/public_html/logs/

