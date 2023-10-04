function showResults(script, $results, packageResult, showResults) {    
    $resultsTables = $results.find('div[name="resultsTables"]')
    $resultsMessages = $results.find('div[name="resultsMessages"]')
    let dd = script;

    if (showResults == true) {
        $results.removeClass('hidden');
    }

    $resultsTables.html('');
    $resultsMessages.html('');

    let showMessages = false;
    let showTables = false;
    let tableCount = 0;

    for (let x = 0; x < packageResult.results.length; x++) {
        let obj = packageResult.results[x];

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

        obj.dataTables?.forEach(function (obj, y) {
            let id = `resultsTable${tableCount}`
            $resultsTables.append(`<table id='${id}' class="display" style="width:100%"></table>`);

            showResultsTable(id, obj, script?.options?.dataTableDom);

            $resultsTables.parent().removeClass('hidden');

            showTables = true;
            tableCount++;
        });
    }

    if (showMessages == false) {
        $resultsMessages.parent().addClass('hidden');
    }

    if (showTables == false) {
        $resultsTables.parent().addClass('hidden');
    }
}

function showResultsTable(id, dataTable, dom) {
    var columns = [];
    let columnNames = Object.keys(dataTable[0]);

    for (var i in columnNames) {
        columns.push({
            data: columnNames[i],
            title: columnNames[i].toUpperCase()
        });
    }

    let setup = {
        //Not sure why we need to reinitialise, but hey
        retrieve: true,

        paging: false,
        fixedHeader: true,
        searching: false,
        data: dataTable,
        columns: columns
    };

    if (dom != null) {
        setup.dom = dom;
    }

    $(`#${id}`).DataTable(setup);    
}
