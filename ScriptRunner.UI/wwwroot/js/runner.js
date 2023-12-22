var scripts;
var selectedScript;
var dynamicFunctions = [];
var tippyInstances = [];

$.ajax({ url: "/api/script", type: 'GET', contentType: 'application/json' }).done(function (response) {
    scripts = response;

    const urlParams = new URLSearchParams(window.location.search);
    const scriptId = urlParams.get('ScriptId');

    let categories = [... new Set(scripts.data.map(obj => obj.category))];
    let el = $('#categories');
    populateDropDown(el, categories.map(obj => ({ id: obj, value: obj })))

    el.on("change", function () {
        showScriptDetails(null)
        let systems = [... new Set(scripts.data.filter(e => e.category == this.value).map(obj => obj.system))];

        let el = $('#scripts');
        el.empty();

        el = $('#systems');
        let items = systems.map(obj => ({ id: obj, value: obj }));
        populateDropDown(el, items)

        el.on("change", function () {
            showScriptDetails(null)

            let category = $('#categories').val();
            let items = scripts.data.filter(e => e.category == category && e.system == this.value)

            let el = $('#scripts');
            populateDropDown(el, items.map(obj => ({ id: obj.id, value: obj.title })))

            el.on("change", function () {
                let script = scripts.data.find(e => e.id == this.value)
                showScriptDetails(script);
            });
            if (items.length == 1 && scriptId == null) { el.val(items[0].id).change(); }
        });
        if (items.length == 1 && scriptId == null) { el.val(items[0].id).change(); }

    });

    if (scriptId != null) {
        let script = scripts.data.find(e => e.id == scriptId)

        showScriptDetails(script)
    }

    tippyInstances.forEach(instance => {
        instance.destroy();
    });
    tippyInstances.length = 0; // clear it

    tippyInstances = tippy('.mytooltip', {
        maxWidth: 600,
        content(reference) {
            const tooltip = reference.getAttribute('data-tooltip')
            return tooltip
        }
    });
});

function showScriptDetails(script) {    
    selectedScript = script;

    let $params = $('#Params');
    $params.empty();

    $('#results').addClass('hidden');

    if (script == null) {
        $('#description').text('');
        $('#tags').text('');
        $('#version').text('');
        $('#versionTooltip').text('');

        $('#execute').addClass('hidden');
        $('#copyScript').addClass('hidden');        

        return;
    } else {
        if ($('#categories').val() != script.category) {
            $('#categories').val(script.category).change();
        }

        if ($('#systems').val() != script.system) {
            $('#systems').val(script.system).change();
        }

        if ($('#scripts').val() != script.id) {
            $('#scripts').val(script.id);
        }

        $('#description').text(script.description);

        if (script.tags?.length > 0) {
            $('#tags').text(script.tags.join(', '));
            $('#tags').parent().parent().removeClass('hidden');
        } else {
            $('#tags').parent().parent().addClass('hidden');
        }        
        
        $('#version').text(`${script.uniqueId}`);
        $('#versionTooltip').attr('data-tooltip', `${script.filename}`);
        $('#versionTooltip').text('?');

        $('#execute').removeClass('hidden');
        $('#copyScript').removeClass('hidden');

        selectedScript = script;
    }

    const urlParams = new URLSearchParams(window.location.search);
    const newParams = new URLSearchParams();
    for (const [name, value] of urlParams) {
        newParams.append(name.toLowerCase(), value);
    }

    let dataTransfers = new Object();

    if (script.params?.length > 0) {
        $params.parent().parent().removeClass('hidden');

        script.params.forEach(el => {
            let $input;
            let $html;

            let value = newParams.get(el.name.toLowerCase()) ?? el.value ?? '';

            if (el.htmlType == "select") {
                let options = `<option value=""></option>`;

                for (const [key, value] of Object.entries(el.data)) {
                    let selected = value == 'true' ? 'selected' : '';
                    options += `<option value="${key}" ${selected}>${key}</option>`;
                }

                let html = getParamTemplateHtml(el);
                html = html.replace(/{Options}/g, options);

                $html = $($.parseHTML(html))
                $input = $html.find('input');
                
            } else if (el.htmlType == "file") {
                if (value?.length > 0) {
                    let file = convertBase64ToFile(value, el.data['FileType']);

                    if (file != null) {
                        const dataTransfer = new DataTransfer();
                        dataTransfer.items.add(file);
                        dataTransfers[el.name] = dataTransfer;

                        el.tooltip = `Original file provided from previous run`
                    }
                }


                let html = getParamTemplateHtml(el);
                $html = $($.parseHTML(html))
                $input = $html.find('input');

                if ('FileType' in el.data) {
                    $input.attr('accept', el.data['FileType']);
                }
            } else {
                let html = getParamTemplateHtml(el);
                $html = $($.parseHTML(html));
                $input = $html.find('input');

                if (el.htmlType == "checkbox") {

                    if (value == 'true') {
                        $input.attr('checked', true)
                    }
                }

                $input.attr('value', value)
            }

            if (el.required) {
                $input.attr('required', true)
                $input.parent().addClass('required', true)
            }

            $params.append($html);
        });
    } else {
        $params.parent().parent().addClass('hidden');
    }

    let $input2 = $('#form').find(':input');
    for (var key in dataTransfers) {
        var value = dataTransfers[key];

        let $file = $('#form').find(`#${key}`);
        $file.files = value.files;
    }

    const fileInput = document.querySelector('input[type="file"]');
    fileInput.files = dataTransfers['Name']?.files

    let $copyEl = $("#copyScript");
    $copyEl.on("mouseenter", async function () {
        if ($copyEl.attr("href") == '') { 
            let href = await generateUrl(script);
            $copyEl.attr("href", href)
        }
    });

    $copyEl.off("click");
    $copyEl.on("click", async function (event) {
        window.copyText($copyEl.attr("href"), 'URL Copied!');

        event.preventDefault();
        return false;
    });     

    let $versionTip = $('#versionTooltip');
    $versionTip.off("click");
    $versionTip.on("click", async function (event) {
        window.copyText(selectedScript.filename, 'Path Copied!');

        event.preventDefault();
        return false;
    });     

    addOptions(script);
}

