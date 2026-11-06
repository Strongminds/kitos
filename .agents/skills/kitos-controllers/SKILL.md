---
name: kitos-controllers
description: Create or refactor KITOS V2 API controllers (external and internal) and their request/response mappers, following repository conventions for routing, validation, Result-based responses, status codes, and DTO ↔ parameter mapping. Use when adding an endpoint, a controller, or a mapper, or when reviewing controller structure.
---

# Creating V2 controllers in KITOS

Reference implementation: `Presentation.Web/Controllers/API/V2/External/ItSystemUsages/ItSystemUsageV2Controller.cs` and its `Mapping/` folder.

## Placement and naming
- External (public API): `Presentation.Web/Controllers/API/V2/External/{Domain}/{Domain}V2Controller.cs`, inherits `ExternalBaseController` (`[PublicApi]`), route `api/v2/{kebab-plural}`.
- Internal (new frontend only): `Presentation.Web/Controllers/API/V2/Internal/{Domain}/{Domain}InternalV2Controller.cs`, inherits `InternalApiV2Controller` (`[InternalApi]`), route `api/v2/internal/{kebab-plural}`.
- Both bases derive from `ApiV2Controller` (`[Authorize]`, V2 JSON serialization, `FromOperationError`, `NoContent`).
- Mappers live next to the controller in `{Domain}/Mapping/`: `I{Domain}ResponseMapper`, `I{Domain}WriteModelMapper`, and `{Enum}MappingExtensions`.
- DTOs: `Presentation.Web/Models/API/V2/Request/{Domain}/` (`*RequestDTO`), `.../Response/{Domain}/` (`*ResponseDTO`), API enums in `.../Types/{Domain}/` (`*Choice`). Never expose domain entities or domain enums.
- Do not add new endpoints to V1 controllers.

## Controller rules
- Primary constructor injecting **interfaces only**: read service, write service, response mapper(s), write model mapper, and `IResourcePermissionsResponseMapper` if permissions are exposed.
- Controllers are thin: validate → map request → call service → map response → convert `Result` to `IActionResult`. No business logic, authorization, repository access, or transactions in controllers; those belong in services (`.agents/skills/kitos-services/SKILL.md`).
- Every action starts with:
  ```csharp
  if (!ModelState.IsValid)
      return BadRequest(ModelState);
  ```
- Identify resources by `Guid` UUIDs, never DB ids. Annotate route/query UUIDs with `[NonEmptyGuid]`; add `[Required]` for mandatory query params and `[FromBody][Required]` where a body is mandatory.
- Lists take optional filters (`Guid?`, `string?`, `DateTime? changedSinceGtEq`), `CommonOrderByProperty? orderByProperty` and `[FromQuery] BoundedPaginationQuery? paginationQuery`.
- Add XML `<summary>`/`<param>` docs on every public action (they feed Swagger) and declare every possible status with `[ApiResponse]`:
  ```csharp
  [ApiResponse(typeof(FooResponseDTO), HttpStatusCode.OK)]
  [ApiResponse(HttpStatusCode.BadRequest)]
  [ApiResponse(HttpStatusCode.Unauthorized)]
  [ApiResponse(HttpStatusCode.NotFound)]
  [ApiResponse(HttpStatusCode.Forbidden)]
  ```
  Add `Conflict` when the service can return `OperationFailure.Conflict`.

## Result → HTTP pattern
Chain on the service `Result`; never unwrap with `.Value` or throw.

| Operation | Shape | Success |
|---|---|---|
| GET one | `service.GetXAndAuthorizeRead(uuid).Select(responseMapper.Map).Match(Ok, FromOperationError)` | 200 |
| GET list | `service.Query(...).Select(responseMapper.Map).Transform(Ok)` | 200 |
| GET permissions | `service.GetPermissions(uuid).Select(permissionsResponseMapper.Map).Match(Ok, FromOperationError)` | 200 |
| POST | `writeService.Create(parameters).Select(map).Match(MapCreatedResponse, FromOperationError)` | 201 + `Location` |
| PUT / PATCH | `writeService.Update(uuid, writeModelMapper.FromPUT/FromPATCH(request)).Select(map).Match(Ok, FromOperationError)` | 200 with full resource |
| DELETE returning `Maybe<OperationError>` | `.Match(FromOperationError, () => StatusCode((int)HttpStatusCode.NoContent))` | 204 |
| DELETE returning `Result<T, OperationError>` | `.Match(_ => NoContent(), FromOperationError)` | 204 |
| Sub-resource GET | `.Bind(root => root.GetChild(uuid).Match<Result<TChild, OperationError>>(c => c, () => new OperationError("… not found", OperationFailure.NotFound)))` | 200 |

