
var scripts;
var selectedScript;

$.ajax({ url: "/api/script", type: 'GET', contentType: 'application/json' }).done(function (response) {
    scripts = response;

    let systems = [... new Set(scripts.data.map(obj => obj.system))];

    let el = $('#systems');
    populateDropDown(el, systems.map(obj => ({ id: obj, value: obj })))

    el.on("change", function () {
        showScriptDetails(null)

        let items = scripts.data.filter(e => e.system == this.value)

        let el = $('#scripts');
        populateDropDown(el, items.map(obj => ({ id: obj.id, value: obj.title })))

        el.on("change", function () {
            let script = scripts.data.find(e => e.id == this.value)
            showScriptDetails(script);
        });
    });

    const urlParams = new URLSearchParams(window.location.search);
    const scriptId = urlParams.get('ScriptId');

    if (scriptId != null) {
        let script = scripts.data.find(e => e.id == scriptId)

        showScriptDetails(script)
    }
});

function showScriptDetails(script) {
    selectedScript = script;

    let params = $('#Params');
    params.empty();

    if (script == null) {
        $('#description').text('');
        $('#tags').text('');
        $('#version').text('');

        $('#execute').addClass('hidden');
        $('#copyScript').addClass('hidden');

        $('#results').addClass('hidden');

        return;
    } else {
        $('#systems').val(script.system).change();
        $('#scripts').val(script.id);

        $('#description').text(script.description);
        $('#tags').text(script.tags);

        let importedDate = new moment(script.importedDate, moment.ISO_8601);

        $('#version').text(`${script.version} (${importedDate.format('DD/MM/YYYY HH:mm')})`);

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

        params.append($html);
    });

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
    $copyEl.on("click", async function (event) {
        navigator.clipboard.writeText($copyEl.attr("href"));

        $.toast({
            type: 'success',
            autoDismiss: true,
            message: 'URL Copied!'
        });

        event.preventDefault();
        return false;
    });     

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

        for (let i = 0; i < script.params.length; i++) {
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
        let value = values[i];
        let param = selectedScript.params.find(f => f.name == value.id);        

        if (param != null) {
            await populateParamValue(param, value);
        }
    };

    return values;    
}

async function populateParamValue(param, value) {

    if (param.type == "select") {
        param.value = value.checked;
    } else if (param.type == "checkbox") {
        param.value = value.checked;
    } else if (param.type == "file") {
        if (value.files.length > 0) {
            let file = new FileReader();

            file.readAsDataURL(value.files[0])
            await new Promise(resolve => file.onload = () => resolve())

            param.value = file.result;
            param.data['FileName'] = value.files[0].name
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

function resolveAfter2Seconds() {
    return new Promise(resolve => {
        setTimeout(() => {
            resolve('2 seconds');
        }, 2000);
    });
}

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
            showResults($results, response, true);

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