window.copyText = function copyText(textToCopy, message) {
    navigator.clipboard.writeText(textToCopy);

    $.toast({
        type: 'success',
        autoDismiss: true,
        message: message
    });
}

function addOptions(script) {
    //Remove the old styles
    $("head [id^=customStyles]").remove();

    //Remove the old functions
    if (dynamicFunctions?.length > 0) {
        dynamicFunctions.forEach(jQuery => {
            $(jQuery.parent).off(jQuery.event, jQuery.selector, jQuery.func);
        });
    }

    //Default Results then Messages
    let layout = "HRM";

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

        if (options.layout != null) {
            layout = options.layout;
        }

        $('#resultsLabel').text(options.resultsLabel ?? "Results");
        $('#messagesLabel').text(options.messagesLabel ?? "Messages");
    }

    let showMessagesLabel = true;
    let showResultsLabel = true;
    let showHeaders = false;

    //Figure out what to hide
    for (let letter of layout) {
        switch (letter) {
            case "m":
                showMessagesLabel = false;
                break;
            case "M":
                showMessagesLabel = true;
                break;
            case "r":
                showResultsLabel = false;
                break;
            case "R":
                showResultsLabel = true;
                break;    
            case "h":
            case "H":
                showHeaders = true;
                break;
        }
    }

    let $messagesLabel = $('#messagesDiv > div > label:first-child');
    let $resultsLabel = $('#resultsDiv > div > label:first-child');

    if (showMessagesLabel) {
        $messagesLabel.removeClass('hidden');
    } else {
        $messagesLabel.addClass('hidden');
    }

    if (showResultsLabel) {
        $resultsLabel.removeClass('hidden');
    } else {
        $resultsLabel.addClass('hidden');
    }

    if (!showHeaders) {
        //$('#results').find('thead').css({ "display": "none" });
        $(`<style id='customStyles_thead' type='text/css'>thead { display: none }</style>\n`).appendTo('head');
    }

    //Remove the H
    layout = layout.replace(/H/ig, '');

    //Change the order
    if (layout.toUpperCase() == "MR") {
        $('#messagesDiv').prependTo($('#results'));
    } else {
        $('#messagesDiv').appendTo($('#results'))
    }


}

function getParamTemplateHtml(el) {
    let html = getParamTemplate(el.htmlType);

    html = html.replace(/{Name}/g, el.name);
    html = html.replace(/{Type}/g, el.htmlType);
    html = html.replace(/{Tooltip}/g, el.tooltip ?? '');
    html = html.replace(/{tooltipVisible}/g, (el.tooltip ?? '').length > 0 ? '' : 'hidden');
    
    return html;
}

function getParamTemplate(htmlType) {

    let templateName;

    switch (htmlType) {
        case 'file':
            templateName = '#paramTemplate-File';
            break;
        case 'checkbox':
            templateName = '#paramTemplate-Checkbox';
            break;
        case 'select':
            templateName = '#paramTemplate-Select';
            break;
        default:
            templateName = '#paramTemplate'
            break;
    }

    return $(templateName).html();
}

