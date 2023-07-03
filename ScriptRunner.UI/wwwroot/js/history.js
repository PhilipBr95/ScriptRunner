function format(row) {

    var params = row.data.params;
    var html = '';

    var ss = params.forEach(function (obj, x) {
        html += `<div><dt>${obj.name}</dt><dd>${obj.value}</dd></div>`
    });

    return `<dl><dd class="paramLabel">Parameters</dd>${html}</dl>`;
}

$(document).ready(function () {
    
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
                    return `<a href="${url}"><img width="16px" height="16px" src="../img/play-solid.svg" title="Execute Script" /></a>`
                }
            },
            {
                "className": 'details-control',
                "orderable": false,
                "data": null,
                "defaultContent": '<a href="" class="showChild"><img class="showChildImg" width="16px" height="16px" src="../img/icons8-info.svg" title="View Parameters" /></a>',
                "width": "0px"
            },
            { "data": "system", "title": "System" },
            { "data": "description", "title": "Package" },
            { "data": "success", "title": "Success" },
            { "data": "actionedBy", "title": "Actioned By" },
            {
                "data": "createdDate", "title": "Created Date", "type": "date", render: function (data, type, row) {
                    let dt = new moment(data, moment.ISO_8601);
                    return dt.format('DD/MM/YYYY hh:mm');
                }

            },
            {
                "data": "extn", "visible": false,
                "render": function (data, type, row, meta) {

                    var htmlDetail = '';
                    row.data.params.forEach(function (item) {
                        htmlDetail += item.value + '|';
                    });
                    return type === 'display' ? htmlDetail : htmlDetail;
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
                    row.child(format(row.data())).show();

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
    let url = `/?ScriptId=${script.data.id}`;

    script.data.params.forEach(el => {
        let value = $(`#${el.name}`).val();
        url += `&${el.name}=${el.value}`;
    });

    return encodeURI(url);
}