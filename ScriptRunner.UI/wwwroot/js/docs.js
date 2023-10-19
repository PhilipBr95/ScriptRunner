function populateContent(gitRepo, tag, scriptFolder) {

	let $markdown = $('#markdown');

	$markdown.load('/files/readme.md', function (responseTxt, statusTxt, xhr) {
		if (statusTxt == "success") {
			var markdown = $markdown.text();
			
			markdown = markdown.replace(/@Model.GitRepo/gi, gitRepo);
			markdown = markdown.replace(/@Model.Tag/gi, tag);
			markdown = markdown.replace(/@Model.ScriptFolder/gi, scriptFolder);
			
			let converter = new showdown.Converter();
			converter.setOption('moreStyling', 'true');
			converter.setOption('simpleLineBreaks', 'true');
			converter.setFlavor('github');

			let html = converter.makeHtml(markdown);

			$markdown.text('');
			$markdown.html(html);
		}
		if (statusTxt == "error")
			alert("Error: " + xhr.status + ": " + xhr.statusText);
	});
}



