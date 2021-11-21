<#
.SYNOPSIS
    Run all tests in Docker.
.DESCRIPTION
    Run all tests, verifying the behavior of the test runner using the Docker image.
.EXAMPLE
    The example below will run all tests
    PS C:\> ./run-tests-in-docker.ps1
#>

docker build -t exercism/fsharp-test-runner .

docker run `
  --network none `
  --read-only `
  --mount type=bind,src=${InputDirectory},dst=/input/ `
  --mount type=bind,src=${OutputDirectory},dst=/output/ `
  --mount type=tmpfs,dst=/tmp `
  exercism/fsharp-test-runner $Exercise /input /output


docker run `
    --network none `
    --read-only `
    --mount type=bind,src="${PWD}/src",dst=/opt/test-runner/tests `
    --mount type=tmpfs,dst=/tmp \
    --workdir /opt/test-runner \
    --entrypoint /opt/test-runner/bin/run-tests.ps1 \
    exercism/test-runner
