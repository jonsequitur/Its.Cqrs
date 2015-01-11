#!/bin/sh

# strip out all "dos" style line endings from files in repo
# NOTE: this command was only tested on OSX, *nix mileage may vary
grep -lIr '$' `git ls-files` | xargs sed -e 's///' -i ''
