var dynamicFunctions = [];
var resultsTable;
function addOptions(script) {
    //Remove the old styles
    $("head [id^=customStyles]").remove();

    //Remove the old functions
    if (dynamicFunctions?.length > 0) {
        dynamicFunctions.forEach(jQuery => {
            $(jQuery.parent).off(jQuery.event, jQuery.selector, jQuery.func);
        });
    }

    //Default layout is Messages then Results(with Headers)
    let layout = "MHR";

    if (script.options != null) {
        let options = script.options;

        //Styles
        if (options.css?.length > 0) {
            for (let i = 0; i < options.css.length; i++) {
                let css = options.css[i];

                $(`<style id='customStyles${i}' type='text/css'>${css}</style>\n`).appendTo('head');
            }
        }

        //Functions
        if (options.jQuery?.length > 0) {
            for (let i = 0; i < options.jQuery.length; i++) {
                let jQuery = options.jQuery[i];

                jQuery.func = new Function('evt', jQuery.function);
                $(jQuery.parent).on(jQuery.event, jQuery.selector, { script: script }, jQuery.func);

                dynamicFunctions.push(jQuery);
            }
        }

        if (options.Layout != null) {
            layout = options.Layout;
        }

        //Is there a custom label for the messages
        $('#resultsLabel').text(options.resultsLabel ?? "Results");
    }

    let showResultsLabel = false;
    let showHeaders = false;

    //Figure out what to hide
    for (let letter of layout) {
        switch (letter) {
            case "r":
            case "R":
                showResultsLabel = true;
                break;
            case "h":
            case "H":
                showHeaders = true;
                break;
        }
    }

    let $resultsLabel = $('#resultsWrapper #tables');

    if (showResultsLabel) {
        $resultsLabel.removeClass('hidden');
    } else {
        $resultsLabel.addClass('hidden');
    }

    if (!showHeaders) {
        //Not sure why this won't work??
        //$('#resultsWrapper thead').css({ "display": "none" });
        $(`<style id='customStyles_thead' type='text/css'>thead { visibility: hidden; } thead > tr > th { padding-top: 0!important; padding-bottom: 0!important } </style>\n`).appendTo('head');
    }

    //Remove the H
    layout = layout.replace(/H/ig, '');

    //Change the order
    if (layout.toUpperCase() == "MR") {
        $('#messages').prependTo($('#tablesAndMessages'));
    } else {
        $('#messages').appendTo($('#tablesAndMessages'))
    }
}

function showResults(script, $results, packageResult, showResults) {    
    let $resultsParent = $results.find('#tables')
    let $resultsDiv = $results.find('#resultsDiv')

    let $messagesParent = $results.find('#messages')
    let $messagesDiv = $results.find('#messagesDiv')
    
    if (showResults == true) {
        $results.removeClass('hidden');
    }

    $resultsDiv.html('');
    $messagesDiv.html('');

    let showMessages = false;
    let showTables = false;
    let tableCount = 0;

    for (let x = 0; x < packageResult.results.length; x++) {
        let obj = packageResult.results[x];

        if (obj.messages != null && obj.messages.length > 0) {
            let id = `resultsMessages${x}`

            let messages = '';
            obj.messages.forEach(function (obj, x) {
                messages += `<div>${obj}</div>`;
            });

            $messagesDiv.append(`<label id='${id}' class="resultsLabel" style="width:100%">${messages}</label>`);            
            $messagesParent.removeClass('hidden');

            showMessages = true;
        }

        obj.dataTables?.forEach(function (dataTable, y) {
            let id = `resultsTable${tableCount}`
            $resultsDiv.append(`<table id='${id}' class="resultsTable my-dataTable" style="width:100%"></table>`);

            showResultsTable(id, dataTable, script?.options.dataTables[tableCount]);

            $resultsParent.removeClass('hidden');

            showTables = true;
            tableCount++;
        });
    }

    if (showMessages == false) {
        $messagesParent.addClass('hidden');
    }

    if (showTables == false) {
        $resultsParent.addClass('hidden');
    }    

    //flashElement($('#tablesAndMessages'));
}

