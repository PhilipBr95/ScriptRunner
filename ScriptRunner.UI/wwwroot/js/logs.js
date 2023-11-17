
$(document).ready(function () {
    
    var table = $('#logs').DataTable({        
        "rowId": 'id',
        ajax: { url: "/api/log", dataSrc: "" },
        "columns": [
            { "data": "categoryName", "title": "Category Name", "width":"15%", className: "logWrap" },
            { "data": "logLevel", "title": "LogLevel", "width": "10%" },
            { "data": "message", "title": "Message", "width": "60%" },
            {
                "data": "createdDate", "width": "15%", "sType": "date-uk", "title": "Created Date", "type": "datetime", render: function (data, type, row) {
                    let dt = new moment(data, moment.ISO_8601);
                    return dt.format('DD/MM/YYYY HH:mm:ss');
                }
            }
        ],
        "order": [[3, 'desc']],
        "pageLength": 25,
        dom: 'frtlip',
        columnDefs: [
            {
                targets: 1,
                createdCell: function (td, cellData, rowData, row, col) {
                    if (cellData == 'Error') {
                        $(td).addClass('logError');

                    }
                }
            }
        ],
    });            
});
