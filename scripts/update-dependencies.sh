#!/bin/bash

dirs=(api transcription.OnCompletion  transcription.OnProcessing  transcription.OnStarted  transcription.common  transcription.models)

for dir in ${dirs[@]}; do
  pushd .
  cd ../src/$dir
  echo `date "+%F %T"` - Updating dependencies for $dir
  dotnet-outdated -u
  popd
done