#!/bin/bash

# Script to synthesize the CDK stack
# This generates CloudFormation templates without deploying

set -e

echo "=========================================="
echo "Synthesizing CDK Stack"
echo "=========================================="

# Navigate to the CDK directory
cd "$(dirname "$0")/.."

# Restore .NET dependencies if needed
echo "Restoring .NET dependencies..."
dotnet restore

# Synthesize the CDK stack
echo "Running CDK synth..."
cdk synth

echo "=========================================="
echo "CDK synthesis completed successfully!"
echo "CloudFormation template location: $(pwd)/cdk.out"
echo "=========================================="
