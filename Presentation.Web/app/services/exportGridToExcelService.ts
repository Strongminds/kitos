﻿module Kitos.Services.System {
    "use strict";

    export class ExportGridToExcelService {
        private exportFlag = false;
        static $inject = ["needsWidthFixService"];
        private checkedColumns: string[];

        constructor(private readonly needsWidthFixService: NeedsWidthFix) { }

        getExcel(e: IKendoGridExcelExportEvent<any>, _: ILoDashWithMixins, timeout: ng.ITimeoutService, kendoGrid: IKendoGrid<any>) {
            const columns = e.sender.columns;

            if (!this.exportFlag) {
                e.preventDefault();
                _.forEach(columns, column => {
                    //check if column has any related columns
                    var columnsWithSameFieldName =  columns.filter(x => x.field === column.field);
                    if (columnsWithSameFieldName.length < 2)
                        return;

                    this.showColumnWithRelatedColumnsIfIsVisible(columnsWithSameFieldName, e);
                });

                timeout(() => {
                    this.exportFlag = true;
                    e.sender.saveAsExcel();
                });

                return;
            }

            this.exportFlag = false;

            // hide columns on visual grid
            _.forEach(columns, column => {
                if (column.tempVisual) {
                    delete column.tempVisual;
                    e.sender.hideColumn(column);
                }
            });

            // render templates
            const sheet = e.workbook.sheets[0];

            // skip header row
            for (let rowIndex = 1; rowIndex < sheet.rows.length; rowIndex++) {
                const row = sheet.rows[rowIndex];

                // -1 as sheet has header and dataSource doesn't
                const dataItem = e.data[rowIndex - 1];

                for (let columnIndex = 0; columnIndex < row.cells.length; columnIndex++) {
                    if (columns[columnIndex].field === "") continue;
                    const cell = row.cells[columnIndex];

                    const template = this.getTemplateMethod(columns[columnIndex]);

                    let computedValue = template(dataItem);
                    if (computedValue == null) {
                        computedValue = "";
                    }
                    cell.value = computedValue;
                }
            }

            // hide loadingbar when export is finished
            kendo.ui.progress(kendoGrid.element, false);
            this.needsWidthFixService.fixWidth();
        }

        private getTemplateMethod(column) {
            let template: Function;

            if (column.excelTemplate) {
                template = column.excelTemplate;
            } else if (typeof column.template === "function") {
                template = <Function>column.template;
            } else if (typeof column.template === "string") {
                template = kendo.template(<string>column.template);
            } else {
                template = t => t;
            }
            return template;
        }

        private showColumnWithRelatedColumnsIfIsVisible(columns: IKendoGridColumn<any>[], e: IKendoGridExcelExportEvent<any>) {
            if (this.checkIfAnyColumnIsVisible(columns))
                this.showSelectedColumns(columns, e);
        }

        private checkIfAnyColumnIsVisible(columns: IKendoGridColumn<any>[]) : boolean {
            let isVisible = false;
            columns.forEach(column => {
                if (!column.hidden)
                    isVisible = true;
            });

            return isVisible;
        }

        private showSelectedColumns(columns: IKendoGridColumn<any>[], e: IKendoGridExcelExportEvent<any>) {
            _.forEach(columns,
                column => {
                    column.tempVisual = true;
                    e.sender.showColumn(column);
                }
            );
        }
    }

    app.service("exportGridToExcelService", ExportGridToExcelService);
}