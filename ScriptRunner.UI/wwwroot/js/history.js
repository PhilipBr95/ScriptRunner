function format(row) {

    var params = row.data.params;
    var html = '';

    if (params == null)
        return;

    var ss = params.forEach(function (obj, x) {
        let value = obj.value;

        if (obj.type == "file") {
            value = "File Content"
        }

        html += `<div><dt>${obj.name}</dt><dd>${value}</dd></div>`
    });

    return `<div><dl><dd class="paramLabel label">Parameters</dd>${html}</dl><a href="" class="paramLabel showResults" name="showResults">Show Results</a></div>`;
}

$(document).ready(function () {

    $('#history').on('click', '.showResults', function (e) {
        var $results = $(this).parent().find('.resultsTemplate');
        $results.removeClass('hidden');

        $(this).addClass('hidden');

        return false;
    })

    $('#history').on('requestChild.dt', function (e, row) {
        row.child(format(row.data())).show();
    })
    
    var table = $('#history').DataTable({
        "rowId": 'id',
        ajax: { url: "/api/history", dataSrc: "" },
        "columns": [
            {
                "className": 'run-control',
                "orderable": false,
                "data": null,
                "width": "0px", render: function (data, type, row) {
                    let url = generateUrl(data);

                    if (url == null)
                        return '';

                    return `<a href="${url}"><img width="16px" height="16px" src="../img/play-solid.svg" title="Execute Script" /></a>`
                }
            },
            {
                "className": 'details-control',
                "orderable": false,
                "data": null,
                "width": "0px", render: function (data, type, row) {

                    if (data?.data?.params == null)
                        return '';

                    return '<a href="" class="showChild"><img class="showChildImg" width="16px" height="16px" src="../img/icons8-info.svg" title="View Parameters" /></a>'
                }
            },
            { "data": "system", "title": "System" },
            { "data": "description", "title": "Detail" },
            { "data": "success", "title": "Success" },
            { "data": "actionedBy", "title": "Actioned By" },
            {
                "data": "createdDate", "sType": "date-uk", "title": "Created Date", "type": "datetime", render: function (data, type, row) {
                    let dt = new moment(data, moment.ISO_8601);
                    return dt.format('DD/MM/YYYY HH:mm:ss');
                }

            },
            {
                "data": "extn", "visible": false,
                "render": function (data, type, row, meta) {

                    if (row.data != null) {
                        var htmlDetail = '';

                        row.data.params.forEach(function (param) {
                            if (param.htmlType != "file" && param.value != null) {
                                htmlDetail += param.value + '|';
                            }
                        });

                        row.data.results.forEach(function (result) {
                            result.dataTables.forEach(function (dataTable) {
                                dataTable.forEach(function (row) {
                                    htmlDetail += Object.values(row) + '|';
                                });
                            });

                            result.messages.forEach(function (message) {
                                htmlDetail += message + '|';
                            });
                        });

                        return type === 'display' ? htmlDetail : htmlDetail;
                    } else {
                        return '';
                    }

                }
            }
        ],
        "order": [[6, 'desc']],
        "pageLength": 25,
        dom: 'frtlip',
        "initComplete": function (settings, json) {
            // Add event listener for opening and closing details
            $('#history').on('click', '.showChild', function (e) {
               
                var tr = $(this).closest('tr');
                var row = table.row(tr);
                let img = tr.find('.showChildImg');

                if (row.child.isShown()) {
                    // This row is already open - close it
                    row.child.hide();

                    tr.removeClass('shown');                    

                    img.attr('src', '../img/icons8-info.svg');
                    img.attr('title', 'View Parameters');
                }
                else {
                    // Open this row
                    var $child = $(format(row.data()));
                    row.child($child).show();

                    var newId = $('#results').length + 1;
                    var $results = $('#results').clone().appendTo($child).prop('id', 'results' + newId);
                    
                    showResults(null, $results, row.data().data, false)

                    tr.addClass('shown');

                    img.attr('src', '../img/circle-xmark-regular.svg');
                    img.attr('title', 'Close');
                }

                return false;
            });
        }
    });            
});

function generateUrl(script) {
    if (script.data != null) {
        let url = `/?ScriptId=${script.data.id}`;
    
        script.data.params.forEach(el => {
            let value = $(`#${el.name}`).val();
            url += `&${el.name}=${encodeURIComponent(el.value)}`;
        });

        return url;
    }
}