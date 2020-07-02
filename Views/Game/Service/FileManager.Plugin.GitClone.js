$(document).ready(function () {
    const uploadButton = $("#filemanager").find('*[data-command="OpenDialogCommand"]');
    const backupButton = $('<a role="button" href="" tabindex="0" class="k-button k-button-icontext" data-overflow="auto" aria-disabled="false"><span class="k-icon fab fa-github"></span>Git Clone</a>');
    uploadButton.after(backupButton);

    backupButton.click(function () {
        gitCloneDialog();
    })
})