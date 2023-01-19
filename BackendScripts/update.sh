#!/bin/bash
set -eu

cd neuropace-behavioral-fps
git pull --rebase
chmod -R a+rX ~/public_html/
chmod a+rwX ~/public_html/logs/
chmod a+rwX ~/public_html/settings.json
