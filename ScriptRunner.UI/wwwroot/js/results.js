﻿function showResults($results, packageResult, showResults) {    
    $resultsTables = $results.find('div[name="resultsTables"]')
    $resultsMessages = $results.find('div[name="resultsMessages"]')

    if (showResults == true) {
        $results.removeClass('hidden');
    }

    $resultsTables.html('');
    $resultsMessages.html('');

    let showMessages = false;
    let showTables = false;
    let tableCount = 0;

    packageResult.results.forEach(function (obj, x) {
        if (obj.messages != null && obj.messages.length > 0) {
            let id = `resultsMessage${x}`

            let messages = '';
            obj.messages.forEach(function (obj, x) {
                messages += `<div>${obj}</div>`;
            });

            $resultsMessages.append(`<label id='${id}' class="display" style="width:100%">${messages}</label>`);
            $resultsMessages.parent().removeClass('hidden');

            showMessages = true;
        }

        obj.dataTables.forEach(function (obj, y) {
            let id = `resultsTable${tableCount}`
            $resultsTables.append(`<table id='${id}' class="display" style="width:100%"></table>`);
            showResultsTable(id, obj);

            $resultsTables.parent().removeClass('hidden');

            showTables = true;
            tableCount++;
        });
    });

    if (showMessages == false) {
        $resultsMessages.parent().addClass('hidden');
    }

    if (showTables == false) {
        $resultsTables.parent().addClass('hidden');
    }
}

function showResultsTable(id, dataTable) {
    var columns = [];
    let columnNames = Object.keys(dataTable[0]);

    for (var i in columnNames) {
        columns.push({
            data: columnNames[i],
            title: columnNames[i].toUpperCase()
        });
    }

    $(`#${id}`).DataTable({
        paging: false,
        fixedHeader: true,
        searching: false,
        data: dataTable,
        columns: columns
    });
}