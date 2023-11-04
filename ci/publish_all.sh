#!/bin/bash

fail_msg() {
    echo "$1"
    exit 1
}

#
# This script should be run in, or with the working directory set to, the root project directory
#

pushd "$(readlink -f "$(dirname "${BASH_SOURCE[0]}")"/..)" > /dev/null

# grab default variables
source ./packaging/vars

[ -n "$NAME" ] || fail_msg "ERROR: Name isn't specified!"
[ -n "$ID" ]   || fail_msg "ERROR: ID isn't specified!"
[ -n "$RIDS" ] || fail_msg "ERROR: Runtime IDs not specified!"

PUBLISH_DIR="$(readlink -f ${PUBLISH_DIR:="./build/publish"})" 

[ -n "$PUBLISH_DIR" ] || fail_msg "ERROR: Publish directory isn't set!"
[ -d "$PUBLISH_DIR" ] || fail_msg "ERROR: Publish location isn't a directory!"


# Publish all runtime identifiers we need
for rid in $RIDS; do
    echo
    echo "Publishing $rid ..."
    echo

    dotnet publish "$NAME/$NAME.csproj" -r $rid -c Release --self-contained -p:PublishSingleFile=true -o "$PUBLISH_DIR/$rid"
done

echo
echo "Done."
echo