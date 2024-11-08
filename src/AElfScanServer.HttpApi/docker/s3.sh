#!/bin/bash

# Enable error handling and set error trap
set -e

# Error handler function
handle_error() {
    echo "Error occurred in script at line: $1"
    echo "Error code: $2"
    exit 1
}

# Set a trap to catch errors and call the error handler
trap 'handle_error $LINENO $?' ERR

# Check if all variables are set
if [ -z "$S3_BUCKET" ] || [ -z "$S3_DIRECTORY" ] || [ -z "$S3_ACCESS_KEY" ] || [ -z "$S3_SECRET_KEY" ] || [ -z "$CONTRACT_VERSION_PARAM" ] || [ -z "$CHAIN_ID" ] || [ -z "$CONTRACT_NAME" ] || [ -z "$CONTRACT_ADDRESS" ]; then
    echo "Missing required environment variables. Please set S3_BUCKET, S3_DIRECTORY, S3_ACCESS_KEY, S3_SECRET_KEY, CONTRACT_VERSION_PARAM, CHAIN_ID, CONTRACT_NAME, CONTRACT_ADDRESS."
    exit 1
fi

export CONTRACT_VERSION="$CONTRACT_VERSION_PARAM"
export CHAIN_ID="$CHAIN_ID"
export CONTRACT_ADDRESS="$CONTRACT_ADDRESS"

# Set AWS CLI environment variables
export AWS_ACCESS_KEY_ID="$S3_ACCESS_KEY"
export AWS_SECRET_ACCESS_KEY="$S3_SECRET_KEY"
export AWS_DEFAULT_REGION="ap-northeast-1"

# Define file name format
FILE_NAME="contractFile-${CHAIN_ID}-${CONTRACT_ADDRESS}-${CONTRACT_NAME}-${CONTRACT_VERSION}.zip"

cd /opt
S3_DOWNLOAD_START=$(date +%s)
# Download file
echo "Downloading file from S3..."
aws s3 cp "s3://${S3_BUCKET}/${S3_DIRECTORY}/${FILE_NAME}" "./${FILE_NAME}" --endpoint-url "https://s3.ap-northeast-1.amazonaws.com" --region "ap-northeast-1"
S3_DOWNLOAD_END=$(date +%s)
S3_DOWNLOAD_TIME=$((S3_DOWNLOAD_END - S3_DOWNLOAD_START))

# Unzip file
UNZIP_START=$(date +%s)
echo "Unzipping file..."
unzip "./${FILE_NAME}" -d "./"
UNZIP_END=$(date +%s)
UNZIP_TIME=$((UNZIP_END - UNZIP_START))
# Find unzipped directory (assuming only one directory is created after unzipping)
UNZIPPED_DIR=$(find . -mindepth 1 -maxdepth 1 -type d)
mkdir -p /opt/contracts/

echo "Unzipped directory is ${UNZIPPED_DIR}"

if [ -z "$UNZIPPED_DIR" ]; then
    echo "No unzipped directory found!"
    exit 1
fi

# Copy all files from unzipped directory to /opt/contracts
cp -r "$UNZIPPED_DIR"/* /opt/contracts/

# Search for a file ending with .sln and set it to environment variable
SOLUTION_FILE=$(find /opt/contracts -name "*.sln" -print -quit)

if [ -n "$SOLUTION_FILE" ]; then
    SOLUTION_FILE_NAME=$(basename "$SOLUTION_FILE")
    export SOLUTION_FILE_NAME
    echo "Found solution file name: $SOLUTION_FILE_NAME"
else
    echo "No .sln file found!"
    exit 1
fi

END_TIME=$(date +%s)
TOTAL_TIME=$((END_TIME - START_TIME))

# Save environment variables to file
echo "CONTRACT_NAME=$CONTRACT_NAME" >> /opt/contract_env.sh
echo "CONTRACT_VERSION=$CONTRACT_VERSION" >> /opt/contract_env.sh
echo "SOLUTION_FILE_NAME=$SOLUTION_FILE_NAME" >> /opt/contract_env.sh
echo "CHAIN_ID=$CHAIN_ID" >> /opt/contract_env.sh
echo "CONTRACT_ADDRESS=$CONTRACT_ADDRESS" >> /opt/contract_env.sh

echo "Executing build.sh script..."
/bin/bash -x /opt/build.sh "$CONTRACT_NAME" "$CONTRACT_VERSION" "$SOLUTION_FILE_NAME"
echo "File download and extraction completed!"

# Output timing information
echo "Statistical time S3 file download time: ${S3_DOWNLOAD_TIME} seconds"
echo "Unzip time: ${UNZIP_TIME} seconds"
echo "build.sh execution time: ${BUILD_SCRIPT_TIME} seconds"
echo "Total execution time for s3.sh: ${TOTAL_TIME} seconds"