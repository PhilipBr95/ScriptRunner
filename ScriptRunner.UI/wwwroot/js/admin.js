let uploadedPackage;

$(function () {
    $('#localScripts').DataTable({
        paging: false,
        ajax: { url: "/api/script" },
        fixedHeader: true,
        searching: false,

        columns: [
            { "data": "system", "title": "System" },
            { "data": "title", "title": "Title" },
            {
                "data": "id", "title": "Id", render: function (data, type, row) {
                    return `<a href='/?ScriptId=${data}'>${data}</a>`;
                }
            },
            {
                "data": "version", "title": "Version", render: function (data, type, row) {
                    return `<div class="mytooltip">${data}<span class="tooltiptext">${row.filename}</span></div>`;
                } },
            {
                "data": "importedDate", "sType": "date-uk", "title": "Imported Date", "type": "datetime", render: function (data, type, row) {
                    let dt = new moment(data, moment.ISO_8601);
                    return dt.format('DD/MM/YYYY HH:mm:ss');
                }
            }
        ],
        "order": [[4, 'desc']],
    });

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

    $('#uploadFile').on('change', function () {
        var form = $("#form")[0];
        var formData = new FormData();
        formData.append("file", $("#uploadFile")[0].files[0]);

        try {
            $.ajax({
                url: "/api/Script/CreateScriptRunnerScript", type: 'POST', contentType: false, processData: false, data: formData
            }).done(function (package) {
                //Store for later
                uploadedPackage = package;

                let $modal = $("#exampleModal");
                populateForm($modal, package);

                $('#new-import').attr("disabled", false);
                $('#new-filename').text('');

                $modal.modal('show');
            }).fail(function (jqXHR, textStatus, errorThrown) {
                alert(jqXHR.responseText);
            });
        } catch (error) {
            console.error('Error:', error);
        }

        return false;
    });

    $('#new-import').on('click', function () {
        let $modal = $("#exampleModal");
        populateScript($modal);

        var jsonData = JSON.stringify(uploadedPackage);
        $.ajax({
            url: "/api/Script/ImportScript", type: 'POST', contentType: 'application/json', dataType: 'text', data: jsonData
        }).done(function (response) {
            $('#new-import').attr("disabled", true);
            $('#new-filename').text(`Script saved locally to ${response}`);

            var token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({                
                url: "/Admin?Handler=Reload", type: 'POST', data: {
                    __RequestVerificationToken: token
                }
            }).done(function () {
                $('#localScripts').DataTable().ajax.reload();
            });

            if ($('#runAfterImport').is(":checked")) {
                setTimeout(() => {
                    let url = `/?ScriptId=${uploadedPackage.id}`;
                    window.location.href = url;
                }, "1000")
            }
        }).fail(function (jqXHR, textStatus, errorThrown) {
            alert(jqXHR.responseText);
        });
    });

});

function populateScript($modal) {
    
    uploadedPackage.id = $modal.find("#new-id").val();
    uploadedPackage.system = $modal.find("#new-system").val();
    uploadedPackage.title = $modal.find("#new-title").val();
    uploadedPackage.description = $modal.find("#new-description").val();
    uploadedPackage.connectionString = $modal.find("#new-connectionString").val();

    uploadedPackage.params.forEach(param => {
        let id = `#new-params-${param.name}`
        let idTooltip = `#new-params-${param.name}-tooltip`

        let $id = $(id);
        let $idTooltip = $(idTooltip);

        param.toolTip = $idTooltip[0].value;

        if ($id[0].value != '') {
            param.value = $id[0].value;
        }            
    });
}

function populateForm($modal, package) {
    $modal.find("#new-id").val(package.id);
    $modal.find("#new-system").val(package.system);
    $modal.find("#new-title").val(package.title);
    $modal.find("#new-description").val(package.description);
    $modal.find("#new-connectionString").val(package.scripts[0].connectionString);
    $modal.find("#new-script").val(package.scripts[0].script);
    
    let $params = $modal.find("#new-params");
    $params.html('');

    let background_dt = '';
    let background_dd = '';

    package.params.forEach(el => {
        background_dt = (background_dt == '') ? 'new-dt-params-alt' : '';
        background_dd = (background_dd == '') ? 'new-dd-params-alt' : '';

        let html = `<dt class="new-dt-params ${background_dt}">${el.name}:</dt>
                                        <dd class="${background_dd}"><input type="text" id="new-params-${el.name}" class="new-input ${background_dd}" value="${el.value ?? ''}" /></dd>
                                        <dt class="new-dt-params ${background_dt}">Tooltip:</dt>
                                        <dd class="${background_dd}"><input type="text" id="new-params-${el.name}-tooltip" class="new-input ${background_dd}" value="${el.tooltip ?? ''}" /></dd>
                                        `
        $params.append(html);
    });
}
