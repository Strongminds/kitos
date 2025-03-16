#!/bin/bash

cd ../..
if [[ "$(uname)" == "Linux" ]]; then
	sudo docker build --tag kitos-pubsub --file PubSub.Application/Dockerfile .
else
	docker build --tag kitos-pubsub --file PubSub.Application/Dockerfile .
fi