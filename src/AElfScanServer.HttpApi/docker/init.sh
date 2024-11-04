#!/bin/bash

set -e

HOME_DIR=$(cd $(dirname "$0") && pwd )

ContractsName=$1
VERSION=$2

ImageName="aelf/build-contracts:dotnet-sdk-6.0.413-jammy-amd64"

[ -z ${ContractsName} ] && { echo "Usage: $0 <parameters> <version>"; exit 1; }
[ -z ${VERSION} ] && { echo "Usage: $0 <parameters> <version>"; exit 1; }

[ ! -d "${HOME_DIR}/build" ] && mkdir -p ${HOME_DIR}/build

[ ! -d "${HOME_DIR}/contracts" ] && { echo "No contracts found."; exit 1; }

[ ! -f "${HOME_DIR}/build.sh" ] && { echo "No build.sh found."; exit 1; }

docker run --rm --name build-contracts \
  -e USER=root \
  -v ${HOME_DIR}/contracts:/opt/contracts \
  -v ${HOME_DIR}/build:/opt/build \
  -v ${HOME_DIR}/build.sh:/opt/build.sh \
  ${ImageName} /bin/bash -x /opt/build.sh \
  ${ContractsName} ${VERSION}
