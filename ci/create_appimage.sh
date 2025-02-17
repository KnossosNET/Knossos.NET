#!/bin/bash

fail_msg() {
    echo "$1"
    exit 1
}

#
# This script should be run in, or with the working directory set to, the root project directory
#

REPO_ROOT="$(readlink -f "$(dirname "${BASH_SOURCE[0]}")"/..)"
pushd "$REPO_ROOT" > /dev/null

# grab default variables
source ./packaging/vars

[ -n "$NAME" ] || fail_msg "ERROR: Name isn't specified!"
[ -n "$ID" ]   || fail_msg "ERROR: ID isn't specified!"
[ -n "$RIDS" ] || fail_msg "ERROR: Runtime IDs not specified!"

PUBLISH_DIR="$(readlink -f ${PUBLISH_DIR:="./build/publish"})" 

[ -n "$PUBLISH_DIR" ] || fail_msg "ERROR: Publish directory isn't set!"
[ -d "$PUBLISH_DIR" ] || fail_msg "ERROR: Publish location isn't a directory!"

OUTPUT_DIR="$(readlink -f ${OUTPUT_DIR:="./build/packages"})" 

[ -n "$OUTPUT_DIR" ] || fail_msg "ERROR: Output directory isn't set!"
[ -d "$OUTPUT_DIR" ] || fail_msg "ERROR: Output location isn't a directory!"

# everything needs a different arch name for some reason
DOTNET_ARCH="x64"
DOCKER_ARCH="amd64"

case "$ARCH" in
    "x86_64" | "x64")
        DOTNET_ARCH="x64"
        DOCKER_ARCH="amd64"
        ;;
    "aarch64" | "arm64")
        DOTNET_ARCH="arm64"
        DOCKER_ARCH="arm64/v8"
        ;;
    *)
        fail_msg "Unknown arch! Aborting!"
        ;;
esac

# make sure needed archive exists
[ -f "$OUTPUT_DIR/Linux_$DOTNET_ARCH.tar.gz" ] || fail_msg "ERROR: Linux $DOTNET_ARCH archive not found!"


# now just run script in appropriate container to generate bundle
docker run --platform linux/$DOCKER_ARCH --rm \
    -e ARCH="$ARCH" \
    -e PUBLISH_DIR="/PublishDir" \
    -e OUTPUT_DIR="/OutputDir" \
    -v "$REPO_ROOT":/repo \
    -v "$PUBLISH_DIR":/PublishDir \
    -v "$OUTPUT_DIR":/OutputDir \
    -t ubuntu:jammy \
    /bin/bash -xc "/repo/ci/bundle_appimage.sh"
