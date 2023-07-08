
$(document).ready(function () {
    
    var table = $('#logs').DataTable({
        "rowId": 'id',
        ajax: { url: "/api/log", dataSrc: "" },
        "columns": [
            { "data": "categoryName", "title": "Category Name" },
            { "data": "logLevel", "title": "LogLevel" },
            { "data": "message", "title": "Message" },
            {
                "data": "createdDate", "title": "Created Date", "type": "date", render: function (data, type, row) {
                    let dt = new moment(data, moment.ISO_8601);
                    return dt.format('DD/MM/YYYY HH:mm:ss');
                }

            }
        ],
        "order": [[3, 'desc']],
        "pageLength": 25,
        dom: 'frtlip'    
    });            
});
