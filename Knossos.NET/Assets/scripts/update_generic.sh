#!/bin/sh
#Warning: Do not edit in VS, it may change the file format

fail_msg() {
    echo "$1"
    exit 1
}

echo "Script Version 1"

[ -n "$update_folder" ] || fail_msg "Error: update_folder isn't specified!"
[ -n "$app_path" ] || fail_msg "Error: app_path isn't specified!"
[ -n "$app_name" ] || fail_msg "Error: app_name isn't specified!"

echo "Update Files Path: $update_folder"
echo "Knet Path: $app_path"
echo "Knet Exec Name: $app_name"
echo "Waiting for Knet to close"

retries=0
while [ $retries -lt 30 ]; do
    sleep 1
    retries=$((retries+1))
    if ! lsof "$app_path/$app_name" >/dev/null 2>&1; then
        echo "Ready"
        break
    fi
done

if [ $retries -eq 30 ]; then
    echo "Time limit reached, canceling..."
    exit 1
fi

echo "Copy Update Files"
cp -a "$update_folder"/* "$app_path"

echo "Launching Knet"
echo "$app_path/$app_name"
chmod +x "$app_path/$app_name"
"$app_path/$app_name" &

echo "Cleanup"
echo "Deleting: $update_folder"
rm -f "$update_folder"/*
rmdir "$update_folder"