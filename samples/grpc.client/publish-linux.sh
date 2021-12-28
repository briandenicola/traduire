#!/bin/bash

dotnet publish -c Release -r linux-x64 --self-contained --nologo -o publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
