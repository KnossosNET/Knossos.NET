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

    dotnet publish "${PROJECT_FILE:="$NAME/$NAME.csproj"}" -r "$rid" -c Release -f net6.0 --self-contained -p:PublishSingleFile=true -o "$PUBLISH_DIR/$rid"
	
	# Rename final executable to remove *.Desktop* from it
	if [ "${OUTPUT_BINARY_NAME:-$NAME}" != "${PROJECT_NAME:-$NAME}" ]; then
		if [[ "$rid" == win-* ]]; then
			[ -f "$PUBLISH_DIR/$rid/${PROJECT_NAME}.exe" ] && mv "$PUBLISH_DIR/$rid/${PROJECT_NAME}.exe" "$PUBLISH_DIR/$rid/${OUTPUT_BINARY_NAME}.exe"
		else
			[ -f "$PUBLISH_DIR/$rid/${PROJECT_NAME}" ] && mv "$PUBLISH_DIR/$rid/${PROJECT_NAME}" "$PUBLISH_DIR/$rid/${OUTPUT_BINARY_NAME}"
			chmod +x "$PUBLISH_DIR/$rid/${OUTPUT_BINARY_NAME}"
		fi
	fi
done

echo
echo "Done."
echo