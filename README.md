# OS2KITOS

## Maintainer and license info
OS2KITOS is maintained by STRONGMINDS ApS (https://www.strongminds.dk)
for OS2 - Offentligt digitaliseringsfællesskab (https://os2.eu/).

Copyright (c) 2014, OS2 - Offentligt digitaliseringsfællesskab.

The OS2KITOS is free software; you may use, study, modify and
distribute it under the terms of version 2.0 of the Mozilla Public
License. See the LICENSE file for details. If a copy of the MPL was not
distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

All source code in this and the underlying directories is subject to
the terms of the Mozilla Public License, v. 2.0. 

## Solution structure

### Backend
This repository maintains the backend services.

### UI
The UI is developed and maintained here: https://github.com/os2kitos/kitos_frontend

## Build and test

Build the solution:

```powershell
msbuild KITOS.sln
```

Run unit tests:

```powershell
dotnet test Tests.Unit.Core.ApplicationServices
dotnet test Tests.Unit.Presentation.Web
```

Run integration tests:

```powershell
dotnet test Tests.Integration.Presentation.Web
```

Integration tests require a running KITOS instance and a database configured through `Tests.Integration.Presentation.Web/Properties/launchSettings.json`.

The default launch profile (`Tests.Integration.Presentation.Web`) now targets **SQL Server** locally:

- `KitosDbProvider=SqlServer`
- `Database__Provider=SqlServer`
- `ConnectionStrings__KitosContext=Server=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Kitos;MultipleActiveResultSets=True;TrustServerCertificate=True`

An alternative profile is included for **PostgreSQL**:

- `Tests.Integration.Presentation.Web (PostgreSql)`
- `KitosDbProvider=PostgreSql`
- `Database__Provider=PostgreSql`
- `ConnectionStrings__KitosContext=Host=localhost;Port=5432;Database=kitos;Username=postgres;Password=postgres`