﻿module Kitos.Models.ItContract {
    export interface IAssociatedAgreementElementTypes {
        ItContract_Id: Number;
        AgreementElementType_Id: number;
        AgreementElementType: Models.OData.Generic.IOptionDTO<IItContract>;
    }
}
