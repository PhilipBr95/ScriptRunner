$.ajax({ url: "/api/script", type: 'GET', contentType: 'application/json' }).done(function (response) {
    scripts = response;

    $('#scripts').DataTable({
        paging: false,
        fixedHeader: true,
        searching: false,
        data: scripts,
        columns: [
            { "data": "system", "title": "System" },
            { "data": "title", "title": "Title" },
            {
                "data": "id", "title": "Id", render: function (data, type, row) {                    
                    return `<a href='/?ScriptId=${data}'>${data}</a>`;
                }
            },
            { "data": "version", "title": "Version" },
            {
                "data": "creationTime", "title": "Created Date", "type": "datetime", render: function (data, type, row) {
                    let dt = new moment(data, moment.ISO_8601);
                    return dt.format('DD/MM/YYYY HH:mm:ss');
                }
            }
        ],
        "order": [[4, 'desc']],
    });
})