﻿#!/bin/bash

#SETUP ENVIRONMENT VARIABLES
ASPNETCORE_ENVIRONMENT=$1
export ASPNETCORE_ENVIRONMENT

RABBIT_MQ_USER=$2
RABBIT_MQ_PASSWORD=$3
export RABBIT_MQ_USER
export RABBIT_MQ_PASSWORD

PUBSUB_API_KEY=$4
export PUBSUB_API_KEY

IDP_HOST_MAPPING=$5
export IDP_HOST_MAPPING

#RUN DOCKER COMPOSE
if [[ "$(uname)" == "Linux" ]]; then
  sudo docker-compose pull 
  sudo docker-compose up -d --remove-orphans 
  sudo docker image prune -f
else
	docker-compose pull 
  docker-compose up -d --remove-orphans 
  docker image prune -f
fi