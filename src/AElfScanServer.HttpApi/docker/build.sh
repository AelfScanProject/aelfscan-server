#!/bin/bash

set -e
source /opt/contract_env.sh

# Error handler function
handle_error() {
    echo "Error occurred in script at line: $1"
    echo "Error code: $2"
    exit 1
}

# Set a trap to catch errors and call the error handler
trap 'handle_error $LINENO $?' ERR

# Get contract name, solution file name, and version from environment variables
ContractsName=$1
VERSION=$2
SLN=$3

# Check if parameters are set
[ -z "${VERSION}" ] && { echo "VERSION environment variable is not set"; exit 1; }
[ -z "${ContractsName}" ] && { echo "Contract name is not set"; exit 1; }
[ -z "${SLN}" ] && { echo "Solution file name is not set"; exit 1; }
[ -z "${CHAIN_ID}" ] && { echo "CHAIN_ID environment variable is not set"; exit 1; }
[ -z "${CONTRACT_ADDRESS}" ] && { echo "CONTRACT_ADDRESS environment variable is not set"; exit 1; }

# Clear the build directory
[ -d "/opt/build/${ContractsName}" ] && rm -rf /opt/build/${ContractsName}/*

cd /opt/contracts

DOWNLOAD_BINARY_START=$(date +%s)
# Download binary files
/bin/bash -x scripts/download_binary.sh
DOWNLOAD_BINARY_END=$(date +%s)
DOWNLOAD_BINARY_TIME=$((DOWNLOAD_BINARY_END - DOWNLOAD_BINARY_START))

RESTORE_START=$(date +%s)
# Restore dependencies
dotnet restore "${SLN}"
RESTORE_END=$(date +%s)
RESTORE_TIME=$((RESTORE_END - RESTORE_START))

echo "Starting contract compilation"

dotnet publish \
  contract/${ContractsName}/${ContractsName}.csproj \
  /p:NoBuild=false \
  /p:Version=${VERSION} \
  -c Release \
  -o /opt/build/${ContractsName}-${VERSION} \
  --verbosity detailed
    
PUBLISH_START=$(date +%s)
if [ "${ContractsName}" = "all" ]; then
  echo "Compiling all contracts"
  for NAME in $(ls contract); do
    if [ -d "contract/${NAME}" ]; then
      dotnet publish \
        "$(pwd)/contract/${NAME}/${NAME}.csproj" \
        /p:NoBuild=false \
        /p:Version=${VERSION} \
        -c Release \
        -o /opt/build/${NAME}-${VERSION}
    fi
  done
else
  echo "Compiling contract ${ContractsName}"
  dotnet publish \
    contract/${ContractsName}/${ContractsName}.csproj \
    /p:NoBuild=false \
    /p:Version=${VERSION} \
    -c Release \
    -o /opt/build/${ContractsName}-${VERSION} \
    --verbosity detailed
fi

PUBLISH_END=$(date +%s)
PUBLISH_TIME=$((PUBLISH_END - PUBLISH_START))

echo "Compilation completed"
echo "Listing all files in /opt/build directory:"
find /opt/build -type f -exec basename {} \;

# Find DLL file and upload to S3
DLL_FILE=$(find /opt/build/${ContractsName}-${VERSION} -type f -name "${ContractsName}.dll" -print -quit)

if [ -z "$DLL_FILE" ]; then
  echo "DLL file to upload was not found!"
  exit 1
fi


# Set upload file name as contractDLL-chainId-contractAddress.dll
UPLOAD_FILE_NAME="contractDLL-${CHAIN_ID}-${CONTRACT_ADDRESS}-${CONTRACT_NAME}-${VERSION}.dll"

S3UPLOAD_START=$(date +%s)
# Upload to S3 with error handling
aws s3 cp "$DLL_FILE" "s3://${S3_BUCKET}/${S3_DIRECTORY}/${UPLOAD_FILE_NAME}" --endpoint-url "https://s3.ap-northeast-1.amazonaws.com" --region "ap-northeast-1"
echo "File upload completed: ${UPLOAD_FILE_NAME}"
S3UPLOAD_END=$(date +%s)
S3UPLOAD_TIME=$((S3UPLOAD_END - S3UPLOAD_START))


echo "Statistical time dotnet restore time: ${RESTORE_TIME} s"
echo "Statistical time binary time: ${DOWNLOAD_BINARY_TIME} s"
echo "Statistical time dotnet publish time: ${PUBLISH_TIME} s"
echo "Statistical time s3 dll upload  time: ${S3UPLOAD_TIME} s"