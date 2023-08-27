
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
    params.html('');

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

    let paramTemplate = $('#paramTemplate').html();
    let urlParams = new URLSearchParams(window.location.search.toLowerCase());
    
    script.params.forEach(el => {
        let html = paramTemplate;
        let value = urlParams.get(el.name.toLowerCase()) ?? el.value ?? '';

        html = html.replace(/{Name}/g, el.name);
        html = html.replace(/{Type}/g, el.htmlType);                
        html = html.replace(/{Tooltip}/g, el.tooltip ?? '');
        html = html.replace(/{tooltipVisible}/g, (el.tooltip ?? '').length > 0 ? '' : 'hidden');

        let $html = $($.parseHTML(html))
        let $input = $html.find('input');

        if (el.required) {
            $input.attr('required', true)
        }
        
        if (el.htmlType == "file") {
            $input.attr('accept',el.value)
        } else {
            $input.attr('value', el.value)
        }

        params.append($html);
    });

    $("[required]").after("<span class='required'>*</span>");

    let $copyEl = $("#copyScript");
    $copyEl.on("mouseenter", function () {
        $copyEl.attr("href", generateUrl(script))
    });
    $copyEl.on("click", function () {
        navigator.clipboard.writeText(generateUrl(script));
        return false;
    });     

}

function generateUrl(script) {
    let url = window.location.href.split('?')[0];

    url += `?ScriptId=${script.id}`;

    script.params.forEach(el => {
        let value = $(`#${el.name}`).val();
        url += `&${el.name}=${value}`;
    });

    return encodeURI(url);
}

function populateDropDown(el, items) {
    el.html('');

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
            if (param.type == "file") {
                let file = new FileReader();

                file.readAsDataURL(value.files[0])
                await new Promise(resolve => file.onload = () => resolve())
                
                param.value = file.result.substring(file.result.indexOf(',')+1)

            } else {
                param.value = value.value;
            }
        }
    };


    return values;    
}

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
            $('#execute').attr("disabled", false);
        }).fail(function (jqXHR, textStatus, errorThrown) {
            alert(jqXHR.responseText);
            $('#execute').attr("disabled", false);
        }).always(function () {
            $('*').css('cursor', '');
        });
    });
});
