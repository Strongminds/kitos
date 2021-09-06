﻿module Kitos.Models.ItContract {
    export interface IOptionExtend extends IEntity {
        Name: string;
        IsActive: boolean;
        Note: string;
        References: Array<IItContract>;
    }
}
