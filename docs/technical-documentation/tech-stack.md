# Tech Stack

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/876085249)

# Introduction

The purpose of this page is to give a brief overview of the core technologies used in KITOS.

# Technology overview

## Frontend technologies



The frontend technology stack is described in details here: <https://github.com/os2kitos/kitos_frontend/blob/main/README.md>

## Backend technologies

### Runtime and core frameworks

* The backend application targets and runs on [**.NET Framework 4.8**](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48).
* [**ASP.NET MVC/WebAPI** ](https://dotnet.microsoft.com/en-us/apps/aspnet/apis)is the web platform through which all API endpoints are realized.
* Additionally we use the [**ODATA**](https://www.nuget.org/packages/Microsoft.AspNet.OData/) extension to enable rich [ODATA V4](https://www.odata.org/documentation/) based queries, which supports the dynamic query requirements of the KITOS overview pages

### Programming languages

All server side programming is written in [**C#**](https://learn.microsoft.com/en-us/dotnet/csharp/) with the features supported by the runtime framework and the latest version of the compiler bundled with the [**Visual Studio Build Tools**](https://visualstudio.microsoft.com/downloads/?q=build+tools).

### Dependency injection

* [**Ninject**](http://www.ninject.org/) is an Inversion of Control (IoC) container used to support dependency injection during object graph composition as well as object instance lifecycle managment (transient/http request context/singleton).

### Scheduled tasks

* To support deferred and recurring tasks such as weekly link checks and notifications (_advis_ in Danish), KITOS uses [**Hangfire**](https://www.hangfire.io/).

### Test automation

* [**XUnit**](https://xunit.net/) is used as the core test framework applied when writing both unit tests as well as API-level integration tests.
* [**MoQ 4 **](https://github.com/moq/moq4)is used for mocking dependencies in unit tests.
* [**AutoFixture**](https://autofixture.github.io/) is used to generate usable test data and improve productivity while writing unit tests. In KITOS AutoFixture is usually referenced indirectly by inheriting from the `WithAutoFixture` base class and using the exposed `A<T>()` and `Many<T>()` convenience methods.

### API Documentation

* Automatic API documentation generation is supported through the use of [**SwashBuckle.WebAPI**](https://github.com/domaindrivendev/Swashbuckle.WebApi). We use the tool for

    * Generation of [OpenAPI ](https://www.openapis.org/)based API documentation in exposed in JSON.
    * Interactive visualization of the API specification document though the Swashbuckle swagger UI. The UI is available at [https://kitos.dk/swagger](https://kitos.dk/swagger)


### Persistence technologies

* [**SQL Server **](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)is used as the primary data storage system.
* [**Entity Framework 6**](https://learn.microsoft.com/en-us/ef/ef6/) is an [ORM ](https://en.wikipedia.org/wiki/Object%E2%80%93relational_mapping)allowing KITOS to access the database through a type safe LINQ based interface in stead of text based SQL.

## Source Control Management (SCM)

* [**Git**](https://git-scm.com/) is used as the sole SCM technology.
* [**GitHub**](https://github.com/os2kitos) is used as the source control hosting platform

## Build and deployment tools

* [**TeamCity**](https://www.jetbrains.com/teamcity/) is used for build and deployment automation (CI/CD)
* [**Powershell**](https://learn.microsoft.com/en-us/powershell/) is used to write build- and deployment scripts, which are then used by TeamCity
* [**WebDeploy**](https://www.iis.net/downloads/microsoft/web-deploy) is used to bundle and deploy the KITOS applications to the target environments (through TeamCity).

## Hosting platform details

### Containerization

_KITOS_ **does not use containers**, but deploys into a virtual machine.

_KITOS PubSub_ **uses containers**

### Operating systems

* [**Windows Server **](https://www.microsoft.com/en-us/windows-server)is used for the main KITOS applications and the SQL Server.
* [**Ubuntu**](https://ubuntu.com/) is for the server used to host the [**ELK** ](https://www.elastic.co/what-is/elk-stack)stack.

### Web server

KITOS applications are exposed to the internet through an [**IIS**](https://www.iis.net/)

### PubSub server

PubSub is exposed through Docker