async function generateUrl(script) {
    let url = window.location.href.split('?')[0];
    url += `?ScriptId=${script.id}`;

    await updateParamValues().then(() => {

        for (let i = 0; i < script.params?.length; i++) {
            let param = script.params[i];

            if (param.value != null) { 
                url += `&${param.name}=${encodeURIComponent(param.value)}`;
            }
        }       
    });

    return url;
}

function populateDropDown(el, items) {
    el.empty();

    let options = '';
    options += '<option value="Select"></option>';
    for (let i = 0; i < items.length; i++) {
        options += '<option value="' + items[i].id + '">' + items[i].value + '</option>';
    }

    el.append(options);
}

async function updateParamValues() {
    var values = $('.valueinputs');

    for (let i = 0; i < values.length; i++) {
        let value = values.eq(i);
        let param = selectedScript.params?.find(f => f.name == value.attr('id'));        

        if (param != null) {
            await populateParamValue(param, value);
        }
    };

    return values;    
}

async function populateParamValue(param, value) {

    if (param.htmlType == "select") {
        param.value = value.val();
    } else if (param.htmlType == "checkbox") {
        param.value = value.is(":checked");
    } else if (param.htmlType == "file") {
        let $files = value.prop('files');
        if ($files.length > 0) {
            let file = new FileReader();

            file.readAsDataURL($files[0])
            await new Promise(resolve => file.onload = () => resolve())

            param.value = file.result;
            param.data['FileName'] = $files[0].name
        }
    } else {
        param.value = value.value;
    }
}

function convertBase64ToFile(image, filetype) {
    try {
        const byteString = atob(image.split(',')[1]);
        const ab = new ArrayBuffer(byteString.length);
        const ia = new Uint8Array(ab);
        for (let i = 0; i < byteString.length; i += 1) {
            ia[i] = byteString.charCodeAt(i);
        }
        const newBlob = new File([ab], `PreviousFile${filetype}`, {
            type: 'image/png',
        });
        return newBlob;
    }
    catch (err) {
        console.error(err);
    }

    return null;
};

$('#execute').on("click", async function (e) {    
    if ($("#form")[0].checkValidity() == false) {
        $("#form")[0].reportValidity()
        return;
    }

    updateParamValues().then(() => {

        $('#execute').attr("disabled", true);
        $('*').css('cursor', 'wait');

        var jsonData = JSON.stringify(selectedScript);
        $.ajax({
            url: "/api/Script", type: 'POST', contentType: 'application/json', dataType: 'json', data: jsonData
        }).done(function (response) {
            var $results = $('#results');
            showResults(selectedScript, $results, response, true);

            const $execute = $('#execute');
            
            $execute.attr("disabled", false);
            $execute[0].scrollIntoView();
        }).fail(function (jqXHR, textStatus, errorThrown) {
            alert(jqXHR.responseText);
            $('#execute').attr("disabled", false);
        }).always(function () {
            $('*').css('cursor', '');
        });
    });
});

$('#viewScriptSelector').on("click", async function (e) {
    var $div = $('#scriptPopup');
    $div.removeClass('hidden');

    let $modal = $("#scriptsModal");
    $modal.modal('show');

    let table;

    if ($.fn.dataTable.isDataTable("#scriptsTable") == false) {
        table = $('#scriptsTable').DataTable({
            retrieve: true,
            paging: true,
            ajax: { url: "/api/script" },
            fixedHeader: true,
            searching: true,
            pageLength: 5,
            dom: "frtip",
            ordering: false,

            columns: [
                { "data": "id", "visible": false },
                { "data": "category", "title": "Category" },
                { "data": "system", "title": "System" },
                { "data": "title", "title": "Title" },
                {
                    "data": "tags", "title": "Tags",
                    "render": function (data, type, row, meta) {

                        if (row.tags != null) {
                            var htmlDetail = row.tags?.join(', ');

                            return type === 'display' ? htmlDetail : htmlDetail;
                        } else {
                            return '';
                        }
                    }
                }
            ],
            initComplete: function () {
                setTimeout(() => {
                    $('div.dataTables_filter input').focus()
                }, 500);
            }
        });

        table.on('click', 'tbody tr', (e) => {
            let classList = e.currentTarget.classList;

            if (classList.contains('selected')) {
                classList.remove('selected');
            }
            else {
                table.rows('.selected').nodes().each((row) => row.classList.remove('selected'));
                classList.add('selected');
            }
        });
    } else {
        table = $('#scriptsTable').DataTable();
        table.rows('.selected').nodes().each((row) => row.classList.remove('selected'));

        setTimeout(() => {
            $('div.dataTables_filter input').focus()
        }, 500);
    }

    $('#runScript').on('click', function () {
        let data = table.rows('.selected').data()[0];
        let url = '/?ScriptId=' + data.id;
        window.location.href = url;
    });

});