- `FromOperationError` maps `OperationFailure` → status (`BadInput` 400, `Forbidden` 403, `NotFound` 404, `Conflict` 409).
- Created responses use a private helper building `{request url}/{uuid}` (or `{rootUuid}/{segment}/{childUuid}` for sub-resources, constrained to `IHasUuidExternal`).
- Sub-resources follow REST nesting: `{rootUuid}/system-relations/{relationUuid}`, `{rootUuid}/external-references/{referenceUuid}`. Collection-style commands use `PATCH {rootUuid}/roles/add|remove`.

## Mapping

```mermaid
flowchart LR
  Req[RequestDTO] -->|WriteModelMapper FromPOST/PUT/PATCH| P[UpdateParameters]
  P --> S[WriteService]
  S -->|Result of Entity| RM[ResponseMapper]
  RM --> Res[ResponseDTO]
```

### Response mapper (domain → DTO)
- `I{Domain}ResponseMapper` with one `Map…DTO(entity)` method per response shape; pure and side-effect free (no service calls, no authorization).
- Map domain enums via `{Enum}MappingExtensions` (e.g. `RiskLevel.ToRiskLevelChoice()`) and references to other entities as `IdentityNamePairResponseDTO` (uuid + name), never raw ids.
- Reuse shared mappers for shared shapes (`IExternalReferenceResponseMapper`, `IResourcePermissionsResponseMapper`).

### Write model mapper (DTO → service parameters)
- `{Domain}WriteModelMapper : WriteModelMapperBase, I{Domain}WriteModelMapper` (constructor takes `ICurrentHttpRequest`). Exposes `FromPOST`, `FromPUT`, `FromPATCH` returning the service's `{Domain}UpdateParameters`, plus `Map{SubResource}` methods for sub-resource commands. The target model is described under "Parameters" in `.agents/skills/kitos-services/SKILL.md`.
- Root-level fields that the service needs to create the entity (e.g. owning organization/system UUID) are combined in the controller: `new SystemUsageCreationParameters(request.SystemUuid, request.OrganizationUuid, writeModelMapper.FromPOST(request))`.
- **POST**: map each provided section (`request.Section.FromNullable().Select(s => MapSection(s, true))`); missing sections stay `Maybe.None`.
- **PUT vs PATCH** share one private `MapUpdate(request, enforceFallbackOnUndefinedProperties)`:
  - PUT → `true`: every section/property is treated as changed; omitted values reset to defaults.
  - PATCH → `false`: only properties present in the JSON body change. Presence is read from the raw request body, so `"x": null` (reset) differs from an omitted `x` (unchanged).
- Section level: `WithResetDataIfPropertyIsDefined(request.Section, nameof(UpdateXRequestDTO.Section), enforce)` turns an explicit `"section": null` into an empty section (reset of all its properties) while an omitted section stays `null` → `Maybe.None`.
- Property level:
  ```csharp
  var rule = CreateChangeRule<UpdateXRequestDTO>(enforceFallbackIfNotProvided);
  Name = rule.MustUpdate(x => x.General!.Name)
      ? source.Name.AsChangedValue()
      : OptionalValueChange<string>.None,
  ```
  The selector path must match the JSON path of the request DTO so `MustUpdate` can detect the property.
- Shared base helpers: `GetOptionalValueChange`, `BaseMapRoleAssignments`, `BaseMapCreateReferences` / `BaseMapUpdateReferences`, `MapCommonReference`. Reuse or follow the private helpers in `ItSystemUsageWriteModelMapper` (`MapEnumChoice`, `MapEnumList`, `MapCrossReferences`, `MapLink`) for enums, UUID lists and links.
- Enum mapping: one static `{Enum}MappingExtensions` per enum pair using `EnumMap<TChoice, TDomain>` with `To{Domain}()` / `To{Choice}()` extensions. Every enum value must be mapped (see `RiskLevelMappingExtensions`).

## Registration
Register mappers in `Presentation.Web/Infrastructure/DI/KitosServiceRegistration.cs`:
```csharp
services.AddScoped<IFooResponseMapper, FooResponseMapper>();
services.AddScoped<IFooWriteModelMapper, FooWriteModelMapper>();
```
Controllers are discovered automatically.

## Tests
- Mapper unit tests in `Tests.Unit.Presentation.Web/Models/V2/` (e.g. `ItSystemUsageWriteModelMapperTest`): cover POST, PUT (all reset), PATCH (only defined properties change; explicit `null` section resets) and every enum mapping.
- Endpoint integration tests in `Tests.Integration.Presentation.Web/{Domain}/V2/` (e.g. `ItSystemUsageApiV2Test`): happy path per verb, 400/403/404/409 branches, and permissions.

## Finish
Run `pwsh ./scripts/quality-check.ps1` and review against `.agents/skills/code-quality/SKILL.md`.
