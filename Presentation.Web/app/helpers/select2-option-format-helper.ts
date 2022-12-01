module Kitos.Helpers {
    export class Select2OptionsFormatHelper {
        public static formatUserWithEmail(user: {text:string, email?: string}): string {
            return Select2OptionsFormatHelper.formatText(user.text, user.email);
        }

        public static formatOrganizationWithCvr(org: {text: string, cvr?: string}): string {
            return Select2OptionsFormatHelper.formatText(org.text, org.cvr);
        }

        public static formatOrganizationWithOptionalObjectContext(org: { text: string, optionalObjectContext?: { cvrNumber: string } }): string {
            return Select2OptionsFormatHelper.formatText(org.text, org.optionalObjectContext?.cvrNumber);
        }

        public static formatChangeLog(changeLog: Models.Api.Organization.ConnectionChangeLogDTO): string {
            const dateText = Helpers.RenderFieldsHelper.renderDate(changeLog.logTime);
            const responsibleEntityText = Helpers.ConnectionChangeLogHelper.getResponsibleEntityTextBasedOnOrigin(changeLog);

            return Select2OptionsFormatHelper.formatText(dateText, responsibleEntityText);
        }

        public static addIndentationToUnitChildren(orgUnit: Models.Api.Organization.OrganizationUnit, indentationLevel: number, idToSkip?: number): Kitos.Models.ViewModel.Generic.Select2OptionViewModelWithIndentation<Models.Api.Organization.OrganizationUnit>[] {
            const options: Kitos.Models.ViewModel.Generic.Select2OptionViewModelWithIndentation<Models.Api.Organization.OrganizationUnit>[] = [];
            Select2OptionsFormatHelper.visitUnit(orgUnit, indentationLevel, options, idToSkip);

            return options;
        }

        public static formatIndentation(result: Models.ViewModel.Generic.Select2OptionViewModelWithIndentation<any>): string {
            const formattedResult = Select2OptionsFormatHelper.visit(result.text, result.indentationLevel);
            return formattedResult;
        }

        public static formatIndentationWithUnitOrigin(result: Models.ViewModel.Generic.Select2OptionViewModelWithIndentation<Models.Api.Organization.OrganizationUnit>): string {
            let isKitosUnit = true;
            if (result.optionalObjectContext.externalOriginUuid) {
                isKitosUnit = false;
            }

            const formattedResult = Select2OptionsFormatHelper.visitUnitWithOrigin(result.text, result.indentationLevel, isKitosUnit);
            return formattedResult;
        }

        private static visit(text: string, indentationLevel: number): string {
            if (indentationLevel <= 0) {
                return text;
            }
            //indentation is four non breaking spaces
            return Select2OptionsFormatHelper.visit("&nbsp&nbsp&nbsp&nbsp" + text, indentationLevel - 1);
        }

        private static visitUnitWithOrigin(text: string, indentationLevel: number, isKitosUnit: boolean, indentationText: string = ""): string {
            if (indentationLevel <= 0) {
                return Select2OptionsFormatHelper.formatIndentationWithOriginText(text, indentationText, isKitosUnit);
            }

            //indentation is four non breaking spaces
            return Select2OptionsFormatHelper.visitUnitWithOrigin(text, indentationLevel - 1, isKitosUnit, indentationText + "&nbsp&nbsp&nbsp&nbsp");
        }

        private static formatIndentationWithOriginText(text: string, indentationText: string, isKitosUnit: boolean) {
            if (isKitosUnit) {
                return `<div><span class="org-structure-legend-square org-structure-legend-color-native-unit right-margin-5px"></span>${indentationText}${text}</div>`;
            }

            return `<div><span class="org-structure-legend-square org-structure-legend-color-fk-org-unit right-margin-5px"></span>${indentationText}${text}</div>`;
        }

        private static formatText(text: string, subText?: string): string {
            let result = `<div>${text}</div>`;
            if (subText) {
                result += `<div class="small">${subText}</div>`;
            }
            return result;
        }

        private static visitUnit(orgUnit: Models.Api.Organization.OrganizationUnit, indentationLevel: number, options: Models.ViewModel.Generic.Select2OptionViewModelWithIndentation<Models.Api.Organization.OrganizationUnit>[], unitIdToSkip?: number) {
            if (unitIdToSkip && orgUnit.id === unitIdToSkip) {
                return;
            }

            const option = {
                id: String(orgUnit.id),
                text: orgUnit.name,
                indentationLevel: indentationLevel,
                optionalObjectContext: orgUnit
            };

            options.push(option);

            orgUnit.children.forEach(child => {
                return Select2OptionsFormatHelper.visitUnit(child, indentationLevel + 1, options, unitIdToSkip);
            });

        }
    }
}
