let uploadedPackage;

$(function () {
    $('#localScripts').DataTable({
        paging: false,
        ajax: { url: "/api/script" },
        fixedHeader: true,
        searching: false,

        columns: [
            { "data": "category", "title": "Category" },
            { "data": "system", "title": "System" },
            { "data": "title", "title": "Title" },
            {
                "data": "id", "title": "Id", render: function (data, type, row) {
                    return `<a href='/?ScriptId=${data}'>${data}</a>`;
                }
            },
            {
                "data": "version", "title": "Version", render: function (data, type, row) {
                    return `<div class="mytooltip" data-tooltip="${row.filename}">${data}</div>`;
                } },
            {
                "data": "importedDate", "sType": "date-uk", "title": "Imported Date", "type": "datetime", render: function (data, type, row) {
                    let dt = new moment(data, moment.ISO_8601);
                    return dt.format('DD/MM/YYYY HH:mm:ss');
                }
            }
        ],
        "order": [[4, 'desc']],
        "initComplete": function (settings, json) {
            tippy('.mytooltip', {
                maxWidth: 600,
                content(reference) {
                    const tooltip = reference.getAttribute('data-tooltip')
                    return tooltip
                }
            });
        }
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
            { "data": "category", "title": "Category" },
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
            }).done(function (payload) {
                //Store for later
                uploadedPackage = payload.package;

                let $modal = $("#exampleModal");
                populateForm($modal, payload);

                $('#new-import').attr("disabled", false);
                $('#new-filename').text('');

                $modal.modal('show');
            }).fail(function (jqXHR, textStatus, errorThrown) {
                alert(jqXHR.responseText);
            });
        } catch (error) {
            console.error('Error:', error);
        }

        //Wipe it, so the user can reselect the same file
        this.value = null;

        return false;
    });

    $('#new-import').on('click', function () {
        let $modal = $("#exampleModal");
        populatePackage($modal);

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

function populatePackage($modal) {

    uploadedPackage.filename = $modal.find("#new-filename").val();
    uploadedPackage.id = $modal.find("#new-id").val();
    uploadedPackage.category = $modal.find("#new-category").val();
    uploadedPackage.system = $modal.find("#new-system").val();
    uploadedPackage.title = $modal.find("#new-title").val();
    uploadedPackage.description = $modal.find("#new-description").val();
    uploadedPackage.connectionString = $modal.find("#new-connectionString").val();

    uploadedPackage.params?.forEach(param => {
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

function populateForm($modal, payload) {
    let package = payload.package;

    let originalSql = payload.originalSql;
    let sql = package.scripts[0].script;

    $modal.find("#new-filename").val(package.filename);
    $modal.find("#new-id").val(package.id);
    $modal.find("#new-category").val(package.category);
    $modal.find("#new-system").val(package.system);
    $modal.find("#new-title").val(package.title);
    $modal.find("#new-description").val(package.description);
    $modal.find("#new-connectionString").val(package.scripts[0].connectionString);

    populateScript();
    
    let $params = $modal.find("#new-params");
    $params.html('');

    let background_dt = '';
    let background_dd = '';

    package.params?.forEach(el => {
        background_dt = (background_dt == '') ? 'new-dt-params-alt' : '';
        background_dd = (background_dd == '') ? 'new-dd-params-alt' : '';

        let html = `<dt class="new-dt-params ${background_dt}">${el.name}:</dt>
                                        <dd class="${background_dd}"><input type="text" id="new-params-${el.name}" class="new-input ${background_dd}" value="${el.value ?? ''}" /></dd>
                                        <dt class="new-dt-params ${background_dt}">Tooltip:</dt>
                                        <dd class="${background_dd}"><input type="text" id="new-params-${el.name}-tooltip" class="new-input ${background_dd}" value="${el.tooltip ?? ''}" /></dd>
                                        `
        $params.append(html);
    });

    function populateScript() {
        let $script = $modal.find("#new-script");

        tippy('#new-script', {
            content: '<p><b>SQL Parameter Replacement</b></p><p>The source SQL has been modified and the Parameter substitutions have been added.</p><p>Please check you are happy with the SQL - The changes have been highlighted</p>',
            allowHTML: true,
            maxWidth: 600
        });

        //Populate and highlight the script
        let lines = sql.split("\n");
        let originalLines = originalSql.split("\n");
        let differences = [];

        for (let i = 0; i < lines.length; i++) {
            let line = lines[i];
            let originalLine = originalLines[i];

            //Does this line look different to the original (due to Params)
            let linePos = findFirstDiffPos(line, originalLine);
            if (linePos != -1) {
                //console.info(`line ${i} - ${linePos} - ${line} vs ${originalLine}`);
                differences.push({ regex: new RegExp(`${escapeRegex(line.substring(0, linePos))}(${escapeRegex(line.substring(linePos))})`, 'gi') });
            }
        }

        //Populate the textarea and apply styling
        $script.val(sql);
        $script.highlightWithinTextarea({
            highlight: getRanges
        });

        function getRanges(input) {
            let ranges = [];
            let match;

            differences.forEach(diff => {
                while (match = diff.regex.exec(input), match !== null) {
                    if (match[1]) {
                        let groupStartIndex = match.index + match[0].indexOf(match[1]);
                        let groupEndIndex = groupStartIndex + match[1].length;
                        ranges.push([groupStartIndex, groupEndIndex]);
                    } else {
                        ranges.push([match.index, match.index + match[0].length]);
                    }

                    if (!diff.regex.global) {
                        // non-global regexes do not increase lastIndex, causing an infinite loop,
                        // but we can just break manually after the first match
                        break;
                    }
                }
            });

            return ranges;
        }

        function findFirstDiffPos(a, b) {
            var longerLength = Math.max(a.length, b.length);
            for (var i = 0; i < longerLength; i++) {
                if (a[i] !== b[i]) {
                    console.info(`Diff - "${a[i]}" vs "${b[i]}"`)
                    return i;
                }
            }

            return -1;
        }

        function escapeRegex(string) {
            return string.replace(/[/\-\\^$*+?.()|[\]{}]/g, '\\$&');
        }
    }
}
