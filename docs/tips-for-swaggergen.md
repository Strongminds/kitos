# Tips for SwaggerGen

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/1057849346)

The service layer code of the v2 UI (src/api) is generated automatically by SwaggerGen using the `yarn swagger`CLI command.

This document contains tips for working with SwaggerGen in various situations.

The config for api code generation is found in `openapitools.json`



**Running swaggerGen for a local backend to test api changes**

If you have local api changes that you want to test using the new UI, pushing the changes to the dev environment can take a long time. To test them locally:

1. Change the url’s in `openapitools.json` to the local api url (`https://localhost:44300`by default)
2. Run `yarn swagger`to update the generated api code.
3. Update the url’s in `proxy.conf.json`to use the local api as well before starting the v2 UI.



**Running swaggerGen locally to handle broken dev swagger**

In situations where the dev api is working but its swagger is broken, you can’t update your UI api services using the dev url. To circumvent this:

1. Start the local backend with the same code that is running on dev (usually the master branch)
2. Follow the steps above in “**Running swaggerGen for a local backend to test api changes**” except (3)
3. Your v2 UI api service code now matches the endpoints on dev and can connect to them.
