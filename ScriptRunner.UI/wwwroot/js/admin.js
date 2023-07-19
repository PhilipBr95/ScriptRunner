$.ajax({ url: "/api/script", type: 'GET', contentType: 'application/json' }).done(function (response) {
    scripts = response;

    $('#localScripts').DataTable({
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
                "data": "importedDate", "title": "Imported Date", "type": "datetime", render: function (data, type, row) {
                    let dt = new moment(data, moment.ISO_8601);
                    return dt.format('DD/MM/YYYY HH:mm:ss');
                }
            }
        ],
        "order": [[4, 'desc']],
    });
})

//$.ajax({ url: "/api/script/remote", type: 'GET', contentType: 'application/json' }).done(function (response) {
//    scripts = response;
$(function() {
    let table = $('#remoteScripts').DataTable({
        ajax: "/api/script/remote",
        processing: true,
        language: {
            processing: "<div class='spinner'></div>",
            emptyTable: "No New Packages Found"
        },
        paging: false,
        fixedHeader: true,
        searching: false,
        columns: [
            {
                "data": null,
                "title": "<input type='checkbox' id='remote-select-all' name='selected[]' />",
                "orderable": false,
                render: function (data, type, row) {
                    return `<input type="checkbox" class="remote-checkbox" name="selectedIds" value="${data.id}" />`;
                }
            },
            { "data": "system", "title": "System" },
            { "data": "title", "title": "Title" },
            {
                "data": "id", "title": "Id", render: function (data, type, row) {
                    return `<a href='/?ScriptId=${data}'>${data}</a>`;
                }
            },
            { "data": "version", "title": "Version" },      
        ],
        "order": [[3, 'desc']],
        dom: 'frtlip',
    });

    // Handle click on "Select all" control
    $('#remote-select-all').on('click', function () {
        // Check/uncheck all checkboxes in the table
        var rows = table.rows({ 'search': 'applied' }).nodes();
        $('input[type="checkbox"]', rows).prop('checked', this.checked);

        enableRemoteSync();
    });

    $('#remoteScripts').on('click', '[name="selectedIds"]', function () {
        enableRemoteSync();
    });

    function enableRemoteSync() {
        let $enabled = $('[name="selectedIds"]:checked').length == 0;
        $("#remote-sync").prop('disabled', $enabled);
    }
});
