#!/bin/bash

dotnet publish -c Release -r osx.11.0-arm64 --self-contained --nologo -o publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
