#!/bin/bash

#cd ../..
if [[ "$(uname)" == "Linux" ]]; then
	sudo docker build -t $1/$2:latest --file PubSub.Application/Dockerfile .
    sudo docker tag $1/$2:latest $1/$2:$3
    sudo docker push $1/$2:latest 
    sudo docker push $1/$2:$$3
else
	docker build -t $1/$2:latest --file PubSub.Application/Dockerfile .
    docker tag $1/$2:latest $1/$2:$3
    docker push $1/$2:latest
    docker push $1/$2:$$3
fi