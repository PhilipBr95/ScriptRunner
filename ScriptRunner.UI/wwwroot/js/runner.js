var scripts;
var selectedScript;

$.ajax({ url: "/api/script", type: 'GET', contentType: 'application/json' }).done(function (response) {
    scripts = response;

    let systems = [... new Set(scripts.map(obj => obj.system))];

    let el = $('#systems');
    populateDropDown(el, systems.map(obj => ({ id: obj, value: obj })))

    el.on("change", function () {
        showScriptDetails(null)

        let items = scripts.filter(e => e.system == this.value)

        let el = $('#scripts');
        populateDropDown(el, items.map(obj => ({ id: obj.id, value: obj.title })))

        el.on("change", function () {
            let script = scripts.find(e => e.id == this.value)
            showScriptDetails(script);
        });
    });

    const urlParams = new URLSearchParams(window.location.search);
    const scriptId = urlParams.get('ScriptId');

    if (scriptId != null) {
        let script = scripts.find(e => e.id == scriptId)

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
        html = html.replace(/{Type}/g, el.type);
        html = html.replace(/{Value}/g, value);
        html = html.replace(/{required}/g, el.required ? 'required' : '');

        params.append(html);
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

function updateParamValues() {
    var values = $('.valueinputs');
    values.each(function (i, obj) {
        var param = selectedScript.params.find(f => f.name == obj.id);

        if (param != null) {
            if (param.optional == false && obj.value == null) {

            }

            param.value = obj.value;
        }            
    });
}

function showResults(packageResult) {
    $('#results').removeClass('hidden');
    $('#resultsTables').html('');
    $('#resultsMessages').html('');

    let showMessages = false;
    let showResults = false;

    packageResult.scriptResults.forEach(function (obj, x) {
        if (obj.messages != null && obj.messages.length > 0) {
            let id = `resultsMessage${x}`

            let messages = '';
            obj.messages.forEach(function (obj, x) {
                messages += `<div>${obj}</div>`;
            });
            
            $('#resultsMessages').append(`<label id='${id}' class="display" style="width:100%">${messages}</label>`);

            $('#resultsMessages').parent().removeClass('hidden');
            showMessages = true;
        }

        obj.dataTables.forEach(function (obj, y) {
            let i = x + y;
            let id = `resultsTable${i}`
            $('#resultsTables').append(`<table id='${id}' class="display" style="width:100%"></table>`);
            showResultsTable(id, obj);

            $('#resultsTables').parent().removeClass('hidden');
            showResults = true;
        });
    });

    if (showMessages == false) {
        $('#resultsMessages').parent().addClass('hidden');
    }

    if (showResults == false) {
        $('#resultsTables').parent().addClass('hidden');
    }
}

function showResultsTable(id, dataTable) {
    var columns = [];
    let columnNames = Object.keys(dataTable[0]);

    for (var i in columnNames) {
        columns.push({
            data: columnNames[i],
            title: columnNames[i].toUpperCase()
        });
    }

    $(`#${id}`).DataTable({
        paging: false,
        fixedHeader: true,
        searching: false,
        data: dataTable,
        columns: columns
    });
}

$('#execute').on("click", function (e) {    
    if ($("#form")[0].checkValidity() == false) {
        $("#form")[0].reportValidity()
        return;
    }

    updateParamValues();

    $('#execute').attr("disabled", true);
    $('*').css('cursor', 'wait');

    var jsonData = JSON.stringify(selectedScript);
    $.ajax({
        url: "/api/Script", type: 'POST', contentType: 'application/json', dataType: 'json', data: jsonData
    }).done(function (response) {
        showResults(response);
        $('#execute').attr("disabled", false);
    }).fail(function (jqXHR, textStatus, errorThrown) {
        alert(jqXHR.responseText);
        $('#execute').attr("disabled", false);
    }).always(function () {
        $('*').css('cursor', '');
    });
});