function showResultsTable(id, dataTable, options) {
    const renderHref = function (column, data, row) {
        let href = Interpolate(column.href, row)

        let css = column.hrefCss ?? "results-href";
        return `<a href="${href}" class="${css}">${data}</a>`
    }

    if (dataTable.length == 0) {
        return;
    }

    var columns = [];
    let columnNames = Object.keys(dataTable[0]);
    let showAllColumns = options?.columns == null || options?.columns?.length == 0;

    for (var col in columnNames) {        
        //Do we care about this column
        let column = options?.columns?.find(key => key.columnName.toLowerCase() === columnNames[col].toLowerCase());
        if (showAllColumns == false && column === undefined)
            continue;
        
        let config = {
            data: columnNames[col],
            title: column?.title ?? (columnNames[col].charAt(0).toUpperCase() + columnNames[col].slice(1)),
            className: 'dt-left'
        }        

        if (column?.href != null) {
            config.render = function (data, type, row) {
                return renderHref(column, data, row);
            }
        }

        //Handle dates
        let dateType = getDateType(columnNames[col]);
        if (dateType != null) {
            console.log("Date/DateTime column: " + columnNames[col]);

            config.sType = column?.sType ?? "date-uk";
            config.type = dateType;
            config.render = function (data, type, row) {
                let dt = new moment(data, moment.ISO_8601);

                if (dateType == "datetime")
                    data = dt.format('DD/MM/YYYY HH:mm:ss');
                else
                    data = dt.format('DD/MM/YYYY');

                if (column?.href != null)
                    return renderHref(column, data, row);
                else
                    return data;
            }
        }

        columns.push(config);
    }

    //Default layout
    let layout = {
        topStart: null,
        topEnd: 'search',
        bottomStart: 'info',
        bottomEnd: 'paging',
        bottom1Start: 'pageLength',
    };

    if (options?.Layout != null) {
        layout = options.Layout;
    }

    let setup = {
        //Not sure why we need to reinitialise, but hey
        retrieve: true,

        autoWidth: true,
        paging: true,
        fixedHeader: true,        
        pageLength: 20,

        layout: layout,
        data: dataTable,
        columns: columns,
        order: [],
        initComplete: function () {
            let $datatable = $(`#${id} > tbody > tr > td`);

            $datatable.hover(function (e) {
                
                var row = this._DT_RowIndex;
                var data = resultsTable.row($(this).closest('tr')).data();

                if (data != undefined) {
                    let columns = Object.keys(data);
                    let col = resultsTable.cell(this).index().column;
                    let column = options?.columns?.find(key => key.columnName.toLowerCase() === columnNames[col].toLowerCase());            

                    //Create a custom href
                    let href = Interpolate(column?.href, data)
                    let onClick = Interpolate(column?.onClick, data)
                    
                    if (onClick != null) {
                        console.log(onClick)

                        $(this).css('cursor', 'pointer');
                        //$(this).attr("href", href);
                        $(this).attr("onclick", onClick);
                    }
                    
                    if (href != null) {
                        let $el = $(this);
                        let css = column.hrefCss ?? "results-href";

                        //A hack to get the href to work
                        //$el.replaceWith(`<td><a href="${href}" class="${css}">${$el.text()}</a></td>`)
                    }
                }

                return false;
            })
        }
    };

    if (options?.Dom != null) {
        setup.dom = options.Dom;
    }

    resultsTable = $(`#${id}`).DataTable(setup);      
}

function Interpolate(string, data) {
    if (string == null)
        return;

    string = string.replace("{", "${");

    let keys = Object.keys(data);
    return string.replace(/\${(.*?)}/gi, (x, g) => data[keys.find(key => key.toLowerCase() === g.toLowerCase())]);
}

function getDateType(columnName) {
    columnName = columnName.toLowerCase();

    if (columnName.includes("datetime"))
        return "datetime";

    if (columnName.includes("date"))
        return "date";

    return null;
}

function flashElement(element) {
    element
        .addClass("quickFlash")
        .on(
            "webkitAnimationEnd oanimationend msAnimationEnd animationend",
            function () {
                $(this)
                    .delay(500)// Wait for milliseconds.
                    .queue(function () {
                        $(this)
                            .removeClass("quickFlash")
                            .dequeue();
                    });
            }
        );
}

function generateUrlFromScript(script) {
    if (script.data != null) {
        let url = `/?ScriptId=${encodeURIComponent(script.data.id)}`;

        script.data.params?.forEach(el => {
            let value = $(`#${el.name}`).val();
            url += `&${el.name}=${encodeURIComponent(el.value)}`;
        });

        return url;
    }
}
