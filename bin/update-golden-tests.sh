#!/usr/bin/env sh

# Synopsis:
# Update the golden tests.

# Example:
# ./bin/update-golden-tests.sh

# Generate the up-to-date results.json
./bin/run-tests-in-docker.sh

# Overwrite the existing files
find tests -name results.json -execdir cp results.json expected_results.json \;
