# Adding a new data property to an existing V2 API

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

> Screenshots remain in Confluence; follow the image links below to view them.

# Introduction

This page describes the process of adding a property to an existing V2 API.

This guide is written based on the addition of “Criticality”:

* **Jira**:

    * <https://os2web.atlassian.net/browse/KITOSUDV-2747>
    *  <https://os2web.atlassian.net/browse/KITOSUDV-3023>

* **GitHub**:

    *  <https://github.com/Strongminds/kitos/pull/733>
    * <https://github.com/Strongminds/kitos/pull/746>


# Prerequisites

This guide assumes that the `CriticalityType` choice type has already been added to KITOS.

For more info see here: [Linked page](../choice-types/creating-a-new-choice-type-udfaldsrum.md)

# Extending WriteModel

In order to extend a _WriteModel_:

## **In the WriteRequestDTO**

Find a suitable section for the field. In case of “_Criticality_”, that section is “_General_”

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

Example data sections in WriteRequestDTO
When the section was selected, add a new property to the class

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

New field added to the WriteRequestDTO
## **In the DataModificationParameters**

As when extending the _WriteRequestDTO_, first select a data section

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

Example data sections in WriteRequestDTO
When the section was selected, add a new property to the class.

**NOTE:** _It’s important to set the default value as “None”._

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

New field added to the DataModificationParameters
## **In the WriteModelMapper**

Find mapping method corresponding with the selected data section (e.g. “_General_” → “_MapGeneralData_”). Next expand the mapped parameters with the new one.

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

WriteModelMapper expanded by a new parameter
## **In the WriteService**

Add the property to the `Update{DataSection}` method (see the last line in the screenshot)

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Since criticality is a choice type, updating the field can be handled through the `_assignmentUpdateService` which requires a set of funtions in return for making sure that only locally available options can be set on the target object.

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
In this case remember to also add a Reset{FieldName} as well as a “setter” method for updating the value if it is not null.

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

The reset method
## Additional notes

In some modules there is a more general “Application service” which is called by the “Write service”. In that case, the responsibility of the write service will be to:

* Wrap the entire operation in a single transaction (one WRITE may cover many different funcion calls
* Translate the update parameters into a series of `function calls` on the application service (unlike in this example where we call a generic domain service on the domain itself)
* Raise a domain event corresponding to the action carried out: Entity {Updated | Created | BeingDeleted}

### Example of this:

* Write Service for DataProcessingRegistrations: <https://github.com/Strongminds/kitos/blob/master/Core.ApplicationServices/GDPR/Write/DataProcessingRegistrationWriteService.cs>

# **Extending ResponseModel**

## **In the ResponseDTO**

Add property to the same data section as in WriteRequestDTO

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

Example data sections in ResponseDTO
[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

Added property (note the use of IdentityNamePairResponseDTO which is used in API V2 as a cross reference to another resource
## **In the ResponseMapper**

Add property to a mapping method corresponding to the data section (e.g. “_General” → “MapGeneral_”)

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)

Added property to mapper
# Testing

## **In the WriteModelMapper Unit tests**

Extend `Configure{DataSection}InputContext` method with the property

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Extend `Assert{DataSection}` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Change number of undefined sections in `GetUndefined{DataSection}PropertiesInput` method to match the number of properties

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `FromPOST_Ignores_Undefined_Properties_In_{DataSection}` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `FromPATCH_Ignores_Undefined_Properties_In_{DataSection}` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `FromPUT_Enforces_Undefined_Properties_In_{DataSection}` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
## In the response mapper test

Extend the `Assign{Section}PropertiesSection` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Add the field to the `MapContractDTO_Maps_No_Properties` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Also extend the `MapContractDTO_Maps_{Section}_Properties` method with the correct Assert method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
## In the write service test

Update the setup method so it includes the property

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
If the field is an _option type_ remember to include:

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `Assert` method (if the field is not e.g. a choice type)

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
If the property IS a choice type, add a new test method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `Can_Create_With` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `Can_Update_With_All` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
## Unit tests for other new methods/classes

If any new methods on the domain object, application/domain services etc were created, prefer to have them covered by unit tests as well.

## **In the ApiV2 Integration Tests**

Update the `Create{DataSection}RequestDTO` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `Assert{DataSection}DataSection` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `Can_Post_With_{DataSection}` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `Can_Patch_With_{DataSection}` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `Can_Post_Full_{Entity}` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
Update the `Can_Put_All` method

[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
[View image in the original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/827490308)
