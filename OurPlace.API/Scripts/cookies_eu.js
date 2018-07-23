// https://github.com/infinum/cookies_eu

$(document).ready(function () {
    console.log("EU button ready")
	$('.cookies-eu-ok').click(function (e) {
		e.preventDefault();
		$.cookie('cookie_eu_consented', 'true', { path: '/', expires: 365 });
		$('.cookies-eu').remove();
	});
});