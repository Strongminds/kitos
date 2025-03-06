﻿#!/bin/bash

ENV=$1
JSON_FILE="../appsettings.${ENV}.json"

if [ ! -f "$JSON_FILE" ]; then
  echo "File $JSON_FILE not found!"
  exit 1
fi

IDP_HOST_MAPPING=$(jq -r '.IdpHostMapping' "$JSON_FILE")
echo "Idp host mapping is: $IDP_HOST_MAPPING"
export IDP_HOST_MAPPING

cd ..

if [[ "$(uname)" == "Linux" ]]; then
	sudo docker compose up --detach
else
	docker compose up --detach
fi