#!/bin/bash

DOCKER_USERNAME=$1
DOCKER_REPOSITORY=$2
DOCKER_TAG=$3

if [[ "$(uname)" == "Linux" ]]; then
    sudo docker build -t $DOCKER_USERNAME/$DOCKER_REPOSITORY:latest --file PubSub.Application/Dockerfile .
    sudo docker tag $DOCKER_USERNAME/$DOCKER_REPOSITORY:latest $DOCKER_USERNAME/$DOCKER_REPOSITORY:$DOCKER_TAG
    sudo docker push $DOCKER_USERNAME/$DOCKER_REPOSITORY:latest 
    sudo docker push $DOCKER_USERNAME/$DOCKER_REPOSITORY:$DOCKER_TAG
else
    docker build -t $DOCKER_USERNAME/$DOCKER_REPOSITORY:latest --file PubSub.Application/Dockerfile .
    docker tag $DOCKER_USERNAME/$DOCKER_REPOSITORY:latest $DOCKER_USERNAME/$DOCKER_REPOSITORY:$DOCKER_TAG
    docker push $DOCKER_USERNAME/$DOCKER_REPOSITORY:latest
    docker push $DOCKER_USERNAME/$DOCKER_REPOSITORY:$DOCKER_TAG
fi