#!/bin/bash

DOCKER_REPOSITORY=$1
DOCKER_TAG=$2

if [[ "$(uname)" == "Linux" ]]; then
    sudo docker build -t $DOCKER_REPOSITORY:latest --file PubSub.Application/Dockerfile .
    sudo docker tag $DOCKER_REPOSITORY:latest $DOCKER_REPOSITORY:$DOCKER_TAG
    sudo docker push $DOCKER_REPOSITORY:$DOCKER_TAG
else
    docker build -t $DOCKER_REPOSITORY:latest --file PubSub.Application/Dockerfile .
    docker tag $DOCKER_REPOSITORY:latest $DOCKER_REPOSITORY:$DOCKER_TAG
    docker --debug push $DOCKER_REPOSITORY:$DOCKER_TAG
fi