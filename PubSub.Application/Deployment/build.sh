#!/bin/bash

cd ../..
sudo docker build -t kitos-pubsub -f PubSub.Application/Dockerfile .