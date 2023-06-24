#!/bin/sh

create_dir(){
  dir=$1
  if [ ! -d "$dir" ]; then
    mkdir -p "$dir"
  fi
}

copy_file(){
  src=$1
  dest=$2
  if [ -f "$src" ]; then
    echo "Copying $src to $dest"
    cp -fr $src $dest
    echo "Copied $src to $dest"
  else
    echo "File not found: $src"
    exit 1
  fi
}

dist="./src/protoeval-cli/dist"
tools_dir="./src/ProtoBufEval/tools"
runtimes_dir="$tools_dir/protoeval"
create_dir $runtimes_dir
create_dir $tools_dir


src_linux_x86="$dist/protoeval_linux_386.zip"
dest_linux_x86="$runtimes_dir/linux-x86.zip"
copy_file $src_linux_x86 $dest_linux_x86


src_linux_arm="$dist/protoeval_linux_arm.zip"
dest_linux_arm="$runtimes_dir/linux-arm.zip"
copy_file $src_linux_arm $dest_linux_arm


src_win_x86="$dist/protoeval_windows_386.zip"
dest_win_x86="$runtimes_dir/win-x86.zip"
copy_file $src_win_x86 $dest_win_x86


src_win_arm="$dist/protoeval_windows_arm.zip"
dest_win_arm="$runtimes_dir/win-arm.zip"
copy_file $src_win_arm $dest_win_arm

src_osx="$dist/protoeval_darwin_all.zip"
dest_osx="$runtimes_dir/osx.zip"
copy_file $src_osx $dest_osx