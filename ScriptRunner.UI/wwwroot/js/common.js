$(function () {

    jQuery.extend(jQuery.fn.dataTableExt.oSort, {
        "date-uk-pre": function (data) {
            let dt = new moment(data, 'DD/MM/YYYY hh:mm:ss');
            console.log(`${data} = ${dt.unix()}`);
            return dt.unix();
        },

        "date-uk-asc": function (a, b) {
            return ((a < b) ? -1 : ((a > b) ? 1 : 0));
        },

        "date-uk-desc": function (a, b) {
            return ((a < b) ? 1 : ((a > b) ? -1 : 0));
        }
    });
});